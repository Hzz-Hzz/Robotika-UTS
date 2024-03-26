using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

#nullable enable

/**
 * InterprocessCommunicationBase (as well as InterprocessCommunicationServer & InterprocessCommunicationClient) are low-level
 * functionality, thus can only send & receive byte[], meanwhile this class is aimed to provide higher-level
 * functionality, thus you can send any serializable type through this class.
 */
public class InterprocessCommunicationWithTypes
{
    private InterprocessCommunicationBase _interprocessCommunication;
    private SerializationAlgorithm _serializationAlgorithm;

#region DELEGATIONS
    public event Logging onLog {
        add => _interprocessCommunication.onLog += value;
        remove => _interprocessCommunication.onLog -= value;
    }
    public event WaitingForClient onWaitingForClient {
        add => _interprocessCommunication.onWaitingForClient += value;
        remove => _interprocessCommunication.onWaitingForClient -= value;
    }
    public event Connected onConnectedToClient {
        add => _interprocessCommunication.onConnected += value;
        remove => _interprocessCommunication.onConnected -= value;
    }
    public event Disconnected onDisconnected {
        add => _interprocessCommunication.onDisconnected += value;
        remove => _interprocessCommunication.onDisconnected -= value;
    }
    public event FailSendMessage onFailToSendMessage {
        add => _interprocessCommunication.onFailToSendMessage += value;
        remove => _interprocessCommunication.onFailToSendMessage -= value;
    }
#endregion

    public event NewMessageNotification onReceiveMessage;

    public InterprocessCommunicationWithTypes(InterprocessCommunicationBase interprocessCommunication, SerializationAlgorithm? serializationAlgorithm=null) {
        _interprocessCommunication = interprocessCommunication;
        _serializationAlgorithm = serializationAlgorithm ?? new NewtonsoftJsonSerializer();
        _interprocessCommunication.onReceiveMessage += this.handleOnReceiveMessage;
    }

    private Queue<byte[]> receivedMessages = new Queue<byte[]>();

    private void handleOnReceiveMessage(IInterprocessCommunication _, byte[] message) {
        if (message.Length == 0)
            return;
        receivedMessages.Enqueue(message);
        onReceiveMessage?.Invoke(this);
    }

    private Thread? listeningThread;
    public async Task startListeningAsync() {
        await _interprocessCommunication.connect();
        await _interprocessCommunication.startListeningLoop();  // dont await
    }

    public async Task stopListeningAndDisconnect() {
        _interprocessCommunication.stopListening();
        _interprocessCommunication.dispose();
    }

    public T? readMessage<T>() {
        var data = receivedMessages.Dequeue();
        return _serializationAlgorithm.DeserializeObject<T>(data);
    }

    public async Task<bool> writeMessage(object message) {
        var data = _serializationAlgorithm.SerializeObject(message);
        return await _interprocessCommunication.write(data);
    }
}


public interface SerializationAlgorithm
{
    public T? DeserializeObject<T>(byte[] data);
    public byte[] SerializeObject(object data);
}

public class NewtonsoftJsonSerializer : SerializationAlgorithm
{
    public T? DeserializeObject<T>(byte[] data) {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
    }

    public byte[] SerializeObject(object data) {
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
    }
}