using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    }

    public async Task startListeningAsync() {
        await _interprocess.connect();

        await Task.Run(_interprocess.listeningLoop);
    }
}