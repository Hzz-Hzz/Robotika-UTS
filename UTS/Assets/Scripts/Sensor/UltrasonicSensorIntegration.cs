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

        private float?[] leftSensorResults = new float?[]{null, null, null};
        private float? midSensorResult = null;
        private float?[] rightSensorResults = new float?[]{null, null, null};

        private void Start() {
            ObstacleInfoUpdated?.Invoke(ObstacleInfoEventArgs.NoObstacle(this));
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
        }

        private void Update() {
            updateSensorResults();
            float? forwardObstacleDistance = null;
            float? forwardObstacleETA = null;
            var shouldGoBackward = false;
            var allowedLeft = true;
            var allowedForward = true;
            var allowedRight = true;

            if (forwardClosestDistance != null) {
                allowedForward = false;
                forwardObstacleDistance = this.forwardClosestDistance;
                forwardObstacleETA = forwardObstacleDistance / _speedSensor.getCurrentSpeed();
            }
            if (leftCollisions != 0)
                allowedLeft = false;
            if (rightCollisions != 0)
                allowedRight = false;
            var obstacleBlocksAllForwardSensorButStillFarAway = (
                !allowedLeft && !allowedRight && forwardObstacleETA > 1 && forwardObstacleDistance > 1);
            if (obstacleBlocksAllForwardSensorButStillFarAway) {
                if (pureLeftCollisions > pureRightCollisions) // obstacle blocks left side more than right side
                    allowedRight = true;
                else allowedLeft = true;
            }

            if (!allowedLeft && !allowedForward && !allowedRight)
                shouldGoBackward = true;
            if (forwardObstacleDistance < 1 || forwardObstacleETA < 1) {  // go bakcward
                shouldGoBackward = true;
            }
            ObstacleInfoUpdated?.Invoke(new ObstacleInfoEventArgs(this, forwardObstacleDistance,
                forwardObstacleETA, allowedLeft, allowedForward, allowedRight, shouldGoBackward));
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
    }
}