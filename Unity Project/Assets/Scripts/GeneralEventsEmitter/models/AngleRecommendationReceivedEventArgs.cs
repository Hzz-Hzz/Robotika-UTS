using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventsEmitter.models
{
    public class AngleRecommendationReceivedEventArgs: EventArgs
    {

        /**
        * return list of recommendations, sorted by most-recommended (index 0) to the least recommended
        * but still recommended (last index).
        *
        * Each item will be represented as a tuple of (distance, angle in rads).
        * Angle in rads will be 0 if you should go forward,
        * positive if you should go right,
        * and negative if you should go left.
         *
         * For more updated information, please see getAngleRecommendation() at Server's C# project, file: RpcFacade.cs
        */
        public List<Tuple<float, double, Vector2>> recomomendations;

        public Tuple<Vector2?, Vector2?> verticallyClosestRoadLeftRightEdge { get; set; }
        public Tuple<Vector2?,Vector2?> horizontallyClosestRoadLeftRightEdge { get; set; }
        public bool isOffRoad { get; set; }
        public Tuple<Vector2?[,], Vector2?[,]> roadEdgeList { get; set; }

        private static Dictionary<int, Direction> verdicts = new ();

        /**
         * Obstacle is assumed to be in front of the car
         * currentCameraRotationDegree: 0 means forward, negative means left, and positive means right.
         *
         * We need obstacleId to make sure that the Direction recommendation doesn't change due to visual changes as we turn left/right
         */
        public Direction getDirectionRecommendationToAvoidObstacle(int obstacleId, float currentCameraRotationDegree, Vector2 obstacleRelativePos) {
            if (verdicts.TryGetValue(obstacleId, out var obstacle))
                return obstacle;

            // normally we would need to inverse the sign (negative/positive) for the rotation, but because we use
            // different standard, then we don't need to inverse the sign.
            // different standard: our positive angle means right/clockwise, unity's positive angle to left/ccw
            var rotation = Quaternion.AngleAxis(currentCameraRotationDegree, Vector3.forward);

            var obstaclePositionRelativeToCameraView = rotation * obstacleRelativePos;
            var left = roadEdgeList.Item1;
            var right = roadEdgeList.Item2;
            var leftDist = averageDistanceToRays(obstaclePositionRelativeToCameraView, left, Single.NaN);
            var rightDist = averageDistanceToRays(obstaclePositionRelativeToCameraView, right, Single.NaN);
            if (Single.IsNaN(leftDist) || Single.IsNaN(rightDist))
                return Direction.ANY;
            if (leftDist + rightDist == 0)
                return Direction.ANY;
            var leftDistRatio = leftDist / (leftDist + rightDist);
            if (leftDistRatio < 0.4)
                verdicts[obstacleId] = Direction.RIGHT;
            else if (leftDistRatio > 0.6)
                verdicts[obstacleId] = Direction.LEFT;
            else verdicts[obstacleId] = Direction.ANY;
            var ret = verdicts[obstacleId];
            return ret;
        }
        private float averageDistanceToRays(Vector2 anchor, Vector2?[,] points, float defaultValue) {
            var totalDistance = 0f;
            var count = 0;
            for (int i = 0; i < points.GetLength(0); i++) {
                if (points[i,0] == null || points[i, 1] == null)
                    continue;
                var translation = points[i, 0]!.Value;
                var rayDirection = points[i, 1]!.Value.normalized;
                var translatedAnchor = anchor - translation;
                var multiplier = Vector2.Dot(rayDirection, translatedAnchor);
                if (multiplier < 0)
                    continue;
                var perpendicularToRay = rayDirection * multiplier;
                totalDistance += Vector2.Distance(perpendicularToRay, translatedAnchor);
                count += 1;
            }

            if (count == 0)
                return defaultValue;
            return totalDistance / count;
        }
        private float averageDistanceOfThreeClosestPoints(Vector2 anchor, Vector2?[,] points, float defaultValue) {
            var totalDistance = 0f;
            var count = 0;
            for (int i = 0; i < points.GetLength(0); i++) {
                if (points[i,0] == null)
                    continue;
                var distance = Vector2.Distance(anchor, points[i,0]!.Value);
                totalDistance += distance;
                count += 1;
            }

            if (count == 0)
                return defaultValue;
            return totalDistance / count;
        }
    }


    public enum Direction
    {
        LEFT, RIGHT, ANY
    }
}