using System;
using UnityEngine;

namespace Sensor
{
    public class AdditionalUltrasonicSensor: MonoBehaviour
    {
        public UltrasonicSensor frontLeft;
        public UltrasonicSensor frontMid;
        public UltrasonicSensor frontRight;
        public UltrasonicSensor rearLeft;
        public UltrasonicSensor rearRight;


        public bool rearLeftObstacleDetected => rearLeft.detectDistance() != null;
        public bool rearRightObstacleDetected => rearRight.detectDistance() != null;
        public bool frontLeftObstacleDetected => frontLeft.detectDistance() != null;
        public bool frontMidObstacleDetected => frontMid.detectDistance() != null;
        public bool frontRightObstacleDetected => frontRight.detectDistance() != null;
        public float? frontObstacleDistance =>
            NumberUtility.Min<float>(frontLeft.detectDistance(), frontRight.detectDistance(), frontMid.detectDistance());

        public bool frontObstacleDetected => (frontLeftObstacleDetected || frontMidObstacleDetected);



        private void Update() {

        }
    }
}