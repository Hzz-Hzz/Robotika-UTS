using System;
using System.Collections.Generic;
using System.Text;
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


        public Tuple<Vector2?, Vector2?> verticallyClosestRoadLeftRightEdge { get; set; }

        public bool isOffRoad { get; set; }
        // public Tuple<Vector2?[,], Vector2?[,]> roadEdgeList { get; set; }


        public Dictionary<int,Direction> obstacleIdVerdicts { get; set; }
        public Dictionary<int, Tuple<float, float, AngleRecommendationReceivedEventArgs>> debugPurposeOnly { get; set; }
        public List<Tuple<float, double, Vector2>> recommendationsWithObstacle { get; set; }


        // /**
        //  * Obstacle is assumed to be in front of the car
        //  * currentCameraRotationDegree: 0 means forward, negative means left, and positive means right.
        //  *
        //  * We need obstacleId to make sure that the Direction recommendation doesn't change due to visual changes as we turn left/right
        //  */
        // public Direction getDirectionRecommendationToAvoidObstacle(int obstacleId, float currentCameraRotationDegree, Vector2 obstacleRelativePos) {
        //     if (obstacleIdVerdicts.TryGetValue(obstacleId, out var obstacle))
        //         return obstacle;
        //
        //     // normally we would need to inverse the sign (negative/positive) for the rotation, but because we use
        //     // different standard, then we don't need to inverse the sign.
        //     // different standard: our positive angle means right/clockwise, unity's positive angle to left/ccw
        //     var cameraRotationWeight = 0.5f;
        //     var rotation = Quaternion.AngleAxis(currentCameraRotationDegree*cameraRotationWeight, Vector3.forward);
        //
        //     var obstaclePositionRelativeToCameraView = rotation * obstacleRelativePos;
        //     var left = roadEdgeList.Item1;
        //     var right = roadEdgeList.Item2;
        //     var leftDist = closestDistanceToPoints(obstaclePositionRelativeToCameraView, left, Single.NaN);
        //     var rightDist = closestDistanceToPoints(obstaclePositionRelativeToCameraView, right, Single.NaN);
        //     if (Single.IsNaN(leftDist) || Single.IsNaN(rightDist))
        //         return Direction.DEFAULT;
        //     if (leftDist + rightDist == 0)
        //         return Direction.DEFAULT;
        //     var leftDistRatio = leftDist / (leftDist + rightDist);
        //     if (leftDistRatio < 0.3)
        //         obstacleIdVerdicts[obstacleId] = Direction.RIGHT;
        //     else if (leftDistRatio > 0.7)
        //         obstacleIdVerdicts[obstacleId] = Direction.LEFT;
        //     else obstacleIdVerdicts[obstacleId] = Direction.DEFAULT;
        //     var ret = obstacleIdVerdicts[obstacleId];
        //     debugPurposeOnly[obstacleId] = new Tuple<float, float, AngleRecommendationReceivedEventArgs>(leftDist, rightDist, this);
        //     return ret;
        // }

        public string debugGetDesmosPoints(Vector2 anchor, Vector2?[,] left, Vector2?[,] right ) {
            var ret = new StringBuilder(500);
            for (int i = 0; i < left.GetLength(0); i++) {
                ret.Append($"({left[i, 0].Value.x}, {left[i, 0].Value.y})\n");
            }
            for (int i = 0; i < right.GetLength(0); i++) {
                ret.Append($"({right[i, 0].Value.x}, {right[i, 0].Value.y})\n");
            }
            ret.Append($"({anchor.x}, {anchor.y})\n");
            return ret.ToString();
        }
        private float closestDistanceToPoints(Vector2 anchor, Vector2?[,] points, float defaultValue) {
            var maxDistance = Single.PositiveInfinity;
            var none = true;
            for (int i = 0; i < points.GetLength(0); i++) {
                if (points[i,0] == null || points[i, 1] == null)
                    continue;
                none = false;
                maxDistance = Math.Min(maxDistance, Vector2.Distance(points[i, 0]!.Value, anchor));
            }
            if (none)
                return defaultValue;
            return maxDistance;
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
        LEFT, RIGHT, DEFAULT
    }

    public static class DirectionEnumExtension
    {
        public static float getSign(this Direction? self) {
            if (self == Direction.LEFT)
                return -1;
            if (self == Direction.RIGHT)
                return 1;
            return 0;
        }
    }
}