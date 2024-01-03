using AzureEntraUserBulkImporter.Models;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzureEntraUserBulkImporter.Services
{
    #region IMainProcessService
    /// <summary>
    /// Azure EntraID へユーザー情報を一括登録するプロセスインタフェースクラス
    /// </summary>
    public interface IMainProcessService : IDisposable
    {
        /// <summary>
        /// メインプロセスの実行を行います。
        /// </summary>
        /// <param name="cts">キャンセレーショントークンソース。プロセスのキャンセルに使用されます。</param>
        /// <returns>非同期操作のタスク</returns>
        public Task MainProcess(CancellationTokenSource cts);
    }
    #endregion

    #region MainProcessService
    /// <summary>
    /// Azure EntraID へユーザー情報を一括登録するプロセス実装クラス
    /// </summary>
    public class MainProcessService : IMainProcessService
    {
        #region Variable・Const
        private readonly EntraIdConfig EntraIdConfig;
        private readonly IConfidentialClientApplication ConfidentialClientApplication;
        private readonly GraphServiceClient GraphClient;
        #endregion

        #region Constructor
        /// <summary>
        /// MainProcessService の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="EntraIdConfig">EntraID の設定。</param>
        /// <param name="confidentialClientApplication">EntraID との通信に使用するクライアントアプリケーション。</param>
        /// <param name="graphClient">Microsoft Graph API へのリクエストを処理するクライアント。</param>
        public MainProcessService(
            IOptions<EntraIdConfig> EntraIdConfig,
            IConfidentialClientApplication confidentialClientApplication,
            GraphServiceClient graphClient)
        {
            this.EntraIdConfig = EntraIdConfig.Value;
            this.ConfidentialClientApplication = confidentialClientApplication;
            this.GraphClient = graphClient;
        }
        #endregion

        #region Method
        /// <summary>
        /// Azure EntraID へユーザー情報を一括登録するプロセスを実行します。
        /// </summary>
        /// <param name="cts">キャンセレーショントークンソース。</param>
        public async Task MainProcess(CancellationTokenSource cts)
        {
            Console.WriteLine("Press any key to start user registration.");
            Console.ReadKey();

            // ユーザー情報をCsvより取得
            var userRegisterInfos = GetUsersToRegister();

            foreach (var userRegisterInfo in userRegisterInfos)
            {
                try
                {
                    // ユーザ属性情報
                    var user = new User
                    {
                        DisplayName = userRegisterInfo.DisplayName,
                        MailNickname = userRegisterInfo.MailNickname,
                        UserPrincipalName = userRegisterInfo.UserPrincipalName,
                        PasswordProfile = new PasswordProfile
                        {
                            ForceChangePasswordNextSignIn = true,
                            Password = userRegisterInfo.Password
                        },
                        AccountEnabled = true
                    };

                    // 全ユーザを取得し、LINQで対象のユーザが存在するか確認
                    var users = await this.GraphClient.Users.Request().GetAsync();
                    var isExist = users.CurrentPage.Any(x => x.UserPrincipalName == user.UserPrincipalName);
                    if (isExist == true)
                    {
                        Console.WriteLine($"User: {user.UserPrincipalName} is already exist.");
                        continue;
                    }

                    // ユーザーを登録
                    var afterInfo = await this.GraphClient.Users.Request().AddAsync(user);

                    // 対象のロールを取得
                    var targetRole = await this.GraphClient.DirectoryRoles
                        .Request()
                        .Filter($"displayName eq '{userRegisterInfo.RoleName}'")
                        .GetAsync();

                    var targetRoleId = targetRole.CurrentPage.FirstOrDefault()?.Id;

                    // 対象のロールが見つかった場合、ユーザーをロールに追加
                    if (targetRoleId != null)
                    {
                        var directoryRoleAssignment = new DirectoryObject
                        {
                            Id = afterInfo.Id
                        };

                        await this.GraphClient.DirectoryRoles[targetRoleId].Members.References
                            .Request()
                            .AddAsync(directoryRoleAssignment);
                    }

                    Console.WriteLine($"User: {afterInfo.DisplayName}, {afterInfo.Mail} is registered.");
                }
                catch (ServiceException ex)
                {
                    Console.WriteLine($"Error registering user or assigning role: {ex.Message}");
                }
            }
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Csvファイルからユーザー情報を取得します。
        /// </summary>
        /// <returns></returns>
        private IEnumerable<UserData> GetUsersToRegister()
        {
            var csvFilePath = "UserCreate.csv"; // CSVファイルのパスを設定

            return System.IO.File.ReadAllLines(csvFilePath)
                .Skip(1) // ヘッダー行をスキップ
                .Select(line => line.Split(','))
                .Select(parts => new UserData
                {
                    DisplayName = parts[0],
                    MailNickname = parts[1],
                    UserPrincipalName = parts[2],
                    Password = parts[3],
                    RoleName = parts[4]
                });
        }
        #endregion

        #region Dispose
        private bool _isDisposed = false;

        /// <summary>
        /// 使用済みのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            if (this._isDisposed) return;

            this._isDisposed = true;
        }
        #endregion
    }
    #endregion
}
