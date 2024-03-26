using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

public interface IInterprocessCommunication
{
    public event Logging onLog;
    public event WaitingForClient onWaitingForClient;
    public event Connected onConnected;
    public event Disconnected onDisconnected;
    public event ReceiveMessage onReceiveMessage;
    public event FailSendMessage onFailToSendMessage;

    public string pipeName { get; }
    public PipeStream pipeStream { get; }




    public Task connect();

    public Task<bool> write(byte[] bytes, bool autoReconnect=true);

    public void dispose();


    public static async Task<byte[]> ReadMessage(PipeStream pipe)
    {
        byte[] buffer = new byte[1024];
        using (var ms = new MemoryStream()) {
            do {
                var readBytes = await pipe.ReadAsync(buffer, 0, buffer.Length);
                ms.Write(buffer, 0, readBytes);
            }
            while (!pipe.IsMessageComplete);

            return ms.ToArray();
        }
    }
}