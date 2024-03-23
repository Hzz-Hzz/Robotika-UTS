using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Numerics;
using MathNet.Numerics.Interpolation;
using WpfApp1;

namespace DataFrameGenerator.TrainingDataExtractStrategy;

public class TranslationToCartesiusDecorator : IContourPointTransformationDecorator
{
    private IContourPointTransformationDecorator? _decorated;
    public TranslationToCartesiusDecorator(IContourPointTransformationDecorator? decorated=null) {
        _decorated = decorated;
    }

    public ContourPoint? applyTransformation(ContourList contourList, ContourPoint? pixel, bool convertForwardBackwardLink = true) {
        pixel = _applyTransformation(contourList, pixel, convertForwardBackwardLink);

        if (_decorated == null)
            return pixel;
        return _decorated.applyTransformation(contourList, pixel, convertForwardBackwardLink);
    }

    public ContourPoint? _applyTransformation(
        ContourList contourList, ContourPoint? pixel, bool convertForwardBackwardLink=true
    ) {
        if (pixel == null)
            return null;

        var imageWidth = contourList.sourceImageWidth;
        var imageHeight = contourList.sourceImageHeight;

        var x = pixel.X - imageWidth / 2.0;
        var y = imageHeight - pixel.Y;
        ContourPoint? link = null;
        ContourPoint? backwardLink = null;

        if (convertForwardBackwardLink) {
            link = _applyTransformation(contourList, pixel.link, false);
            backwardLink = _applyTransformation(contourList, pixel.backwardLink, false);
        }
        var ret = new ContourPoint(x, y, pixel.area, link);
        ret.backwardLink = backwardLink;
        return ret;
    }

}