using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SteerDirectionManager
{
    float maxSteerAngle;
    float maxSteerAngularSpeed;  // angular per second
    float currentSteerAngle = 0.0f;

    private int lastFrameUpdate = 0;

    public SteerDirectionManager(float maxSteerAngle, float maxSteerAngularSpeed)
    {
        this.maxSteerAngle = maxSteerAngle;
        this.maxSteerAngularSpeed = maxSteerAngularSpeed;
    }

    /**
     * Warning: This method should be called at least once per frame, but won't make any changes if called multiple
     * times within the same frame.
     */
    public void updateSteer(Transform currentState, Vector3 targetPosition)
    {
        if (lastFrameUpdate == Time.frameCount)
            return;
        // assert this method should be called at minimum once per frame
        Debug.Assert(lastFrameUpdate + 1 == Time.frameCount, $"{lastFrameUpdate+1} {Time.frameCount}");
        lastFrameUpdate = Time.frameCount;

        var targetDirectionWithinPovOfCurrentState = currentState.InverseTransformPoint(targetPosition);
        var targetAngle = Vector3.SignedAngle(Vector3.forward, targetDirectionWithinPovOfCurrentState, Vector3.up);
        var angleDifference = targetAngle - currentSteerAngle;

        var maximumAngleDistance = maxSteerAngularSpeed * Time.deltaTime;
        if (Math.Abs(angleDifference) > maximumAngleDistance)
            angleDifference = Math.Sign(angleDifference) * maximumAngleDistance;

        var newAngle = currentSteerAngle + angleDifference;
        if (Math.Abs(newAngle) > maxSteerAngle)
            newAngle = Math.Sign(newAngle) * maxSteerAngle;
        currentSteerAngle = newAngle;
        // Debug.Log($"{currentSteerAngle} {angleDifference/maximumAngleDistance} {maximumAngleDistance} {targetAngle}");
    }

    public float getSteerAngle()
    {
        return currentSteerAngle;
    }
}