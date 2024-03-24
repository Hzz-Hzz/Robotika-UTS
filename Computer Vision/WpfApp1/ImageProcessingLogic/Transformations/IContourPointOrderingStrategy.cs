using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;

public interface IContourPointOrderingStrategy
{
    public List<ContourPoint> getContourPointAsList(ContourList contourList);
}