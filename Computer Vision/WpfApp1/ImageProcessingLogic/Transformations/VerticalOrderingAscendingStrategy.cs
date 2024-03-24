using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;


// ascending could mean bottom-to-top, or could also mean top-to-bottom, depends on
// whether the Y=0 axis at the top or at the bottom of the screen.
public class VerticalOrderingAscendingStrategy : IContourPointOrderingStrategy
{
    public VerticalOrderingAscendingStrategy() {
    }

    public List<ContourPoint> getContourPointAsList(ContourList contourList) {
        List<ContourPoint> ret = new();
        foreach (var contour in contourList.contours) {
            ret.Add(contour);
        }
        ret.Sort((a, b) => a.Y.CompareTo(b.Y));
        return ret;
    }
}