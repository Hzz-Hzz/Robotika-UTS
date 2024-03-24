using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Windows.Documents;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessingLogic.TrainingDataExtractStrategy;

namespace WpfApp1;

public class SurroundingMap
{
    private ContourList roadEdgeList;

    private const float worldSpaceStartX = -6;
    private const float worldSpaceEndX = 6;
    private const float worldSpaceStartY = 0;
    private const float worldSpaceEndY = 10;



    private static IContourPointTransformationDecorator _transformation = new TranslationToCartesiusDecorator(
        new CubicSplineInterpScalingDecorator());
    public static SurroundingMap fromCameraContourList(ContourList contourList) {
        var transformedContourList = contourList.applyTransformation(_transformation);
        var ret = new SurroundingMap(transformedContourList);
        return ret;
    }

    private Vector2 origin = Vector2.Zero;
    public ContourList? intersectionPoints { get; private set; }

    public void updateIntersectionPoints() {
        var angleRange = getMinAndMaxAngleFromContourPoints(roadEdgeList.contours);

        var rayCastLines = getCircleRayCastLines(origin, 10,
            angleRange.Item1+0.1f, angleRange.Item2-0.1f, (float)Math.PI / 36);

        var result = new List<ContourPoint>();
        foreach (var rayCastTarget in rayCastLines) {
            var raycastResult = roadEdgeList.getIntersectionPoints(origin, rayCastTarget);
            result.AddRange(raycastResult);
        }

        intersectionPoints = new ContourList(result, -1, -1);
    }

    private static Tuple<float, float> getMinAndMaxAngleFromContourPoints(List<ContourPoint> contourPoints) {
        double min = Math.PI/2;
        double max = Math.PI/2;  // 90 degree initial point

        foreach (var contourPoint in contourPoints) {
            var angle = contourPoint.vector2.getVectorAngle();
            min = Math.Min(min, angle);
            max = Math.Max(max, angle);
        }

        return new Tuple<float, float>((float)min, (float)max);
    }


    /**
     * Angles are in radians
     */
    private static List<Vector2> getCircleRayCastLines(
        Vector2 origin, float rayCastLength, float startAngle, float endAngle, float stepAngle
    ) {
        var ret = new List<Vector2>();
        float currentAngle = startAngle;

        while (currentAngle < endAngle) {
            var y = (float)(rayCastLength * Math.Sin(currentAngle));
            var x = (float)(rayCastLength * Math.Cos(currentAngle));
            ret.Add(new Vector2(origin.X + x, origin.Y + y));
            currentAngle += stepAngle;
        }
        return ret;
    }






    private ContourDrawer roadEdgeContourPointDrawer = new (3, new MCvScalar(255, 255, 255), LineType.Filled);
    private ContourDrawer intersectionContourPointDrawer = new (3, new MCvScalar(0, 0, 255), LineType.Filled);
    public void drawOnMat(Mat mat) {
        var transformerToDrawOnMat = new TranslateContourPointToItOnDrawOnMat(mat.Width, mat.Height,
            worldSpaceStartX, worldSpaceStartY,
            worldSpaceEndX, worldSpaceEndY);

        var transformedRoadEdgeList = roadEdgeList.applyTransformation(transformerToDrawOnMat);

        roadEdgeContourPointDrawer.drawContourPoints(transformedRoadEdgeList, mat, 1);
        roadEdgeContourPointDrawer.drawContourLinks(transformedRoadEdgeList, mat, 0.1);
        if (intersectionPoints == null)
            return;

        var originContourPoint = transformerToDrawOnMat._applyTransformation(null, origin.toContourPoint());
        Debug.Assert(originContourPoint != null);

        var transformedIntersectionPoints = intersectionPoints.applyTransformation(transformerToDrawOnMat);
        intersectionContourPointDrawer.drawContourPoints(transformedIntersectionPoints, mat, 1);
        foreach (var targetPoint in transformedIntersectionPoints.contours) {
            CvInvoke.Line(mat, originContourPoint.point, targetPoint.point,
                new MCvScalar(240, 240, 255), 1, LineType.FourConnected);
        }
    }


    private SurroundingMap(ContourList roadEdgeList) {
        this.roadEdgeList = roadEdgeList;
    }
}

class TranslateContourPointToItOnDrawOnMat : IContourPointTransformationDecorator
{
    private float matWidth;
    private float matHeight;
    private float worldSpaceStartX;
    private float worldSpaceEndX;
    private float worldSpaceStartY;
    private float worldSpaceEndY;

    private float worldSpaceWidth => worldSpaceEndX - worldSpaceStartX;
    private float worldSpaceHeight => worldSpaceEndY - worldSpaceStartY;

    public TranslateContourPointToItOnDrawOnMat(int matWidth, int matHeight,
        float contourPointWorldSpaceStartX, float contourPointWorldSpaceStartY,
        float contourPointWorldSpaceEndX, float contourPointWorldSpaceEndY,
        CubicSplineInterpScalingDecorator? decorator=null
    ) {
        this.matWidth = matWidth;
        this.matHeight = matHeight;
        worldSpaceStartX = contourPointWorldSpaceStartX;
        worldSpaceStartY = contourPointWorldSpaceStartY;
        worldSpaceEndX = contourPointWorldSpaceEndX;
        worldSpaceEndY = contourPointWorldSpaceEndY;
        _decorated = decorator;
    }

    public IContourPointTransformationDecorator? _decorated { get; set; }
    public ContourPoint? _applyTransformation(ContourList originalContourList, ContourPoint? point) {
        var scalingX = matWidth / worldSpaceWidth;
        var scalingY = matHeight / worldSpaceHeight;
        var transformationX = -worldSpaceStartX;
        var transformationY = -worldSpaceStartY;

        var x = (transformationX + point.X) * scalingX;
        var y = (transformationY + point.Y) * scalingY;
        var area = point.area;  // dont know what to do
        var ret = new ContourPoint(x, y, area, null);
        return ret;
    }
}


public static class Vector2ExtensionSurroundingMap
{
    public static double getVectorAngle(this Vector2 from) {
        var ret = Math.Atan2(from.Y, from.X);
        ret %= 360.0;
        return ret;
    }

    public static double getAngleBetween(this Vector2 from, Vector2 to, bool ignoreAngleDirection=false) {
        var ret = to.getVectorAngle() - from.getVectorAngle();
        if (ignoreAngleDirection) {
            ret %= 360.0;
            ret += 360.0;
            ret %= 360.0;
        }
        return ret;
    }

    public static ContourPoint toContourPoint(this Vector2 vector2) {
        return new ContourPoint(vector2.X, vector2.Y, -1, null);
    }
}