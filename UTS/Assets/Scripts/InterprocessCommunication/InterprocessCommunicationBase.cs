using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

public abstract class InterprocessCommunicationBase : IInterprocessCommunication
{
    public event Logging? onLog;
    public event WaitingForClient? onWaitingForClient;
    public event EstablishingNetwork? onEstablishingNetwork;
    public event Connected? onConnected;
    /**
     * This event will only be called called if listening loop is running
     */
    public event Disconnected? onDisconnected;
    public event ReceiveMessage? onReceiveMessage;
    public event FailSendMessage? onFailToSendMessage;

    protected void OnLog(IInterprocessCommunication sender, string msg) {
        onLog?.Invoke(sender, msg);
    }
    protected void OnWaitingForClient(IInterprocessCommunication sender) {
        onWaitingForClient?.Invoke(sender);
    }
    protected void OnEstablishingNetwork(IInterprocessCommunication sender) {
        onEstablishingNetwork?.Invoke(sender);
    }
    protected void OnConnected(IInterprocessCommunication sender) {
        onConnected?.Invoke(sender);
    }
    protected void OnDisconnected(IInterprocessCommunication sender, Exception? e) {
        onDisconnected?.Invoke(sender, e);
    }
    protected void OnReceiveMessage(IInterprocessCommunication sender, byte[] content) {
        onReceiveMessage?.Invoke(sender, content);
    }
    protected void OnFailToSendMessage(IInterprocessCommunication sender, Exception? e, byte[] message) {
        onFailToSendMessage?.Invoke(sender, e, message);
    }



    public virtual string pipeName { get; protected set; }
    public virtual PipeStream pipeStream { get; protected set; }

    public async virtual Task tryCatchConnectionExceptions(Func<Task> func, Func<Exception, Task> handler=null) {
        handler ??= connectionErrorHandling;
        try {
            await func.Invoke();
        }
        catch (IOException e) {
            if (e.Message.ToLower().Contains("pipe is broken")) {
                await handler(e);
                return;
            }
            throw;
        }catch (TimeoutException e) {
            await handler(e);
        }
    }

    public async virtual Task connectionErrorHandling(Exception? e) {
        OnDisconnected(this, e);
        Thread.Sleep(100);
        connect();
    }


    public virtual Task<bool> tryConnect() {
        var task = new TaskCompletionSource<bool>();

        tryCatchConnectionExceptions(async () => {
            await connect();
            task.SetResult(true);
        }, async (e) => task.SetResult(false));
        return task.Task;
    }

    public abstract Task connect();
    public abstract Task<bool> write(byte[] bytes, bool autoReconnect=true);

    public void dispose()  {
        pipeStream.Close();
        pipeStream.Dispose();
    }


    public void applyDefaultLoggingEvent() {
        onWaitingForClient += (_) => onLog?.Invoke(this, "waiting for client...");
        onConnected += (_) => this.onLog?.Invoke(this, "connected...");
        onDisconnected += (_, e) => this.onLog?.Invoke(this, "disconnected...");
        onReceiveMessage += (_b, content) => this.onLog?.Invoke(this, $"Received {content.Length} bytes message");
        onFailToSendMessage += (_, e, msg) => this.onLog?.Invoke(this, "fail sending msg");
    }
}