using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCar : MonoBehaviour
{
    public Transform path;

    private List<Transform> nodes;
    private int currentNode = 0;

    public WheelCollider LeftWheel;
    public WheelCollider RightWheel;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello: " + gameObject.name);
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransforms.Length; i++)
        {
            if (pathTransforms[i] != path.transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }
        
    }

    private void MoveWheels()
    {
        LeftWheel.motorTorque = 40f;
        RightWheel.motorTorque = 40f;
    }

    private void RunSteer()
    {
        float MaxSteerAngle = 45f;
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
        float newsteer = (relativeVector.x / relativeVector.magnitude) * MaxSteerAngle;
        LeftWheel.steerAngle = newsteer;
        RightWheel.steerAngle = newsteer;
    }

    private void Sensors()
    {
        RaycastHit hit;
        float sensor_length = 20f;

        if (Physics.Raycast(transform.position, transform.forward, out hit, sensor_length))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                Debug.DrawLine(transform.position, hit.point, Color.red);
            }
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        checkwaypointDistance();
        MoveWheels();
        RunSteer();
        Sensors();
    }   

    private void checkwaypointDistance()
    {
        if (Vector3.Distance(this.transform.position, nodes[currentNode].position) < 1f)
        {
            if (currentNode == nodes.Count - 1)
            {
                currentNode = 0;
            }
            else
            {
                currentNode++;
            }
        }
    }
}
