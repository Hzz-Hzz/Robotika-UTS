using System;
using System.Collections;
using Sensor.models;
using UnityEngine;
using UnityEngine.Events;

namespace Sensor
{
    public class RotatableUltrasonicSensor: MonoBehaviour
    {
        public UnityEvent<UltrasonicSensorResultAvailable> OnScanResultAvailable;
        public UltrasonicSensor _ultrasonicSensor;
        public bool scanning = false;
        private float angleVelocity;
        private float angleStart;
        private float angleEnd;
        private float currentAngle;

        public RotatableUltrasonicSensor(float angleVelocity, float angleStart, float angleEnd) {
            this.angleVelocity = angleVelocity;
            this.angleStart = angleStart;
            this.angleEnd = angleEnd;
        }

        public IEnumerator startScanning() {
            scanning = true;
            while (true) {


                scanning = false;
            }
        }
    }
}