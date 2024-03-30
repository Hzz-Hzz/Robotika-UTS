using System;
using EventsEmitter.models;
using UnityEngine;

namespace Actuators

{
    public class CarCameraRotator : MonoBehaviour
    {
        private SteerDirectionManager _steerDirectionManagerObstacles = new SteerDirectionManager(45, 240);
        private Camera _carCameraGameObject;

        private void Update()
        { 
            Debug.Log("Rotating " + gameObject.name + "...");
            transform.Rotate(0, 1, 0);
        }
    }
}