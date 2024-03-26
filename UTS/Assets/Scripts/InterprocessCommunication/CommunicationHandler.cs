using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Random = System.Random;


public class CommunicationHandler
{


    public static InterprocessCommunicationWithTypes _interprocess;
    public static System.Diagnostics.Stopwatch _stopwatch = new System.Diagnostics.Stopwatch();



    public async static void initialize() {
        _stopwatch.Start();
        var server = new InterprocessCommunicationClient("NuelValenRobotik");
        server.onLog += (_, msg) => Debug.Log(msg);
        server.applyDefaultLoggingEvent();

        _interprocess = new InterprocessCommunicationWithTypes(server);
        _interprocess.onReceiveMessage += ReceiveMessage;

    }

    private static void ReceiveMessage(InterprocessCommunicationWithTypes sender) {
        var msg = sender.readMessage<string>();
        Console.WriteLine(msg);
    }

    public static void update() {
        if (_stopwatch.ElapsedMilliseconds < 2000)
            return;
        var time = _stopwatch.ElapsedMilliseconds;
        _stopwatch.Restart();

        var item = new Tuple<int, string>((int)time, "Halo bang");
        _interprocess.writeMessage(item);
    }

    public static void disconnect() {
        _interprocess.stopListeningAndDisconnect();
    }

}