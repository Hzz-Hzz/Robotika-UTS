using System;
using System.Linq;
using EventsEmitter.models;
using JetBrains.Annotations;
using Sensor;
using Sensor.models;
using UnityEngine;
using UnityEngine.Serialization;
using Gyroscope = Sensor.Gyroscope;

namespace Actuators
{
    public class CarActuatorManager : MonoBehaviour
    {
        public WheelCollider leftBack;
        public WheelCollider leftFront;
        public WheelCollider rightBack;
        public WheelCollider rightFront;
        public Rigidbody rigidBodyForSpeedSensor;
        public Gyroscope gyroscope;

        private ConstSpeedTorqueManager _torqueManager;
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Quaternion originalCameraRotationRelativeToParent;
        private float targetCameraRotation = 0.0f;
        public GameObject camera;
        private SteerDirectionManager cameraRotationManager = new SteerDirectionManager(15, 4, 10);
        private float targetCarAngleBasedOnRecommendationAndCamRotation =>
            cameraRotationManager.getSteerAngle()*0.5f + angleRecommendation;


        private const float backwardTorque = 80;
        private const float carLength = 1.98f;
        private CarAngularSteeringDegreeCalculator _angularDegreeSteeringCalculator = new CarAngularSteeringDegreeCalculator(
            0.7, 2.88, 0.83, carLength, 3.5 + 0.1);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null, -1);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
            originalCameraRotationRelativeToParent = camera.transform.localRotation;

            _torqueManager = new ConstSpeedTorqueManager(_speedSensor, 350, -1, 50,
                0.6f, 45);
        }

        private float? goBackwardUntilTimestamp = null;

        private const float EXTREME_SLOPE = 2f;
        private const float MEDIUM_SLOPE = 0.25f;

        private void Update() {
            var slope = gyroscope.getSlope();
            if (slope >= EXTREME_SLOPE)
                _torqueManager.maxSpeed = 8;
            else if (slope >= MEDIUM_SLOPE)
                _torqueManager.maxSpeed = 8;
            else _torqueManager.maxSpeed = 10;

             // unused for now
            _speedSensor ??= new SpeedSensor(rigidBodyForSpeedSensor);

            var angle = targetCarAngleBasedOnRecommendationAndCamRotation * 0.5f;
            // angle = (Math.Abs(angle) < 4)? 0f : angle - 4;
            if (slope >= EXTREME_SLOPE)
                angle = getAngleThatWillPutTheCarInMiddleOfRoad(angle);
            angle = turnLeftOrRightIfObstacleWillBeHit(angle);

            steerAngle = (Math.Abs(angle)<=45)? angle : Math.Sign(angle)*45;
            motorTorque = _torqueManager.getMotorTorque(targetCarAngleBasedOnRecommendationAndCamRotation);
            brakeIfFreeFalling();

            handleGoingBackwardBecauseWhenObstacleAlreadyHit();
            updateCameraRotation();
            Debug.Log($"Recommended: (angle:{targetCarAngleBasedOnRecommendationAndCamRotation:00.00},l:{angleRecommendationCollisionLength:00.00}) " +
                      $"actualAngle: {steerAngle:00.00} " +
                      $"Speed: {_speedSensor.getCurrentSpeed():00.00}  ObsDist: {_obstacleInfoEventArgs.forwardObstacleDistance:00.00}");
        }

        private float getAngleThatWillPutTheCarInMiddleOfRoad(float angle) {
            if (_angleRecommendationReceivedEventArgs == null)
                return angle;
            if (_angleRecommendationReceivedEventArgs.isOffRoad)
                return angle;
            Debug.Log("too extreme slope, going middle");
            var verticallyClosest = _angleRecommendationReceivedEventArgs.verticallyClosestRoadLeftRightEdge;
            if (verticallyClosest.Item1 == null || verticallyClosest.Item2 == null)
                return angle;
            var leftX = Math.Abs(verticallyClosest.Item1.Value.x);
            var rightX = Math.Abs(verticallyClosest.Item2.Value.x);
            var total = leftX + rightX;
            var ratioLeft = leftX / total;
            var tooCloseToLeftSide = ratioLeft < 0.4;
            var tooCloseToRightSide = ratioLeft > 0.6;
            if (!tooCloseToLeftSide && !tooCloseToRightSide) {
                return 0;
            }

            if (tooCloseToLeftSide)
                return 5;
            return -5;
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
        private readonly float additionalDegreeToMakeSure = 10;
        private float turnLeftOrRightIfObstacleWillBeHit(float angle) {
            if (_angleRecommendationReceivedEventArgs?.isOffRoad ?? false)  // top priority, the vehicle could fall down otherwise
                return angle;

            _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            var targetCarRotationIsExtreme = Math.Abs(targetCarAngleBasedOnRecommendationAndCamRotation) > 15
                                             && angleRecommendationCollisionLength >= 2;
            if (!_obstacleInfoEventArgs.allowedToGoLeft && angle < 0)
                angle = 2;
            if (!_obstacleInfoEventArgs.allowedToGoRight && angle > 0)
                angle = -2;


            if (_obstacleInfoEventArgs.allowedToGoForward) {
                return angle;
            }
            Debug.Log(_obstacleInfoEventArgs.obstacleId);

            Debug.Assert(_obstacleInfoEventArgs.forwardObstacleDistance != null);
            var obstacleHitsMiddleSensor =
                (_obstacleInfoEventArgs.allowedToGoLeft == _obstacleInfoEventArgs.allowedToGoRight);

            var direction = Direction.ANY;
             if (_angleRecommendationReceivedEventArgs != null) {
                 var obstacleRelativePos = new Vector2(0, _obstacleInfoEventArgs.forwardObstacleDistance.Value);
                 var sensor = _obstacleInfoEventArgs.sensor;
                 if (sensor.midSensorResult != null) // obstacle hits the middle
                     obstacleRelativePos.x = 0;
                 else if (sensor.leftSensorResults[0] != null)
                     obstacleRelativePos.x = -carLength/2;
                 else if (sensor.rightSensorResults[0] != null)
                     obstacleRelativePos.x = carLength/2;
                 direction = _angleRecommendationReceivedEventArgs.getDirectionRecommendationToAvoidObstacle(
                     _obstacleInfoEventArgs.obstacleId, cameraRotationManager.getSteerAngle(), obstacleRelativePos);
             }
            if (direction == Direction.ANY)
                angle = getAngleForObstacleAvoidingDefaultBehaviour(angle);
            else {
                var sign = (direction == Direction.LEFT) ? -1 : 1;
                angle = sign * (additionalDegreeToMakeSure
                                + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle());
            }

            return angle;
        }

        private float getAngleForObstacleAvoidingDefaultBehaviour(float angle) {
            var targetAngle = additionalDegreeToMakeSure + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();
            if (!_obstacleInfoEventArgs.allowedToGoLeft && !_obstacleInfoEventArgs.allowedToGoRight) {
                angle = _obstacleInfoEventArgs.sensor.slopeOfObstacleIsGoingRightForward()!.Value
                    ? targetAngle : -targetAngle;
            }else if (!_obstacleInfoEventArgs.allowedToGoLeft)
                angle = targetAngle;
            else if (!_obstacleInfoEventArgs.allowedToGoRight)
                angle = -targetAngle;
            else  // can go left or right
                angle = Math.Sign(targetCarAngleBasedOnRecommendationAndCamRotation) * targetAngle;

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
        private AngleRecommendationReceivedEventArgs _angleRecommendationReceivedEventArgs;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recomomendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }

            if (Math.Abs(e.recomomendations[0].Item2)>360) {
                Debug.Log($"Angle recommendation buggy: {e.recomomendations[0].Item2}");
                return;
            }
            _angleRecommendationReceivedEventArgs = e;
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