using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Ruler : MonoBehaviour
{
    public GameObject start;
    public GameObject end;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos() {
        var start = this.start.transform.position;
        var end = this.end.transform.position;
        var distanceVector = (end - start);
        var length = distanceVector.magnitude;
        distanceVector.y = 0;
        var horizontalLength = distanceVector.magnitude;

        var prevColor = Gizmos.color;


        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(start, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);

        var guiStyle = new GUIStyle();
        guiStyle.normal.textColor = Color.red;
        guiStyle.normal.background = Texture2D.whiteTexture;
        guiStyle.fontSize = 20;
        Handles.Label(end+Vector3.up*0.5f, $"l={length:0.00},hl={horizontalLength:0.00}\n" +
                                           $"({distanceVector.x:0.000},{distanceVector.z:0.000})", guiStyle);
        Gizmos.color = prevColor;
    }
}
