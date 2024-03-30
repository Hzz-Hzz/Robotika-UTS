using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisplayBoundingRect : MonoBehaviour
{
     void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var collider = GetComponentInChildren<Collider>(false);
            var size = collider.bounds.size;
            Gizmos.DrawWireCube(collider.bounds.center, size);
        }
}
