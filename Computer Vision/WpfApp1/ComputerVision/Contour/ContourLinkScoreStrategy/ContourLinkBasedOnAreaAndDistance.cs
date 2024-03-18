using System;

namespace WpfApp1.ContourLinkScoreStrategy;

public class ContourLinkBasedOnAreaAndDistance: ILinkScoreCalculation
{
    public double getScore(ContourPoint anchor, ContourPoint target) {
        var biggerArea = Math.Max(anchor.area, target.area);
        var smallerArea = Math.Min(anchor.area, target.area);
        var ratio = Math.Log(1 + biggerArea / smallerArea);  // the smaller the better

        var distance = anchor.distance(target, 1.0, 2);
        var anglePenalty = 1.0;
        if (anchor.backwardLink != null) {  // prefer straight lines
            var anglePercentage = ContourPoint.calculateAngle(anchor.backwardLink, anchor, target) / Math.PI;
            anglePenalty = Math.Log(3 - anglePercentage, 2);
            // anglePenalty = Math.Log(2 + anglePenalty, 2);
        }
        return distance * ratio * anglePenalty;  // the smaller the better
    }

    public int nodePrioritySorter(ContourList contourList, ContourPoint anchor, ContourPoint target) {
        var score1 = calculatePriorityRank(contourList, anchor);
        var score2 = calculatePriorityRank(contourList, target);
        return score1 - score2;
    }

    public int calculatePriorityRank(ContourList contourList, ContourPoint point) {
        var horizontalPenalty1 =  (int)(contourList.sourceImageWidth - point.X);
        var horizontalPenalty2 = (int)(point.X);
        var verticalPenalty = (int)(contourList.sourceImageHeight - point.Y);

        return verticalPenalty * 2 * contourList.sourceImageWidth / contourList.sourceImageHeight
               + Math.Min(horizontalPenalty1, horizontalPenalty2);
    }


}