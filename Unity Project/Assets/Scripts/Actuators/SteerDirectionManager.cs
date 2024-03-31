using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SteerDirectionManager
{
    float maxSteerAngle;
    float maxSteerAngularSpeed;  // angular per second
    float currentSteerAngle = 0.0f;
    public float goingZeroMultiplier { get; set; }

    private int lastFrameUpdate = 0;

    public SteerDirectionManager(float maxSteerAngle, float maxSteerAngularSpeed, float goingZeroMultiplier=1)
    {
        this.maxSteerAngle = maxSteerAngle;
        this.maxSteerAngularSpeed = maxSteerAngularSpeed;
        this.goingZeroMultiplier = goingZeroMultiplier;
    }

    /**
     * Warning: This method should be called at least once per frame, but won't make any changes if called multiple
     * times within the same frame.
     */
    public void updateSteer(Transform currentState, Vector3 targetPosition) {
        var targetDirectionWithinPovOfCurrentState = currentState.InverseTransformPoint(targetPosition);
        var targetAngle = Vector3.SignedAngle(Vector3.forward, targetDirectionWithinPovOfCurrentState, Vector3.up);
        updateSteerBasedOnAngle(targetAngle);
    }

    public void updateSteerBasedOnAngle(float targetAngle) {
        if (lastFrameUpdate == Time.frameCount)
            return;
        // assert this method should be called at minimum once per frame
        Debug.Assert(lastFrameUpdate + 1 == Time.frameCount, $"{lastFrameUpdate+1} {Time.frameCount}");
        lastFrameUpdate = Time.frameCount;

        var angleDifference = targetAngle - currentSteerAngle;


        var maximumAngleDistance = maxSteerAngularSpeed * Time.deltaTime;
        var goingZero = Math.Abs(targetAngle) < Math.Abs(currentSteerAngle) ||
                        Math.Sign(targetAngle) != Math.Sign(currentSteerAngle);
        if (goingZero)
            maximumAngleDistance *= goingZeroMultiplier;  // not perfect but ok
        if (Math.Abs(angleDifference) > maximumAngleDistance)
            angleDifference = Math.Sign(angleDifference) * maximumAngleDistance;

        var newAngle = currentSteerAngle + angleDifference;
        if (Math.Abs(newAngle) > maxSteerAngle)
            newAngle = Math.Sign(newAngle) * maxSteerAngle;
        currentSteerAngle = newAngle;
    }

    public float getSteerAngle()
    {
        return currentSteerAngle;
    }
}