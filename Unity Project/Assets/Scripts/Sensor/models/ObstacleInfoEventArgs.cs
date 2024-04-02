using System;
using JetBrains.Annotations;

namespace Sensor.models
{
    public class ObstacleInfoEventArgs: EventArgs
    {
        [CanBeNull] public readonly UltrasonicSensorIntegration sensor;
        public int obstacleId;
        public readonly bool allowedToGoLeft;
        public readonly bool allowedToGoForward;
        public readonly bool allowedToGoRight;
        public readonly bool shouldGoBackward;
        public readonly float? forwardObstacleETA;
        public float? forwardObstacleDistance;
        public ForwardSensorEnum? whichForwardSensorEnum;

        public ObstacleInfoEventArgs(
            [CanBeNull] UltrasonicSensorIntegration sensor, int obstacleId, float? forwardObstacleDistance, float? ForwardObstacleETA, ForwardSensorEnum? whichForwardSensorEnum,
            bool allowedToGoLeft, bool allowedToGoForward, bool allowedToGoRight, bool shouldGoBackward
        ) {
            this.sensor = sensor;
            this.obstacleId = obstacleId;
            this.forwardObstacleDistance = forwardObstacleDistance;
            this.forwardObstacleETA = ForwardObstacleETA;
            this.whichForwardSensorEnum = whichForwardSensorEnum;
            this.allowedToGoLeft = allowedToGoLeft;
            this.allowedToGoForward = allowedToGoForward;
            this.allowedToGoRight = allowedToGoRight;
            this.shouldGoBackward = shouldGoBackward;
        }


        public static ObstacleInfoEventArgs NoObstacle([CanBeNull] UltrasonicSensorIntegration sensor, int obstacleId) {
            return new ObstacleInfoEventArgs(sensor, obstacleId,
                null, null, null,
                true, true, true, false);
        }
    }

    public enum ForwardSensorEnum
    {
        LEFT_FORWARD, MID_FORWARD, RIGHT_FORWARD
    }
}