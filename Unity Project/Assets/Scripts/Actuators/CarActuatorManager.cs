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
        private SteerDirectionManager cameraRotationManager = new SteerDirectionManager(60, 8, 5);
        private float targetCarAngleBasedOnRecommendationAndCamRotation => cameraRotationManager.getSteerAngle()+angleRecommendation;


        private readonly float backwardTorque = 80;
        private CarAngularSteeringDegreeCalculator _angularDegreeSteeringCalculator = new CarAngularSteeringDegreeCalculator(
            0.7, 2.88, 0.83, 1.98, 3.5 + 0.1);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
            camera = GetComponentsInChildren<Camera>().First().gameObject;
            originalCameraRotationRelativeToParent = camera.transform.localRotation;

            _torqueManager = new ConstSpeedTorqueManager(_speedSensor, 180, -1, 30,
                0.4f, 45);
        }

        private float? goBackwardUntilTimestamp = null;

        private void Update() {
            var slope = Vector3.Angle(new Vector3(transform.forward.x, 0, transform.forward.z),
                transform.forward);
            if (slope >= 2)
                _torqueManager.maxSpeed = 7;
            else if (slope >= 0.25)
                _torqueManager.maxSpeed = 5;
            else _torqueManager.maxSpeed = 8;

             // unused for now
            _speedSensor ??= new SpeedSensor(rigidBodyForSpeedSensor);

            var angle = targetCarAngleBasedOnRecommendationAndCamRotation;
            angle = turnLeftOrRightIfObstacleWillBeHit(angle);

            float choosenAngle = _obstacleInfoEventArgs.shouldGoBackward? -targetCarAngleBasedOnRecommendationAndCamRotation:angle;
            _steerDirectionManagerObstacles.updateSteerBasedOnAngle(choosenAngle);
            // steerAngle = _steerDirectionManagerObstacles.getSteerAngle();
            steerAngle = (Math.Abs(angle)<=45)? angle : Math.Sign(angle)*45;
            motorTorque = _torqueManager.getMotorTorque(targetCarAngleBasedOnRecommendationAndCamRotation);
            brakeIfFreeFalling();

            handleGoingBackwardBecauseWhenObstacleAlreadyHit();
            updateCameraRotation();
            Debug.Log($"Recommended: (angle:{targetCarAngleBasedOnRecommendationAndCamRotation:00.00},l:{angleRecommendationCollisionLength:00.00}) " +
                      $"actualAngle: {steerAngle:00.00} " +
                      $"Speed: {_speedSensor.getCurrentSpeed():00.00}  ObsDist: {_obstacleInfoEventArgs.forwardObstacleDistance:00.00}");
        }

        private void brakeIfFreeFalling() {
            if (freeFallEventArgs.numOfFreeFall < 3) {
                leftBack.brakeTorque = 0;
                rightBack.brakeTorque = 0;
                return;
            }
            Debug.Log("Free falling");
            leftBack.brakeTorque = motorTorque;
            rightBack.brakeTorque = motorTorque;
            motorTorque = 0;
        }

        private void updateCameraRotation() {
            // biar ga pusin kameranya goyang kiri kanan
            var target = (Math.Abs(targetCarAngleBasedOnRecommendationAndCamRotation) < 3)?
                0 : targetCarAngleBasedOnRecommendationAndCamRotation;
            cameraRotationManager.updateSteerBasedOnAngle(target);
            camera.transform.localRotation = originalCameraRotationRelativeToParent
                                        *Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up);
            Debug.DrawLine(transform.position,
                transform.position + Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up)*transform.forward.normalized*10,
                Color.yellow);
        }

        private float doNotUseRoadRecommendation = -1;
        private float turnLeftOrRightIfObstacleWillBeHit(float angle) {
            _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            var targetCarRotationIsExtreme = Math.Abs(targetCarAngleBasedOnRecommendationAndCamRotation) > 15
                                             && angleRecommendationCollisionLength >= 2;
            var doNotOverrideAngle = targetCarRotationIsExtreme
                                     && _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle() < 40
                                     && _obstacleInfoEventArgs.forwardObstacleETA > 1
                                     && Time.time > doNotUseRoadRecommendation;
            if (!_obstacleInfoEventArgs.allowedToGoLeft && angle < 0 && !doNotOverrideAngle)
                angle = 0;
            if (!_obstacleInfoEventArgs.allowedToGoRight && angle > 0 && !doNotOverrideAngle)
                angle = 0;

            // _angularDegreeSteeringCalculator.updateSteeringDirection(targetCarAngleBasedOnRecommendationAndCamRotation);
            // _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            // if (!_angularDegreeSteeringCalculator.willHitObstacle())
                // return angle;
            // if (_obstacleInfoEventArgs.forwardObstacleETA > 1 && _obstacleInfoEventArgs.forwardObstacleDistance > 3)
                // return angle;

            if (!_obstacleInfoEventArgs.allowedToGoForward) {
                var jaga_jaga = 5;
                var targetAngle = jaga_jaga + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();
                if (!_obstacleInfoEventArgs.allowedToGoLeft && !doNotOverrideAngle)
                    angle = targetAngle;
                else if (!_obstacleInfoEventArgs.allowedToGoRight && !doNotOverrideAngle)
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
                goBackwardUntilTimestamp = Time.time + 2;
                doNotUseRoadRecommendation = Time.time + 6;
            }
            if (goBackwardUntilTimestamp != null && Time.time < goBackwardUntilTimestamp) {
                motorTorque = -backwardTorque;
                if (_speedSensor.isGoingForward(transform) || _speedSensor.getCurrentSpeed() < 0.5) {
                    goBackwardUntilTimestamp = Time.time + 2;  // refresh
                    doNotUseRoadRecommendation = Time.time + 6;
                }
            } else if (Time.time < goBackwardUntilTimestamp + 2) {
                // if (!_speedSensor.isGoingForward(transform))
                    // steerAngle = Math.Abs(steerAngle) > 20 ? steerAngle : -Math.Sign(steerAngle) * 20;
                // else
                    // steerAngle = Math.Abs(steerAngle) > 20 ? steerAngle : Math.Sign(steerAngle) * 20;
            }
        }

        public void OnObstacleInfoUpdated(ObstacleInfoEventArgs obstacleInfoEvent) {
            _obstacleInfoEventArgs = obstacleInfoEvent;
        }

        private FreeFallStateChangedEventArgs freeFallEventArgs;
        public void OnFreeFallingStateChanged(FreeFallStateChangedEventArgs eventArgs) {
            freeFallEventArgs = eventArgs;
        }

        private float angleRecommendation = 0;
        private float angleRecommendationCollisionLength = 0;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recomomendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }

            if (Math.Abs(e.recomomendations[0].Item2)>360) {
                Debug.Log($"Angle recommendation buggy: {e.recomomendations[0].Item2}");
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