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

        private ConstSpeedTorqueManager _torqueManager;
        // private MotorTorqueManager _motorTorqueManager = new MotorTorqueManager(75, 0.1f, 30);
        private SteerDirectionManager _steerDirectionManagerObstacles = new SteerDirectionManager(45, 240);
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Quaternion originalCameraRotationRelativeToParent;
        private float targetCameraRotation = 0.0f;
        public GameObject camera;
        private SteerDirectionManager cameraRotationManager = new SteerDirectionManager(60, 50);
        private float targetCarAngleBasedOnRecommendationAndCamRotation => cameraRotationManager.getSteerAngle()+angleRecommendation;


        private CarAngularSteeringDegreeCalculator _angularDegreeSteeringCalculator = new CarAngularSteeringDegreeCalculator(
            0.7, 2.88, 0.83, 1.98, 3.5 + 0.1);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
            camera = GetComponentsInChildren<Camera>().First().gameObject;
            originalCameraRotationRelativeToParent = camera.transform.localRotation;

            _torqueManager = new ConstSpeedTorqueManager(_speedSensor, 150, 8, 20, 0.4f, 45);
        }

        private float? goBackwardUntilTimestamp = null;

        private void Update() {
            var slope = Vector3.Angle(new Vector3(transform.forward.x, 0, transform.forward.z),
                transform.forward);  // unused for now
            _speedSensor ??= new SpeedSensor(rigidBodyForSpeedSensor);

            var angle = targetCarAngleBasedOnRecommendationAndCamRotation;
            angle = turnLeftOrRightIfObstacleWillBeHit(angle);

            float choosenAngle = _obstacleInfoEventArgs.shouldGoBackward? -targetCarAngleBasedOnRecommendationAndCamRotation:angle;
            _steerDirectionManagerObstacles.updateSteerBasedOnAngle(choosenAngle);
            steerAngle = _steerDirectionManagerObstacles.getSteerAngle();
            motorTorque = _torqueManager.getMotorTorque(targetCarAngleBasedOnRecommendationAndCamRotation);

            handleGoingBackwardBecauseWhenObstacleAlreadyHit();
            updateCameraRotation();
            Debug.Log($"Recommended: (angle:{targetCarAngleBasedOnRecommendationAndCamRotation:00.00},l:{angleRecommendationCollisionLength:00.00}) " +
                      $"actualAngle: {steerAngle:00.00} " +
                      $"Speed: {_speedSensor.getCurrentSpeed():00.00}  ObsDist: {_obstacleInfoEventArgs.forwardObstacleDistance:00.00}");
        }

        private void updateCameraRotation() {
            // biar ga pusin kameranya goyang kiri kanan
            var target = (Math.Abs(targetCameraRotation) < 3)? 0 : targetCameraRotation;

            cameraRotationManager.updateSteerBasedOnAngle(target);
            camera.transform.localRotation = originalCameraRotationRelativeToParent
                                        *Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up);
            Debug.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up)*transform.forward.normalized*10,
                Color.yellow);
        }

        private float turnLeftOrRightIfObstacleWillBeHit(float angle) {
            if (!_obstacleInfoEventArgs.allowedToGoLeft && angle < 0)
                angle = 1;
            if (!_obstacleInfoEventArgs.allowedToGoRight && angle > 0)
                angle = -1;

            // _angularDegreeSteeringCalculator.updateSteeringDirection(targetCarAngleBasedOnRecommendationAndCamRotation);
            // _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            // if (!_angularDegreeSteeringCalculator.willHitObstacle())
                // return angle;
                if (_obstacleInfoEventArgs.forwardObstacleETA > 2 && _obstacleInfoEventArgs.forwardObstacleDistance > 4)
                    return angle;

            if (!_obstacleInfoEventArgs.allowedToGoForward) {
                var jaga_jaga = 3;
                var targetAngle = jaga_jaga + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();
                if (!_obstacleInfoEventArgs.allowedToGoLeft)
                    angle = targetAngle;
                else if (!_obstacleInfoEventArgs.allowedToGoRight)
                    angle = -targetAngle;
                else  // can go left or right
                    angle = Math.Sign(targetCarAngleBasedOnRecommendationAndCamRotation) * targetAngle;
            }

            return angle;
        }

        private void handleGoingBackwardBecauseWhenObstacleAlreadyHit() {
            if (_obstacleInfoEventArgs.forwardObstacleDistance == null)
                return;
            if (_obstacleInfoEventArgs.shouldGoBackward) {
                goBackwardUntilTimestamp = Time.time + 1;
            }
            if (goBackwardUntilTimestamp != null && Time.time < goBackwardUntilTimestamp) {
                motorTorque = -_torqueManager.motorTorque;
                if (_speedSensor.isGoingForward(transform) || _speedSensor.getCurrentSpeed() < 0.5)
                    goBackwardUntilTimestamp = Time.time + 1;  // refresh
            } else if (Time.time < goBackwardUntilTimestamp + 1) {
                if (!_speedSensor.isGoingForward(transform))
                    steerAngle = Math.Abs(steerAngle) > 10 ? steerAngle : -Math.Sign(steerAngle) * 10;
                else
                    steerAngle = Math.Abs(steerAngle) > 10 ? steerAngle : Math.Sign(steerAngle) * 10;
            }
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
            var pos = transform.position;
            targetCameraRotation = angleRecommendation;

            Debug.DrawLine(pos,
                pos + Quaternion.AngleAxis(targetCarAngleBasedOnRecommendationAndCamRotation, Vector3.up)*transform.forward.normalized*10,
                Color.magenta);
        }


        public float steerAngle {
            get {
                return leftFront.steerAngle;
            }
            set {
                Debug.DrawLine(transform.position,
                    transform.position + Quaternion.AngleAxis(value, Vector3.up)*transform.forward.normalized*10,
                    Color.blue);
                _angularDegreeSteeringCalculator.updateSteeringDirection(value);
                leftFront.steerAngle = (float) _angularDegreeSteeringCalculator.getLeftSteeringDegree();
                rightFront.steerAngle = (float) _angularDegreeSteeringCalculator.getRightSteeringDegree();
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