using System.Diagnostics;
using System.Windows.Documents;
using Emgu.CV.Util;
using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;


// both decorator and strategy pattern
public interface IContourPointTransformationDecorator
{
    IContourPointTransformationDecorator? _decorated { get; set; }

    // convertForwardBackwardLink seems like a code smell (tight coupling)
    public ContourPoint? applyTransformation(ContourList originalContourList, ContourPoint? pixel) {
        var transformedContourPoint = _applyTransformation(originalContourList, pixel);

        if (_decorated == null)
            return transformedContourPoint;
        return _decorated.applyTransformation(originalContourList, transformedContourPoint);
    }

    protected ContourPoint? _applyTransformation(ContourList originalContourList, ContourPoint? point);


    public ContourPoint? applyTransformationForwardBackwardAsWell(ContourList contourList, ContourPoint? point) {
        var ret = applyTransformation(contourList, point);
        var link = applyTransformation(contourList, point.link);
        var backwardLink = applyTransformation(contourList, point.backwardLink);
        ret.link = link;
        ret.backwardLink = backwardLink;
        return ret;
    }





}


public static class ContourListExtension
{
    public static ContourList applyTransformation(this ContourList contourList, IContourPointTransformationDecorator decorator) {
        var ret = new ContourList(new VectorOfVectorOfPoint(), contourList.sourceImageWidth,
            contourList.sourceImageHeight, null);
        var mappingTable = new Dictionary<ContourPoint, ContourPoint?>();

        // transform one by one
        foreach (var originalContour in contourList.contours) {
            var transformedContour = decorator.applyTransformation(contourList, originalContour);
            mappingTable.Add(originalContour, transformedContour);
            if (transformedContour != null)
                ret.contours.Add(transformedContour);
        }

        // fix the links
        foreach (var originalContour in contourList.contours) {
            ContourPoint? transformedContour;
            if (!mappingTable.TryGetValue(originalContour, out transformedContour))
                continue;
            Debug.Assert(transformedContour != null);

            if (originalContour.link != null)
                transformedContour.link = mappingTable!.GetValueOrDefault(originalContour.link);
            if (originalContour.backwardLink != null)
                transformedContour.backwardLink = mappingTable!.GetValueOrDefault(originalContour.backwardLink);
        }

        return ret;
    }
}