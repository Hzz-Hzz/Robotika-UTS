using UnityEngine;

namespace Sensor
{
    public class SpeedSensor
    {
        public SpeedSensor(Rigidbody rigidbody) {
            Rigidbody = rigidbody;
        }

        public Rigidbody Rigidbody;
        public float getCurrentSpeed() {
            return Rigidbody.velocity.magnitude;
        }
    }
}