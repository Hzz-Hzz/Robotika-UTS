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
        _serverStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
    }



    public override async Task connect() {
        tryDisconnect();
        dispose();
        initializePipeStream();
    }

    public override  Task write(byte[] bytes) {
        throw new NotImplementedException();
    }

    public async Task listeningLoop() {
        while (true) {
            await tryCatchConnectionExceptions(async () => {
                if (!_serverStream.IsConnected)
                    await connect();

                OnWaitingForClient(this);
                _serverStream.WaitForConnection();
                OnConnectedToClient(this);
                prevStateConnected = true;

                while (_serverStream.IsConnected) {
                    var readSomething = IInterprocessCommunication.ReadMessage(_serverStream);
                    foreach (var text in readSomething) {
                        Console.Write(text);
                        Console.Write(' ');
                    }

                    Console.WriteLine();
                }
            });
        }
    }






    private bool prevStateConnected = true;
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
