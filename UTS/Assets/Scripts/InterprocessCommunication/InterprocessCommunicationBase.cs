using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

public abstract class InterprocessCommunicationBase : IInterprocessCommunication
{
    public event Logging? onLog;
    public event WaitingForClient? onWaitingForClient;
    public event ConnectedToClient? onConnectedToClient;
    public event Disconnected? onDisconnected;
    public event ReceiveMessage? onReceiveMessage;
    public event FailSendMessage? onFailToSendMessage;

    protected void OnLog(IInterprocessCommunication sender, string msg) {
        onLog?.Invoke(sender, msg);
    }
    protected void OnWaitingForClient(IInterprocessCommunication sender) {
        onWaitingForClient?.Invoke(sender);
    }
    protected void OnConnectedToClient(IInterprocessCommunication sender) {
        onConnectedToClient?.Invoke(sender);
    }
    protected void OnDisconnected(IInterprocessCommunication sender, Exception? e) {
        onDisconnected?.Invoke(sender, e);
    }
    protected void OnReceiveMessage(IInterprocessCommunication sender, byte[] content) {
        onReceiveMessage?.Invoke(sender, content);
    }
    protected void OnFailToSendMessage(IInterprocessCommunication sender, Exception? e) {
        onFailToSendMessage?.Invoke(sender, e);
    }



    public virtual string pipeName { get; protected set; }
    public virtual PipeStream pipeStream { get; protected set; }

    public async virtual Task tryCatchConnectionExceptions(Func<Task> func) {
        try {
            await func.Invoke();
        }
        catch (IOException e) {
            if (e.Message.ToLower().Contains("pipe is broken")) {
                connectionErrorHandling(e);
                return;
            }
            throw;
        }
    }

    public async virtual Task connectionErrorHandling(Exception e) {
        OnDisconnected(this, e);
        Thread.Sleep(50);
        connect();
    }


    public abstract Task connect();
    public abstract Task write(byte[] bytes);

    public void dispose()  {
        pipeStream.Close();
        pipeStream.Dispose();
    }


    public void applyDefaultLoggingEvent() {
        onWaitingForClient += (_) => onLog?.Invoke(this, "waiting for client...");
        onConnectedToClient += (_) => this.onLog?.Invoke(this, "connected...");
        onDisconnected += (_, e) => this.onLog?.Invoke(this, "disconnected...");
        onReceiveMessage += (_b, content) => this.onLog?.Invoke(this, $"Received {content.Length} bytes message");
        onFailToSendMessage += (_, e) => this.onLog?.Invoke(this, "fail sending msg");
    }
}