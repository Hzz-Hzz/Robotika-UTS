using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGizmos : MonoBehaviour
{
    readonly Color linecolor = Color.cyan;

    void OnDrawGizmos()
    {
        Gizmos.color = linecolor;

        var currTransform = gameObject.transform;
        var currPos = currTransform.position;
        Gizmos.DrawLine(currPos, currPos + currTransform.forward * 40);
        Gizmos.DrawWireSphere(currPos, 0.2f);
    }
}
