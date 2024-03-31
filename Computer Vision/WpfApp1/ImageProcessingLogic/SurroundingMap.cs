global using AngleRecommendation = System.Tuple<float, double, System.Numerics.Vector2>;
global using AngleRecommendationsReturnType = System.Collections.Generic.List<System.Tuple<float, double, System.Numerics.Vector2>>;

using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Windows.Documents;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using ImageProcessingLogic.TrainingDataExtractStrategy;
using MathNet.Numerics;

namespace WpfApp1;

public class SurroundingMap
{
    private ContourList roadEdgeList;

    private const float worldSpaceStartX = -20;
    private const float worldSpaceEndX = 20;
    private const float worldSpaceStartY = 0;
    private const float worldSpaceEndY = 10;
    private readonly float maximumDegree = (float) Math.PI / 36f;


    private static IContourPointTransformationDecorator _transformation = new TranslationToCartesiusDecorator(
        new CubicSplineInterpScalingDecorator());
    public static SurroundingMap fromCameraContourList(ContourList contourList) {
        var transformedContourList = contourList.applyTransformation(_transformation);
        var ret = new SurroundingMap(transformedContourList);
        return ret;
    }

    private Vector2 origin = Vector2.Zero;
    public ContourList? intersectionPoints { get; private set; }


    public const float raycastLength = 10;
    private List<Vector2> raycastLines {
        get {
            var angleRange = getMinAndMaxAngleFromContourPoints(roadEdgeList.contours);

            var rayCastLines = getCircleRayCastLines(origin, raycastLength,
                angleRange.Item1+0.01f, angleRange.Item2-0.01f, maximumDegree);
            return rayCastLines;
        }
    }


    private AngleRecommendationsReturnType recommendedAngles;

    public void updateIntersectionPoints(bool includeAnglesThatDoesNotIntersects=true, float extensionLength=0f) {
        var rayCastLines = this.raycastLines;
        // to make a smooth angle recommendatin, we should include a perfect 90 degree raycast
        // rayCastLines.AddRange(getCircleRayCastLines(origin, 10, (float)Math.PI/2,
            // (float)Math.PI/2+0.001f, 10));  // 10 is quite arbitrary

        var result = new List<ContourPoint>();
        offroad = false;


        foreach (var rayCastTarget in rayCastLines) {
            var raycastResult = roadEdgeList.getIntersectionPoints(origin, rayCastTarget, extensionLength);
            ifOneRayIntersectsTwoPointFartherThanThresholdMeansWeReOffRoad(raycastResult);
            var shortestCollisionPoint = selectClosestPoint(origin, raycastResult);
            if (shortestCollisionPoint != null)
                result.Add(shortestCollisionPoint);
            else if (includeAnglesThatDoesNotIntersects)
                result.Add(rayCastTarget.toContourPoint());
        }
        intersectionPoints = new ContourList(result, -1, -1);
        recommendedAngles = calculateRecommendedIntersectionPoints();
        if (offroad)
            handleOffRoadByModifyingRecommendedAngles();
    }

    private bool offroad = false;
    private void ifOneRayIntersectsTwoPointFartherThanThresholdMeansWeReOffRoad(List<ContourPoint> raycastResult) {
        var horizontallyClosest = getVerticallyClosestPointOnLeftAndOnRight();
        if (horizontallyClosest.Item1 != null
            && horizontallyClosest.Item2 != null
            && !ContourPoint.inTheSameLink(horizontallyClosest.Item1, horizontallyClosest.Item2))
            return;

        raycastResult.Sort((a, b) => distanceFromOrigin(a).CompareTo(distanceFromOrigin(b)));
        var threshold = 4.5;
        for (int i = 1; i < raycastResult.Count; i++) {
            if (distanceFromOrigin(raycastResult[i]) -
                distanceFromOrigin(raycastResult[i - 1]) > threshold) {
                offroad = true;
                ContourPoint.inTheSameLink(horizontallyClosest.Item1, horizontallyClosest.Item2);
            }
        }
    }

    private void handleOffRoadByModifyingRecommendedAngles() {
        var angleSign = roadEdgesMostlyOnRightSide() ? 1 : -1;
        var avgAngle = getAverageAngleBasedOnRoadEdgeVectorDirections();
        for (int i = 0; i < recommendedAngles.Count; i++) {
            var rec = recommendedAngles[i];
            recommendedAngles[i] = new AngleRecommendation(rec.Item1,
                angleSign*(60 - Math.Abs(avgAngle)), rec.Item3);
        }
        throwIfAnyAngleIsNaN(recommendedAngles);
        Console.WriteLine($"OFFROAD  {getAverageAngleBasedOnRoadEdgeVectorDirections()}");
    }

    private bool roadEdgesMostlyOnRightSide() {
        var left = 0;
        var right = 0;
        foreach (var contourPoint in roadEdgeList.contours) {
            if (contourPoint.X > 0)
                right++;
            else if (contourPoint.X < 0)
                left++;
        }

        return left < right;
    }

    /**
     * Find horizontally-closest road edge
     */
    public Tuple<Vector2?, Vector2?> getHorizontallyClosestPointOnLeftAndOnRight() {
        var leftPoints = getRoadEdgeListOnOneSide(true);
        var rightPoints = getRoadEdgeListOnOneSide(false);
        var leftClosest = leftPoints.MinBy((e) => Math.Abs(e.X));  // find the one closest to zero
        var rightClosest = rightPoints.MinBy((e) => Math.Abs(e.X));
        return new Tuple<Vector2?, Vector2?>(leftClosest?.vector2, rightClosest?.vector2);
    }
    public Tuple<ContourPoint?, ContourPoint?> getVerticallyClosestPointOnLeftAndOnRight() {
        var leftPoints = getRoadEdgeListOnOneSide(true);
        var rightPoints = getRoadEdgeListOnOneSide(false);
        var leftClosest = leftPoints.MinBy((e) => Math.Abs(e.Y));  // find the one closest to zero
        var rightClosest = rightPoints.MinBy((e) => Math.Abs(e.Y));
        return new Tuple<ContourPoint?, ContourPoint?>(leftClosest, rightClosest);
    }

    public List<ContourPoint> getRoadEdgeListOnOneSide(bool leftSide) {
        Func<ContourPoint, bool> condition = (e => e.X <= 0);
        if (!leftSide)
            condition = (e => e.X >= 0);
        return roadEdgeList.contours.Where(condition).ToList();
    }

    /**
     * See docs of calculateRecommendedIntersectionPoints
     */
    public AngleRecommendationsReturnType getMostRecommendedIntersectionPoints() {
        var angleRangeLeftRight = getMinAndMaxAngleFromContourPoints(roadEdgeList.contours);
        var angleRange = Math.Abs(angleRangeLeftRight.Item1 - angleRangeLeftRight.Item2);

        var anglePriority = getAverageAngleBasedOnRoadEdgeVectorDirections();

        anglePriority = (float)(Math.PI / 2 - anglePriority);  // clockwise
        // 0.01 to anticipate floating errors
        throwIfAnyAngleIsNaN(recommendedAngles);
        var averageOfRecommendedGroups = getAverageOfAdjacentVectorsGrouping(recommendedAngles, maximumDegree+0.01f);
        averageOfRecommendedGroups.Sort((a,b)=>
            -priorityScoreCalculation(a,anglePriority).CompareTo(priorityScoreCalculation(b,anglePriority)));
        throwIfAnyAngleIsNaN(averageOfRecommendedGroups);

        var minimumAngleRangeBeforeInfluenced = 90*Math.PI / 180;
        // if (angleRange < minimumAngleRangeBeforeInfluenced)
            // influenceAnglesBasedOnOtherVectors(averageOfRecommendedGroups, anglePriority,
                // (float)(1 - angleRange / minimumAngleRangeBeforeInfluenced));

        throwIfAnyAngleIsNaN(averageOfRecommendedGroups);
        return averageOfRecommendedGroups;
    }

    private void throwIfAnyAngleIsNaN(AngleRecommendationsReturnType list) {
        foreach (var i in list) {
            if (Double.IsNaN(i.Item2))
                throw new Exception("NaN is found");
        }
    }

    // Harmful buat "correcting position" ketika off-road
    private void influenceAnglesBasedOnOtherVectors(AngleRecommendationsReturnType recommended, float influencerAngle, float influencePercentage) {
        for (int i = 0; i < recommended.Count; i++) {
            var curr = recommended[i];
            var newAngle = curr.Item2 + influencerAngle * influencePercentage;
            var newDirectionVector = Vector2.UnitY.rotate(-newAngle);  // negative because rotate() is CCW, meanwhile our angles are clockwise
            recommended[i] = new AngleRecommendation(curr.Item1, newAngle, newDirectionVector);
        }
    }

    public float getAverageAngleBasedOnRoadEdgeVectorDirections() {
        var sum = 0.0;
        var count = 0;
        foreach (var contourPoint in this.roadEdgeList.contours) {
            var angle = contourPoint.getThisAngle();
            if (angle == null) continue;
            sum += angle.Value;
            count++;
        }
        if (count == 0) return 0;
        return (float)sum / count;
    }

    /**
     * return list of recommendations, sorted by most-recommended (left) to the least recommended but still recommended (right).
     * Each item will be a tuple represents (distance, angle in rads).
     * Angle in rads, will be 0 if you should go forward,
     * positive if you should go right,
     * and negative if you should go left.
     */
    private AngleRecommendationsReturnType calculateRecommendedIntersectionPoints() {
        var ret = intersectionPoints.contours.Select(e => new Tuple<float, double, Vector2>(
            (e.vector2 - origin).Length(), getGlobalAngle(e.vector2), e.vector2)).ToList();
        if (ret.Count == 0)
            return ret;

        var best = ret.MaxBy(e => priorityScoreCalculation(e));
        ret = ret.Where(e
            => Math.Abs(best.Item1 - e.Item1) / best.Item1 < 0.15f
        ).ToList();

        return ret;
    }

    private double getGlobalAngle(Vector2 vector2) {
        return vector2.getAngleBetween(Vector2.UnitY);
    }

    private AngleRecommendationsReturnType getAverageOfAdjacentVectorsGrouping(AngleRecommendationsReturnType angleRecommendations, float maximumAdjacentDegree) {
        if (angleRecommendations.Count == 0)
            return angleRecommendations;
        angleRecommendations = angleRecommendations.shallowCopy();
        angleRecommendations.Sort((tuple1, tuple2) => tuple1.Item2.CompareTo(tuple2.Item2));  // sort by angle

        AngleRecommendationsReturnType averageResults = new();

        var multiplyTuple = (AngleRecommendation t, float constant) =>
            new AngleRecommendation(t.Item1 * constant, t.Item2 * constant, t.Item3 * constant);
        var addTuple = (AngleRecommendation t, AngleRecommendation u) =>
            new AngleRecommendation(t.Item1 + u.Item1, t.Item2 + u.Item2, t.Item3 + u.Item3);

        var summation = new AngleRecommendation(0, 0, Vector2.Zero);
        var numberOfMembers = 0;
        double? angleOfPrevMember = null;
        foreach (var angleRec in angleRecommendations) {
            angleOfPrevMember ??= angleRec.Item2;  // useful for first iteration only
            if (Math.Abs(angleRec.Item2 - angleOfPrevMember.Value) > maximumAdjacentDegree) {
                averageResults.Add(multiplyTuple(summation, 1f/numberOfMembers));
                summation = new AngleRecommendation(0, 0, Vector2.Zero);
                numberOfMembers = 0;
            }
            summation = addTuple(summation, angleRec);
            numberOfMembers++;
            angleOfPrevMember = angleRec.Item2;
        }
        averageResults.Add(multiplyTuple(summation, 1f/numberOfMembers));
        throwIfAnyAngleIsNaN(averageResults);
        return averageResults;
    }

    // maximize score
    private double priorityScoreCalculation(Tuple<float, double, Vector2> distanceAndAngle, float anglePriority=0) {
        var distance = distanceAndAngle.Item1;
        var angle = distanceAndAngle.Item2;

        // anglePriority=0 means prioritize the one closer to 0 degree (straight forward).
        // Give negative sign to sort it ascendingly (because we will sort the overall score descendingly)
        var angleStraightness = -Math.Abs(angle - anglePriority);

        var distancePriorityWeight = 100;
        var distanceScore = distance * distancePriorityWeight;  // distance is more prioritized than angle straightness
        return distanceScore + angleStraightness;
    }



    private ContourPoint? selectClosestPoint(Vector2 originPoint, List<ContourPoint> contourPoints) {
        if (contourPoints.Count == 0)
            return null;
        ContourPoint? ret = null;
        var minLen = Double.PositiveInfinity;
        foreach (var contourPoint in contourPoints) {
            var length = distanceFromOrigin(contourPoint, originPoint);
            if (length < minLen) {
                minLen = length;
                ret = contourPoint;
            }
        }
        return ret;
    }

    private float distanceFromOrigin(ContourPoint contourPoint, Vector2? origin=null) {
        origin ??= this.origin;
        return (contourPoint.vector2 - origin.Value).Length();
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

        while (currentAngle <= endAngle) {
            var y = (float)(rayCastLength * Math.Sin(currentAngle));
            var x = (float)(rayCastLength * Math.Cos(currentAngle));
            ret.Add(new Vector2(origin.X + x, origin.Y + y));
            currentAngle += stepAngle;
        }
        return ret;
    }






    private ContourDrawer roadEdgeContourPointDrawer = new (3, new MCvScalar(255, 255, 255), LineType.Filled);
    private ContourDrawer roadEdgeContourPointDirectionDrawer = new (1, new MCvScalar(150, 255, 150), LineType.FourConnected);
    private ContourDrawer intersectionContourPointDrawer = new (2, new MCvScalar(255, 150, 150), LineType.Filled);
    public void drawOnMat(Mat mat) {
        var transformerToDrawOnMat = new TranslateContourPointToItOnDrawOnMat(mat.Width, mat.Height,
            worldSpaceStartX, worldSpaceStartY,
            worldSpaceEndX, worldSpaceEndY);

        var transformedRoadEdgeList = roadEdgeList.applyTransformation(transformerToDrawOnMat);

        roadEdgeContourPointDrawer.drawContourPoints(transformedRoadEdgeList, mat, 1);
        roadEdgeContourPointDirectionDrawer.drawContourLinks(transformedRoadEdgeList, mat, 0.1);
        if (intersectionPoints == null)
            return;

        var originContourPoint = transformerToDrawOnMat._applyTransformation(null, origin.toContourPoint());
        Debug.Assert(originContourPoint != null);

        drawRaycastLinesUntilItsEnd(mat, originContourPoint, transformerToDrawOnMat);
        drawRaycastLinesUntilCollisionPoint(mat, originContourPoint, transformerToDrawOnMat);
        if (recommendedAngles.Count > 0) {
            var mostRecommended = transformerToDrawOnMat._applyTransformation(null,
                getMostRecommendedIntersectionPoints()[0].Item3.toContourPoint());
            CvInvoke.Line(mat, originContourPoint.point,  mostRecommended!.point, new MCvScalar(0, 255, 0));
        }
    }

    private void drawRaycastLinesUntilItsEnd(Mat mat, ContourPoint originContourPoint, IContourPointTransformationDecorator transformerToDrawOnMat) {
        var targetRayCastAsContourList = new ContourList(raycastLines.Select(e => e.toContourPoint()).ToList(), -1, -1);
        var targetRayCast = targetRayCastAsContourList.applyTransformation(transformerToDrawOnMat).contours;
        foreach (var targetPoint in targetRayCast) {
            CvInvoke.Line(mat, originContourPoint.point, targetPoint.point,
                new MCvScalar(50, 50, 105), 1, LineType.FourConnected);
        }
    }

    private void drawRaycastLinesUntilCollisionPoint(Mat mat, ContourPoint originContourPoint,
        IContourPointTransformationDecorator transformerToDrawOnMat
    ) {
        var transformedIntersectionPoints = intersectionPoints.applyTransformation(transformerToDrawOnMat);
        intersectionContourPointDrawer.drawContourPoints(transformedIntersectionPoints, mat, 1);
        foreach (var targetPoint in transformedIntersectionPoints.contours) {
            CvInvoke.Line(mat, originContourPoint.point, targetPoint.point,
                new MCvScalar(255, 200, 200), 1, LineType.FourConnected);
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
        var y = matHeight - (transformationY + point.Y) * scalingY;
        var area = point.area;  // dont know what to do
        var ret = new ContourPoint(x, y, area, null);
        ret.order = point.order;  // just for make debugging easier
        return ret;
    }
}


public static class Vector2ExtensionSurroundingMap
{
    public static double getVectorAngle(this Vector2 from) {
        var ret = Math.Atan2(from.Y, from.X);
        ret += 2 * Math.PI;
        ret %= 2*Math.PI;
        return ret;
    }

    public static double getAngleBetween(this Vector2 from, Vector2 to, bool ignoreAngleDirection=false) {
        var ret = to.getVectorAngle() - from.getVectorAngle();
        if (ignoreAngleDirection) {
            ret += 2 * Math.PI;
            ret %= 2*Math.PI;
        }
        return ret;
    }

    public static ContourPoint toContourPoint(this Vector2 vector2) {
        return new ContourPoint(vector2.X, vector2.Y, -1, null);
    }

    public static Vector2 rotate(this Vector2 self, double rotationAngle) {
        var angle = self.getVectorAngle();
        angle += rotationAngle;
        var x = self.X * Math.Cos(angle) - self.Y * Math.Sin(angle);
        var y = self.X * Math.Sin(angle) + self.Y * Math.Cos(angle);
        return new Vector2((float)x, (float)y);
    }
}

public static class ListCopyExtension
{
    public static List<T> shallowCopy<T>(this List<T> list) {
        return list.Select(e => e).ToList();
    }

}