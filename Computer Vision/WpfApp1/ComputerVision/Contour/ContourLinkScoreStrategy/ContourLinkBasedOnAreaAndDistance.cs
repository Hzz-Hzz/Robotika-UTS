using System;

namespace WpfApp1.ContourLinkScoreStrategy;

public class ContourLinkBasedOnAreaAndDistance: ILinkScoreCalculation
{
    public double getScore(ContourPoint toBeUpdated, ContourPoint target) {
        var biggerArea = Math.Max(toBeUpdated.area, target.area);
        var smallerArea = Math.Min(toBeUpdated.area, target.area);
        var ratio = Math.Log(1 + biggerArea / smallerArea);  // the smaller the better

        var distance = toBeUpdated.distance(target, 1.0, 2);
        var anglePenalty = 1.0;
        if (toBeUpdated.backwardLink != null) {  // prefer straight lines
            var anglePercentage = ContourPoint.calculateAngle(toBeUpdated.backwardLink, toBeUpdated, target) / Math.PI;
            anglePenalty = Math.Log(3 - anglePercentage, 2);
            // anglePenalty = Math.Log(2 + anglePenalty, 2);
        }
        return distance * ratio * anglePenalty;  // the smaller the better
    }

    public bool isHardConstraintSatisfied(ContourList contourList, ContourPoint toBeUpdated, ContourPoint target) {
        if (toBeUpdated.backwardLink == null)
            return true;
        var prevDistance = toBeUpdated.distance(toBeUpdated.backwardLink, 1f);
        var currDistance = toBeUpdated.distance(target);
        return currDistance / prevDistance < 3;
    }

    public int nodePrioritySorter(ContourList contourList, ContourPoint toBeUpdated, ContourPoint target) {
        var score1 = calculatePriorityRank(contourList, toBeUpdated);
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