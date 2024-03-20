using WpfApp1;

namespace ModelTrainer.TrainingDataExtractStrategy;

public class TrainingDataOrderingStrategy
{
    private ContourList contourList;

    public TrainingDataOrderingStrategy(ContourList contourList) {
        this.contourList = contourList;
    }

    public List<ContourPoint> getContourOrdering() {
        return contourList.contours;  // TODO
    }
}