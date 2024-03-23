using WpfApp1;

namespace DataFrameGenerator.TrainingDataExtractStrategy;


// both decorator and strategy pattern
public interface IContourPointTransformationDecorator
{
    // convertForwardBackwardLink seems like a code smell (tight coupling)
    public ContourPoint? applyTransformation(ContourList contourList, ContourPoint? pixel, bool convertForwardBackwardLink = true);
}