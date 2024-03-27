using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;


using AngleRecRet = System.Collections.Generic.List<System.Tuple<float, double>>;


// facade to do Remote Procedural Call
public class RpcFacade
{
    private static bool alreadyInitialized = false;  // for current implementation we will force people to use treat this class as singleton


    public InterprocessCommunicationRpc<QueryCommandsEnum> interprocessCommunication;
    private System.Diagnostics.Stopwatch _stopwatch = new ();


    private void registerMethods() {

    }



    [CanBeNull] private AngleRecRet _getAngleRecommendationCachedResult;
    [ItemCanBeNull]
    public async Task<AngleRecRet> getAngleRecommendation(byte[] bytes) {
        if (_stopwatch.ElapsedMilliseconds < 1000 / 20 && _getAngleRecommendationCachedResult != null) // 20FPS cap
            return _getAngleRecommendationCachedResult;
        _stopwatch.Restart();

        _getAngleRecommendationCachedResult =
            await interprocessCommunication.call<AngleRecRet?>(
                QueryCommandsEnum.GET_ANGLE_RECOMMENDATION, bytes);
        return _getAngleRecommendationCachedResult;
    }

    public Task<Tuple<Vector2?, Vector2?>> getClosestSurrounding() {
        return interprocessCommunication.call<Tuple<Vector2?, Vector2?>>(
            QueryCommandsEnum.GET_ROAD_EDGE_DISTANCES);
    }




    #region otherDeclarations

    public RpcFacade() {
        if (alreadyInitialized)
            throw new Exception("RpcFacade already Initialized somewhere else");
        alreadyInitialized = true;

        var server = new InterprocessCommunicationClient("NuelValenRobotik");
        server.onLog += (sender, msg) => Debug.Log(msg);
        server.applyDefaultLoggingEvent();

        var interpWithTypes = new InterprocessCommunicationWithTypes(server);
        interprocessCommunication = new InterprocessCommunicationRpc<QueryCommandsEnum>(interpWithTypes);
        interprocessCommunication.doNotSendErrorOnAlreadyClosedPipe = true;
        registerMethods();
    }

    public void startListening() {
        _stopwatch.Start();
        Debug.Log("Call startListening()");
        interprocessCommunication.startListeningAsyncFireAndForgetButKeepException(
            (e) => Debug.LogError((e?.ToString())));
    }

    public void stopListening() {
        Task.Run(interprocessCommunication.stopListeningAndDisconnect);
    }


    #endregion
}