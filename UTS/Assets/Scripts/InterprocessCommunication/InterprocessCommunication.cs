using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


#nullable enable
public class InterprocessCommunication
{

    public event Logging onLog;
    public event WaitingForClient onWaitingForClient;
    public event ConnectedToClient onConnectedToClient;
    public event Disconnected onDisconnected;
    public event ReceiveMessage onReceiveMessage;
    public event FailSendMessage onFailToSendMessage = (_, e) => throw new Exception("fail to send message", e);

    public event QueueOverflow<LimitedQueue<byte[]>> onQueueOverflow {
        add => writeQueueBuffer.onQueueOverflow += value;
        remove => writeQueueBuffer.onQueueOverflow -= value;
    }


    private string pipeName;
    PipeStream namedPipeStream;
    private bool isServer;
    public NamedPipeServerStream? getServerNamedPipeline => namedPipeStream as NamedPipeServerStream;
    public NamedPipeClientStream? getClientNamedPipeline => namedPipeStream as NamedPipeClientStream;

    private CancellationTokenSource listeningCancellationTokenSource = new();

    private LimitedQueue<byte[]> writeQueueBuffer;


    public InterprocessCommunication(string pipeName, bool isServer, int writeQueueBufferLimit = Int16.MaxValue) {
        this.isServer = isServer;
        this.pipeName = pipeName;
        writeQueueBuffer = new LimitedQueue<byte[]>(writeQueueBufferLimit);
        writeQueueBuffer.onQueueOverflow += (_) => onLog?.Invoke(this, "Writing queue overflow");

        onDisconnected = (_, e) => onLog?.Invoke(this, "Disconnected...");
        onWaitingForClient = (_) => onLog?.Invoke(this, "Waiting for connection...");
        onConnectedToClient = (_) => onLog?.Invoke(this, "Connected...");
    }

    # region CONNECTION_INITIALIZATION

    /**
     * Exactly the same as reconnect(), just for readability.
     */
    public async Task initialize(int reconnectTimeout = 5000) {
        await reconnect(reconnectTimeout);
    }

    public async Task reconnect(int reconnectTimeout = 5000) {
        if (isServer)
            initializeAsServer();
        else await initializeAsClient(true, reconnectTimeout);
    }

    private void initializeAsServer() {
        reinitializeAll();
        namedPipeStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut);
    }

    private async Task initializeAsClient(bool reconnect = false, int reconnectTimeout = 5000) {
        reinitializeAll();

        onLog?.Invoke(this, "Connecting...");
        namedPipeStream = new NamedPipeClientStream(pipeName);
        if (reconnect)
            await getClientNamedPipeline?.ConnectAsync(reconnectTimeout);
        onLog?.Invoke(this, "Connected to the server");
    }

    private void reinitializeAll() {
        tryDisconnect();
        getServerNamedPipeline?.Close();
        getServerNamedPipeline?.Dispose();

        getClientNamedPipeline?.Close();
        getClientNamedPipeline?.Dispose();
    }

    #endregion


    #region LISTENING;

    /**
     * WARNING: This function is blocking
     */
    public async Task startConnectAndListening() {
        if (isServer)
            await startListeningAsServer();
        else await startListeningAsClient();
    }

    private async Task startListeningAsServer() {
        await stopListeningAndDisconnect();
        listeningCancellationTokenSource = new CancellationTokenSource();

        while (!listeningCancellationTokenSource.IsCancellationRequested) {
            await handleConnectionErrors(async () => {
                if (!await IsConnected())
                    await reconnect();

                onWaitingForClient?.Invoke(this);
                await getServerNamedPipeline!.WaitForConnectionAsync();
                onConnectedToClient?.Invoke(this);
                await startListening(getServerNamedPipeline!);
            });
        }
    }

    private async Task startListeningAsClient() {
        var clientNamedPipe = this.getClientNamedPipeline;
        if (clientNamedPipe == null)
            throw new InvalidOperationException("NamedPipeline was not initialized as server");

        while (await IsConnected()) {
            await handleConnectionErrors(async () => {
                if (!await IsConnected())
                    await reconnect();
                startListening(clientNamedPipe);
            });
        }
    }

    private async Task startListening(PipeStream serverNamedPipe) {
        var reader = new BinaryReader(serverNamedPipe);
        while (await IsConnected()) {
            var sizeRaw = await blockingReadExactly(4);
            var size = BitConverter.ToInt32(sizeRaw);
            if (handleSpecialCode(size))
                continue;
            var data = await blockingReadExactly(size);
            onReceiveMessage?.Invoke(this, data);
        }
    }


    private async Task<byte[]> blockingReadExactly(int bytesCount) {
        List<byte[]> resultingArrays = new();
        var cancelToken = new CancellationTokenSource();

        byte[] tempBytes = new byte[bytesCount];
        int offset = 0;
        while (bytesCount > 0 && await IsConnected()) {
            var readAsync = namedPipeStream.ReadAsync(tempBytes, offset, bytesCount, cancelToken.Token);
            var result = await Task.WhenAny(readAsync, Task.Delay(30));
            if (result != readAsync)
                continue;

            var numOfBytes = await readAsync;
            offset += numOfBytes;
            bytesCount -= numOfBytes;
        }
        cancelToken.Cancel();

        return tempBytes;
        if (!await IsConnected())
            throw new OperationCanceledException();
        if (resultingArrays.Count == 1)
            return resultingArrays[0];
        return ConcatArrays(resultingArrays);
    }

    public static T[] ConcatArrays<T>(List<T[]> p) {
        var position = 0;
        var outputArray = new T[p.Sum(a => a.Length)];
        foreach (var curr in p) {
            Array.Copy(curr, 0, outputArray, position, curr.Length);
            position += curr.Length;
        }

        return outputArray;
    }

    #endregion


    #region WRITING

    public async Task writeBytes(byte[] content, bool immediateFlush = true) {
        addToBuffer(content);
        if (immediateFlush)
            await flushBuffer();
    }

    private void addToBuffer(byte[] bytes) {
        writeQueueBuffer.Enqueue(bytes);
        onLog?.Invoke(this,
            $"Putting {bytes.Length} bytes to WriteQueue... Now the queue has {writeQueueBuffer.Count} items remaining...");
    }

    public async Task flushBuffer(int maxRetry = 10) {
        bool success = false;
        for (int i = 0; i < maxRetry && !success; i++) {
            await handleConnectionErrors(async () => {
                while (writeQueueBuffer.Count > 0) {
                    if (!await IsConnected()) {
                        await initializeAsClient(true);
                        break;
                    }

                    onLog?.Invoke(this, "Writing to namedpipeline");
                    var content = writeQueueBuffer.Peek();
                    var streamWriter = new BinaryWriter(namedPipeStream);

                    await Task.Run(() => {
                        streamWriter.Write(content.Length);
                        streamWriter.Write(content);
                        streamWriter.Flush();
                        writeQueueBuffer.Dequeue();
                    });
                    success = true;
                    break;
                }
            });
        }

        if (!success)
            onFailToSendMessage?.Invoke(this, null);
    }

    #endregion

    private async Task handleConnectionErrors(Func<Task> func) {
        try {
            await func.Invoke();
        }
        catch (OperationCanceledException e) {
            onDisconnected?.Invoke(this, e);
        }
        catch (IOException e) {
            onDisconnected?.Invoke(this, e);
        }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            if (e.Message.Contains("Pipe is broken")) {
                onDisconnected?.Invoke(this, e);
                await reconnect();
                return;
            }

            if (e.Message.Contains("closed pipe")) {
                onDisconnected?.Invoke(this, e);
                await reconnect();
                return;
            }

            throw;
        }
    }


    private bool handleSpecialCode(int size) {
        if (size == Int32.MaxValue)
            return true;
        return false;
    }
    public async Task<bool> IsConnected() {
        var cancellationToken = new CancellationTokenSource();
        try {
            // based on https://stackoverflow.com/a/53760006/7069108, we should write something to update the IsConnected
            // so let's use Int32.MaxValue in-lieu-of the data size to do "ping"/healthcheck
            var task = namedPipeStream.WriteAsync(BitConverter.GetBytes(Int32.MaxValue), 0, 4, cancellationToken.Token);
            await Task.WhenAny(task, Task.Delay(50));
            return namedPipeStream.IsConnected && !listeningCancellationTokenSource.IsCancellationRequested;
        }
        catch (IOException e) {
            cancellationToken.Cancel();
            return false;
        }
        catch (OperationCanceledException e) {
            cancellationToken.Cancel();
            return false;
        }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException) {
            if (e.Message.Contains("Pipe is broken") || e.Message.Contains("closed pipe")) {
                cancellationToken.Cancel();
                return false;
            }
            throw;
        }
    }

    public async Task stopListeningAndDisconnect() {
        listeningCancellationTokenSource?.Cancel();
        tryDisconnect();
        await namedPipeStream.DisposeAsync();
    }

    private void tryDisconnect() {
        try {
            getServerNamedPipeline?.Disconnect();
        }catch (InvalidOperationException _) { }
    }

}

public delegate void WaitingForClient(InterprocessCommunication sender);
public delegate void ConnectedToClient(InterprocessCommunication sender);
public delegate void Disconnected(InterprocessCommunication sender, Exception? e);
public delegate void ReceiveMessage(InterprocessCommunication sender, byte[] bytes);
public delegate void Logging(InterprocessCommunication sender, string msg);
public delegate void FailSendMessage(InterprocessCommunication sender, Exception? exception);
public delegate void QueueOverflow<T>(T sender);


public class LimitedQueue<T> : Queue<T> {
// Credits: Espo https://stackoverflow.com/a/1305/7069108
    public int Limit { get; set; }
    public event QueueOverflow<LimitedQueue<T>> onQueueOverflow;

    public LimitedQueue(int limit) : base(limit)
    {
        Limit = limit;
    }

    public new void Enqueue(T item) {
        while (Count >= Limit) {
            Dequeue();
            onQueueOverflow?.Invoke(this);
        }
        base.Enqueue(item);
    }
}
