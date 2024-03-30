using System;
using JetBrains.Annotations;

namespace Sensor.models
{
    public class ObstacleInfoEventArgs: EventArgs
    {
        [CanBeNull] public readonly UltrasonicSensorIntegration sensorIntegration;
        public readonly bool allowedToGoLeft;
        public readonly bool allowedToGoForward;
        public readonly bool allowedToGoRight;
        public readonly bool shouldGoBackward;
        public readonly float? forwardObstacleETA;
        public float? forwardObstacleDistance;

        public ObstacleInfoEventArgs(
            [CanBeNull] UltrasonicSensorIntegration sensorIntegration, float? forwardObstacleDistance, float? ForwardObstacleETA,
            bool allowedToGoLeft, bool allowedToGoForward, bool allowedToGoRight, bool shouldGoBackward
        ) {
            this.sensorIntegration = sensorIntegration;
            this.forwardObstacleDistance = forwardObstacleDistance;
            this.forwardObstacleETA = ForwardObstacleETA;
            this.allowedToGoLeft = allowedToGoLeft;
            this.allowedToGoForward = allowedToGoForward;
            this.allowedToGoRight = allowedToGoRight;
            this.shouldGoBackward = shouldGoBackward;
        }

        public static ObstacleInfoEventArgs NoObstacle([CanBeNull] UltrasonicSensorIntegration sensor) {
            return new ObstacleInfoEventArgs(sensor,
                null, null,
                true, true, true, false);
        }
    }


    public class Obstacle
    {
        public float distance;
        public float currentSpeed;
        public float estimatedTimeArrival;


        public Obstacle(float distance, float currentSpeed, float estimatedTimeArrival) {
            this.distance = distance;
            this.currentSpeed = currentSpeed;
            this.estimatedTimeArrival = estimatedTimeArrival;
        }
    }

}