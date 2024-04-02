global using AngleRecommendationsReturnType = System.Collections.Generic.List<System.Tuple<float, double, System.Numerics.Vector2>>;
global using Obstacles = System.Collections.Generic.List<System.Tuple<System.Numerics.Vector2, System.Numerics.Vector2>>;

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
        interprocessCommunication.registerMethod(QueryCommandsEnum.GET_ROAD_EDGE_DISTANCES_CHOOSE_VERTICALLY_CLOSEST_LEFT_RIGHT, getVerticallyClosestSurrounding);
        interprocessCommunication.registerMethod(QueryCommandsEnum.IS_OFF_ROAD, isOffRoad);
        interprocessCommunication.registerMethod(QueryCommandsEnum.GET_ROAD_EDGE_LIST, getRoadEdgeVectors);
    }


    /**
     * See docs at  SurroundingMap.cs at calculateRecommendedIntersectionPoints()
     */
    public AngleRecommendationsReturnType? getAngleRecommendation(byte[] bytes, Obstacles obstacles) {
        if (bytes == null)
            return new AngleRecommendationsReturnType();
        var ret = viewModelVisualServer.processImage(bytes, obstacles);
        return ret;
    }

    public Tuple<Vector2?, Vector2?> getClosestSurrounding() {
        if (viewModelVisualServer.prevSurroundingMap == null)
            return new Tuple<Vector2?, Vector2?>(Vector2.Zero, Vector2.Zero);
        return viewModelVisualServer.prevSurroundingMap.getHorizontallyClosestPointOnLeftAndOnRight();
    }
    public Tuple<Vector2?, Vector2?> getVerticallyClosestSurrounding() {
        if (viewModelVisualServer.prevSurroundingMap == null)
            return new Tuple<Vector2?, Vector2?>(Vector2.Zero, Vector2.Zero);
        var result = viewModelVisualServer.prevSurroundingMap.getVerticallyClosestPointOnLeftAndOnRight();
        return new Tuple<Vector2?, Vector2?>(result.Item1?.vector2, result.Item2?.vector2);
    }
    public bool isOffRoad() {
        return viewModelVisualServer.prevSurroundingMap?.offroad ?? false;
    }

    public Tuple<Vector2?[,], Vector2?[,]> getRoadEdgeVectors() {
        if (viewModelVisualServer.prevSurroundingMap == null)
            return new Tuple<Vector2?[,], Vector2?[,]>(new Vector2?[0, 0], new Vector2?[0, 0]);
        return viewModelVisualServer.prevSurroundingMap.getListOfRoadEdgeAsVectors();
    }


    #region otherDeclarations


    public RpcFacade(ViewModelVisualServer viewModelVisualServer) {
        this.viewModelVisualServer = viewModelVisualServer;

        var server = new InterprocessCommunicationServer("NuelValenRobotik");
        server.onLog += (sender, msg) => Console.WriteLine(msg);
        server.onConnected += (e) => viewModelVisualServer.setStatusToClientConnected();
        server.onWaitingForClient += (_) => viewModelVisualServer.setStatusToWaitingForClient();
        server.applyDefaultLoggingEvent(receiveMessage: false);

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