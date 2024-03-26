using System;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace InterprocessCommunication
{
    public class Temp
    {


        public static InterprocessCommunicationClient _interprocess = new InterprocessCommunicationClient("NuelValenRobotik");
        public static System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();



        public async static void initialize() {
            _interprocess.onLog += (_, msg) => Debug.Log(msg);
            _interprocess.onReceiveMessage += (_, msg) => {
                var something = msg.Select(e => e.ToString()).ToArray();
                Debug.Log($"Message received: {String.Join(",", something)}");
            };

            await _interprocess.tryConnect();
            _interprocess.applyDefaultLoggingEvent();
            new Thread(() => _interprocess.startListeningLoop()).Start();

            _stopwatch.Start();
        }

        public static void update() {
            if (_stopwatch.ElapsedMilliseconds < 1000)
                return;
            _stopwatch.Restart();
            Temp._interprocess.write(Encoding.UTF8.GetBytes("abc"));
        }

        public static void disconnect() {
            _interprocess.dispose();
        }
    }
}