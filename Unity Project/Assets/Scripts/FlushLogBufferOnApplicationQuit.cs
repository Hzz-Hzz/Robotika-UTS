using System;
using System.Text;
using UnityEngine;

namespace DefaultNamespace
{
    public sealed class CustomLogger
    {
        public const bool enabled = true;
        public const int flushInterval = 10;


        private static StringBuilder logs = new StringBuilder(2000);
        private static int cnt = 0;

        public static void Log(string message, bool immediateFlush=false, string lineSeparator="\n") {
            if (!enabled)
                return;
            logs.Append(message);
            logs.Append(lineSeparator);
            cnt++;
            if (immediateFlush || cnt % flushInterval == 0)
                Flush();
        }

        public static void Flush() {
            Debug.Log(logs.ToString());
            Clear();
        }
        public static void Clear() {
            logs.Clear();
            cnt = 0;
        }

    }

    public class FlushLogBufferOnApplicationQuit : MonoBehaviour {
        private void OnApplicationQuit() {
            CustomLogger.Flush();
        }
    }
}