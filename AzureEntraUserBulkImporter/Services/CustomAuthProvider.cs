using Microsoft.Graph;
using Microsoft.Identity.Client;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureEntraUserBulkImporter.Services
{
    /// <summary>
    /// カスタム認証プロバイダー。
    /// Microsoft Graph API へのリクエストにアクセストークンを提供します。
    /// </summary>
    public class CustomAuthProvider : IAuthenticationProvider
    {
        private IConfidentialClientApplication ClientApp;

        /// <summary>
        /// CustomAuthProvider の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="clientApp">認証に使用する IConfidentialClientApplication。</param>
        public CustomAuthProvider(IConfidentialClientApplication clientApp)
        {
            this.ClientApp = clientApp;
        }

        /// <summary>
        /// HTTPリクエストに認証情報を付与します。
        /// </summary>
        /// <param name="request">認証が必要な HTTPリクエスト。</param>
        /// <returns>非同期操作</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            // アプリケーションのアクセストークンを取得
            var authResult = await this.ClientApp.AcquireTokenForClient(new[] { "https://graph.microsoft.com/.default" }).ExecuteAsync();

            // HTTPリクエストに認証ヘッダーを追加
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        }
    }
}
