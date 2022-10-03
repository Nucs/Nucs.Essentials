using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Nucs {
    public record Logger {
        private readonly ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger;
        }

        public Logger(ILoggerFactory factory, string name) {
            _logger = factory.CreateLogger(name);
        }

        public void Trace(string message) {
            _logger.LogTrace(message);
        }

        public void Trace(string message, object argument) {
            _logger.LogTrace(message, argument);
        }

        public void Trace(string message, string argument) {
            _logger.LogTrace(message, argument);
        }

        public void Trace(StringBuilder sb) {
            _logger.LogTrace(sb.ToString());
        }

        public void Trace(string message, params object[] args) {
            _logger.LogTrace(message, args);
        }

        public void Trace(string message, double argument) {
            _logger.LogTrace(message, argument);
        }

        public void Error(string message) {
            _logger.LogError(message);
        }

        public void Error(Exception e) {
            _logger.LogError(e.ToString());
        }

        public void Error(string message, Exception e) {
            _logger.LogError(message, e);
        }

        public void Warn(string message) {
            _logger.LogWarning(message);
        }

        public void Warn(string message, object argument) {
            _logger.LogWarning(message, argument);
        }

        public void Warn(string message, string argument) {
            _logger.LogWarning(message, argument);
        }

        public void Info(string message) {
            _logger.LogInformation(message);
        }

        public void Info(string message, object argument) {
            _logger.LogInformation(message, argument);
        }

        public void Info(string message, object arg1, object arg2, object arg3) {
            _logger.LogTrace(message, arg1, arg2, arg3);
        }

        public void Debug(string message) {
            _logger.LogDebug(message);
        }

        public void Log(LogLevel level, string message) {
            _logger.Log(level, message);
        }

        public void Fatal(string message) {
            _logger.LogCritical(message);
        }

        public void WarnException(string message, Exception e) {
            _logger.LogWarning(message, e);
        }

        public void ErrorException(string message, Exception e) {
            _logger.LogError(message, e);
        }

        public void LogData(string dataName, Dictionary<string, string> data) {
            var sb = new StringBuilder();
            sb.Append($"Data: {dataName}");

            foreach (string dataKey in data.Keys)
                sb.Append($", {dataKey} {data[dataKey]}");

            _logger.LogTrace(sb.ToString());
        }

        public void LogData(string dataName, Dictionary<string, string> data, LogLevel level) {
            var sb = new StringBuilder();
            sb.Append($"Data: {dataName}");

            foreach (string dataKey in data.Keys)
                sb.Append($", {dataKey} {data[dataKey]}");

            _logger.Log(level, sb.ToString());
        }
    }
}