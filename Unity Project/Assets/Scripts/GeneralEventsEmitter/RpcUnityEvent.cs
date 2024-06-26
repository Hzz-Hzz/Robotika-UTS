using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventsEmitter.models;
using Sensor;
using UnityEngine;
using UnityEngine.Events;

using AngleRecommendation = System.Tuple<float, double, UnityEngine.Vector2>;

namespace InterprocessCommunication
{
    [System.Serializable]
    public class AngleRecommendationReceivedEvent : UnityEvent<AngleRecommendationReceivedEventArgs>
    {
    }

    public class RpcUnityEvent: MonoBehaviour
    {
        public AngleRecommendationReceivedEvent AngleRecommendationReceived;

        public CircularUltrasonicSensor circularUltrasonicSensor;
        public long imageVersion = 0;
        private byte[] imageData;

        private RpcFacade _rpcFacade;

        void Start() {
            if (AngleRecommendationReceived == null)
                AngleRecommendationReceived = new AngleRecommendationReceivedEvent();

            _rpcFacade = new RpcFacade();
            _rpcFacade.startListening();
            StartCoroutine(sendImageDataToServerAndGetFeedback());
            StartCoroutine(detectConnectionErrorCoroutine());
        }

        public void OnImageUpdated(ImageUpdatedEventArgs imageData) {
            imageVersion++;
            this.imageData = imageData.imageData;
        }

        public Dictionary<int, Direction> obstacleIdVerdicts = new();
        public Dictionary<int, Tuple<float, float, AngleRecommendationReceivedEventArgs>> debugPurposeOnly = new();

        IEnumerator sendImageDataToServerAndGetFeedback() {
            var currentImageVersion = -1L;

            while (true) {
                if (imageData == null)
                    yield return null;
                if (currentImageVersion == imageVersion)
                    yield return null;
                currentImageVersion = imageVersion;

                var obstacles = circularUltrasonicSensor.getObstacles();
                var angleRecommendationWithObstacleTask = _rpcFacade.getAngleRecommendation(imageData, obstacles);
                yield return angleRecommendationWithObstacleTask.toCoroutine();

                var closestRoadEdgeInformationTask = _rpcFacade.getClosestSurrounding();
                var verticallyClosestRoadEdgeInformationTask = _rpcFacade.getVerticallyClosestSurrounding();
                var isOffRoadTask = _rpcFacade.isOffRoad();
                var roadEdgeVectorsTask = _rpcFacade.getRoadEdgeVectors();
                yield return Task.WhenAll(angleRecommendationWithObstacleTask, closestRoadEdgeInformationTask,
                    verticallyClosestRoadEdgeInformationTask, isOffRoadTask, roadEdgeVectorsTask).toCoroutine();

                var angleRecommendationNoObstacle = angleRecommendationWithObstacleTask.Result;
                if (angleRecommendationNoObstacle == null)
                    continue;
                Func<AngleRecommendation,AngleRecommendation> toDegree = (e) => new Tuple<float, double, Vector2>(
                    e.Item1, 180 * e.Item2 / Math.PI, e.Item3);
                var angleRecommendationWithObstacle = angleRecommendationWithObstacleTask.Result.Select(toDegree).ToList();

                // tryToDirectTheCarToMiddleOfRoad(angleRecommendation, closestRoadEdgeInformationTask.Result);
                AngleRecommendationReceived?.Invoke(new AngleRecommendationReceivedEventArgs{
                    recommendationsWithObstacle = angleRecommendationWithObstacle,
                    verticallyClosestRoadLeftRightEdge = verticallyClosestRoadEdgeInformationTask.Result,
                    isOffRoad = isOffRoadTask.Result,
                    // roadEdgeList = roadEdgeVectorsTask.Result,
                    obstacleIdVerdicts = obstacleIdVerdicts,
                    debugPurposeOnly = debugPurposeOnly,
                });
            }
        }

        private void tryToDirectTheCarToMiddleOfRoad(List<Tuple<float, double, Vector2>> angleRecommendation,
            Tuple<Vector2?, Vector2?> roadLeftAndRightBoundary
        ) {
            if (angleRecommendation.Count == 0
                || roadLeftAndRightBoundary == null
                || roadLeftAndRightBoundary.Item1 == null
                || roadLeftAndRightBoundary.Item2 == null)
                return;
            var currentRecommendation = angleRecommendation[0];
            var theRecommendationIntersectsWithRoadEdge = (currentRecommendation.Item1 < 9);
            if (theRecommendationIntersectsWithRoadEdge)
                return;
            var leftDistance = Math.Abs(roadLeftAndRightBoundary.Item1.Value.x);
            var rightDistance = Math.Abs(roadLeftAndRightBoundary.Item2.Value.x);
            if (Math.Abs(leftDistance - rightDistance) < 0.07)  // just assume they're equal
                return;
            var mid = 0;
            var twoDegree = 2;

            var targetDegree = mid + twoDegree;
            // negative is ccw
            var directioNVector = Quaternion.AngleAxis(-2f, Vector3.forward) * currentRecommendation.Item3;

            if (leftDistance > rightDistance) { // slightly go left
                targetDegree = mid - twoDegree;
                // positive angle is clockwise
                directioNVector = Quaternion.AngleAxis(2f, Vector3.forward)*currentRecommendation.Item3;
            }

            angleRecommendation.Insert(0, new Tuple<float, double, Vector2>(currentRecommendation.Item1, targetDegree, directioNVector));
        }


        void OnApplicationQuit() {
            _rpcFacade.stopListening();
        }

        private IEnumerator detectConnectionErrorCoroutine() {
            yield return new WaitForSeconds(4);
            if (!_rpcFacade.isConnected())
                RpcFacade.showConnectionError();
        }
    }

    public static class TaskToCoroutineExtension {
        public static IEnumerator toCoroutine(this Task task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            if (task.IsFaulted) {
                throw task.Exception;
            }
        }
        public static IEnumerator toCoroutine<T>(this Task<T> task) {
            while (!task.IsCompleted) {
                yield return null;
            }

            if (task.IsFaulted) {
                throw task.Exception;
            }
        }


    }
}