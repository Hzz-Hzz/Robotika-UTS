using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;

public class TranslationToCartesiusDecorator : IContourPointTransformationDecorator
{

    public TranslationToCartesiusDecorator(IContourPointTransformationDecorator? decorated=null) {
        _decorated = decorated;
    }


    public IContourPointTransformationDecorator? _decorated { get; set; }

    public ContourPoint? _applyTransformation(
        ContourList contourList, ContourPoint? point
    ) {
        if (point == null)
            return null;

        var imageWidth = contourList.sourceImageWidth;
        var imageHeight = contourList.sourceImageHeight;

        var x = point.X - imageWidth / 2.0;
        var y = imageHeight - point.Y;

        var ret = new ContourPoint(x, y, point.area, null);
        ret.order = point.order;  // just for make debugging easier
        return ret;
    }

}