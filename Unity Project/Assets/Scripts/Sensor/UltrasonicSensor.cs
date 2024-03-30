using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class UltrasonicSensor : MonoBehaviour
{
    [FormerlySerializedAs("length")] public float sensorLength = 10;
    readonly Color linecolor = Color.cyan;

    void OnDrawGizmos()
    {
        Gizmos.color = linecolor;

        var currTransform = gameObject.transform;
        var currPos = currTransform.position;
        Gizmos.DrawLine(currPos, currPos + currTransform.forward * sensorLength);
        Gizmos.DrawWireSphere(currPos, 0.05f);
    }

    private readonly string targetTag = "Terrain";
    public float? detectDistance() {
        RaycastHit raycastHit;
        var pos = transform.position;

        var raycastResult = Physics.RaycastAll(pos, transform.forward, sensorLength);
        raycastResult = raycastResult.Where(e => e.collider.CompareTag(targetTag)).ToArray();
        if (raycastResult.Length == 0) {
            return null;
        }

        RaycastHit closest = raycastResult[0];
        foreach (var raycast in raycastResult) {
            if (raycast.distance < closest.distance)
                closest = raycast;
        }

        Debug.DrawLine(transform.position, closest.point, Color.red);
        return Vector3.Distance(pos, closest.point);
    }
}
