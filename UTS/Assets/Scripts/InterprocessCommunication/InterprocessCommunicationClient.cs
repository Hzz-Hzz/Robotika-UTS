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



    /**
     * This is NOT thread safe
     */
    public override async Task connect() {
        dispose();
        _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await _clientStream.ConnectAsync(200);
        OnConnected(this);
        _clientStream.ReadMode = PipeTransmissionMode.Message;
    }


    public override async Task startListeningLoop() {
        try {
            _listeningForIncomingMessageCancellationToken?.Cancel();
            _listeningForIncomingMessageCancellationToken = new CancellationTokenSource();
            await listeningLoop(_listeningForIncomingMessageCancellationToken.Token);
        }
        finally {
            _listeningForIncomingMessageCancellationToken?.Cancel();
        }
    }

    protected override async Task listeningLoop(CancellationToken? cancellationToken=null) {
        cancellationToken ??= new CancellationToken(false);

        var previousIsConnected = new Reference<bool>(false);

        await tryCatchConnectionExceptions(async () => {
            while (!cancellationToken.Value.IsCancellationRequested) {
                if (previousIsConnected.item && !pipeStream.IsConnected)
                    OnDisconnected(this, null);
                if (!pipeStream.IsConnected)
                    await tryCatchConnectionExceptions(connect, async (e) => {
                        previousIsConnected.item = false;
                    });
                else {
                    previousIsConnected.item = true;
                }
                await base.listeningLoop(cancellationToken);
            }
        }, async (e) => {
            previousIsConnected.item = false;
        });
    }
}


class Reference<T>
{
    public T item;
    public Reference(T item) {
        this.item = item;
    }
}