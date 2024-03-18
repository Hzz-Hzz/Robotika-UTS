using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    readonly SteerDirectionManager _steerManager = new SteerDirectionManager(35, 35);
    readonly MotorTorqueManager _motorTorqueManager = new MotorTorqueManager(0, 0.3f, 45);



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // _motorTorqueManager.motorTorque = (Time.realtimeSinceStartup >= 3f) ? 35 : 35 / (3 - Time.realtimeSinceStartup);
        var targetDirection = getTargetDirection();

        var steerAngle = _steerManager.getSteerAngle();
        var motorTorque = _motorTorqueManager.getMotorTorque(steerAngle);
        updateTorque(motorTorque);
        updateSteerAngle(steerAngle);
        _steerManager.updateSteer(transform.transform, targetDirection);
    }

    void updateSteerAngle(float steerAngle)
    {
        frontLeft.steerAngle = steerAngle;
        frontRight.steerAngle = steerAngle;
    }

    void updateTorque(float motorTorque)
    {
        rearLeft.motorTorque = motorTorque;
        rearRight.motorTorque = motorTorque;

        // frontRight.motorTorque = motorTorque;
        // frontLeft.motorTorque = motorTorque;
    }


    Vector3 getTargetDirection()
    {
        var transform = gameObject.transform;
        return transform.position + transform.forward * 20;
    }
}
