using UnityEngine;

namespace Sensor
{
    public class SpeedSensor
    {
        public SpeedSensor(Rigidbody rigidbody) {
            this.rigidbody = rigidbody;
        }

        public Rigidbody rigidbody;
        public float getCurrentSpeed() {
            return rigidbody.velocity.magnitude;
        }

        public bool isGoingForward(Transform transform) {
            return Vector3.Dot(transform.forward, rigidbody.velocity) > 0;
        }
    }
}