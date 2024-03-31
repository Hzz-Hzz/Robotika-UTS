using UnityEngine;

namespace Sensor
{
    public class Gyroscope: MonoBehaviour
    {
        public GameObject gameObject;
        public float getSlope() {
            var forward = gameObject.transform.forward;
            return Vector3.Angle(new Vector3(forward.x, 0, forward.z), forward);
        }
    }
}