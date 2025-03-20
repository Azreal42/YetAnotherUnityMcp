using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace YetAnotherUnityMcp.Editor
{
    /// <summary>
    /// Monitor and report performance metrics for WebSocket communication
    /// </summary>
    public class CommandExecutionMonitor
    {
        // Singleton instance
        private static CommandExecutionMonitor _instance;
        public static CommandExecutionMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CommandExecutionMonitor();
                }
                return _instance;
            }
        }
        
        // Metrics
        private class OperationMetrics
        {
            public int Count { get; set; }
            public float TotalTime { get; set; }
            public float MaxTime { get; set; }
            public float MinTime { get; set; } = float.MaxValue;
            public DateTime LastOccurrence { get; set; }
            
            public float AverageTime => Count > 0 ? TotalTime / Count : 0;
        }
        
        private Dictionary<string, OperationMetrics> _metrics = new Dictionary<string, OperationMetrics>();
        private DateTime _lastReportTime = DateTime.Now;
        private bool _enableLogging = true;
        private int _reportIntervalSeconds = 60; // Log a report every minute by default
        
        // Enable or disable performance logging
        public void SetLoggingEnabled(bool enabled)
        {
            _enableLogging = enabled;
        }
        
        // Set how often performance reports should be logged
        public void SetReportInterval(int seconds)
        {
            _reportIntervalSeconds = Mathf.Max(1, seconds);
        }
        
        // Clear all collected metrics
        public void ClearMetrics()
        {
            _metrics.Clear();
            _lastReportTime = DateTime.Now;
        }
        
        // Record an operation's execution time
        public void RecordOperation(string operationName, float executionTimeMs)
        {
            if (!_metrics.TryGetValue(operationName, out var metric))
            {
                metric = new OperationMetrics();
                _metrics[operationName] = metric;
            }
            
            metric.Count++;
            metric.TotalTime += executionTimeMs;
            metric.MaxTime = Mathf.Max(metric.MaxTime, executionTimeMs);
            metric.MinTime = Mathf.Min(metric.MinTime, executionTimeMs);
            metric.LastOccurrence = DateTime.Now;
            
            // Check if it's time to log a report
            TimeSpan elapsed = DateTime.Now - _lastReportTime;
            if (_enableLogging && elapsed.TotalSeconds >= _reportIntervalSeconds)
            {
                LogPerformanceReport();
                _lastReportTime = DateTime.Now;
            }
        }
        
        // Log a performance report with all metrics
        public void LogPerformanceReport()
        {
            if (_metrics.Count == 0)
            {
                return;
            }
            
            Debug.Log("===== WebSocket Performance Report =====");
            Debug.Log($"Time: {DateTime.Now.ToString("HH:mm:ss")}");
            Debug.Log("Operation | Count | Avg (ms) | Min (ms) | Max (ms) | Last");
            Debug.Log("--------------------------------------------------------");
            
            foreach (var entry in _metrics)
            {
                string operationName = entry.Key;
                var metric = entry.Value;
                
                // Format time as mm:ss ago
                TimeSpan timeSinceLastOccurrence = DateTime.Now - metric.LastOccurrence;
                string lastOccurrenceFormatted = $"{timeSinceLastOccurrence.Minutes:D2}:{timeSinceLastOccurrence.Seconds:D2} ago";
                
                Debug.Log($"{operationName.PadRight(20)} | {metric.Count,5} | {metric.AverageTime,8:F2} | {metric.MinTime,8:F2} | {metric.MaxTime,8:F2} | {lastOccurrenceFormatted}");
            }
            
            Debug.Log("===========================================");
        }
        
        // Create a timer to measure an operation's duration
        public OperationTimer StartOperation(string operationName)
        {
            return new OperationTimer(operationName, this);
        }
        
        // Timer class for easy performance measurement using 'using' blocks
        public class OperationTimer : IDisposable
        {
            private string _operationName;
            private CommandExecutionMonitor _monitor;
            private float _startTimeMs;
            
            public OperationTimer(string operationName, CommandExecutionMonitor monitor)
            {
                _operationName = operationName;
                _monitor = monitor;
                _startTimeMs = Time.realtimeSinceStartup * 1000f;
            }
            
            public void Dispose()
            {
                float endTimeMs = Time.realtimeSinceStartup * 1000f;
                float elapsedMs = endTimeMs - _startTimeMs;
                _monitor.RecordOperation(_operationName, elapsedMs);
            }
        }
    }
}