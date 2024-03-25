using System;
using System.Text;
using UnityEngine;

namespace InterprocessCommunication
{
    public class Temp
    {


        public static InterprocessCommunicationClient _interprocess = new InterprocessCommunicationClient("NuelValenRobotik");

        public async static void initialize() {
            _interprocess.onLog += (_, msg) => Debug.Log(msg);
            await _interprocess.connect();
            _interprocess.applyDefaultLoggingEvent();
        }

        public static void update() {
            Temp._interprocess.write(Encoding.UTF8.GetBytes("abc"));
        }

        public static void disconnect() {
            _interprocess.dispose();
        }
    }
}