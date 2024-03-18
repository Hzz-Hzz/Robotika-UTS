using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawGizmosChildren : MonoBehaviour
{
    readonly Color linecolor = Color.white;
    private List<Transform> nodes = new List<Transform>();


    void OnDrawGizmos()
    {
        Gizmos.color = linecolor;
        Transform[] pathtransform = GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathtransform.Length; i++)
        {
            if (pathtransform[i] != transform)
            {
                nodes.Add(pathtransform[i]);
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var currentNode = nodes[i].position;
            var previousNodes = currentNode;
            if (i > 0)
            {
                previousNodes = nodes[i - 1].position;
            } else if (nodes.Count > 1)
            {
                previousNodes = nodes[nodes.Count - 1].position;
            }
            Gizmos.DrawLine(previousNodes, currentNode);
            Gizmos.DrawWireSphere(currentNode, 0.5f);
        }
    }
}
