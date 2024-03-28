using System;
using System.Linq;
using EventsEmitter.models;
using JetBrains.Annotations;
using Sensor;
using Sensor.models;
using UnityEngine;

namespace Actuators
{
    public class CarActuatorManager : MonoBehaviour
    {
        public WheelCollider leftBack;
        public WheelCollider leftFront;
        public WheelCollider rightBack;
        public WheelCollider rightFront;
        public Rigidbody rigidBodyForSpeedSensor;

        private MotorTorqueManager _motorTorqueManager = new MotorTorqueManager(75, 0.1f, 30);
        private SteerDirectionManager _steerDirectionManagerObstacles = new SteerDirectionManager(45, 70);
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
        }

        private float? goBackwardUntilTimestamp = null;

        private void Update() {
            var angle = angleRecommendation;
            if (!_obstacleInfoEventArgs.allowedToGoForward) {
                if (!_obstacleInfoEventArgs.allowedToGoLeft)
                    angle = 30;
                if (!_obstacleInfoEventArgs.allowedToGoRight)
                    angle = -30;
            }
            else {
                if (!_obstacleInfoEventArgs.allowedToGoLeft && angle < 0)
                    angle = 0;
                if (!_obstacleInfoEventArgs.allowedToGoRight && angle > 0)
                    angle = 0;
            }

            float choosenAngle = _obstacleInfoEventArgs.shouldGoBackward? -angleRecommendation:angle;
            _steerDirectionManagerObstacles.updateSteerBasedOnAngle(choosenAngle);
            var currentAngle = _steerDirectionManagerObstacles.getSteerAngle();
            steerAngle = currentAngle;
            motorTorque = _motorTorqueManager.getMotorTorque(currentAngle);
            if (_obstacleInfoEventArgs.shouldGoBackward) {
                goBackwardUntilTimestamp = Time.time + 3;
            }

            if (goBackwardUntilTimestamp != null && Time.time < goBackwardUntilTimestamp) {
                motorTorque = -Math.Max(Math.Abs(motorTorque), _motorTorqueManager.motorTorque / 2);
                if (_obstacleInfoEventArgs.forwardObstacleDistance < 2)
                    goBackwardUntilTimestamp = Time.time + 3;  // refresh
            } else if (Time.time < goBackwardUntilTimestamp + 3) {
                if (!_speedSensor.isGoingForward(transform))
                    steerAngle = Math.Abs(steerAngle) > 10 ? steerAngle : -Math.Sign(steerAngle) * 10;
                else
                    steerAngle = Math.Abs(steerAngle) > 10 ? steerAngle : Math.Sign(steerAngle) * 10;
            }

            Debug.Log($"Recommended: (angle:{angleRecommendation:00.00},l:{angleRecommendationCollisionLength:00.00}) " +
                      $"actualAngle: {steerAngle:00.00} " +
                      $"Speed: {_speedSensor.getCurrentSpeed():00.00}  ObsDist: {_obstacleInfoEventArgs.forwardObstacleDistance:00.00}");
        }

        public void OnObstacleInfoUpdated(ObstacleInfoEventArgs obstacleInfoEvent) {
            _obstacleInfoEventArgs = obstacleInfoEvent;
        }

        private float angleRecommendation = 0;
        private float angleRecommendationCollisionLength = 0;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recomomendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }
            angleRecommendationCollisionLength = e.recomomendations[0].Item1;
            angleRecommendation = (float)e.recomomendations[0].Item2;
        }


        public float steerAngle {
            get {
                return leftFront.steerAngle;
            }
            set {
                leftFront.steerAngle = value;
                rightFront.steerAngle = value;
            }
        }

        public float motorTorque {
            get {
                return leftBack.motorTorque;
            }
            set {
                leftBack.motorTorque = value;
                rightBack.motorTorque = value;
            }
        }

    }
}