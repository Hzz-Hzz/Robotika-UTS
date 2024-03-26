using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


public class Communication
{
    public readonly static InterprocessCommunicationRpc<CommunicationCodes> interprocessCommunication;

    static Communication() {
        var server = new InterprocessCommunicationServer("NuelValenRobotik");
        server.onLog += (sender, msg) => Console.WriteLine(msg);
        server.applyDefaultLoggingEvent();

        var interpWithTypes = new InterprocessCommunicationWithTypes(server);
        interprocessCommunication = new InterprocessCommunicationRpc<CommunicationCodes>(interpWithTypes);
        registerMethods();
    }

    public static async Task startListening() {
        interprocessCommunication.onConnectedToClient += (_) => {
            new Thread(async () => {
                Console.WriteLine("Sleeping");

                Thread.Sleep(3000);
                Console.WriteLine("Carrying out operations");
                var result = await interprocessCommunication.call<int>(CommunicationCodes.SUBSTRACTION, 2, 3, 5);
                Console.WriteLine($" =================== {result} =================== ");
            }).Start();
        };

        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException();
    }

    private static void registerMethods() {
        interprocessCommunication.registerMethod(CommunicationCodes.ADDITION, (Func<int, int, int, int>) addition);
        interprocessCommunication.registerMethod(CommunicationCodes.SUBSTRACTION, (Func<int, int, int, int>) substraction);
    }


    public static int addition(int a, int b, int c) {
        return a + b * c;
    }

    public static int substraction(int a, int b, int c) {
        return a - b - c;
    }
}