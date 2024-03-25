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
public class InterprocessCommunicationWithTypes
{
    private InterprocessCommunication _interprocessCommunication;

#region DELEGATIONS
    public event Logging onLog {
        add => _interprocessCommunication.onLog += value;
        remove => _interprocessCommunication.onLog -= value;
    }
    public event WaitingForClient onWaitingForClient {
        add => _interprocessCommunication.onWaitingForClient += value;
        remove => _interprocessCommunication.onWaitingForClient -= value;
    }
    public event ConnectedToClient onConnectedToClient {
        add => _interprocessCommunication.onConnectedToClient += value;
        remove => _interprocessCommunication.onConnectedToClient -= value;
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

    public event ReceiveMessageWithTypes onReceiveMessage;

    public InterprocessCommunicationWithTypes(InterprocessCommunication interprocessCommunication) {
        _interprocessCommunication = interprocessCommunication;
        _interprocessCommunication.onReceiveMessage += this.handleOnReceiveMessage;
    }

    private Queue<byte[]> receivedMessages = new Queue<byte[]>();

    private void handleOnReceiveMessage(InterprocessCommunication _, byte[] message) {
        receivedMessages.Enqueue(message);
        onReceiveMessage?.Invoke(this);
    }

    private Thread? listeningThread;
    public async Task startListeningAsync() {
        await _interprocessCommunication.initialize();
        Task.Run(_interprocessCommunication.startConnectAndListening);  // dont await
    }

    public async Task stopListeningAndDisconnect() {
        await _interprocessCommunication.stopListeningAndDisconnect();
    }

    public async Task restartListeningAsync() {
        await stopListeningAndDisconnect();
        await startListeningAsync();
    }

    public T readMessage<T>() {
        var data = receivedMessages.Dequeue();
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
    }

    public async Task writeMessage<T>(object message) {
        var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
        await _interprocessCommunication.writeBytes(data);
    }
}


public delegate void ReceiveMessageWithTypes(InterprocessCommunicationWithTypes sender);

class MarshallingCodec
{  // https://stackoverflow.com/a/19468007/7069108
    public static  T FromByteArray<T>(byte[] rawValue)
    {
        GCHandle handle = GCHandle.Alloc(rawValue, GCHandleType.Pinned);
        T structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return structure;
    }

    public static byte[] ToByteArray(object your_object, int maxLength)
    {
        var size = Marshal.SizeOf(your_object);
        var bytes = new byte[size];
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(your_object, ptr, false);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);
        return bytes;
    }
}

