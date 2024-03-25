using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



#nullable enable
public class InterprocessCommunicationClient: InterprocessCommunicationBase
{
    private NamedPipeClientStream _clientStream;

    public InterprocessCommunicationClient(string pipeName) {
        this.pipeName = pipeName;
        _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
    }

    public override  string pipeName { get; protected set;  }
    public override PipeStream pipeStream => _clientStream;



    public override async Task connect() {
        OnLog(this, "Reinitializing...");

        dispose();
        _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        _clientStream.Connect(500);
        _clientStream.ReadMode = PipeTransmissionMode.Message;
    }




    private bool reconnecting = false;
    /**
     * autoReconnect will try to connect, but only ONCE per method call. NO guarantee that your msg will be sent
     * You should either check the return type or watching for OnFailToSendMessage event to watch for failing messages.
     *
     * return: boolean true if success, or false if fail.
     */
    public override async Task<bool> write(byte[] bytes, bool autoReconnect=true) {
        var success = new Reference<bool>(false);
        await tryCatchConnectionExceptions(async () => {
                if (!_clientStream.IsConnected && autoReconnect && !reconnecting) {
                    this.reconnecting = true;
                    await connect();
                    this.reconnecting = false;
                }

                await _clientStream.WriteAsync(bytes);
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

    public async Task duringWriteErrorHandling(Exception? e) {
        var success = new Reference<bool>(false);
        while (!success.item) {
            await tryCatchConnectionExceptions(async () => {
                await base.connectionErrorHandling(e);
                success.item = true;
            }, async (_) => { });
            await Task.Delay(50);
        }
    }
}


class Reference<T>
{
    public T item;
    public Reference(T item) {
        this.item = item;
    }
}