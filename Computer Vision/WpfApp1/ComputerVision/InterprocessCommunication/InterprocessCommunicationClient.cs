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
        dispose();
        _clientStream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        await _clientStream.ConnectAsync();
        _clientStream.ReadMode = PipeTransmissionMode.Message;
    }

    public override  async Task write(byte[] bytes) {
        await _clientStream.WriteAsync(bytes);
    }
}
