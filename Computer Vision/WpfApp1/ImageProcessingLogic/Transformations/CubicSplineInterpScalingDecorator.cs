using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Emgu.CV;
using MathNet.Numerics.Interpolation;
using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;

public class CubicSplineInterpScalingDecorator : IContourPointTransformationDecorator
{
    private static readonly double[] x_ratio_based_on_y_source =   {317.0, 143, 88, 65, 50, 41, 35, 29, 27, 25};  // for every 2.5 of actual X

    private CubicSpline y_pixel_to_x_pixel_quarter_ratio =
        CubicSpline.InterpolatePchipSorted(y_pixel, x_ratio_based_on_y_source);


    private static readonly double[] y_pixel = {0.0, 193.0, 254.0, 281.0, 296.0, 306.0, 312.0, 318.0, 321, 324};
    private static readonly double[] y_actual =   {0.0, 5.0, 10.0, 15.0, 20.0, 25.0, 30.0, 35.0, 40.0, 45.0};
    private static readonly double max_y_pixel;
    private CubicSpline y_pixel_to_y_actual =
        CubicSpline.InterpolatePchipSorted(y_pixel, y_actual);

    static CubicSplineInterpScalingDecorator() { max_y_pixel = y_pixel.Max(); }
    public IContourPointTransformationDecorator? _decorated { get; set; }


    public CubicSplineInterpScalingDecorator(IContourPointTransformationDecorator? decorated=null) {
        _decorated = decorated;
    }



    public ContourPoint _applyTransformation(
        ContourList _, ContourPoint? point
    ) {
        if (point == null)
            return null;

        var y = yPixelToYActual(point.Y);
        var x = xPixelToXActual(point.X, point.Y);
        var area = point.area;  // idk how to convert this. TODO
        ContourPoint? link = null;
        ContourPoint? backwardLink = null;

        var ret = new ContourPoint(x, y, area, null);
        ret.order = point.order;  // just for make debugging easier
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
        return x_pixel * 2.5 / xRatioPerQuarter;
    }
}