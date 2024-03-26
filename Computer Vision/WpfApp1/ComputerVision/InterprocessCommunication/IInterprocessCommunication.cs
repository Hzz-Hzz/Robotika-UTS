using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

public interface IInterprocessCommunication
{
    public string pipeName { get; }
    public PipeStream pipeStream { get; }




    public Task connect();

    public Task<bool> write(byte[] bytes, bool autoReconnect=true);

    public void dispose();


    public static async Task<byte[]> ReadMessage(PipeStream pipe, CancellationToken cancellationToken)
    {  // credits https://stackoverflow.com/a/46797865/7069108
        byte[] buffer = new byte[1024];
        using (var ms = new MemoryStream()) {
            do {
                Console.WriteLine("Reading async...");
                var readBytes = await pipe.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                Console.WriteLine("DONE Reading async...");
                ms.Write(buffer, 0, readBytes);
            }
            while (!pipe.IsMessageComplete);

            return ms.ToArray();
        }
    }
}