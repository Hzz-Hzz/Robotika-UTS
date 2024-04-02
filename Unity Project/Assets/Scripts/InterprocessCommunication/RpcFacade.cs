using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DefaultNamespace;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;


using AngleRecommendation = System.Tuple<float, double, UnityEngine.Vector2>;
using Obstacles = System.Collections.Generic.List<System.Tuple<System.Numerics.Vector2, System.Numerics.Vector2>>;


// facade to do Remote Procedural Call
public class RpcFacade
{
    private static bool alreadyInitialized = false;  // for current implementation we will force people to use treat this class as singleton


    public InterprocessCommunicationRpc<QueryCommandsEnum> interprocessCommunication;
    private System.Diagnostics.Stopwatch _stopwatch = new ();


    private void registerMethods() {

    }



    [CanBeNull] private List<AngleRecommendation> _getAngleRecommendationCachedResult;
    [ItemCanBeNull]
    public async Task<List<AngleRecommendation>> getAngleRecommendation(byte[] bytes, Obstacles obstacles) {
        if (_stopwatch.ElapsedMilliseconds < 1000 / 20 && _getAngleRecommendationCachedResult != null) // 20FPS cap
            return _getAngleRecommendationCachedResult;
        _stopwatch.Restart();

        var result = await interprocessCommunication.call<List<Tuple<float, double, System.Numerics.Vector2>>?>(
            QueryCommandsEnum.GET_ANGLE_RECOMMENDATION, bytes, obstacles);
        _getAngleRecommendationCachedResult = result.Select(e=>
            new AngleRecommendation(e.Item1, e.Item2, new Vector2(e.Item3.X, e.Item3.Y))).ToList();
        return _getAngleRecommendationCachedResult;
    }

    public Task<Tuple<Vector2?, Vector2?>> getClosestSurrounding() {
        return interprocessCommunication.call<Tuple<Vector2?, Vector2?>>(
            QueryCommandsEnum.GET_ROAD_EDGE_DISTANCES);
    }
    public Task<Tuple<Vector2?, Vector2?>> getVerticallyClosestSurrounding() {
        return interprocessCommunication.call<Tuple<Vector2?, Vector2?>>(
            QueryCommandsEnum.GET_ROAD_EDGE_DISTANCES_CHOOSE_VERTICALLY_CLOSEST_LEFT_RIGHT);
    }
    public Task<bool> isOffRoad() {
        return interprocessCommunication.call<bool>(
            QueryCommandsEnum.IS_OFF_ROAD);
    }
    public Task<Tuple<Vector2?[,], Vector2?[,]>> getRoadEdgeVectors() {
        return interprocessCommunication.call<Tuple<Vector2?[,], Vector2?[,]>>(
            QueryCommandsEnum.GET_ROAD_EDGE_LIST);
    }

    public readonly Func<bool> isConnected;


    #region otherDeclarations

    public RpcFacade() {
        if (alreadyInitialized)
            throw new Exception("RpcFacade already Initialized somewhere else");
        alreadyInitialized = true;

        var server = new InterprocessCommunicationClient("NuelValenRobotik");
        server.onLog += (sender, msg) => CustomLogger.Log(msg);
        server.onDisconnected += (sender, exception) => showConnectionError();
        server.applyDefaultLoggingEvent(receiveMessage: false);

        var interpWithTypes = new InterprocessCommunicationWithTypes(server);
        interprocessCommunication = new InterprocessCommunicationRpc<QueryCommandsEnum>(interpWithTypes);
        interprocessCommunication.doNotSendErrorOnAlreadyClosedPipe = true;
        registerMethods();
        isConnected = () => server.isConnected;
    }

    public static void showConnectionError() {
        EditorUtility.DisplayDialog("Cant connect to visual server",
            "Mohon jangan lupa jalanin visual servernya Kak, untuk memproses visual dari kamera mobil " +
            "(sudah izin ke Pak Wisnu terkait keterbatasan ini kak)", "ok");
    }

    public void startListening() {
        _stopwatch.Start();
        CustomLogger.Log("Call startListening()");
        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException(
            (e) => CustomLogger.Log((e?.ToString())));
    }

    public void stopListening() {
        Task.Run(interprocessCommunication.stopListeningAndDisconnect);
    }


    #endregion
}