using System;
using Sensor;

namespace Actuators
{
    public class ConstSpeedTorqueManager
    {
        public SpeedSensor speedSensor;
        public float maxSpeed { get; set; }
        public float steadyTorque { get; set; }
        public float maxMotorTorque = 30f;
        public float speedMultiplierAtExtreemeSteeringDegree = 0.1f;  // 10% by default
        public float extremeStreeingDegree = 55.0f;

        private int speedUp = 0;  // 0 means steady, 1 means speed up, -1 means go backward

        public ConstSpeedTorqueManager(SpeedSensor speedSensor,  float maxMotorTorque, float maxSpeed, float steadyTorque, float speedMultiplierAtExtreemeSteeringDegree, float extremeStreeingDegree) {
            this.speedSensor = speedSensor;
            this.steadyTorque = steadyTorque;
            this.maxSpeed = maxSpeed;
            this.maxMotorTorque = maxMotorTorque;
            this.speedMultiplierAtExtreemeSteeringDegree = speedMultiplierAtExtreemeSteeringDegree;
            this.extremeStreeingDegree = extremeStreeingDegree;
        }


        public float motorTorque => getMotorTorque(prevSteeringDegree);
        private float prevSteeringDegree;
        public float getMotorTorque(float steeringDegree) {
            prevSteeringDegree = steeringDegree;
            var targetSpeed = getTargetSpeed(steeringDegree);
            var currSpeed = speedSensor.getCurrentSpeed();
            if (currSpeed < targetSpeed * 0.9)
                return maxMotorTorque;
            if (currSpeed > targetSpeed * 1.1 && speedSensor.isGoingForward())
                return -maxMotorTorque;
            return steadyTorque;
        }

        /**
         * if steeringDegree == 0, then return motorTorque.
         * if abs(steeringDegree) == torquePercentageAtExtreemeSteeringDegree, then return torqueScaleAtExtreemeSteeringDegree * motorTorque
         *
         */
        public float getTargetSpeed(float steeringDegree)
        {
            steeringDegree = Math.Abs(steeringDegree);

            // assume f(x) = 1/(ax+c); x=steeringDegree,
            // we need to find A, C such that f(0)=1 and f(extremeStreeingDegree) = torqueScaleAtExtreemeSteeringDegree
            // f(0)=1 --> c = 1
            // f(x) = torqueScaleAtExtreemeSteeringDegree
            // --> a = 1/(torqueScaleAtExtreemeSteeringDegree * extremeStreeingDegree) - c/extremeStreeingDegree

            var c = 1;
            var a = 1 / (speedMultiplierAtExtreemeSteeringDegree * extremeStreeingDegree) - c / extremeStreeingDegree;
            // var a = 1 / (t * s) - c / s;
            var speedMultiplier = 1 / (a*steeringDegree + c);
            return (float)(speedMultiplier * maxSpeed);
        }
    }
}