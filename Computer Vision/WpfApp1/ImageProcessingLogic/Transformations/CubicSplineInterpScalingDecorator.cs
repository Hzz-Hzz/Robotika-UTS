using System.ComponentModel.DataAnnotations;
using MathNet.Numerics.Interpolation;
using WpfApp1;

namespace ImageProcessingLogic.TrainingDataExtractStrategy;

public class CubicSplineInterpScalingDecorator : IContourPointTransformationDecorator
{
    // private static readonly double[] y_actual_source = {0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0};
    private static readonly double[] x_ratio_based_on_y_source =   {464.0, 133.0, 71.0, 50.0, 37.0, 29.0, 26.0, 22.0};  // for every 0.25 of actual X

    private CubicSpline y_pixel_to_x_pixel_quarter_ratio =
        CubicSpline.InterpolatePchipSorted(y_pixel, x_ratio_based_on_y_source);


    private static readonly double[] y_pixel = {0.0, 350.0, 413.0, 437.0, 449.0, 457.0, 462.0, 466.0};
    private static readonly double[] y_actual =   {0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0};
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