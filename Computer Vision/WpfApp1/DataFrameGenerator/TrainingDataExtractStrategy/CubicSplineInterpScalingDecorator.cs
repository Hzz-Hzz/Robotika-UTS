using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Numerics;
using MathNet.Numerics.Interpolation;
using WpfApp1;

namespace DataFrameGenerator.TrainingDataExtractStrategy;

public class CubicSplineInterpScalingDecorator : IContourPointTransformationDecorator
{
    // private static readonly double[] y_actual_source = {0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0};
    private static readonly double[] x_ratio_based_on_y_source =   {464.0, 133.0, 71.0, 50.0, 37.0, 29.0, 26.0, 22.0};  // for every 0.25 of actual X

    private CubicSpline y_pixel_to_x_pixel_quarter_ratio =
        CubicSpline.InterpolatePchipSorted(y_pixel, x_ratio_based_on_y_source);


    private static readonly double[] y_pixel = {0.0, 350.0, 413.0, 437.0, 449.0, 457.0, 462.0, 466.0};
    private static readonly double[] y_actual =   {0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0};
    private static readonly double max_y_pixel;
    private CubicSpline y_pixel_to_y_actual =
        CubicSpline.InterpolatePchipSorted(y_pixel, y_actual);

    static CubicSplineInterpScalingDecorator() { max_y_pixel = y_pixel.Max(); }


    private IContourPointTransformationDecorator? _decorated;


    public CubicSplineInterpScalingDecorator(IContourPointTransformationDecorator? decorated=null) {
        _decorated = decorated;
    }


    public ContourPoint? applyTransformation(
        ContourList contourList, ContourPoint? pixel, bool convertForwardBackwardLink=true
    ) {
        pixel = _applyTransformation(contourList, pixel, convertForwardBackwardLink);
        if (_decorated == null)
            return pixel;
        return _decorated.applyTransformation(contourList, pixel, convertForwardBackwardLink);
    }




    public ContourPoint _applyTransformation(
        ContourList _, ContourPoint? pixel, bool convertForwardBackwardLink=true
    ) {
        if (pixel == null)
            return null;

        var y = yPixelToYActual(pixel.Y);
        var x = xPixelToXActual(pixel.X, pixel.Y);
        var area = pixel.area;  // idk how to convert this. TODO
        ContourPoint? link = null;
        ContourPoint? backwardLink = null;
        if (convertForwardBackwardLink) {
            link = _applyTransformation(_, pixel.link, false);
            backwardLink = _applyTransformation(_, pixel.backwardLink, false);
        }

        var ret = new ContourPoint(x, y, area, link);
        ret.backwardLink = backwardLink;
        return ret;
    }

    public double yPixelToYActual(double y_pixel) {
        if (y_pixel < 0)
            throw new ValidationException();
        if (y_pixel > max_y_pixel) {  // in case of we need to do "extrapolation", do linear estimation
            var value2 = max_y_pixel;
            var value1 = max_y_pixel * 9.8/10;
            var result2 = yPixelToYActual(value2);
            var result1 = yPixelToYActual(value1);
            var gradient = (result2 - result1) / (value2 - value1);

            var additional = gradient * (y_pixel - value2);    // x2 must be greater than x1 in this case
            return result2 + additional;
        }

        return y_pixel_to_y_actual.Interpolate(y_pixel);
    }

    public double xPixelToXActual(double x_pixel, double y_pixel) {
        if (y_pixel < 0)
            throw new ValidationException();
        if (y_pixel > max_y_pixel) {  // in case of we need to do "extrapolation", do linear estimation
            var value2 = max_y_pixel;
            var value1 = max_y_pixel * 9.8/10;
            var result2 = xPixelToXActual(x_pixel, value2);
            var result1 = xPixelToXActual(x_pixel, value1);
            var gradient = (result2 - result1) / (value2 - value1);

            var additional = gradient * (y_pixel - value2);    // x2 must be greater than x1 in this case
            return result2 + additional;
        }

        double xRatioPerQuarter = y_pixel_to_x_pixel_quarter_ratio.Interpolate(
            Math.Min(max_y_pixel, y_pixel));
        return x_pixel * 0.25 / xRatioPerQuarter;
    }
}