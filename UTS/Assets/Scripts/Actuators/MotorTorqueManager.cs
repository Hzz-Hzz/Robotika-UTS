using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Will return motor torque based on current steer-angle such that it won't fall: reduce the speed for extreme-steering angle.
 */
public class MotorTorqueManager
{
    public float motorTorque = 30f;
    public float torqueScaleAtExtreemeSteeringDegree = 0.1f;  // 10% by default
    public float extremeStreeingDegree = 55.0f;

    public MotorTorqueManager(float motorTorque, float torqueScaleAtExtreemeSteeringDegree, float extremeStreeingDegree)
    {
        this.motorTorque = motorTorque;
        this.torqueScaleAtExtreemeSteeringDegree = torqueScaleAtExtreemeSteeringDegree;
        this.extremeStreeingDegree = extremeStreeingDegree;
    }

    /**
     * if steeringDegree == 0, then return motorTorque.
     * if abs(steeringDegree) == torquePercentageAtExtreemeSteeringDegree, then return torqueScaleAtExtreemeSteeringDegree * motorTorque
     *
     */
    public float getMotorTorque(float steeringDegree)
    {
        steeringDegree = Math.Abs(steeringDegree);

        // assume f(x) = 1/(ax+c); x=steeringDegree,
        // we need to find A, C such that f(0)=1 and f(extremeStreeingDegree) = torqueScaleAtExtreemeSteeringDegree
        // f(0)=1 --> c = 1
        // f(x) = torqueScaleAtExtreemeSteeringDegree
        // --> a = 1/(torqueScaleAtExtreemeSteeringDegree * extremeStreeingDegree) - c/extremeStreeingDegree

        var c = 1;
        var a = 1 / (torqueScaleAtExtreemeSteeringDegree * extremeStreeingDegree) - c / extremeStreeingDegree;
        // var a = 1 / (t * s) - c / s;
        var motorTorqueMultiplier = 1 / (a*steeringDegree + c);
        return (float)(motorTorqueMultiplier * motorTorque);
    }
}