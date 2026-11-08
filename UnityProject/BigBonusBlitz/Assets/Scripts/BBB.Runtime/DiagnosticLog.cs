using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BBB.Runtime
{
    /// <summary>
    /// エラーや「押せない」を後から調べるための記録（ゲーム画面のログとは別。2026-11-09 本人）。
    /// レバー・結果・入力の鍵・押せなかった理由・例外を 1 行ずつファイルへ書く。
    /// 置き場所: Application.persistentDataPath/diagnostics/bbb-日付.log（4MB を超えたら .old に回す）。
    /// </summary>
    public static class DiagnosticLog
    {
        const long MaxBytes = 4L * 1024 * 1024;
        static readonly object Gate = new object();
        static StreamWriter writer;
        static string dir;

        /// <summary>いま書いているファイル（まだ何も書いていなければ null）。</summary>
        public static string FilePath { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Close(); dir = null; FilePath = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            dir = Path.Combine(Application.persistentDataPath, "diagnostics");   // メインスレッドで決めておく
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            Application.quitting -= Close;
            Application.quitting += Close;
            Write($"START  version={Application.version} platform={Application.platform} editor={Application.isEditor}");
        }

        static void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            Write($"{type.ToString().ToUpperInvariant()}  {message}\n{stack}".TrimEnd());
        }

        public static void Write(string line)
        {
            if (dir == null) return;
            lock (Gate)
            {
                try
                {
                    if (writer == null) Open();
                    else if (writer.BaseStream.Length > MaxBytes) { writer.Dispose(); writer = null; Rotate(); Open(); }
                    writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line);
                }
                catch (Exception) { /* 記録の失敗でゲームを止めない */ }
            }
        }

        static void Open()
        {
            Directory.CreateDirectory(dir);
            FilePath = Path.Combine(dir, "bbb-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes) Rotate();
            writer = new StreamWriter(FilePath, true, new UTF8Encoding(false)) { AutoFlush = true };
        }

        static void Rotate()
        {
            string old = FilePath + ".old";
            if (File.Exists(old)) File.Delete(old);
            File.Move(FilePath, old);
        }

        static void Close()
        {
            lock (Gate) { writer?.Dispose(); writer = null; }
        }
    }
}
