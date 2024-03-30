using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CapsulePathFollowing : MonoBehaviour
{
    public Transform targetPath;
    private Transform[] paths;
    private int currentPath = 0;
    private int speed = 6;
    private Vector3 pathLocationRandomizer;

    // Start is called before the first frame update
    void Start()
    {
        paths = targetPath.GetComponentsInChildren<Transform>();
        paths = paths.Where(x => x.transform != transform).ToArray();
        updatePathLocationRandomizer();
    }

    // Update is called once per frame
    void Update()
    {
        updateTargetPosition();

        var step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition(), step)
                             + .5f * Time.deltaTime * Vector3.up;
        rotateTowardTargetPosition(15 * Time.deltaTime);
    }


    public float slerpTime;

    public void rotateTowardTargetPosition(float stepRotationDegree)
    {
        Debug.Assert(stepRotationDegree > 0);
        var targetPositionV3 = targetPosition();
        targetPositionV3.y = transform.position.y;
        var targetRotation = Quaternion.LookRotation(targetPositionV3 - transform.position);
        var currRotation = transform.rotation;
        transform.rotation = Quaternion.Slerp(currRotation, targetRotation, slerpTime);
        slerpTime = Math.Min(1, Time.deltaTime * (1f));

        Debug.DrawLine(transform.position, transform.position + transform.forward * 5, Color.red);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawLine(transform.position, targetPosition());
    }

    Vector3 targetPosition()
    {
        if (paths.Length == 0)
            return Vector3.forward;
        return paths[currentPath].transform.position + pathLocationRandomizer;
    }

    void updateTargetPosition()
    {
        if (Vector3.Distance(gameObject.transform.position, targetPosition()) > 2)
            return;
        slerpTime = 0;
        currentPath = (currentPath + 1) % paths.Length;
        updatePathLocationRandomizer();
    }

    void updatePathLocationRandomizer() {
        pathLocationRandomizer = Random.insideUnitCircle.normalized * Random.value;
    }
}
