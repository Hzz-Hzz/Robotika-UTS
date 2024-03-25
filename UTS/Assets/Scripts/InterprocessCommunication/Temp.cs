using System;
using System.Text;

namespace InterprocessCommunication
{
    public class Temp
    {


        public static InterprocessCommunicationClient _interprocess = new InterprocessCommunicationClient("NuelValenRobotik");

        public async static void initialize() {
            await _interprocess.connect();
            _interprocess.onLog += (_, msg) => Console.WriteLine(msg);
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