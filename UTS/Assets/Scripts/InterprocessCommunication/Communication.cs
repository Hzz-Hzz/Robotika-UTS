using System;
using System.Threading;
using UnityEngine;


public class Communication
{
    public readonly static InterprocessCommunicationRpc<CommunicationCodes> interprocessCommunication;

    static Communication() {
        var server = new InterprocessCommunicationClient("NuelValenRobotik");
        server.onLog += (sender, msg) => Debug.Log(msg);
        server.applyDefaultLoggingEvent();

        var interpWithTypes = new InterprocessCommunicationWithTypes(server);
        interprocessCommunication = new InterprocessCommunicationRpc<CommunicationCodes>(interpWithTypes);
        registerMethods();
    }

    public static void startListening() {
        Debug.Log("Call startListening()");

        interprocessCommunication.onConnectedToClient += (_) => {
            new Thread(async () => {
                Debug.Log("Sleeping");

                Thread.Sleep(1000);
                Debug.Log("Carrying out operations");
                var result = await interprocessCommunication.call<int>(CommunicationCodes.ADDITION, 2, 3, 5);
                Debug.Log($" =================== {result} =================== ");
            }).Start();
        };

        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException();
    }

    public static void stopListening() {
        interprocessCommunication.stopListeningAndDisconnect();
    }

    private static void registerMethods() {
        interprocessCommunication.registerMethod(CommunicationCodes.ADDITION, (Func<int, int, int, int>) addition);
        interprocessCommunication.registerMethod(CommunicationCodes.SUBSTRACTION, (Func<int, int, int, int>) substraction);
    }


    public static int addition(int a, int b, int c) {
        return a + b * c;
    }

    public static int substraction(int a, int b, int c) {
        return a - b + c;
    }

}