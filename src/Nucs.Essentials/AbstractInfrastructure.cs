using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nucs.Configuration;
using Nucs.DependencyInjection;
using Nucs.Reflection;
using Nucs.Threading;

namespace Nucs {

    public class AbstractInfrastructure {

        #region Variables

        public const string DateFormat = "yyyyMMdd"; //for DateTime
        public const string TimeFormat = @"hh\:mm\:ss\.fff"; //for TimeSpan
        public const string Time24HrFormat = @"HH\:mm\:ss\.fff"; //for TimeSpan
        public const string TimeLongFormat = @"hh\:mm\:ss\.ffffff"; //for TimeSpan
        public const string TimeFormatHHMM = @"hhmm";
        public const string TimeFormatShort = @"hh\:mm\:ss"; //for TimeSpan
        public const string TimeFilenameFormatShort = @"hhmmss"; //for TimeSpan
        public const string DateTimeFormat = "yyyyMMdd HH:mm:ss"; //for DateTime
        public const string DateTimeLongFormat = "yyyyMMdd HH:mm:ss.fff"; //for DateTime
        public const string DateTimeVeryLongFormat = "yyyyMMdd HH:mm:ss.ffffff"; //for DateTime

        public const string TimeFolderNameFormat = "HH'-'mm'-'ss"; //Time for foldername
        public const string TimeSpanHourMinuteSecondFormat = @"hh\:mm\:ss"; //for TimeSpan
        public const string HourMinuteSecondFormat = "HH:mm:ss"; //for DateTime
        public const string HourMinuteSecondMillisecondFormat = "HH:mm:ss.fff"; //for DateTime

        /// <summary>
        ///     Gets the <see cref="TimeZoneInfo"/> required to convert current time to NY time.
        ///     If null then current zone time is NY.
        /// </summary>
        public static readonly CultureInfo UsCulture = CultureInfo.CreateSpecificCulture("en-US");

        public static CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

        /// <summary>
        ///     Return code used when exiting current process.
        /// </summary>
        public static int ExitCode = 0;

        public const int RoundingDecimals = 2;


        public const int MissingValue = -9999999;
        public const string MissingValueStr = "-9999999";

        #region Software Environment

        /// <summary>
        ///     Is This system running a research backtest to observe results on charter
        /// </summary>
        public static readonly bool IsResearch;

        /// <summary>
        ///     Checks if this app is configured to trade with real money by checking the ExecutionProvider to be SterlingOrderProcessor
        ///     AND if the ExecutionAccount doesn't contain the work "demo"
        /// </summary>
        public static readonly bool IsProduction;

        public static bool HasSystemInitialized => HasSystemInitializedTask.Task.IsCompletedSuccessfully;
        public static readonly TaskCompletionSource HasSystemInitializedTask = new TaskCompletionSource();


        /// <summary>
        ///     List of all known XML paths in currently running system.
        /// </summary>
        internal static readonly List<string> KnownConfigPaths = new List<string>();

        /// <summary>
        ///     A timestamp recorded when AbstractInfrastructure is statically intializing. One of the first lines of code
        ///     that execute when our apps start.
        /// </summary>
        public static DateTime StartTimeUtc;

        #endregion

        #endregion Variables

        static AbstractInfrastructure() {
            try {
                StartTimeUtc = DateTime.UtcNow;
                //CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                //handle known configs
                if (Assembly.GetExecutingAssembly().Location == "") {
                    //when unit testing
                    KnownConfigPaths = new List<string>();
                } else {
                    //non unit test
                    if (Directory.Exists(Path.Combine(SystemHelper.GetExecutablePath(), "..\\Config\\"))) {
                        foreach (var configs in Directory.EnumerateFiles(Path.Combine(SystemHelper.GetExecutablePath(), "..\\Config\\"), "*.*", SearchOption.AllDirectories)) {
                            KnownConfigPaths.Add(Path.GetFullPath(configs));
                        }
                    } else if (Directory.Exists(Path.Combine(SystemHelper.GetExecutablePath(), ".\\Configuration\\"))) {
                        foreach (var configs in Directory.EnumerateFiles(Path.Combine(SystemHelper.GetExecutablePath(), ".\\Configuration\\"), "*.*", SearchOption.AllDirectories)) {
                            KnownConfigPaths.Add(Path.GetFullPath(configs));
                        }
                    } else if (Directory.Exists(Path.Combine(SystemHelper.GetExecutablePath(), ".\\Config\\"))) {
                        foreach (var configs in Directory.EnumerateFiles(Path.Combine(SystemHelper.GetExecutablePath(), ".\\Config\\"), "*.*", SearchOption.AllDirectories)) {
                            KnownConfigPaths.Add(Path.GetFullPath(configs));
                        }
                    }

                    KnownConfigPaths = KnownConfigPaths.OrderBy(s => s.Length).ToList();
                }

                //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                try {
                    //set up readonly infrastracture parameters
                    var cmdlines = Environment.CommandLine;
                    IsResearch = cmdlines.Contains("--research");
                    IsProduction = cmdlines.Contains("--prod") || cmdlines.Contains("--production");

                    ExternalIpAddressTask = new LazyTask<IPAddress>(GetExternalIpAddress, LazyThreadSafetyMode.PublicationOnly);
                } finally {
                    Types.Setup();
                }
            } catch (Exception e) {
                Debug.WriteLine(e.ToString());
                throw;
            }
        }

        public static LazyTask<IPAddress>? ExternalIpAddressTask;
        public static byte[] ExternalIpBytes => ExternalIpAddressTask.Value.GetAwaiter().GetResult()?.GetAddressBytes();
        public static readonly CachedFilePool CachedFile = new CachedFilePool();

        private static async Task<IPAddress> GetExternalIpAddress() {
            #if !DEBUG
            return default;
            #endif
            string result = string.Empty;
            string[] checkIpUrl = {
                "https://checkip.amazonaws.com/",
                "https://ipinfo.io/ip",
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://wtfismyip.com/text"
            };

            using (var client = new WebClient()) {
                client.Proxy = null;
                client.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0) " +
                                               "(compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

                foreach (var url in checkIpUrl) {
                    try {
                        result = await client.DownloadStringTaskAsync(url).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(result))
                            continue;
                    } catch (Exception) {
                        //swallow
                    }

                    try {
                        return IPAddress.Parse(result.Trim('\t', ' ', '\n', '\r'));
                    } catch (Exception) {
                        //swallow
                        continue;
                    }
                }
            }

            throw new InvalidOperationException($"Unable to discover external Ip address of this machine, there might be no internet...");
        }
    }
}