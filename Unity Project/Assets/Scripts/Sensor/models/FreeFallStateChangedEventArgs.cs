using System;

namespace Sensor.models
{
    public class FreeFallStateChangedEventArgs: EventArgs
    {
        public bool isFreeFalling;
        public int numOfFreeFall;
        public float percentageOfFreeFall;
    }
}