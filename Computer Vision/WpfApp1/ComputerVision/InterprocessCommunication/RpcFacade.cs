using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1;


// facade to do Remote Procedural Call
public class RpcFacade
{
    private InterprocessCommunicationRpc<QueryCommandsEnum> interprocessCommunication;
    private ViewModelVisualServer viewModelVisualServer;

    private void registerMethods() {
        interprocessCommunication.registerMethod(QueryCommandsEnum.GET_ANGLE_RECOMMENDATION, getAngleRecommendation);
    }

    public void getAngleRecommendation(byte[] bytes) {
        viewModelVisualServer.processImage(bytes);
    }


    #region otherDeclarations


    public RpcFacade(ViewModelVisualServer viewModelVisualServer) {
        this.viewModelVisualServer = viewModelVisualServer;

        var server = new InterprocessCommunicationServer("NuelValenRobotik");
        server.onLog += (sender, msg) => Console.WriteLine(msg);
        server.applyDefaultLoggingEvent();

        var interpWithTypes = new InterprocessCommunicationWithTypes(server);
        interprocessCommunication = new InterprocessCommunicationRpc<QueryCommandsEnum>(interpWithTypes);
        interprocessCommunication.doNotSendErrorOnAlreadyClosedPipe = true;
        registerMethods();
    }

    public async Task startListening() {
        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException(
            (e) => Console.Error.WriteLine(e.ToString()));
    }


    #endregion
}