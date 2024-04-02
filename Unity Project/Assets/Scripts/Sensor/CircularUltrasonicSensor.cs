using System;
using System.Collections.Generic;
using System.Linq;
using Actuators;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

namespace Sensor
{
    public class CircularUltrasonicSensor: MonoBehaviour
    {
        public UnityEvent ObstacleHit;
        public CarActuatorManager carActuatorManager;

        // number of this array should be numberOfSensor + 2. this first & last index won't be a sensor
        public int[] degrees;

        private UltrasonicSensor[] ultrasonics;

        private void Start() {
            var temporaryDegrees = degrees.ToList();
            temporaryDegrees.Sort();
            degrees = temporaryDegrees.ToArray();
            ultrasonics = new UltrasonicSensor[degrees.Length];

            // exclude first and last index
            for (int i = 1; i < degrees.Length-1; i++) {
                var degree = degrees[i];
                var gameobjName = $"anchor{degree}/{degree}";
                var child = transform.Find(gameobjName);
                if (child == null)
                    throw new Exception($"Cannot find {gameobjName}");
                ultrasonics[i] = child.GetComponent<UltrasonicSensor>() ;
            }
        }

        public List<Tuple<System.Numerics.Vector2, System.Numerics.Vector2>> getObstacles() {
            var rotation = -carActuatorManager.cameraRotationManager.getSteerAngle() * 0.5;

            List<Tuple<System.Numerics.Vector2, System.Numerics.Vector2>> ret = new();
            for (int i = 1; i < ultrasonics.Length-1; i++) {  // exclude first and last index because they're null
                var distance = ultrasonics[i].detectDistance();
                if (distance == null)
                    continue;
                if (distance < 0.5)
                    ObstacleHit?.Invoke();
                var leftPoint = getBoundaries(i, i - 1, distance.Value, (float)rotation);
                var rightPoint = getBoundaries(i, i+1, distance.Value, (float)rotation);
                if (leftPoint != null)
                    ret.Add(leftPoint);
                if (rightPoint != null)
                    ret.Add(rightPoint);
            }
            return ret;
        }

        [CanBeNull]
        private Tuple<System.Numerics.Vector2, System.Numerics.Vector2> getBoundaries(
            int indexStart, int indexEnd, float raycastDistance, float additionalRotation
        ) {
            if (indexEnd >= degrees.Length || indexEnd < 0 || indexStart >= degrees.Length || indexStart < 0)
                return null;
            float degreeStart = degrees[indexStart];
            float degreeEnd = degrees[indexEnd];
            // degreeEnd = degreeStart + (degreeEnd - degreeStart) / 2;

            degreeStart += additionalRotation;
            degreeEnd += additionalRotation;

            var xStart = (float)(raycastDistance * Math.Cos(toRad(90 - degreeStart)));
            var yStart = (float)(raycastDistance * Math.Sin(toRad(90 - degreeStart)));
            var startingPoint = new System.Numerics.Vector2(xStart, yStart);

            var xEnd = (float)(raycastDistance * Math.Cos(toRad(90 - degreeEnd)));
            var yEnd = (float)(raycastDistance * Math.Sin(toRad(90 - degreeEnd)));
            var endingPoint = new System.Numerics.Vector2(xEnd, yEnd);
            return new Tuple<System.Numerics.Vector2, System.Numerics.Vector2>(startingPoint, endingPoint);
        }

        private double toRad(float deg) {
            return Math.PI * deg / 180;
        }
    }
}