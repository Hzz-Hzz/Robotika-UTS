namespace WpfApp1.ContourLinkScoreStrategy;

public interface ILinkScoreCalculation
{
    // the smaller the better (closer)
    public double getScore(ContourPoint toBeUpdated, ContourPoint target);
    public bool isHardConstraintSatisfied(ContourList contourList, ContourPoint toBeUpdated, ContourPoint target);

    // the smallest one's link will be calculated first (top priority)
    public int nodePrioritySorter(ContourList contourList, ContourPoint toBeUpdated, ContourPoint target);
}