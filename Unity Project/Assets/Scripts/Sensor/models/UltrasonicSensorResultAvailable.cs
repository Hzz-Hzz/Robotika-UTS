using System;

namespace Sensor.models
{
    public class UltrasonicSensorResultAvailable: EventArgs
    {
        public ScanResult[] _scanResults;
        public UltrasonicSensorResultAvailable(ScanResult[] scanResults) {
            _scanResults = scanResults;
        }
    }

    public class ScanResult
    {
        public float startAngle;
        public float endAngle;
        public float minimumDistance;
        public float maximumDistance;

        public ScanResult(float startAngle, float endAngle, float minimumDistance, float maximumDistance) {
            this.startAngle = startAngle;
            this.endAngle = endAngle;
            this.minimumDistance = minimumDistance;
            this.maximumDistance = maximumDistance;
        }
    }
}