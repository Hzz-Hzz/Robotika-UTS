using System;
using Sensor.models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Sensor
{
    public class SuspensionForceSensor: MonoBehaviour
    {
        public UnityEvent<FreeFallStateChangedEventArgs> FreeFallingStateChanged;
        public WheelCollider[] _wheelColliders;

        private bool isFreeFalling = false;

        private void Start() {
            FreeFallingStateChanged?.Invoke(new FreeFallStateChangedEventArgs(){isFreeFalling = isFreeFalling});
        }

        private void Update() {
            var prevIsFreeFalling = isFreeFalling;
            int numOfFreeFall = 0;

            WheelHit wheelHit;
            foreach (var wheelCollider in _wheelColliders) {
                if (!wheelCollider.GetGroundHit(out wheelHit)) {
                    numOfFreeFall++;
                }
            }

            var stateChanged = prevIsFreeFalling != isFreeFalling;
            if (stateChanged)
                FreeFallingStateChanged?.Invoke(new FreeFallStateChangedEventArgs() {
                    isFreeFalling = (numOfFreeFall!=0),
                    numOfFreeFall = numOfFreeFall,
                    percentageOfFreeFall = (float)(numOfFreeFall) / _wheelColliders.Length
                });
        }
    }
}