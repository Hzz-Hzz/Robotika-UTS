using System.Numerics;
using DataFrameGenerator.TrainingDataExtractStrategy;
using Microsoft.ML.Data;
using WpfApp1;

namespace PredictorModel;

public class DataModel
{
    public static DataModel fromContourList(ContourList contourList) {
        var verticalOrderingAscendingStrategy = new VerticalOrderingAscendingStrategy();
        var datapoints = verticalOrderingAscendingStrategy.getContourPointAsList(contourList);
        var ret = new DataModel();

        float x, y, xdir, ydir, len;
        fromValue(contourList, getOrNull(datapoints, 0), out x, out y, out xdir, out ydir, out len);
        ret.x_0 = x;
        ret.y_0 = y;
        ret.xdir_0 = xdir;
        ret.ydir_0 = ydir;
        ret.len_0 = len;

        fromValue(contourList, getOrNull(datapoints, 1), out x, out y, out xdir, out ydir, out len);
        ret.x_1 = x;
        ret.y_1 = y;
        ret.xdir_1 = xdir;
        ret.ydir_1 = ydir;
        ret.len_1 = len;

        fromValue(contourList, getOrNull(datapoints, 2), out x, out y, out xdir, out ydir, out len);
        ret.x_2 = x;
        ret.y_2 = y;
        ret.xdir_2 = xdir;
        ret.ydir_2 = ydir;
        ret.len_2 = len;

        fromValue(contourList, getOrNull(datapoints, 3), out x, out y, out xdir, out ydir, out len);
        ret.x_3 = x;
        ret.y_3 = y;
        ret.xdir_3 = xdir;
        ret.ydir_3 = ydir;
        ret.len_3 = len;

        fromValue(contourList, getOrNull(datapoints, 4), out x, out y, out xdir, out ydir, out len);
        ret.x_4 = x;
        ret.y_4 = y;
        ret.xdir_4 = xdir;
        ret.ydir_4 = ydir;
        ret.len_4 = len;

        return ret;
    }

    private static T? getOrNull<T>(List<T> lst, int index) where T: class {
        if (lst.Count <= index) {
            return null;
        }
        return lst[index];
    }

    private readonly static IContourPointTransformationDecorator _transformationDecorator =
        new CubicSplineInterpScalingDecorator(
            new TranslationToCartesiusDecorator());

    private static void fromValue(ContourList contourList, ContourPoint? point,
        out float x, out float y, out float xdir, out float ydir, out float len
    ) {
        if (point == null) {
            x = 0;     y = 0;
            xdir = 0;  ydir = 0;   len = 0;
            return;
        }
        point = _transformationDecorator.applyTransformation(contourList, point, true);
        x = (float)point.X;
        y = (float)point.Y;
        var direction = point.link==null? new Vector2(0,0) : (point.link.vector2 - point.vector2);
        len = direction.Length();

        direction = Vector2.Normalize(direction);
        xdir = direction.X;
        ydir = direction.Y;
    }


    [LoadColumn(0)]
    public float x_1 { set; get; }
    [LoadColumn(1)]
    public float y_1 { set; get; }
    [LoadColumn(2)]
    public float xdir_1 { set; get; }
    [LoadColumn(3)]
    public float ydir_1 { set; get; }
    [LoadColumn(4)]
    public float len_1 { set; get; }

    [LoadColumn(5)]
    public float x_2 { set; get; }
    [LoadColumn(6)]
    public float y_2 { set; get; }
    [LoadColumn(7)]
    public float xdir_2 { set; get; }
    [LoadColumn(8)]
    public float ydir_2 { set; get; }
    [LoadColumn(9)]
    public float len_2 { set; get; }

    [LoadColumn(10)]
    public float x_3 { set; get; }
    [LoadColumn(11)]
    public float y_3 { set; get; }
    [LoadColumn(12)]
    public float xdir_3 { set; get; }
    [LoadColumn(13)]
    public float ydir_3 { set; get; }
    [LoadColumn(14)]
    public float len_3 { set; get; }

    [LoadColumn(15)]
    public float x_4 { set; get; }
    [LoadColumn(16)]
    public float y_4 { set; get; }
    [LoadColumn(17)]
    public float xdir_4 { set; get; }
    [LoadColumn(18)]
    public float ydir_4 { set; get; }
    [LoadColumn(19)]
    public float len_4 { set; get; }

    [LoadColumn(20)]
    public float x_0 { set; get; }
    [LoadColumn(21)]
    public float y_0 { set; get; }
    [LoadColumn(22)]
    public float xdir_0 { set; get; }
    [LoadColumn(23)]
    public float ydir_0 { set; get; }
    [LoadColumn(24)]
    public float len_0 { set; get; }

    [LoadColumn(25)]  // for training only It's not the resulting prediction
    public float angle { set; get; }
    [LoadColumn(26)]  // for training only. It's not the resulting prediction
    public float speed { set; get; }

    // [ColumnName("Score")]  // The default value for prediction result is "Score"
    // public float PredictionResult { get; set; }
}