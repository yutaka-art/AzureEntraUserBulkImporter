using System;
using System.Threading;

namespace AzureEntraUserBulkImporter
{
    #region [EntryPoint]
    /// <summary>
    /// エントリポイント
    /// </summary>
    public class Program
    {
        /// <summary>
        /// エントリポイント
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            Console.WriteLine("*Program start*");

            // Initialize exception handler.
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            // Service
            var service = DI.DIContainer.GetService<Services.IMainProcessService>();
            var cts = new CancellationTokenSource();

            service.MainProcess(cts).Wait();

            Console.WriteLine("*Program end*");
        }

        /// <summary>
        /// 集約例外ハンドル
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベントオブジェクト</param>
        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as System.Exception;
            if (ex == null)
            {
                ex = new ApplicationException("UnhandledException is occurred, but exception object is null.");
            }
            TrackException(ex);
            Console.ReadKey();

            Environment.Exit(1);
        }

        /// <summary>
        /// 例外のトレース
        /// </summary>
        /// <param name="ex">例外</param>
        private static void TrackException(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    #endregion
}
