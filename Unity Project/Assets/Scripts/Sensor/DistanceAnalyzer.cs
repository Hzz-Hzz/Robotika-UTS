using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sensor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class DistanceAnalyzer : MonoBehaviour
{
    public Rigidbody rigidbody;

    public float currentSpeed => _speedSensor.getCurrentSpeed();
    public float overallSpeedAverage => totalDistanceTraveled / Time.time;

    public float averageSpeedInLastOneSecond {
        get {
            return deltaTimeAndTraveledDistanceQueue.Sum(e => {
                return e.Item2;
            });
        }
        set { }
    }


    [FormerlySerializedAs("distanceTraveled")] public float totalDistanceTraveled = 0.0f;
    private SpeedSensor _speedSensor;

    // Start is called before the first frame update
    void Start() {
        _speedSensor = new SpeedSensor(rigidbody);
    }

    // Update is called once per frame
    void Update() {
        var multiplier = 1f;
        if (!_speedSensor.isGoingForward())
            multiplier = -1f;
        var velocity = _speedSensor.getCurrentSpeed();
        var distanceTraveled = velocity * Time.deltaTime;
        totalDistanceTraveled += multiplier * distanceTraveled;
        updateDeltaTimeAndTraveledDistanceQueue(Time.deltaTime, distanceTraveled);
    }

    private Queue<Tuple<float, float>> deltaTimeAndTraveledDistanceQueue = new();

    public void updateDeltaTimeAndTraveledDistanceQueue(float deltaTime, float distanceTraveled) {
        deltaTimeAndTraveledDistanceQueue.Enqueue(new Tuple<float, float>(deltaTime, distanceTraveled));

        // make sure totalDeltaTime is not greater than 1 second
        float totalDeltaTime = deltaTimeAndTraveledDistanceQueue.Sum(e=>e.Item1);
        while (totalDeltaTime > 1) {
            var dequeued = deltaTimeAndTraveledDistanceQueue.Dequeue();
            totalDeltaTime -= dequeued.Item1;
        }
    }
}

[CustomEditor(typeof(DistanceAnalyzer)), CanEditMultipleObjects]
public class DistanceSensorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DistanceAnalyzer myObject = (DistanceAnalyzer) target;

        EditorGUILayout.LabelField("averageSpeedInLastOneSecond", myObject.averageSpeedInLastOneSecond.ToString());
        EditorGUILayout.LabelField("overallSpeedAverage", myObject.overallSpeedAverage.ToString());
        EditorGUILayout.LabelField("currentSpeed", myObject.currentSpeed.ToString());
    }
}