using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public class CommunicationHandler
{
    [CanBeNull] private static CommunicationHandler _communicationHandler;
    public static CommunicationHandler handler {
        get {
            Debug.Log("Getting handler");
            _communicationHandler = _communicationHandler ?? new CommunicationHandler();
            return _communicationHandler;
        }
    }

    private InterprocessCommunicationWithTypes _interprocessCommunication =
        new(new InterprocessCommunication("NuelValenRobotik", false));

    private CommunicationHandler() {
        _interprocessCommunication.onLog += ((_, msg) => Debug.Log(msg));
    }

    public bool started = false;
    public async Task startListeningAsync() {
        if (started)
            return;
        await _interprocessCommunication.startListeningAsync();
        started = true;
    }

    public Task stopListeningAndDisconnect() {
        return _interprocessCommunication.stopListeningAndDisconnect();
    }

    public async Task sendImage(byte[] image) {
        await _interprocessCommunication.writeMessage<byte[]>(image);
    }
}