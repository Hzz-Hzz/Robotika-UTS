using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;



#nullable enable
public class InterprocessCommunicationServer: InterprocessCommunicationBase
{
    private NamedPipeServerStream _serverStream;
    public override PipeStream pipeStream => _serverStream;

    public InterprocessCommunicationServer(string pipeName) {
        this.pipeName = pipeName;
        initializePipeStream();
    }

    private void initializePipeStream() {
        _serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message,
            PipeOptions.Asynchronous);
    }



    public override async Task connect() {
        tryDisconnect();
        dispose();
        initializePipeStream();
    }

    public override async Task startListeningLoop() {
        try {
            _listeningForIncomingMessageCancellationToken?.Cancel();
            _listeningForIncomingMessageCancellationToken = new CancellationTokenSource();
            await serverStartListeningLoop(_listeningForIncomingMessageCancellationToken.Token);
        }
        finally {
            _listeningForIncomingMessageCancellationToken?.Cancel();
        }
    }
    private async Task serverStartListeningLoop(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            await tryCatchConnectionExceptions(async () => {
                if (!_serverStream.IsConnected)
                    await connect();

                OnWaitingForClient(this);
                _serverStream.WaitForConnection();
                prevStateConnected = true;
                OnConnected(this);

                await listeningLoop(cancellationToken);
            });
        }
    }




    private bool prevStateConnected = false;
    public void tryDisconnect() {
        try {
            if (prevStateConnected && !_serverStream.IsConnected)
                OnDisconnected(this, null);
            if (_serverStream.IsConnected) {
                _serverStream.Disconnect();
            }

            prevStateConnected = _serverStream.IsConnected;
        }
        catch (OperationCanceledException _) { }
    }
    public void dispose() {
        tryDisconnect();
        IInterprocessCommunication parentMethod = this;
        parentMethod.dispose();
    }
}
