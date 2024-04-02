using System;
using UnityEngine;

namespace Actuators
{
    /**
     * For more information about the variables, please refer to
     * image file named "angular obstacle hit prediction calculation.psd"
     */
    public class CarAngularSteeringDegreeCalculator
    {
        // known-constants
        private double P1;
        private double P2;
        private double P3;
        private double L;
        private double Lo;

        // inputs
        private double Theta1;
        private double d;  // distance of front vehicle towards the obstacle

        // calculated
        private double Theta2;
        private double Ra;  // angular radius
        private double Ro;  // radius to the obstacle
        private double Rv1;  // vehicle radius 1
        private double Rv2;  // vehicle radius 2

        private readonly double minimumAlpha1ToBeAssumedNotStraightAngle = 1;
        private bool inputUpdated;

        public CarAngularSteeringDegreeCalculator(double p1, double p2, double p3, double l, double lo) {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            L = l;
            Lo = lo;
            inputUpdated = true;
        }

        private double angleSign = -1;  // -1 if going left, +1 if going right


        // NOTE: alpha1 & theta1 represents left wheel IIF turning left (negative degree).
        // They represents right wheel IIF turning right (positive degree).
        public void updateSteeringDirection(float alpha1Degree) {
            angleSign = Math.Sign(alpha1Degree);
            Theta1 = calculateTheta1FromAlpha1(alpha1Degree);
            inputUpdated = true;
        }
        public double calculateTheta1FromAlpha1(double alpha1Degree) {
            alpha1Degree = Math.Abs(alpha1Degree);
            if (alpha1Degree < minimumAlpha1ToBeAssumedNotStraightAngle) {  // prevent division by zero
                return 90;
            }
            return toRad(90 - alpha1Degree);
        }

        public void updateObstacleDistance(double obstacleDistance) {  // rads
            d = obstacleDistance;
            inputUpdated = true;
        }


        /**
         * NOTE: alpha1 and theta1 represents left wheel IIF turning left (negative degree).
         * They represents right wheel IIF turning right (positive degree).
         */
        public double getLeftSteeringDegree() {
            if (angleSign < 0)
                return -(90 - toDeg(Theta1));
            return getUnsignedAlpha2();
        }
        public double getRightSteeringDegree() {
            if (angleSign < 0)
                return -getUnsignedAlpha2();
            return 90 - toDeg(Theta1);
        }
        public double getUnsignedAlpha2() {
            if (Math.Abs(Theta1 - 90) < 0.001)
                return 0;
            updateAll();
            return 90 - toDeg(Theta2);
        }
        public bool willHitObstacle(double? Alpha1, double? obstacleDistance) {
            var obsDistance = this.d;
            var theta1 = this.Theta1;
            if (Alpha1 != null)
                theta1 = calculateTheta1FromAlpha1(Alpha1.Value);
            if (obstacleDistance != null)
                obsDistance = obstacleDistance.Value;

            if (Math.Abs(theta1 - 90) < 0.001 && !Double.IsPositiveInfinity(obsDistance))  // if going straight, prevent potential zero division error
                return true;
            var ra = calculateRaFromTheta1(theta1);
            var ro = calculateRo(ra, obsDistance);
            var rv1 = calculateRv1(ra);
            var rv2 = calculateRv2(ra);
            Debug.Assert(ra < ro + Lo);
            return (ro < rv1) || (ro < rv2);
        }

        private void updateAll() {
            if (!inputUpdated)
                return;
            Ra = calculateRaFromTheta1(Theta1);
            Theta2 = calculateTheta2(Ra);
            Ro = calculateRo(Ra, d);
            Rv1 = calculateRv1(Ra);
            Rv2 = calculateRv2(Ra);
            inputUpdated = false;
        }


        private double calculateRaFromTheta1(double Theta1) {
            return P2 * Math.Tan(Theta1);
        }

        private double calculateTheta2(double Ra) {
            return Math.Atan((Ra + L) / P2);
        }

        private double calculateRo(double Ra, double d) {
            return Math.Sqrt(square(Ra) + square(P1 + P2 + d)) - Lo;
        }
        private double calculateRv1(double Ra) {
            return Math.Sqrt(square(Ra + L) + square(P1 + P2));
        }
        private double calculateRv2(double Ra) {
            return Math.Sqrt(square(Ra + L) + square(P3));
        }

        private double square(double x) {
            return x * x;
        }

        public float getRecommendedAlpha1ToAvoidObstacle(double? obstacleDistance=null, int maxDegree=45, float onImpossibleDefaultValue=45) {
            var obsDist = this.d;
            if (obstacleDistance != null)
                obsDist = obstacleDistance.Value;

            for (int i = 1; i <= maxDegree; i++) {
                if (!willHitObstacle(Alpha1: i, obstacleDistance: obsDist))
                    return i;
            }

            return onImpossibleDefaultValue;  // when no possible angle found
        }

        private static double toDeg(double rads) {
            return rads / Math.PI * 180;
        }
        private static double toRad(double deg) {
            return deg * Math.PI / 180;
        }
    }
}