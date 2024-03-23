using WpfApp1;

namespace DataFrameGenerator.TrainingDataExtractStrategy;

public interface IContourPointOrderingStrategy
{
    public List<ContourPoint> getContourPointAsList(ContourList contourList);
}