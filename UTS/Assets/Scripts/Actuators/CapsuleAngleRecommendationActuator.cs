using System;
using System.Collections;
using System.Linq;
using System.Threading;
using EventsEmitter.models;
using UnityEngine;
using UnityEngine.Serialization;

namespace Actuators
{
    public class CapsuleAngleRecommendationActuator: MonoBehaviour
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();


        private void Start() {
            cancellationTokenSource = new CancellationTokenSource();
            StartCoroutine(rotateToward(cancellationTokenSource.Token));
            targetAngle = 0;
            moveForwardSpeed = maximumMoveForwardSpeed;
        }

        private float targetAngle = 0;  // 0 means keep forward, positive means go left, negative means go right
        public float rotationSpeed = 30f;  // degree / second
        public float maximumMoveForwardSpeed = 10f;
        private float moveForwardSpeed;

        private float? farthestDistance=null;

        private void Update() {
            var transform = gameObject.transform;
            gameObject.transform.position = transform.position
                                            + moveForwardSpeed * Time.deltaTime * transform.forward.normalized;
        }

        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            var recommendation = e.recomomendations;
            if (recommendation == null)
                return;
            if (recommendation.Count == 0) {
                Debug.Log("Received empty-array angle recommmendation");
                return;
            }

            farthestDistance = recommendation.Max(tuple => {
                return tuple.Item1;
            });
            if (farthestDistance > 0.5)
                moveForwardSpeed = maximumMoveForwardSpeed;
            else if (farthestDistance > 0.4) {  // slow down
                moveForwardSpeed = Math.Max(0.05f, (float)(maximumMoveForwardSpeed * farthestDistance / 0.5));
            } else {  // slow down
                moveForwardSpeed = Math.Max(0.05f, (float)(maximumMoveForwardSpeed *farthestDistance / 0.5  * farthestDistance / 0.4));
            }

            // spec dari server dalam bentuk radians, dan positifnya ke arah counter-clockwise.
            // Sedangkan spec kita (unity) dalam bentuk degree dan arah positifnya ke arah clockwise (kiri negatif kanan positif)
            var targetDegree = recommendation.Average(e => e.Item2);  // average angle of selected ones
            var degree = (float)(targetDegree * 180 / Math.PI);
            targetAngle = -((float)degree - 90);
            Debug.Log($"angle: {targetAngle:00.00}, dist: {farthestDistance:00.00} speed: {moveForwardSpeed:00.00}");
        }

        IEnumerator rotateToward(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested) {
                var currentAngle = gameObject.transform.rotation;
                var targetAngle = currentAngle * Quaternion.Euler(0, this.targetAngle, 0);
                var totalRequiredDuration = Math.Abs(this.targetAngle) / rotationSpeed;
                var t = Math.Min(1, Time.deltaTime / totalRequiredDuration);

                var newRotation = Quaternion.Slerp(currentAngle, targetAngle, t);
                gameObject.transform.rotation = newRotation;
                this.targetAngle *= 1 - t;
                yield return null;
            }
        }

    }
}