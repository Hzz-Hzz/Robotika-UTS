using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CapsuleKeyboardControl : MonoBehaviour
{
    private int speed = 6;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var step = 0.0f;
        if (getKey("w"))
            step += speed * Time.deltaTime;
        if (getKey("s"))
            step -= speed * Time.deltaTime;

        if (getKey("left shift")) {
            step /= 2;
        }
        transform.position += transform.forward.normalized * step
                              + .5f * Time.deltaTime * Vector3.up;
        var rotationSpeed = 22 * Time.deltaTime;
        doRotation(rotationSpeed);
    }

    public void doRotation(float stepRotationDegree) {
        var rotation = 0.0f;
        if (getKey("a"))
            rotation -= stepRotationDegree;
        if (getKey("d"))
            rotation += stepRotationDegree;
        transform.rotation *= Quaternion.Euler(0, rotation, 0);
    }

    private bool getKey(string key) {
        return Input.GetKeyDown(key) || Input.GetKey(key);
    }
}
