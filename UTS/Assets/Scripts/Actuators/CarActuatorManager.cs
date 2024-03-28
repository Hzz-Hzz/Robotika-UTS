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

        private MotorTorqueManager _motorTorqueManager = new MotorTorqueManager(55, 0.1f, 30);
        private SteerDirectionManager _steerDirectionManager = new SteerDirectionManager(45, 50);
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
        }

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
            _steerDirectionManager.updateSteerBasedOnAngle(choosenAngle);
            var currentAngle = _steerDirectionManager.getSteerAngle();
            steerAngle = currentAngle;
            motorTorque = _motorTorqueManager.getMotorTorque(currentAngle);
            if (_obstacleInfoEventArgs.shouldGoBackward)
                motorTorque = -Math.Abs(motorTorque);

            Debug.Log($"Recommended angle: {angleRecommendation:00.00}  actualAngle: {steerAngle:00.00} Speed: {_speedSensor.getCurrentSpeed():00.00}");
        }

        public void OnObstacleInfoUpdated(ObstacleInfoEventArgs obstacleInfoEvent) {
            _obstacleInfoEventArgs = obstacleInfoEvent;
        }

        private float angleRecommendation = 0;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recomomendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }
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