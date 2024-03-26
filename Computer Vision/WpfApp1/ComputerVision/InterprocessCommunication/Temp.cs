using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MathNet.Numerics.Random;
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

        new Thread(() => simulateSendingMessage()).Start();
        new Thread(stop).Start();
    }

    public async Task startListeningAsync() {
        await _interprocess.connect();

        await Task.Run(_interprocess.startListeningLoop);
    }

    private async Task simulateSendingMessage() {  // TODO remove
        while (true) {
            if (!_interprocess.isConnected) {
                Console.WriteLine("Not connected, so doesn't send any message");
                Thread.Sleep(1500);
                continue;
            }
            Console.WriteLine("sending msg");
            await _interprocess.write(new byte[]{5, 6, 7, 8, 9, 10});
            Console.WriteLine("msg sent");
            Thread.Sleep(4000);
        }
    }

    private void stop() {
        Thread.Sleep(12000 + (int)Random.Shared.NextInt64(2000));
        Console.WriteLine("stop listening ===========");
        _interprocess.stopListening();

    }
}