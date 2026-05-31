using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Mirrors Unity Debug.Log/Warning/Error/Exception output to newline-delimited JSON.
/// The original Unity console output is untouched; this only subscribes to the log callback
/// and writes each received message to disk for easier filtering/sharing outside the Editor.
/// </summary>
public sealed class JsonDebugLogWriter : MonoBehaviour
{
    private const string LoggerObjectName = "JsonDebugLogWriter";
    private const string LogDirectoryName = "JsonLogs";
    private const string LogFilePrefix = "debug-log";
    private const string LogFileExtension = "jsonl";

    private static JsonDebugLogWriter _instance;
    private static readonly object PendingLogsLock = new();
    private static readonly Queue<PendingDebugLog> PendingLogs = new();

    private bool _writeLogs = true;
    [SerializeField] private bool _includeStackTrace = true;
    [SerializeField] private int _maxLogsPerFrame = 100;
    [SerializeField] private string _logFilePath;

    private StreamWriter _writer;
    private bool _subscribed;

    public string LogFilePath => _logFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateLogger()
    {
        if (_instance != null) return;

        var loggerObject = new GameObject(LoggerObjectName);
        DontDestroyOnLoad(loggerObject);
        _instance = loggerObject.AddComponent<JsonDebugLogWriter>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeWriter();
        SubscribeToUnityLogs();

        if (_writer != null)
        {
            Debug.Log($"[JsonDebugLogWriter] Mirroring Unity logs to JSON lines file: {_logFilePath}", this);
        }
    }

    private void Update()
    {
        FlushPendingLogs(_maxLogsPerFrame);
    }

    private void OnDestroy()
    {
        UnsubscribeFromUnityLogs();
        FlushPendingLogs(int.MaxValue);
        CloseWriter();

        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        FlushPendingLogs(int.MaxValue);
        CloseWriter();
    }

    private void InitializeWriter()
    {
        if (!_writeLogs) return;

        string directory = Path.Combine(Application.persistentDataPath, LogDirectoryName);
        Directory.CreateDirectory(directory);

        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        _logFilePath = Path.Combine(directory, $"{LogFilePrefix}-{timestamp}.{LogFileExtension}");
        _writer = new StreamWriter(_logFilePath, append: false) { AutoFlush = true };
    }

    private void SubscribeToUnityLogs()
    {
        if (_subscribed) return;

        Application.logMessageReceivedThreaded += OnUnityLogMessageReceived;
        _subscribed = true;
    }

    private void UnsubscribeFromUnityLogs()
    {
        if (!_subscribed) return;

        Application.logMessageReceivedThreaded -= OnUnityLogMessageReceived;
        _subscribed = false;
    }

    private void OnUnityLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!_writeLogs) return;

        var pendingLog = new PendingDebugLog
        {
            timestampUtc = DateTime.UtcNow.ToString("o"),
            threadId = Environment.CurrentManagedThreadId,
            type = type.ToString(),
            message = condition,
            stackTrace = _includeStackTrace ? stackTrace : string.Empty
        };

        lock (PendingLogsLock)
        {
            PendingLogs.Enqueue(pendingLog);
        }
    }

    private void FlushPendingLogs(int maxLogsToFlush)
    {
        if (_writer == null) return;

        int logsFlushed = 0;
        while (logsFlushed < maxLogsToFlush)
        {
            PendingDebugLog pendingLog;
            lock (PendingLogsLock)
            {
                if (PendingLogs.Count == 0) break;
                pendingLog = PendingLogs.Dequeue();
            }

            var entry = new DebugLogEntry
            {
                timestampUtc = pendingLog.timestampUtc,
                realtimeSinceStartup = Time.realtimeSinceStartup,
                frame = Time.frameCount,
                threadId = pendingLog.threadId,
                type = pendingLog.type,
                message = pendingLog.message,
                stackTrace = pendingLog.stackTrace
            };

            _writer.WriteLine(JsonUtility.ToJson(entry));
            logsFlushed++;
        }
    }

    private void CloseWriter()
    {
        if (_writer == null) return;

        _writer.Flush();
        _writer.Dispose();
        _writer = null;
    }

    private struct PendingDebugLog
    {
        public string timestampUtc;
        public int threadId;
        public string type;
        public string message;
        public string stackTrace;
    }

    [Serializable]
    private struct DebugLogEntry
    {
        public string timestampUtc;
        public float realtimeSinceStartup;
        public int frame;
        public int threadId;
        public string type;
        public string message;
        public string stackTrace;
    }
}
