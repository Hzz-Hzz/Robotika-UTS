using System;
using System.Collections.Generic;

namespace EventsEmitter.models
{
    public class AngleRecommendationReceivedEventArgs: EventArgs
    {

        /**
        * return list of recommendations, sorted by most-recommended (index 0) to the least recommended
        * but still recommended (last index).
        *
        * Each item will be represented as a tuple of (distance, angle in rads).
        * Angle in rads will be 0 if you should go forward,
        * positive if you should go right,
        * and negative if you should go left.
         *
         * For more updated information, please see getAngleRecommendation() at Server's C# project, file: RpcFacade.cs
        */
        public List<Tuple<float, double>> recomomendations;
    }
}