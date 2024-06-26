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

    /**
     * Due to technical issue, for InterprocessCommunicationClient, this event may be fired with byte[0]
     * in the middle of being disconnected/reconnected (but isConnected will show the wrong info at that moment)
     * So please handle it yourself.
     */
    public event ReceiveMessage? onReceiveMessage;
    public event StopListening? onStopListening;
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



    public virtual bool isConnected => pipeStream.IsConnected;
    public virtual string pipeName { get; protected set; }
    public virtual PipeStream pipeStream { get; protected set; }



    protected CancellationTokenSource? _listeningForIncomingMessageCancellationToken;
    public abstract Task startListeningLoop();

    /**
     * Please call this method inside tryCatchConnectionExceptions
     */
    protected virtual async Task listeningLoop(CancellationToken? cancellationToken=null) {
        cancellationToken ??= new CancellationToken(false);
        await Task.Delay(50);
        while (pipeStream.IsConnected && !cancellationToken.Value.IsCancellationRequested) {
            var readSomething = await IInterprocessCommunication.ReadMessage(pipeStream, cancellationToken.Value);
            if (readSomething.Length == 0 && !pipeStream.IsConnected) {
                OnDisconnected(this, null);
                return;
            }
            OnReceiveMessage(this, readSomething);
        }
    }

    /**
     * This function may not immediately stop the listening process.
     */
    public virtual void stopListening() {
        _listeningForIncomingMessageCancellationToken?.Cancel();
    }

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
        } catch (InvalidOperationException e) {
            if (e.Message.ToLower().Contains("pipe hasn't been connected yet")) {
                await handler(e);
                return;
            }
            throw;
        }
        catch (TimeoutException e) {
            await handler(e);
        }
    }

    protected async virtual Task connectionErrorHandling(Exception? e) {
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


    private bool reconnecting = false;  // lock to prevent calling connect() asynchronously
    /**
     * autoReconnect will try to connect, but only ONCE per method call. NO guarantee that your msg will be sent
     * You should either check the return type or watching for OnFailToSendMessage event to watch for failing messages.
     *
     * return: boolean true if success, or false if fail.
     */
    public virtual async Task<bool> write(byte[] bytes, bool autoReconnect=true) {
        var success = new Reference<bool>(false);
        await tryCatchConnectionExceptions(async () => {
                if (!pipeStream.IsConnected && autoReconnect && !reconnecting) {
                    this.reconnecting = true;
                    await connect();
                    this.reconnecting = false;
                }

                await pipeStream.WriteAsync(bytes);
                success.item = true;
            },
            (e) => {
                success.item = false;
                OnFailToSendMessage(this, e, bytes);
                this.reconnecting = false;
                return Task.CompletedTask;
            });
        return success.item;
    }

    public void dispose()  {
        pipeStream.Close();
        pipeStream.Dispose();
    }


    public void applyDefaultLoggingEvent(bool receiveMessage=true) {
        onWaitingForClient += (_) => onLog?.Invoke(this, "waiting for client...");
        onConnected += (_) => this.onLog?.Invoke(this, "connected...");
        onDisconnected += (_, e) => this.onLog?.Invoke(this, "disconnected...");
        if (receiveMessage)
            onReceiveMessage += (_b, content) => this.onLog?.Invoke(this, $"Received {content.Length} bytes message");
        onFailToSendMessage += (_, e, msg) => this.onLog?.Invoke(this, "fail sending msg");
        onStopListening += (_) => this.onLog?.Invoke(this, "Stop listening...");
    }
}