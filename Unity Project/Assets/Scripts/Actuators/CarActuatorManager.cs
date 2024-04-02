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
        public AdditionalUltrasonicSensor _additionalUltrasonic;

        private ConstSpeedTorqueManager _torqueManager;
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Quaternion originalCameraRotationRelativeToParent;
        public GameObject camera;
        public SteerDirectionManager cameraRotationManager = new SteerDirectionManager(35, 15, 10);
        private float targetCarAngleBasedOnRecommendationAndCamRotation =>
            cameraRotationManager.getSteerAngle()*0.4f + angleRecommendation;
        private float targetCameraAngleBasedOnRecommendationAndCamRotation =>
            cameraRotationManager.getSteerAngle()*0.5f + angleRecommendation;



        private const float backwardTorque = 80;
        private const float carLength = 2.2f;
        private CarAngularSteeringDegreeCalculator _angularDegreeSteeringCalculator = new CarAngularSteeringDegreeCalculator(
            0.7, 2.88, 0.83, carLength, 3.5 + 0.2);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null, -1);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
            originalCameraRotationRelativeToParent = camera.transform.localRotation;

            _torqueManager = new ConstSpeedTorqueManager(_speedSensor, 350, -1, 50,
                0.45f, 45);
        }

        private float? goBackwardUntilTimestamp = null;

        private const float EXTREME_SLOPE = 5f;
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

            var angle = targetCarAngleBasedOnRecommendationAndCamRotation * 0.75f;
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

        private Direction getDirectionThatWillPutTheCarInMiddleOfRoad() {
            if (_angleRecommendationReceivedEventArgs == null)
                return Direction.DEFAULT;
            if (_angleRecommendationReceivedEventArgs.isOffRoad)
                return Direction.DEFAULT;
            if (freeFallEventArgs.numOfFreeFall > 0)
                return Direction.DEFAULT;
            Debug.Log("too extreme slope, going middle");
            var verticallyClosest = _angleRecommendationReceivedEventArgs.verticallyClosestRoadLeftRightEdge;
            if (verticallyClosest.Item1 == null || verticallyClosest.Item2 == null)
                return Direction.DEFAULT;
            var leftX = Math.Abs(verticallyClosest.Item1.Value.x);
            var rightX = Math.Abs(verticallyClosest.Item2.Value.x);
            var total = leftX + rightX;
            var ratioLeft = leftX / total;
            var tooCloseToLeftSide = ratioLeft < 0.5;
            var tooCloseToRightSide = ratioLeft > 0.5;
            if (!tooCloseToLeftSide && !tooCloseToRightSide) {
                return Direction.DEFAULT;
            }

            if (tooCloseToLeftSide)
                return Direction.RIGHT;
            return Direction.LEFT;
        }

        private float getAngleThatWillPutTheCarInMiddleOfRoad(float angle) {
            var dir = getDirectionThatWillPutTheCarInMiddleOfRoad();
            if (dir == Direction.DEFAULT)
                return 0;
            if (dir == Direction.RIGHT)
                return 4;
            return -4;
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
            var target = (Math.Abs(targetCameraAngleBasedOnRecommendationAndCamRotation) < 3)?
                0 : targetCameraAngleBasedOnRecommendationAndCamRotation;
            cameraRotationManager.updateSteerBasedOnAngle(target);
            camera.transform.localRotation = originalCameraRotationRelativeToParent
                                        *Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up);
            Debug.DrawLine(camera.transform.position,
                camera.transform.position +
                Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up)*this.transform.forward.normalized*10,
                Color.yellow);
        }

        private float doNotUseRoadRecommendation = -1;
        private readonly float additionalDegreeToMakeSure = 4;



        private float turnLeftOrRightIfObstacleWillBeHit(float angle) {
            if (_angleRecommendationReceivedEventArgs == null)
                return angle;
            if (_angleRecommendationReceivedEventArgs.isOffRoad)  // top priority, the vehicle could fall down otherwise
                return angle;

            _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            var targetCarRotationIsExtreme = Math.Abs(targetCarAngleBasedOnRecommendationAndCamRotation) > 15
                                             && angleRecommendationCollisionLength >= 2;


            if (_additionalUltrasonic.rearLeftObstacleDetected && angle < 0)
                angle = 3;
            else if (_additionalUltrasonic.rearRightObstacleDetected && angle > 0)
                angle = -3;

            if (_additionalUltrasonic.frontObstacleDetected
                && _angularDegreeSteeringCalculator.willHitObstacle(targetCarAngleBasedOnRecommendationAndCamRotation, _additionalUltrasonic.frontObstacleDistance)
            ) {
                angle = Math.Sign(targetCarAngleBasedOnRecommendationAndCamRotation) *
                        (additionalDegreeToMakeSure + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle(
                            _additionalUltrasonic.frontObstacleDistance,
                            onImpossibleDefaultValue: Single.NaN));
                if (!Single.IsNaN(angle))
                    return angle;

                // if impossible, go to other direction
                var left = _additionalUltrasonic.frontLeft.detectDistance(Single.PositiveInfinity);
                var right = _additionalUltrasonic.frontRight.detectDistance(Single.PositiveInfinity);
                var turnDegree = 45;
                // var turnDegree = Math.Min(45f / frontObstacleDistance!.Value, 45f);
                if (left < right)
                    return turnDegree;
                return -turnDegree;
            }

            return angle;


            // if (avoidingDirection != null && _obstacleInfoEventArgs.obstacleId == avoidingDirectionObjectId) {
            //     var recommended = _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();
            //     if (_obstacleInfoEventArgs.forwardObstacleDistance == null)
            //         avoidingDirection = null;
            //     else return avoidingDirectionAngle;
            // }
            //
            // Debug.Log(_angleRecommendationReceivedEventArgs.verticallyClosestRoadLeftRightEdge);
            // if (_obstacleInfoEventArgs.allowedToGoForward) {
            //     return angle;
            // }
            // Debug.Log(_obstacleInfoEventArgs.obstacleId);
            //
            // Debug.Assert(_obstacleInfoEventArgs.forwardObstacleDistance != null);
            // var obstacleHitsMiddleSensor =
            //     (_obstacleInfoEventArgs.allowedToGoLeft == _obstacleInfoEventArgs.allowedToGoRight);
            //
            // var direction = Direction.DEFAULT;
            //  if (_angleRecommendationReceivedEventArgs != null) {
            //      var obstacleRelativePos = new Vector2(0, _obstacleInfoEventArgs.forwardObstacleDistance.Value);
            //      var sensor = _obstacleInfoEventArgs.sensor;
            //      if (_obstacleInfoEventArgs.whichForwardSensorEnum == ForwardSensorEnum.MID_FORWARD) // obstacle hits the middle
            //          obstacleRelativePos.x = 0;
            //      else if (_obstacleInfoEventArgs.whichForwardSensorEnum == ForwardSensorEnum.LEFT_FORWARD)
            //          obstacleRelativePos.x = -carLength/2;
            //      else if (_obstacleInfoEventArgs.whichForwardSensorEnum == ForwardSensorEnum.RIGHT_FORWARD)
            //          obstacleRelativePos.x = carLength/2;
            //
            //      var cameraAngle = cameraRotationManager.getSteerAngle();
            //      // if (Math.Abs(cameraAngle) > 10)
            //          direction = _angleRecommendationReceivedEventArgs.getDirectionRecommendationToAvoidObstacle(
            //              _obstacleInfoEventArgs.obstacleId, cameraAngle, obstacleRelativePos);
            //      // else direction = getDirectionThatWillPutTheCarInMiddleOfRoad();
            //
            //
            //      var impossibleToTurnToAnyDirection = _angularDegreeSteeringCalculator.willHitObstacle(45-additionalDegreeToMakeSure,
            //          _obstacleInfoEventArgs.forwardObstacleDistance);
            //      if (impossibleToTurnToAnyDirection)
            //          direction = Direction.DEFAULT;  // if not possible, fallback to default behaviour
            //  }
            // if (direction == Direction.DEFAULT)
            //     angle = getAngleForObstacleAvoidingDefaultBehaviour(angle);
            // else {
            //     var sign = (direction == Direction.LEFT) ? -1 : 1;
            //     angle = sign * (additionalDegreeToMakeSure
            //                     + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle());
            // }
            //
            // return angle;
        }

        private Direction? avoidingDirection = null;
        private int avoidingDirectionObjectId = -1;
        private float avoidingDirectionAngle = 0;
        private float getAngleForObstacleAvoidingDefaultBehaviour(float angle) {
            var targetAngle = additionalDegreeToMakeSure + _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();

            if (_obstacleInfoEventArgs.forwardObstacleDistance != null) {
                _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance.Value);
                var recommended = _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle();
                if (recommended < 45) {
                    avoidingDirection = targetCarAngleBasedOnRecommendationAndCamRotation > 0
                        ? Direction.RIGHT : Direction.LEFT;
                    avoidingDirectionObjectId = _obstacleInfoEventArgs.obstacleId;
                    if (avoidingDirection == Direction.LEFT && _obstacleInfoEventArgs.sensor.leftCollisions >
                        _obstacleInfoEventArgs.sensor.rightCollisions) {  // do nothing
                    }else if (avoidingDirection == Direction.RIGHT && _obstacleInfoEventArgs.sensor.rightCollisions >
                              _obstacleInfoEventArgs.sensor.leftCollisions) {
                    }
                    else {
                        avoidingDirectionAngle = avoidingDirection.getSign() * (recommended + additionalDegreeToMakeSure);
                        return avoidingDirectionAngle;
                    };
                }
            }

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
            if (_additionalUltrasonic.frontObstacleDistance != null
                && _additionalUltrasonic.frontObstacleDistance < 1
                && _speedSensor.getCurrentSpeed() < 0.5
            ) {  // already hit the object and cannot move
                goBackwardUntilTimestamp = Time.time + 2;
                doNotUseRoadRecommendation = Time.time + 6;
            }
            if (goBackwardUntilTimestamp != null && Time.time < goBackwardUntilTimestamp) {
                motorTorque = -backwardTorque;
                if (_speedSensor.isGoingForward(transform)) {
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
        public void OnObstacleHit() {
            var noActiveGoBackwardTimestamp =
                (goBackwardUntilTimestamp == null || goBackwardUntilTimestamp < Time.time);
            if (_speedSensor.getCurrentSpeed() < 0.5 && noActiveGoBackwardTimestamp)
                goBackwardUntilTimestamp = Time.time + 2;
        }

        private FreeFallStateChangedEventArgs freeFallEventArgs;
        public void OnFreeFallingStateChanged(FreeFallStateChangedEventArgs eventArgs) {
            freeFallEventArgs = eventArgs;
        }

        private float angleRecommendation = 0;
        private float angleRecommendationIgnoreObstacle = 0;
        private float angleRecommendationCollisionLength = 0;
        private AngleRecommendationReceivedEventArgs _angleRecommendationReceivedEventArgs;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recommendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }



            if (e.recommendationsWithObstacle.Count == 0) {
                Debug.Log("recommendationsWithObstacle array is empty");
                return;
            }
            if (Math.Abs(e.recommendationsWithObstacle[0].Item2)>360) {
                Debug.Log($"Angle recommendationsWithObstacle buggy: {e.recommendations[0].Item2}");
                return;
            }
            angleRecommendationIgnoreObstacle = (float) e.recommendations[0].Item2;
            // angleRecommendationIgnoreObstacle = (float) e.recommendationsWithObstacle[0].Item2;

            _angleRecommendationReceivedEventArgs = e;
            angleRecommendationCollisionLength = e.recommendationsWithObstacle[0].Item1;
            angleRecommendation = (float)e.recommendationsWithObstacle[0].Item2;
            var pos = transform.position;

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

public static class NumberUtility
{
    public static float? Min<T>(params float?[] items) where T :
        struct,
        IComparable,
        IComparable<T>,
        IConvertible,
        IEquatable<T>,
        IFormattable {
        float? minimum = null;
        foreach (var item in items) {
            minimum ??= item;
            if (item != null)
                minimum = Math.Min(minimum.Value, item.Value);
        }
        return minimum;
    }
}