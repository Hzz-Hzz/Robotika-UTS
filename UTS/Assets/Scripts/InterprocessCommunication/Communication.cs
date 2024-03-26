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
        interprocessCommunication.doNotSendErrorOnAlreadyClosedPipe = true;
        registerMethods();
    }

    public static void startListening() {
        Debug.Log("Call startListening()");

        interprocessCommunication.onConnectedToClient += (_) => {
            new Thread(async () => {
                Debug.Log("Sleeping");

                Thread.Sleep(1000);
                Debug.Log("Carrying out operations");
                var result = await interprocessCommunication.call<System.Numerics.Vector2>(CommunicationCodes.ADDITION,
                    new System.Numerics.Vector2(1, 3), new System.Numerics.Vector2(-5, 2), 3);
                Debug.Log($" =================== {result} =================== ");
            }).Start();
        };

        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException(
            (e) => Debug.LogError((e.ToString())));
    }

    public static void stopListening() {
        interprocessCommunication.stopListeningAndDisconnect();
    }

    private static void registerMethods() {
        interprocessCommunication.registerMethod(CommunicationCodes.ADDITION,
            (Func<System.Numerics.Vector2, System.Numerics.Vector2, int, System.Numerics.Vector2>) addition);
        interprocessCommunication.registerMethod(CommunicationCodes.SUBSTRACTION, (Func<int, int, int, int>) substraction);
    }


    public static System.Numerics.Vector2 addition(System.Numerics.Vector2 a, System.Numerics.Vector2 b, int c) {
        return (a + b) * c;
    }

    public static int substraction(int a, int b, int c) {
        return a - b + c;
    }

}