using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Protocolo.Framework.Generic.Logging
{
    public sealed class ServerLogger : ILogger
    {
        private static readonly string[] LevelNames = { "DEBUG", "INFO ", "WARN ", "ERROR", "FATAL", "OFF" };
        private static readonly ConsoleColor[] LevelColors = { ConsoleColor.DarkGray, ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.White };

        private static volatile LogOptions _options = LogOptions.Load();
        private static readonly object _writerLock = new object ();
        private static readonly Channel<LogEntry> _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(_options.QueueCapacity) { SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false, FullMode = BoundedChannelFullMode.Wait });
        private static readonly Thread _writerThread;
        private readonly string _name;
        private static string _currentDate = string.Empty;
        private static StreamWriter _fileWriter;
        private static long _droppedMessages;
        private static long _lastFlushTicks;
        private static int _shutdown;
        public ServerLogger(string name) => _name = string.IsNullOrWhiteSpace(name) ? "Desconocido" : name;

        static ServerLogger()
        {
            _lastFlushTicks = DateTime.UtcNow.Ticks;
            _writerThread = new Thread(DrainQueue) { IsBackground = true, Name = "Logger-Writer" };
            _writerThread.Start();

            AppDomain.CurrentDomain.ProcessExit += static (_, _) => Shutdown();
        }

        public bool IsEnabled(LogLevel level)
        {
            var options = _options;
            return options.MinimumLevel != LogLevel.Off && level >= options.MinimumLevel;
        }

        public void Debug(object message, Exception exception = null) => Enqueue(LogLevel.Debug, message, exception);
        public void Info(object message, Exception exception = null) => Enqueue(LogLevel.Info, message, exception);
        public void Warn(object message, Exception exception = null) => Enqueue(LogLevel.Warn, message, exception);
        public void Error(object message, Exception exception = null) => Enqueue(LogLevel.Error, message, exception);
        public void Fatal(object message, Exception exception = null) => Enqueue(LogLevel.Fatal, message, exception);

        private void Enqueue(LogLevel level, object message, Exception exception)
        {
            if (!IsEnabled(level) || Volatile.Read(ref _shutdown) != 0)
            {
                return;
            }

            var options = _options;
            var entry = new LogEntry(DateTime.Now, level, _name, message, exception);
            if (_channel.Writer.TryWrite(entry))
            {
                return;
            }

            Interlocked.Increment(ref _droppedMessages);
            if (level < LogLevel.Error)
            {
                return;
            }

            lock (_writerLock)
            {
                Write(entry, options);
                _fileWriter?.Flush();
                _lastFlushTicks = DateTime.UtcNow.Ticks;
            }
        }

        private static void DrainQueue()
        {
            var reader = _channel.Reader;

            while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
            {
                lock (_writerLock)
                {
                    var options = _options;
                    var dropped = Interlocked.Exchange(ref _droppedMessages, 0);
                    if (dropped > 0)
                    {
                        Write(new LogEntry(DateTime.Now, LogLevel.Warn, "LogManager", "Se descartaron " + dropped + " mensajes de log porque la cola estaba llena.", null), options);
                    }

                    while (reader.TryRead(out var entry))
                    {
                        Write(entry, options);
                    }

                    var nowTicks = DateTime.UtcNow.Ticks;
                    if (_fileWriter != null && nowTicks - _lastFlushTicks >= options.FlushIntervalTicks)
                    {
                        _fileWriter.Flush();
                        _lastFlushTicks = nowTicks;
                    }
                }
            }

            lock (_writerLock)
            {
                while (reader.TryRead(out var entry))
                {
                    Write(entry, _options);
                }

                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                _fileWriter = null;
                _currentDate = string.Empty;
            }
        }

        private static void Write(LogEntry entry, LogOptions options)
        {
            var message = entry.Message == null ? string.Empty : entry.Message.ToString();
            if (entry.Exception != null)
            {
                message = message + Environment.NewLine + entry.Exception;
            }

            var levelIndex = (int)entry.Level;
            if (levelIndex < 0 || levelIndex >= LevelNames.Length)
            {
                levelIndex = (int)LogLevel.Off;
            }

            string line = string.Concat("[", entry.Time.ToString(options.TimestampFormat), " ", LevelNames[levelIndex], "] [", entry.LoggerName, "] ", message);

            if (options.ConsoleEnabled && entry.Level >= options.ConsoleLevel)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = LevelColors[levelIndex];
                Console.WriteLine(line);
                Console.ForegroundColor = previousColor;
            }

            if (!options.FileEnabled || entry.Level < options.FileLevel)
            {
                return;
            }

            string date = entry.Time.ToString("yyyy_MM_dd");
            if (date != _currentDate)
            {
                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                Directory.CreateDirectory(options.LogDirectory);
                _fileWriter = new StreamWriter(Path.Combine(options.LogDirectory, date + ".log"), append: true, Encoding.UTF8, 64 * 1024) { AutoFlush = false };
                _currentDate = date;
            }

            _fileWriter?.WriteLine(line);
        }

        public static void Flush()
        {
            lock (_writerLock)
            {
                _fileWriter?.Flush();
                _lastFlushTicks = DateTime.UtcNow.Ticks;
            }
        }

        public static void ConfigureLevels(string minimumLevel, string consoleLevel = null, string fileLevel = null)
        {
            var options = _options;
            var resolvedMinimumLevel = LogOptions.ParseLevel(minimumLevel, options.MinimumLevel);

            _options = options.WithLevels(resolvedMinimumLevel, LogOptions.ParseLevel(consoleLevel, resolvedMinimumLevel), LogOptions.ParseLevel(fileLevel, resolvedMinimumLevel));
        }

        public static void Shutdown()
        {
            if (Interlocked.Exchange(ref _shutdown, 1) != 0)
            {
                return;
            }

            _channel.Writer.TryComplete();
            _writerThread.Join(1500);

            lock (_writerLock)
            {
                while (_channel.Reader.TryRead(out var entry))
                {
                    Write(entry, _options);
                }

                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                _fileWriter = null;
                _currentDate = string.Empty;
            }
        }

        private readonly record struct LogEntry(DateTime Time, LogLevel Level, string LoggerName, object Message, Exception Exception);

        private sealed class LogOptions
        {
            public readonly LogLevel MinimumLevel;
            public readonly LogLevel ConsoleLevel;
            public readonly LogLevel FileLevel;
            public readonly bool ConsoleEnabled;
            public readonly bool FileEnabled;
            public readonly string LogDirectory;
            public readonly string TimestampFormat;
            public readonly int QueueCapacity;
            public readonly long FlushIntervalTicks;

            private LogOptions(LogLevel minimumLevel, LogLevel consoleLevel, LogLevel fileLevel, bool consoleEnabled, bool fileEnabled, string logDirectory, string timestampFormat, int queueCapacity, long flushIntervalTicks)
            {
                MinimumLevel = minimumLevel;
                ConsoleLevel = consoleLevel;
                FileLevel = fileLevel;
                ConsoleEnabled = consoleEnabled;
                FileEnabled = fileEnabled;
                LogDirectory = logDirectory;
                TimestampFormat = timestampFormat;
                QueueCapacity = queueCapacity;
                FlushIntervalTicks = flushIntervalTicks;
            }

            public LogOptions WithLevels(LogLevel minimumLevel, LogLevel consoleLevel, LogLevel fileLevel)
            {
                return new LogOptions(minimumLevel, consoleLevel, fileLevel, ConsoleEnabled, FileEnabled, LogDirectory, TimestampFormat, QueueCapacity, FlushIntervalTicks);
            }

            public static LogOptions Load()
            {
                var minimumLevel = ReadLevel("PROTOCOLO_LOG_LEVEL", LogLevel.Info);

                return new LogOptions(
                    minimumLevel,
                    ReadLevel("PROTOCOLO_LOG_CONSOLE_LEVEL", minimumLevel),
                    ReadLevel("PROTOCOLO_LOG_FILE_LEVEL", LogLevel.Info),
                    ReadBool("PROTOCOLO_LOG_CONSOLE", true),
                    ReadBool("PROTOCOLO_LOG_FILE", true),
                    ReadString("PROTOCOLO_LOG_DIR", "logs"),
                    ReadString("PROTOCOLO_LOG_TIMESTAMP", "dd/MM/yyyy HH:mm:ss"),
                    ReadInt("PROTOCOLO_LOG_QUEUE_CAPACITY", 32768, 1024, 1048576),
                    TimeSpan.FromMilliseconds(ReadInt("PROTOCOLO_LOG_FLUSH_MS", 1000, 50, 60000)).Ticks);
            }

            public static LogLevel ParseLevel(string value, LogLevel fallback)
            {
                return Enum.TryParse(value, ignoreCase: true, out LogLevel level) ? level : fallback;
            }

            private static LogLevel ReadLevel(string key, LogLevel fallback)
            {
                return ParseLevel(Environment.GetEnvironmentVariable(key), fallback);
            }

            private static bool ReadBool(string key, bool fallback)
            {
                string value = Environment.GetEnvironmentVariable(key);
                if (string.IsNullOrWhiteSpace(value))
                {
                    return fallback;
                }

                return value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase);
            }

            private static string ReadString(string key, string fallback)
            {
                var value = Environment.GetEnvironmentVariable(key);
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }

            private static int ReadInt(string key, int fallback, int min, int max)
            {
                var value = Environment.GetEnvironmentVariable(key);
                if (!int.TryParse(value, out var number))
                {
                    return fallback;
                }

                return Math.Min(max, Math.Max(min, number));
            }
        }
    }
}
