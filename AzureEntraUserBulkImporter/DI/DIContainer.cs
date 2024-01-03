using AzureEntraUserBulkImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace AzureEntraUserBulkImporter.DI
{
    #region [DIContainer]
    /// <summary>
    /// 依存性注入コンテナを提供するクラス。
    /// アプリケーションのサービスと構成を管理します。
    /// </summary>
    public static class DIContainer
    {
        // サービスプロバイダーのインスタンス
        private static ServiceProvider Provider;

        // 構成設定のインスタンス
        private static IConfiguration Configuration;

        /// <summary>
        /// 静的コンストラクタで構成とサービスを初期化します。
        /// </summary>
        static DIContainer()
        {
            InitConfiguration();
            Provider = ConfigureServices();
        }

        /// <summary>
        /// アプリケーションの構成を初期化します。
        /// appsettings.json と環境変数から設定を読み込みます。
        /// </summary>
        private static void InitConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        /// <summary>
        /// アプリケーションで利用するサービスを設定します。
        /// ロギング、EntraID認証、カスタム認証プロバイダ、メインプロセスサービスを登録します。
        /// </summary>
        /// <returns>構成されたサービスプロバイダー</returns>
        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace)
                       .AddConfiguration(Configuration.GetSection("Logging"))
                       .AddConsole()
                       .AddDebug();
            });

            services.AddSingleton<IConfiguration>(Configuration);
            services.Configure<EntraIdConfig>(Configuration.GetSection("AzureAd"));

            // IConfidentialClientApplication の DI 登録
            services.AddSingleton<IConfidentialClientApplication>(provider =>
            {
                var EntraIdConfig = provider.GetRequiredService<IOptions<EntraIdConfig>>().Value;
                return ConfidentialClientApplicationBuilder
                    .Create(EntraIdConfig.ClientId)
                    .WithTenantId(EntraIdConfig.TenantId)
                    .WithClientSecret(EntraIdConfig.ClientSecret)
                    .Build();
            });

            // CustomAuthProvider の DI 登録
            services.AddSingleton<CustomAuthProvider>();

            // GraphServiceClient の DI 登録
            services.AddSingleton<GraphServiceClient>(provider =>
            {
                var authProvider = provider.GetRequiredService<CustomAuthProvider>();
                return new GraphServiceClient(authProvider);
            });

            // IMainProcessService の DI 登録
            services.AddTransient<IMainProcessService, MainProcessService>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 指定された型のサービスインスタンスを取得します。
        /// </summary>
        /// <typeparam name="TService">取得するサービスの型</typeparam>
        /// <returns>サービスのインスタンス</returns>
        public static TService GetService<TService>()
        {
            return Provider.GetService<TService>();
        }
    }
    #endregion
}
