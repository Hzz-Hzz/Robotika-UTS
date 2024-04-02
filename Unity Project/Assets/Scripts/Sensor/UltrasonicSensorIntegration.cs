using System;
using System.Linq;
using Sensor.models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Sensor
{
    public class UltrasonicSensorIntegration: MonoBehaviour
    {
        public UnityEvent<ObstacleInfoEventArgs> ObstacleInfoUpdated;
        public UltrasonicSensor leftSensor2;
        public UltrasonicSensor leftSensor1;
        public UltrasonicSensor leftSensor0;
        public UltrasonicSensor midSensor;
        public UltrasonicSensor rightSensor0;
        public UltrasonicSensor rightSensor1;
        public UltrasonicSensor rightSensor2;



        public Rigidbody rigidBodyForSpeedSensor;
        private SpeedSensor _speedSensor;

        public float?[] leftSensorResults = new float?[]{null, null, null};
        public float? midSensorResult = null;
        public float?[] rightSensorResults = new float?[]{null, null, null};

        private void Start() {
            ObstacleInfoUpdated?.Invoke(ObstacleInfoEventArgs.NoObstacle(this, -1));
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
        }

        private int obstacleId = 0;
        private float? prevObstacleDistance = null;
        private void Update() {
            updateSensorResults();
            float? forwardObstacleDistance = null;
            float? forwardObstacleETA = null;
            ForwardSensorEnum? whichForwardSensorEnum = null;
            var shouldGoBackward = false;
            var allowedLeft = true;
            var allowedForward = true;
            var allowedRight = true;

            if (forwardClosestDistance != null) {
                allowedForward = false;
                whichForwardSensorEnum = whichForwardSensor;
                forwardObstacleDistance = this.forwardClosestDistance;
                forwardObstacleETA = forwardObstacleDistance / _speedSensor.getCurrentSpeed();
            }
            if (forwardObstacleDistance == null && prevObstacleDistance != null
                || prevObstacleDistance != null && forwardObstacleDistance > prevObstacleDistance + 1
                // -2 because normally we're getting closer to the obstacle, so need extra threshold
                || prevObstacleDistance != null && forwardObstacleDistance < prevObstacleDistance - 2)
                obstacleId++;
            prevObstacleDistance = forwardObstacleDistance;
            if (leftCollisions != 0)
                allowedLeft = false;
            if (rightCollisions != 0)
                allowedRight = false;
            var obstacleBlocksAllForwardSensor = !allowedLeft && !allowedRight;
            var obstacleBlocksAllForwardSensorButStillFarAway = (
                obstacleBlocksAllForwardSensor && forwardObstacleETA > 1 && forwardObstacleDistance > 1);

            if (obstacleBlocksAllForwardSensor) {
                if (slopeOfObstacleIsGoingRightForward() ?? false)
                    allowedRight = true;
                else allowedLeft = true;
            } else if (obstacleBlocksAllForwardSensorButStillFarAway) {
                if (pureLeftCollisions > pureRightCollisions) // obstacle blocks left side more than right side
                    allowedRight = true;
                else allowedLeft = true;
            }

            if (!allowedLeft && !allowedForward && !allowedRight)
                shouldGoBackward = true;
            if (forwardObstacleDistance < 0.8 || forwardObstacleETA < 0.3) {  // go bakcward
                shouldGoBackward = true;
            }
            ObstacleInfoUpdated?.Invoke(new ObstacleInfoEventArgs(this, obstacleId, forwardObstacleDistance,
                forwardObstacleETA, whichForwardSensorEnum, allowedLeft, allowedForward, allowedRight, shouldGoBackward));
        }

        private void updateSensorResults() {
            leftSensorResults[2] = this.leftSensor2.detectDistance();
            leftSensorResults[1] = this.leftSensor1.detectDistance();
            leftSensorResults[0] = this.leftSensor0.detectDistance();
            midSensorResult = this.midSensor.detectDistance();
            rightSensorResults[0] = this.rightSensor0.detectDistance();
            rightSensorResults[1] = this.rightSensor1.detectDistance();
            rightSensorResults[2] = this.rightSensor2.detectDistance();
        }

        /**
         * Return true if right forward (like slash do), or false if left-forward (like backslash do)
         */
        public bool? slopeOfObstacleIsGoingRightForward() {
            var left = leftSensorResults[0] ?? midSensorResult;
            if (left == null)
                return null;
            var right = rightSensorResults[0] ?? midSensorResult;
            if (right == null)
                return null;
            if (left == right)
                return null;
            if (left < right)
                return true;
            return false;
        }

        public int pureLeftCollisions => leftSensorResults.Skip(1).Count(e => e != null);
        public int leftCollisions => leftSensorResults.Count(e => e != null);
        public int rightCollisions => rightSensorResults.Count(e => e != null);
        public int pureRightCollisions => rightSensorResults.Skip(1).Count(e => e != null);

        public int numberOfAllCollision =>
            leftSensorResults.Count(e => e != null) + rightSensorResults.Count(e => e != null) +
            (midSensorResult == null? 0 : 1);

        public float? forwardClosestDistance {
            get {
                var result = Math.Min(Math.Min(leftSensorResults[0]??Single.PositiveInfinity,
                    rightSensorResults[0]??Single.PositiveInfinity),
                    midSensorResult??Single.PositiveInfinity);
                if (result == Single.PositiveInfinity)
                    return null;
                return result;
            }
        }

        public ForwardSensorEnum? whichForwardSensor {
            get {
                var forwardClosestDistance = this.forwardClosestDistance;
                if (forwardClosestDistance == null)
                    return null;
                if (leftSensorResults[0] == forwardClosestDistance)
                    return ForwardSensorEnum.LEFT_FORWARD;
                if (rightSensorResults[0] == forwardClosestDistance)
                    return ForwardSensorEnum.RIGHT_FORWARD;
                if (midSensorResult == forwardClosestDistance)
                    return ForwardSensorEnum.MID_FORWARD;
                return null;
            }
        }
    }
}