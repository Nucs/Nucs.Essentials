using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nucs.Exceptions;

namespace Nucs {
    public static class SystemHelper {
        public const string ConfigFileExt = ".xml";
        public const string FrameworkSettingsFile = "..\\Config\\FrameworkSettings.xml";

        public const float memory = 1024f;

        public const  uint million = 1000000;
        public const  uint billion = 1000000000;
        public const uint thousands = 100000;
        public static Logger Logger { get; private set; }

        public static void SetupLogger(ILogger logger) {
            Logger = new Logger(logger);
        }
        
        public static void SetupLogger(Logger logger) {
            Logger = logger;
        }
        

        public static HashSet<string> GetSymbolsFromFile(string file) {
            var symbols = File.ReadAllText(file)
                              .Trim('\n', '\r')
                              .Replace("\r", "")
                              .Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(v => v.Split(',')[0])
                              .ToArray();

            return new HashSet<string>(symbols);
        }

        public static List<string> GetSymbolsListFromFile(string file) {
            return GetSymbolsFromFile(file).ToList();
        }

        public static Dictionary<string, int> GetSymbolValuePairFromFile(string file) {
            Dictionary<string, int> symbolParameters = new Dictionary<string, int>();

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            using (StreamReader sr = new StreamReader(file)) {
                string line;
                string[] row = null;
                int count = 0;

                while ((line = sr.ReadLine()) != null) {
                    if (line != String.Empty) {
                        row = line.Split(',');
                        symbolParameters.Add(row[0], Convert.ToInt32(row[1]));
                        count++;
                    }
                }
            }

            return symbolParameters;
        }

        public static string SearchConfigFile(string path, string fileName) {
            foreach (var configPath in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)) {
                if (configPath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) {
                    return configPath;
                }
            }

            throw new FileNotFoundException($"Search is unable to find configuration file '{fileName}' at '{path}' directory.");
        }

        /// <remarks>Alias for GetSettingsFile</remarks>
        public static string GetConfigurationFile(string file) {
            return GetSettingsFile(file);
        }

        public static string GetSettingsFile(string file) {
            if (!Path.IsPathRooted(file) || file.StartsWith(".")) {
                var @fixed = Path.GetFileName(file);
                if (@fixed != file) {
                    Logger?.Debug($"PATH {file} turning to {@fixed}");
                    file = @fixed;
                }
            }

            foreach (var configPath in AbstractInfrastructure.KnownConfigPaths) {
                if (configPath.EndsWith(file, StringComparison.OrdinalIgnoreCase)) {
                    return configPath;
                }
            }

            throw new FileNotFoundException($"Unable to find configuration file '{file}' at 'Config' directory.");
        }

        public static string? TryGetSettingsFile(string file) {
            if (!Path.IsPathRooted(file) || file.StartsWith(".")) {
                var @fixed = Path.GetFileName(file);
                if (@fixed != file) {
                    Logger?.Debug($"PATH {file} turning to {@fixed}");
                    file = @fixed;
                }
            }

            foreach (var configPath in AbstractInfrastructure.KnownConfigPaths) {
                if (configPath.EndsWith(file, StringComparison.OrdinalIgnoreCase)) {
                    return configPath;
                }
            }

            return null;
        }

        public static bool TryGetSettingsFile(string file, out string path) {
            if (!Path.IsPathRooted(file) || file.StartsWith("."))
                file = Path.GetFileName(file);

            foreach (var configPath in AbstractInfrastructure.KnownConfigPaths) {
                if (configPath.EndsWith(file, StringComparison.OrdinalIgnoreCase)) {
                    path = configPath;
                    return true;
                }
            }

            path = null;
            return false;
        }

        public static string GetExecutablePath() {
            var path = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(path) || path.EndsWith("mscorlib.dll"))
                return Environment.CurrentDirectory + "\\";

            return Path.GetDirectoryName(path) + "\\";
        }

        /// <summary>
        ///     Depth-first recursive delete, with handling for descendant 
        ///     directories open in Windows Explorer.
        ///     http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
        /// </summary>
        public static void DeleteDirectory(string path) {
            path = Path.GetFullPath(path);
            var newPath = path.TrimEnd('\\', '/') + Path.GetRandomFileName();
            try {
                Retry.Do(() => {
                    try {
                        Directory.Move(path, newPath);
                    } catch (FileNotFoundException e) {
                        throw new AbortRetryAndThrowException(e);
                    } catch (DirectoryNotFoundException e) {
                        throw new AbortRetryAndThrowException(e);
                    } catch (SecurityException e) {
                        throw new AbortRetryAndThrowException(e);
                    } catch (PathTooLongException e) {
                        throw new AbortRetryAndThrowException(e);
                    } catch (NotSupportedException e) {
                        throw new AbortRetryAndThrowException(e);
                    }
                }, TimeSpan.FromMilliseconds(100), 300, @throw: true);
            } catch (IOException e) {
                throw new SystemException($"Unable to delete directory {path}", e);
            }

            ApplicationEvents.Awaitables.Add(Task.Delay(5000).ContinueWith(_ => {
                if (!Directory.Exists(newPath))
                    return;

                try {
                    Directory.Delete(newPath, true);
                } catch (Exception e) {
                    // ignored
                }
            }));
        }

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
        /// </summary>
        public static void DeleteDirectoryDirectly(string path) {
            path = Path.GetFullPath(path);
            if (!Directory.Exists(path))
                return;
            bool logged = false;
            while (true) {
                try {
                    Directory.Delete(path, true);
                    if (logged)
                        Logger?.Info($"Tradingsystem continues, file access in '{path}' was taken successfully.");
                    return;
                } catch (IOException e) when (e.Message.Contains("cannot access")) {
                    if (!logged) {
                        Logger?.Error($"{e.Message} please close the file/excel so tradingsystem can continue.");
                        logged = true;
                    }
                    // ignored
                } catch (Exception e) {
                    // ignored
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true
        /// </summary>
        public static void ClearDirectory(string path) {
            File.SetAttributes(path, FileAttributes.Normal);

            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files) {
                File.SetAttributes(file, FileAttributes.Normal);

                File.Delete(file);
            }

            foreach (string directory in dirs) {
                Directory.Delete(directory, true);
            }
        }

        static void SetAttributesNormal(DirectoryInfo dir) {
            foreach (var subDir in dir.GetDirectories())
                SetAttributesNormal(subDir);

            foreach (var file in dir.GetFiles()) {
                file.Attributes = FileAttributes.Normal;
            }
        }


        public static double BytesToKiloBytes(long byteCount) {
            if (byteCount == 0)
                return byteCount;

            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num);
        }

        /// <summary>
        ///     Joins <paramref name="list"/> to a csv string separated by <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="delimiter"></param>
        /// <returns>a csv line</returns>
        public static unsafe string ToCsv(this List<string> list, char delimiter = ',') {
            if (list.Count == 1)
                return list[0];

            //we caluclate the expected size of the string.
            int itemsToMerge = list.Count;
            int charsLen = list.Count - 1; //every item minus one has a delimiter
            //sum the len of all strings
            for (int i = 0; i < itemsToMerge; i++)
                charsLen += list[i].Length;

            int cursor = 0; //the offset of the current location in the buffer
            string @return = new string(',', charsLen); //initiallize a new string filled with ',' (you can't initialize a string without filled memory.

            fixed (char* buffer = @return) {
                //we copy every string
                string current;
                for (int i = 0; cursor < charsLen; i++, cursor += current.Length + 1) //+1 is for the delimiter
                {
                    current = list[i];
                    fixed (char* ptr_src = current)
                        Unsafe.CopyBlock(buffer + cursor, ptr_src, (uint) (sizeof(char) * current.Length));
                }
            }

            return @return;
        }

        /// <summary>
        ///     Joins <paramref name="list"/> to a csv string separated by <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static unsafe string ToCsv(this string[] list, char delimiter = ',') {
            if (list.Length == 1)
                return list[0];

            //we caluclate the expected size of the string.
            int itemsToMerge = list.Length;
            int charsLen = list.Length - 1; //every item minus one has a delimiter
            //sum the len of all strings
            for (int i = 0; i < itemsToMerge; i++)
                charsLen += list[i].Length;

            int cursor = 0; //the offset of the current location in the buffer
            string @return = new string(',', charsLen); //initiallize a new string filled with ',' (you can't initialize a string without filled memory.

            fixed (char* buffer = @return) {
                //we copy every string
                string current;
                for (int i = 0; cursor < charsLen; i++, cursor += current.Length + 1) //+1 is for the delimiter
                {
                    current = list[i];
                    fixed (char* ptr_src = current)
                        Unsafe.CopyBlock(buffer + cursor, ptr_src, (uint) (sizeof(char) * current.Length));
                }
            }

            return @return;
        }

        public static void CloneDirectory(string root, string dest) {
            Directory.CreateDirectory(dest);
            foreach (var directory in Directory.GetDirectories(root)) {
                string dirName = Path.GetFileName(directory);
                if (!Directory.Exists(Path.Combine(dest, dirName))) {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }

                CloneDirectory(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root)) {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
            }
        }

        /*public static void LinkDirectory(string root, string dest, IDictionary<string, LinkAction> actions, bool linkAllDirectories = false) {
            var start = DateTime.UtcNow;
            root = Path.GetFullPath(root);
            List<string> pendingPaths = new List<string>();
            pendingPaths.Add(root);
            for (int i = 0; i < pendingPaths.Count; i++) {
                var src = Path.GetFullPath(pendingPaths[i]);
                string srcName = src == root ? @"\" : src.Substring(root.Length);

                if (actions.TryGetValue(srcName, out var action)) {
                    switch (action) {
                        case LinkAction.Ignore:
                            goto _nextItem;
                        case LinkAction.Copy:
                            CloneDirectory(src, Path.Combine(dest, srcName));
                            goto _nextItem;
                        case LinkAction.Link:
                            ReparsePointFactory.Provider.CreateLink(Path.Combine(dest, srcName), src, LinkType.Symbolic);
                            goto _nextItem;
                        case LinkAction.LinkContent:
                            foreach (var dir in Directory.GetDirectories(src)) {
                                if (!pendingPaths.Contains(dir))
                                    pendingPaths.Add(dir);
                            }

                            if (!Directory.Exists(Path.Combine(dest, srcName))) {
                                Directory.CreateDirectory(Path.Combine(dest, srcName));
                            }

                            foreach (var file in Directory.GetFiles(src)) {
                                srcName = src == root ? @"\" : file.Substring(root.Length);

                                switch (Path.GetExtension(file).ToLowerInvariant()) {
                                    //make link from all
                                    case ".pdb":
                                    case ".json":
                                    case ".csv":
                                    case ".exe":
                                    case ".txt":
                                    case ".xml":
                                    case ".dll":
                                        if (actions.TryGetValue(srcName, out action)) {
                                            switch (action) {
                                                case LinkAction.Copy:
                                                    File.Copy(file, Path.Combine(dest, file.Substring(root.Length)));
                                                    break;
                                                case LinkAction.Link:
                                                case LinkAction.LinkContent:
                                                    ReparsePointFactory.Provider.CreateLink(Path.Combine(dest, srcName), file, LinkType.Symbolic);
                                                    break;
                                                case LinkAction.Ignore: break;
                                                default:                throw new ArgumentOutOfRangeException();
                                            }
                                        } else
                                            ReparsePointFactory.Provider.CreateLink(Path.Combine(dest, srcName), file, LinkType.Symbolic);

                                        break;
                                    case "yield":
                                        break;
                                    default:
                                        File.Copy(file, Path.Combine(dest, file.Substring(root.Length)));
                                        break;
                                }
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                } else {
                    if (linkAllDirectories) {
                        ReparsePointFactory.Provider.CreateLink(Path.Combine(dest, srcName), src, LinkType.Symbolic);
                    } else {
                        CloneDirectory(src, Path.Combine(dest, srcName));
                    }

                    continue;
                }

                _nextItem: ;
            }

            Logger?.Trace($"Linked '{root}' to '{dest}' within {(DateTime.UtcNow - start).TotalMilliseconds:N1}ms");
        }*/

        public static List<string> GetCsvsWithHeaderAndData(List<string> files) {
            List<string> filesWithData = new List<string>();

            foreach (string file in files) {
                if (File.Exists(file)) {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                    using (StreamReader sr = new StreamReader(file)) {
                        string header = sr.ReadLine();

                        string data = sr.ReadLine();

                        if (!String.IsNullOrEmpty(data)) {
                            filesWithData.Add(file);
                        }
                    }
                } else {
                    Logger?.Trace("Warning File does not Exist: " + file);
                }
            }

            return filesWithData;
        }

        public static List<string> MergeCsvWithHeaderAndData(List<string> files) {
            files = GetCsvsWithHeaderAndData(files);

            List<string> data = new List<string>();

            string header = String.Empty;

            foreach (string file in files) {
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                using (StreamReader sr = new StreamReader(file)) {
                    if (String.IsNullOrEmpty(header)) {
                        header = sr.ReadLine();
                        data.Add(header);
                    } else {
                        string duplicateHeader = sr.ReadLine();
                    }

                    string dataLine = string.Empty;

                    while ((dataLine = sr.ReadLine()) != null) {
                        data.Add(dataLine);
                    }
                }
            }

            return data;
        }


        private static bool _cpuUsageSupported = true;

        public static int CpuUsage() {
            //TODO:
            return 0;
            /*try {
                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue();
                return (int) cpuCounter.NextValue();
            } catch (PlatformNotSupportedException) {
                _cpuUsageSupported = false;
                return 0;
            }*/
        }

        public static void DisplayProcessStats() {
            using (Process proc = Process.GetCurrentProcess()) {
                Logger?.Trace($"[Stats] PID {proc.Id}, NumberThreads: {proc.Threads.Count}, %{(_cpuUsageSupported ? ", CPU usage " + CpuUsage() + " %" : "")}, Physical memory usage: {Math.Round((proc.WorkingSet64 / memory) / memory, 0)} MB, Paged system memory size: {Math.Round((proc.PagedSystemMemorySize64 / memory) / memory, 0)} MB., Paged memory size: {Math.Round((proc.PagedMemorySize64 / memory) / memory, 0)} MB, Peak Paged Memory : {Math.Round((proc.PeakPagedMemorySize64 / memory) / memory, 0)} MB, Peak Virtual Memory : {Math.Round((((proc.PeakVirtualMemorySize64 / memory) / memory) / memory), 0)} GB, Peak Working Set : {Math.Round((proc.PeakWorkingSet64 / memory) / memory, 0)} MB, User processor time: {(proc.UserProcessorTime.Minutes % 10)} %, Privileged processor time: {(proc.PrivilegedProcessorTime.Minutes % 10)} %, Total processor time: {(proc.TotalProcessorTime.Minutes % 10)} %, Privileged processor time: {(proc.PrivilegedProcessorTime.Minutes % 10)}");
            }
        }

        public static double ConvertStringToDouble(string endsWithKMB) {
            String output = endsWithKMB;
            String tempvalue = endsWithKMB.Substring(0, endsWithKMB.Length - 1);
            double mainval = 0;
            String lastchar = endsWithKMB.Substring(endsWithKMB.Length - 1).ToUpper();

            if (lastchar == "B") {
                mainval = double.Parse(tempvalue) * 1000000000;
            } else if (lastchar == "M") {
                mainval = double.Parse(tempvalue) * 1000000;
            } else if (lastchar == "K") {
                mainval = double.Parse(tempvalue) * 1000;
            } else {
                mainval = 0;
            }

            return mainval;
        }


        public static int GetStaticHashCode(string strText)
        {

            if (string.IsNullOrEmpty(strText)) {
                return 0;
            }
            Int64 hashCode = 0;
            //Unicode Encode Covering all characterset
            byte[] byteContents = Encoding.Unicode.GetBytes(strText);
            System.Security.Cryptography.SHA256 hash = 
                new System.Security.Cryptography.SHA256CryptoServiceProvider();
            byte[] hashText = hash.ComputeHash(byteContents);
            //32Byte hashText separate
            //hashCodeStart = 0~7  8Byte
            //hashCodeMedium = 8~23  8Byte
            //hashCodeEnd = 24~31  8Byte
            //and Fold
            Int64 hashCodeStart = BitConverter.ToInt64(hashText, 0);
            Int64 hashCodeMedium = BitConverter.ToInt64(hashText, 8);
            Int64 hashCodeEnd = BitConverter.ToInt64(hashText, 24);
            hashCode = hashCodeStart ^ hashCodeMedium ^ hashCodeEnd;
            return (int)unchecked(hashCode%int.MaxValue);
        } 


        //Function to get random number
        public static readonly Random SharedRandom = new Random(AbstractInfrastructure.IsProduction ? unchecked((int) (DateTime.Now.Ticks % int.MaxValue)) : 32461759);

        public static int RandomNumber(int min, int max) {
            return SharedRandom.Next(min, max);
        }

        public static Dictionary<string, long> GetSymbolFloat(string floatFile) {
            Dictionary<string, long> symbolFloat = new Dictionary<string, long>();
            var lines = File.ReadAllLines(floatFile);
            string[] row = null;
            int count = 0;
            foreach (var line in lines) {
                // Skip the Header
                if (count != 0 && line != String.Empty) {
                    row = line.Split(',');
                    string floatStr = row[1].Replace(" ", "");

                    try {
                        symbolFloat.Add(row[0], Convert.ToInt64(floatStr));
                    } catch (Exception ex) {
                        Logger?.Error("Reading Bad Float Data: " + line);

                        // We will replace Bad Float Data with 0
                        symbolFloat.Add(row[0], AbstractInfrastructure.MissingValue);
                    }
                }

                count++;
            }

            return symbolFloat;
        }


        public static long RoundSharesTo100<T>(T value) {
            long valueLong = Convert.ToInt64(value);

            if (valueLong % 100 == 0) {
                return valueLong;
            } else {
                long originalValue = valueLong;
                long addOn = 100 - (Math.Abs(valueLong) % 100);
                valueLong += (Math.Sign(valueLong) * addOn);

                return valueLong;
            }
        }

        // We multiply by 1% give ourselves extra room for trading
        // Changed on June 20 2016 - to use Current price and not increment by 1.01
        public static double EstimateBuyingPower<T>(T orderSize, double price) {
            // return Math.Round(price * 1.01* Math.Abs(Convert.ToInt32(orderSize)), RoundingDecimals);

            return Math.Round(price * 1.0 * Math.Abs(Convert.ToInt32(orderSize)), AbstractInfrastructure.RoundingDecimals);
        }

        public static long GetNumberUnFormatted(string numberStr) {
            if (string.IsNullOrEmpty(numberStr))
                return 0;
            
            uint multiplier = 0;

            if (numberStr.EndsWith("K") || numberStr.EndsWith("k")) {
                multiplier = thousands;
            } else if (numberStr.EndsWith("M") || numberStr.EndsWith("m")) {
                multiplier = million;
            } else if (numberStr.EndsWith("B") || numberStr.EndsWith("b")) {
                multiplier = billion;
            }

            long number = multiplier * Int64.Parse(numberStr.Remove(numberStr.Length - 1, 1));

            return number;
        }

        public static double? GetNumberUnFormatted(ReadOnlySpan<char> numberStr) {
            if (numberStr.IsEmpty || numberStr.Equals(AbstractInfrastructure.MissingValueStr, StringComparison.Ordinal)) {
                return null;
            }
            uint multiplier = numberStr[^1] switch {
                'k' or 'K' => thousands,
                'm' or 'M' => million,
                'b' or 'B' => billion,
                _          => 1
            };

            double number = multiplier * double.Parse(multiplier == 1 ? numberStr : numberStr[..^1]);

            return number;
        }

        public static string ToKMB(string inputvalue) {
            if (!String.IsNullOrEmpty(inputvalue) && inputvalue != AbstractInfrastructure.MissingValueStr) {
                decimal num = Convert.ToDecimal(inputvalue);

                if (num > 999999999 || num < -999999999) {
                    return num.ToString("0,,,.###B", CultureInfo.InvariantCulture);
                } else if (num > 999999 || num < -999999) {
                    return num.ToString("0,,.##M", CultureInfo.InvariantCulture);
                } else if (num > 999 || num < -999) {
                    return num.ToString("0,.#K", CultureInfo.InvariantCulture);
                } else {
                    return num.ToString(CultureInfo.InvariantCulture);
                }
            } else
                return inputvalue;
        }

        public static string ToKMB(double num) {
            if (Math.Abs(num - AbstractInfrastructure.MissingValue) < 0.00001)
                return num.ToString(CultureInfo.InvariantCulture);

            switch (num) {
                case > 999999999:
                case < -999999999: return num.ToString("0,,,.B", CultureInfo.InvariantCulture);
                case > 999999:
                case < -999999: return num.ToString("0,,.M", CultureInfo.InvariantCulture);
                case > 999:
                case < -999: return num.ToString("0,.K", CultureInfo.InvariantCulture);
                default: return num.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string ToKMB(decimal num) {
            switch (num) {
                case > 999999999:
                case < -999999999: return num.ToString("0,,,.B", CultureInfo.InvariantCulture);
                case > 999999:
                case < -999999: return num.ToString("0,,.M", CultureInfo.InvariantCulture);
                case > 999:
                case < -999: return num.ToString("0,.K", CultureInfo.InvariantCulture);
                default: return num.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static string ToKMB(long num) {
            if (num == AbstractInfrastructure.MissingValue)
                return num.ToString(CultureInfo.InvariantCulture);

            switch (num) {
                case > 999999999:
                case < -999999999: return num.ToString("0,,,.B", CultureInfo.InvariantCulture);
                case > 999999:
                case < -999999: return num.ToString("0,,.M", CultureInfo.InvariantCulture);
                case > 999:
                case < -999: return num.ToString("0,.K", CultureInfo.InvariantCulture);
                default: return num.ToString(CultureInfo.InvariantCulture);
            }
        }

        public static TimeSpan GetTimeSpanFromHourMinute(string hourminStr) {
            int hour = Int32.Parse(hourminStr.Substring(0, 2));
            int min = Int32.Parse(hourminStr.Substring(2, 2));

            TimeSpan hourMin = new TimeSpan(hour, min, 0);

            return hourMin;
        }

        public static string[] ReadAllLines(string file, Encoding encoding = null, int? retries = null, TimeSpan? every = null) {
            return Retry.Do(() => {
                try {
                    using FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 25_000_000);
                    var lines = new List<string>(1028);
                    using var treader = new StreamReader(fs, Encoding.UTF8);
                    while (!treader.EndOfStream) {
                        var line = treader.ReadLine();
                        lines.Add(line);
                    }

                    return lines.ToArray();
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static string ReadAllText(string file, Encoding encoding = null, int? retries = null, TimeSpan? every = null) {
            return Retry.Do(() => {
                try {
                    return File.ReadAllText(file, encoding ?? Encoding.UTF8);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static FileStream OpenFile(string file, FileMode fileMode, FileAccess fileAccess, FileShare fileShare, int bufferSize, FileOptions opts = FileOptions.None, int? retries = null, TimeSpan? every = null) {
            return Retry.Do(() => {
                try {
                    return new FileStream(file, fileMode, fileAccess, fileShare, bufferSize, opts);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static ValueTask<string> ReadAllTextAsyncValueTask(string file, Encoding encoding = null, int? retries = null, TimeSpan? every = null) {
            return Retry.DoAsyncValueTask(async () => {
                try {
                    return await File.ReadAllTextAsync(file, encoding ?? Encoding.UTF8).ConfigureAwait(false);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static Task<string> ReadAllTextAsync(string file, Encoding encoding = null, int? retries = null, TimeSpan? every = null) {
            return Retry.DoAsync<string>(async () => {
                try {
                    using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 256_000);
                    using var reader = new StreamReader(fs, encoding ?? Encoding.UTF8);
                    return await reader.ReadToEndAsync().ConfigureAwait(false);
                    // Do something with fileText...
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static byte[] ReadAllBytes(string file, int? retries = null, TimeSpan? every = null) {
            return Retry.Do(() => {
                try {
                    return File.ReadAllBytes(file);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static void AppendAllText(string path, string contents, int? retries = null, TimeSpan? every = null) {
            Retry.Do(() => {
                try {
                    File.AppendAllText(path, contents);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static void AppendAllLines(string path, IEnumerable<string> contents, int? retries = null, TimeSpan? every = null) {
            Retry.Do(() => {
                try {
                    File.AppendAllLines(path, contents);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static void WriteAllBytes(string path, byte[] bytes, int? retries = null, TimeSpan? every = null) {
            Retry.Do(() => {
                try {
                    File.WriteAllBytes(path, bytes);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static void WriteAllText(string path, string text, int? retries = null, TimeSpan? every = null, Encoding encoding = null) {
            Retry.Do(() => {
                try {
                    File.WriteAllText(path, text, encoding ?? Encoding.UTF8);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (IOException e) when (e.Message.Contains("The filename, directory name")) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        public static void Copy(string source, string destinition, bool @override = true, int? retries = null, TimeSpan? every = null) {
            Retry.Do(() => {
                try {
                    File.Copy(source, destinition, @override);
                } catch (FileNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (DirectoryNotFoundException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (SecurityException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (PathTooLongException e) {
                    throw new AbortRetryAndThrowException(e);
                } catch (NotSupportedException e) {
                    throw new AbortRetryAndThrowException(e);
                }
            }, every ?? TimeSpan.FromMilliseconds(100), retries ?? 300, @throw: true);
        }

        [DebuggerHidden, DebuggerNonUserCode]
        public static void Debug() {
            Logger?.Debug("Debugger is waiting attachement,");
            while (!Debugger.IsAttached)
                Thread.Sleep(100);

            Debugger.Break();
        }
    }

    public enum LinkAction {
        Copy,
        Link,
        LinkContent,
        Ignore,
    }
}