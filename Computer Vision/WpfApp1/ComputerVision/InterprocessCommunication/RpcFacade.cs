using System;
using System.Collections.Generic;
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
        interprocessCommunication.registerMethod(QueryCommandsEnum.GET_ROAD_EDGE_DISTANCES, getClosestSurrounding);
    }


    /**
     * return list of recommendations, sorted by most-recommended (index 0) to the least recommended
     * but still recommended (last index).
     *
     * Each item will be represented as a tuple of (distance, angle in rads).
     * Angle in rads will be 0 if you should go forward,
     * positive if you should go right,
     * and negative if you should go left.
     */
    public List<Tuple<float, double>>? getAngleRecommendation(byte[] bytes) {
        if (bytes == null)
            return new List<Tuple<float, double>>();
        return viewModelVisualServer.processImage(bytes);
    }

    public Tuple<Vector2?, Vector2?> getClosestSurrounding() {
        if (viewModelVisualServer.prevSurroundingMap == null)
            return new Tuple<Vector2?, Vector2?>(Vector2.Zero, Vector2.Zero);
        return viewModelVisualServer.prevSurroundingMap.getHorizontallyClosestPointOnLeftAndOnRight();
    }


    #region otherDeclarations


    public RpcFacade(ViewModelVisualServer viewModelVisualServer) {
        this.viewModelVisualServer = viewModelVisualServer;

        var server = new InterprocessCommunicationServer("NuelValenRobotik");
        server.onLog += (sender, msg) => Console.WriteLine(msg);
        server.onConnected += (e) => viewModelVisualServer.setStatusToClientConnected();
        server.onWaitingForClient += (_) => viewModelVisualServer.setStatusToWaitingForClient();
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