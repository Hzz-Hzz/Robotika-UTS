using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1;


public class Temp
{
    private ViewModelVisualServer _viewModelVisualServer;
    private InterprocessCommunicationServer _interprocess;



    public Temp(ViewModelVisualServer viewModelVisualServer) {
        _viewModelVisualServer = viewModelVisualServer;
        _interprocess = new("NuelValenRobotik");
        _interprocess.onLog += (_, msg) => Console.WriteLine(msg);
        _interprocess.onReceiveMessage += (_, msg) => Console.WriteLine($"Message received: {Encoding.UTF8.GetString(msg)}");
        _interprocess.applyDefaultLoggingEvent();

        new Thread(simulateSendingMessage).Start();
    }

    public async Task startListeningAsync() {
        await _interprocess.connect();

        await Task.Run(_interprocess.startListeningLoop);
    }

    private void simulateSendingMessage() {  // TODO remove
        while (true) {
            if (!_interprocess.isConnected) {
                Console.WriteLine("Not connected, so doesn't send any message");
                Thread.Sleep(1500);
                continue;
            }
            _interprocess.pipeStream.Write(new byte[]{5, 6, 7, 8, 9, 10});
            Thread.Sleep(4000);
        }
    }
}