using System;

namespace WpfApp1.ContourLinkScoreStrategy;

public class ContourLinkBasedOnAreaAndDistance: ILinkScoreCalculation
{
    public double getScore(ContourPoint anchor, ContourPoint target) {
        var biggerArea = Math.Max(anchor.area, target.area);
        var smallerArea = Math.Min(anchor.area, target.area);
        var ratio = biggerArea / smallerArea;  // the smaller the better

        var distance = anchor.distance(target);
        return distance * ratio;  // the smaller the better
    }
}