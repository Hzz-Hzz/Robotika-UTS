using System;
using System.Linq;
using EventsEmitter.models;
using UnityEngine;

namespace Actuators
{
    public class CarAngleRecommendationActuator : MonoBehaviour
    {
        public WheelCollider leftBack;
        public WheelCollider leftFront;
        public WheelCollider rightBack;
        public WheelCollider rightFront;

        private MotorTorqueManager _motorTorqueManager = new MotorTorqueManager(7.0f, 0.1f, 30);
        private SteerDirectionManager _steerDirectionManager = new SteerDirectionManager(40, 25);


        private Vector3 direction;

        private void Start() {
            direction = gameObject.transform.position + Vector3.forward * 5;
        }

        private void Update() {
            // _steerDirectionManager.updateSteer(gameObject.transform, direction);
            // var angle = _steerDirectionManager.getSteerAngle();
            // var torque = _motorTorqueManager.getMotorTorque(angle);
            // leftFront.steerAngle = angle;
            // rightFront.steerAngle = angle;
            // leftBack.motorTorque = torque;
            // rightBack.motorTorque = torque;
            // Debug.Log($"angle: {angle:00.00}  torque: {torque:00.00}");
        }

        /**
         * TODO REMOVE COLLIDE BLACKLIST
         * TODO REMOVE COLLIDE BLACKLIST
         * TODO REMOVE COLLIDE BLACKLIST
         * TODO REMOVE COLLIDE BLACKLIST
         * TODO REMOVE COLLIDE BLACKLIST
         */
        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            if (e.recomomendations.Count == 0) {
                Debug.Log("Recommendation array is empty");
                return;
            }
            leftFront.steerAngle = (float)e.recomomendations.Average(e=>e.Item2);
            rightFront.steerAngle = (float)e.recomomendations.Average(e=>e.Item2);
            leftBack.motorTorque = 15;
            rightBack.motorTorque = 15;
            Debug.Log(leftFront.steerAngle);

            // var avgDirectionVector = Vector2.zero;
            // foreach (var recomomendation in e.recomomendations) {
            // avgDirectionVector = avgDirectionVector + recomomendation.Item3;
            // }
            // avgDirectionVector *= 1f / e.recomomendations.Count;
            // Debug.Log($"direction: {avgDirectionVector}");

            // var gameobjTransform = gameObject.transform;
            // direction = gameobjTransform.position + new Vector3(
            // avgDirectionVector.x, gameobjTransform.position.y, avgDirectionVector.y);

        }

    }
}