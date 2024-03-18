namespace WpfApp1.ContourLinkScoreStrategy;

public interface ILinkScoreCalculation
{
    // the smaller the better (closer)
    public double getScore(ContourPoint anchor, ContourPoint target);
}