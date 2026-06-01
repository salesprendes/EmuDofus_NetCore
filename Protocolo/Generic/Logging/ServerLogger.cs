using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Protocolo.Framework.Generic.Logging
{
    public sealed class ServerLogger : ILogger
    {
        private readonly string _name;

        // Unbounded, single-reader: callers just TryWrite and return — no locking on the hot path.
        private static readonly Channel<LogEntry> _channel =
            Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

        private static string _currentDate = string.Empty;
        private static StreamWriter _fileWriter;

        static ServerLogger()
        {
            new Thread(DrainQueue)
            {
                IsBackground = true,
                Name = "Logger-Writer"
            }.Start();
        }

        public ServerLogger(string name) => _name = name;

        public void Debug(object message) => Enqueue("DEBUG", message);
        public void Info(object message)  => Enqueue("INFO",  message);
        public void Warn(object message, Exception exception = null)  => Enqueue("WARN",  Combine(message, exception));
        public void Error(object message, Exception exception = null) => Enqueue("ERROR", Combine(message, exception));
        public void Fatal(object message, Exception exception = null) => Enqueue("FATAL", Combine(message, exception));

        private void Enqueue(string level, object body)
        {
            var now = DateTime.Now;
            var line = $"[{now:HH:mm:ss} ({Environment.CurrentManagedThreadId})] - {level} : [{_name}] {body}";
            _channel.Writer.TryWrite(new LogEntry(level, line, now));
        }

        private static object Combine(object message, Exception exception) =>
            exception is null ? message : $"{message}{Environment.NewLine}{exception}";

        // Background thread: single consumer — no locks needed for console or file.
        private static void DrainQueue()
        {
            var reader = _channel.Reader;
            while (reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
            {
                while (reader.TryRead(out var entry))
                {
                    WriteConsole(entry.Level, entry.Line);
                    WriteFile(entry.Time, entry.Level, entry.Line);
                }
            }
        }

        private static void WriteConsole(string level, string line)
        {
            Console.ForegroundColor = level switch
            {
                "DEBUG" => ConsoleColor.DarkGray,
                "WARN"  => ConsoleColor.Yellow,
                "ERROR" => ConsoleColor.Red,
                "FATAL" => ConsoleColor.DarkRed,
                _       => ConsoleColor.White
            };
            Console.WriteLine(line);
            Console.ResetColor();
        }

        private static void WriteFile(DateTime now, string level, string line)
        {
            if (level == "DEBUG") return;

            var date = now.ToString("yyyy_MM_dd");
            if (date != _currentDate)
            {
                _fileWriter?.Flush();
                _fileWriter?.Dispose();
                Directory.CreateDirectory("logs");
                _fileWriter = new StreamWriter($"logs/{date}.log", append: true, Encoding.UTF8) { AutoFlush = true };
                _currentDate = date;
            }
            _fileWriter?.WriteLine(line);
        }

        private readonly record struct LogEntry(string Level, string Line, DateTime Time);
    }
}
