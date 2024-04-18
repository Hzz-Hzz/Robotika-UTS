using System;
using System.Collections;
using System.Linq;
using System.Threading;
using DefaultNamespace;
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
        public GameObject circularUltrasonic;

        private ConstSpeedTorqueManager _torqueManager;
        private ObstacleInfoEventArgs _obstacleInfoEventArgs;
        private SpeedSensor _speedSensor;

        private Quaternion originalCameraRotationRelativeToParent;
        private Quaternion originalCircularUltrasonicRotationRelativeToParent;
        public GameObject camera;
        public SteerDirectionManager cameraRotationManager = new SteerDirectionManager(35, 30, 2);
        private float targetCarAngleBasedOnRecommendationAndCamRotation =>
            cameraRotationManager.getSteerAngle()*0.4f + angleRecommendation;
        private float targetCameraAngleBasedOnRecommendationAndCamRotation =>
            cameraRotationManager.getSteerAngle()*0.5f + angleRecommendation;



        private const float backwardTorque = 120;
        private const float carLength = 2.2f;
        private CarAngularSteeringDegreeCalculator _angularDegreeSteeringCalculator = new CarAngularSteeringDegreeCalculator(
            0.7, 2.88, 0.83, carLength, 3.5 + 0.2);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
            _obstacleInfoEventArgs = ObstacleInfoEventArgs.NoObstacle(null, -1);
            _speedSensor = new SpeedSensor(rigidBodyForSpeedSensor);
            originalCameraRotationRelativeToParent = camera.transform.localRotation;
            originalCircularUltrasonicRotationRelativeToParent = circularUltrasonic.transform.localRotation;

            _torqueManager = new ConstSpeedTorqueManager(_speedSensor, 350, 16, 50,
                0.5f, 50);
        }



        private void Update() {
            setBrake(0);
            modifyMaxSpeedBasedOnSlope();
            updateSteerAngle();
            motorTorque = _torqueManager.getMotorTorque(targetCarAngleBasedOnRecommendationAndCamRotation);
            brakeIfFreeFalling();

            handleGoingBackwardBecauseWhenObstacleAlreadyHit();
            updateCameraRotation();
            CustomLogger.Log($"Recommended: (angle:{targetCarAngleBasedOnRecommendationAndCamRotation:00.00},l:{angleRecommendationCollisionLength:00.00}) " +
                      $"actualAngle: {steerAngle:00.00} " +
                      $"Speed: {_speedSensor.getCurrentSpeed():00.00}  torque: {motorTorque:00.00}");
        }

        private void updateSteerAngle() {
            var angle = targetCarAngleBasedOnRecommendationAndCamRotation * 0.75f;
            angle = turnLeftOrRightIfObstacleWillBeHit(angle);
            if (Math.Abs(angle) <= 45) {
                steerAngle = angle;
            }
            else {
                steerAngle = Math.Sign(angle) * 45;
                CustomLogger.Log($"Angle was too extreme ({angle}). Capping it to +- 45...");
            }
        }

        private float lastTimeObstacleWasFound = 13.0f;

        private const float EXTREME_SLOPE = 5f;
        private const float MEDIUM_SLOPE = 0.25f;
        private void modifyMaxSpeedBasedOnSlope() {
            var slope = gyroscope.getSlope();
            if (slope >= EXTREME_SLOPE)
                _torqueManager.maxSpeed = 10;
            else if (slope >= MEDIUM_SLOPE)
                _torqueManager.maxSpeed = 10;
            else if (Time.time - lastTimeObstacleWasFound < 1)
                _torqueManager.maxSpeed = 12;
            else _torqueManager.maxSpeed = 16;
        }

        private Direction getDirectionThatWillPutTheCarInMiddleOfRoad() {

            if (_angleRecommendationReceivedEventArgs.isOffRoad)
                return Direction.DEFAULT;
            if (freeFallEventArgs.numOfFreeFall > 0)
                return Direction.DEFAULT;

            var ratio = getLeftRightRatio();
            var ratioLeft = ratio.Item3;
            var tooCloseToLeftSide = ratioLeft < 0.5;
            var tooCloseToRightSide = ratioLeft > 0.5;
            CustomLogger.Log($"too extreme slope, going middle. ({ratio.Item1},{ratio.Item2})");

            if (!tooCloseToLeftSide && !tooCloseToRightSide) {
                return Direction.DEFAULT;
            }

            if (tooCloseToLeftSide)
                return Direction.RIGHT;
            return Direction.LEFT;
        }

        [CanBeNull]
        private Tuple<float,float,float> getLeftRightRatio() {
            if (_angleRecommendationReceivedEventArgs == null)
                return null;
            var verticallyClosest = _angleRecommendationReceivedEventArgs.verticallyClosestRoadLeftRightEdge;
            if (verticallyClosest.Item1 == null || verticallyClosest.Item2 == null)
                return null;
            var leftX = Math.Abs(verticallyClosest.Item1.Value.x);
            var rightX = Math.Abs(verticallyClosest.Item2.Value.x);
            var total = leftX + rightX;
            var ratioLeft = leftX / total;
            return new Tuple<float, float, float>(leftX, rightX, ratioLeft);
        }

        private float getAngleThatWillPutTheCarInMiddleOfRoad(float angle) {
            var dir = getDirectionThatWillPutTheCarInMiddleOfRoad();
            CustomLogger.Log($"Going to middle is modifying the direction to {dir.ToString()}");
            if (dir == Direction.DEFAULT)
                return 0;
            var ratio = getLeftRightRatio();
            if (dir == Direction.RIGHT)
                return 5 * (float)Math.Atan(1 / ratio.Item3);
            return -5  * (float)Math.Atan(ratio.Item3);
        }

        private void brakeIfFreeFalling() {
            if (freeFallEventArgs.numOfFreeFall < 3) {
                leftBack.brakeTorque = 0;
                rightBack.brakeTorque = 0;
                return;
            }
            CustomLogger.Log("Free falling");
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
            // circularUltrasonic.transform.localRotation = originalCircularUltrasonicRotationRelativeToParent
                                                         // *Quaternion.AngleAxis(cameraRotationManager.getSteerAngle()*0.2, Vector3.up);
            Debug.DrawLine(camera.transform.position,
                camera.transform.position +
                Quaternion.AngleAxis(cameraRotationManager.getSteerAngle(), Vector3.up)*this.transform.forward.normalized*10,
                Color.yellow);
        }

        private float doNotUseRoadRecommendation = -1;
        private readonly float additionalDegreeToMakeSure = 4;



        private float turnLeftOrRightIfObstacleWillBeHit(float angle) {
            if (_angleRecommendationReceivedEventArgs == null) {
                CustomLogger.Log("_angleRecommendationReceivedEventArgs is null... Not modifying the angle");
                return angle;
            }
            // top priority, the vehicle could fall down otherwise.
            if (_angleRecommendationReceivedEventArgs.isOffRoad) {
                CustomLogger.Log("Off road... Not modifying the angle");
                return angle;
            }

            _angularDegreeSteeringCalculator.updateObstacleDistance(_obstacleInfoEventArgs.forwardObstacleDistance ?? Double.PositiveInfinity);
            var targetCarRotationIsExtreme = Math.Abs(targetCarAngleBasedOnRecommendationAndCamRotation) > 15
                                             && angleRecommendationCollisionLength >= 2;


            if (_additionalUltrasonic.rearLeftObstacleDetected && angle < 0) {
                refreshObstacleFoundTimeTracker();
                var obstDist = _additionalUltrasonic.rearLeft.detectDistance();
                var fromAngle = angle;
                angle = additionalDegreeToMakeSure;
                CustomLogger.Log($"Rear-left obstacle detected at {obstDist}. Modifying angle from {fromAngle} to {angle}");
            }else if (_additionalUltrasonic.rearRightObstacleDetected && angle > 0) {
                refreshObstacleFoundTimeTracker();
                var obstDist = _additionalUltrasonic.rearRight.detectDistance();
                var fromAngle = angle;
                angle = additionalDegreeToMakeSure;
                angle = -angle;
                CustomLogger.Log($"Rear-right obstacle detected at {obstDist}. Modifying angle from {fromAngle} to {angle}");
            }

            if (_additionalUltrasonic.frontObstacleDetected
                && _angularDegreeSteeringCalculator.willHitObstacle(targetCarAngleBasedOnRecommendationAndCamRotation, _additionalUltrasonic.frontObstacleDistance)
            ) {
                refreshObstacleFoundTimeTracker();
                CustomLogger.Log($"Front-obstacle detected and will be hit");
                var sign = Math.Sign(targetCarAngleBasedOnRecommendationAndCamRotation);
                sign = (sign == 0) ? -1 : sign;
                var frontObstacle = _additionalUltrasonic.frontObstacleDistance;
                var calculatedAngle = _angularDegreeSteeringCalculator.getRecommendedAlpha1ToAvoidObstacle(
                    frontObstacle, maxDegree: 60,  // will be capped
                    onImpossibleDefaultValue: Single.NaN);
                angle = sign * (additionalDegreeToMakeSure + calculatedAngle);
                if (!Single.IsNaN(angle)) {
                    CustomLogger.Log($"Modifying angle to {angle}. targetCarAngleBasedOnRecommendationAndCamRotation={targetCarAngleBasedOnRecommendationAndCamRotation:00.00}," +
                                     $"sign={sign}, calculatedAngle={calculatedAngle:00.00}, angle={angle:00.00}");
                    return angle;
                }

                var left = _additionalUltrasonic.frontLeft.detectDistance(-1);
                var right = _additionalUltrasonic.frontRight.detectDistance(-1);
                var turnDegree = 45;
                // var turnDegree = Math.Min(45f / frontObstacleDistance!.Value, 45f);
                setBrake(_speedSensor.getCurrentSpeed() * 10);

                if (left != -1 && right != -1) {
                    return sign * 45;
                    // StartCoroutine(motorTorqueCoroutine(2, -backwardTorque, null));
                    // CustomLogger.Log($"Both left right has obstacle, going backward...");
                    // return 0;
                }
                if (left == -1) {  // no obstacle left side
                    turnDegree = -turnDegree;  // going left
                }
                CustomLogger.Log($"Impossible to achieve the angle. Will go to the other side {turnDegree}. Obst: {left},{right}");
                return turnDegree;
            }

            return angle;
        }

        private void setBrake(float brake) {
            leftBack.brakeTorque = brake;
            rightBack.brakeTorque = brake;
        }

        private void refreshObstacleFoundTimeTracker() {
            lastTimeObstacleWasFound = Math.Max(Time.time, lastTimeObstacleWasFound);
        }

        [CanBeNull] private CancellationTokenSource motorTorqueCoroutinCancellation = null;
        private IEnumerator motorTorqueCoroutine(float duration, float torque, [CanBeNull] Func<bool> keepRunning) {
            motorTorqueCoroutinCancellation?.Cancel();
            motorTorqueCoroutinCancellation = new CancellationTokenSource();
            var cancellationToken = motorTorqueCoroutinCancellation.Token;
            var prevTorque = motorTorque;
            var end = Time.time + duration;
            while (Time.time < end && !cancellationToken.IsCancellationRequested && (keepRunning?.Invoke() ?? true)) {
                motorTorque = torque;
                yield return null;
            }
            motorTorque = prevTorque;
            motorTorqueCoroutinCancellation = null;
            yield break;
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
            Func<bool> runningCriteria = () =>
                _angularDegreeSteeringCalculator.willHitObstacle(40, _additionalUltrasonic.frontObstacleDistance);

            if (_additionalUltrasonic.frontObstacleDistance != null
                && _additionalUltrasonic.frontObstacleDistance < 1
                && _speedSensor.getCurrentSpeed() < 0.5
                && motorTorque > 0
            ) {  // already hit the object and cannot move
                refreshObstacleFoundTimeTracker();
                CustomLogger.Log("OBSTACLE WAS HIT");
                StartCoroutine(motorTorqueCoroutine(5, -backwardTorque, runningCriteria));
            }
        }

        public void OnObstacleInfoUpdated(ObstacleInfoEventArgs obstacleInfoEvent) {
            _obstacleInfoEventArgs = obstacleInfoEvent;
            refreshObstacleFoundTimeTracker();
        }
        public void OnObstacleHit() {
            Func<bool> runningCriteria = () =>
                _angularDegreeSteeringCalculator.willHitObstacle(40, _additionalUltrasonic.frontObstacleDistance);
            if (_speedSensor.getCurrentSpeed() < 0.5 && motorTorque >= 0 && motorTorqueCoroutinCancellation == null)
                StartCoroutine(motorTorqueCoroutine(5, -backwardTorque, runningCriteria));
            CustomLogger.Log("Obstacle hit detection from circular ultrasonic");
        }

        private FreeFallStateChangedEventArgs freeFallEventArgs;
        public void OnFreeFallingStateChanged(FreeFallStateChangedEventArgs eventArgs) {
            freeFallEventArgs = eventArgs;
        }

        private float angleRecommendation = 0;
        private float angleRecommendationCollisionLength = 0;
        private AngleRecommendationReceivedEventArgs _angleRecommendationReceivedEventArgs;
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recommendationsWithObstacle.Count == 0) {
                CustomLogger.Log("recommendationsWithObstacle array is empty");
                return;
            }
            if (Math.Abs(e.recommendationsWithObstacle[0].Item2)>360) {
                CustomLogger.Log($"Angle recommendationsWithObstacle buggy: {e.recommendationsWithObstacle[0].Item2}");
                return;
            }
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
                return Math.Max(leftFront.steerAngle, rightFront.steerAngle);
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
                return Math.Max(leftBack.motorTorque, rightBack.motorTorque);
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