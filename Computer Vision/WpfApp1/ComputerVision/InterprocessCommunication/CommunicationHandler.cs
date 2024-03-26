using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MathNet.Numerics.Random;
using WpfApp1;


public class CommunicationHandler
{
    private ViewModelVisualServer _viewModelVisualServer;
    private InterprocessCommunicationWithTypes _interprocessCommunication;

    public CommunicationHandler(ViewModelVisualServer viewModelVisualServer) {
        _viewModelVisualServer = viewModelVisualServer;
    }

    public void startListeningAsync() {
        var server = new InterprocessCommunicationServer("NuelValenRobotik");
        server.onLog += (sender, msg) => Console.WriteLine(msg);
        server.applyDefaultLoggingEvent();


        _interprocessCommunication = new InterprocessCommunicationWithTypes(server);
        _interprocessCommunication.onReceiveMessage += messageReceived;
        Task.Run(_interprocessCommunication.startListeningAsync);
    }

    private void messageReceived(InterprocessCommunicationWithTypes sender) {
        var msg = sender.readMessage<Tuple<int, string>>();
        Console.WriteLine($"{msg.Item1} -- {msg.Item2}");
    }
}