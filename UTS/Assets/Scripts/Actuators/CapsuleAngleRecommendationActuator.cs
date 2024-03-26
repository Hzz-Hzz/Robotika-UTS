using System;
using System.Collections;
using System.Threading;
using EventsEmitter.models;
using UnityEngine;

namespace Actuators
{
    public class CapsuleAngleRecommendationActuator: MonoBehaviour
    {
        private CancellationTokenSource previousCancellationTokenSource = new CancellationTokenSource();


        private void Update() {
            gameObject.transform.position = gameObject.transform.position + gameObject.transform.forward.normalized * Time.deltaTime;
        }

        public void OnReceiveAngleRecommendation(AngleRecommendationReceivedEventArgs e) {
            var recommendation = e.recomomendations;
            if (recommendation == null)
                return;
            if (recommendation.Count == 0) {
                Debug.Log("Received empty-array angle recommmendation");
                return;
            }

            previousCancellationTokenSource?.Cancel();
            previousCancellationTokenSource = new CancellationTokenSource();

            var degree = (float)(recommendation[0].Item2 * 180 / Math.PI);
            var degreeRelative = (float)degree - 90;
            var targetAngle = Quaternion.Euler(0, 90, 0);
            Debug.Log($"Target degreeRelative {degreeRelative}");

            var rotationSpeed = 90;  // 30 degree per second
            StartCoroutine(rotateToward(previousCancellationTokenSource.Token, targetAngle,
                Time.time, 1000 * degree / rotationSpeed));
        }

        IEnumerator rotateToward(CancellationToken cancellationToken, Quaternion targetAngle, float startingTime, float targetDurationMs) {
            var currentAngle = gameObject.transform.rotation;

            while (!cancellationToken.IsCancellationRequested) {
                var t = (Time.time - startingTime) / targetDurationMs;
                t = Math.Min(t, 1.0f);
                gameObject.transform.rotation = Quaternion.Slerp(currentAngle, targetAngle, t);
                // gameObject.transform.rotation = targetAngle;
                yield return null;

                if (Time.time - startingTime > targetDurationMs)
                    yield break;
            }
        }

    }
}