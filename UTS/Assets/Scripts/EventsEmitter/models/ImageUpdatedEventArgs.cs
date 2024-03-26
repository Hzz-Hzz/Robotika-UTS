using System;

namespace EventsEmitter.models
{
    public class ImageUpdatedEventArgs: EventArgs
    {
        public byte[] imageData;
        public bool paused;
    }
}