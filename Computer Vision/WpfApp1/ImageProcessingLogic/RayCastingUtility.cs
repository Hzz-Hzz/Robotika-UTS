using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace WpfApp1;

public static class RayCastingUtility
{
    public static bool isCounterClockwise(Vector2 a, Vector2 b, Vector2 c) {  // check if A -> B -> C counter-clockwise
        return (c.Y-a.Y) * (b.X-a.X) > (b.Y-a.Y) * (c.X-a.X);
    }

    public static bool checkIfTwoLineIntersect(Vector2 startLineA, Vector2 endLineA, Vector2 startLineB, Vector2 endLineB) {
        return isCounterClockwise(startLineA, startLineB, endLineB) != isCounterClockwise(endLineA, startLineB, endLineB)
               && isCounterClockwise(startLineA, endLineA, startLineB) != isCounterClockwise(startLineA, endLineA, endLineB);
    }

    public static Vector2? getIntersectionPoint(Vector2 startLineA, Vector2 endLineA, Vector2 startLineB,
        Vector2 endLineB, float extensionLength=0.0f
    ) {
        // if (!checkIfTwoLineIntersect(startLineA, endLineA, startLineB, endLineB))
            // return null;

        var p = startLineA;
        var pr = endLineA - startLineA;
        pr = pr.getVectorOfSameDirection(pr.Length() + extensionLength);
        var q = startLineB;
        var qs = endLineB - startLineB;
        qs = qs.getVectorOfSameDirection(qs.Length() + extensionLength);

        var prqsCrossProduct = pr.Cross(qs);
        var pqCrossPr = (q - p).Cross(pr);

        if (prqsCrossProduct == 0 && pqCrossPr == 0) {
            return null;  // Collinear
        }
        if (prqsCrossProduct == 0 && pqCrossPr != 0) {
            Debug.Assert(!checkIfTwoLineIntersect(p, p+pr, q, q+qs));
            return null;  // Parallel, not intersecting
        }

        var multiplierPR = (q - p).Cross(qs) / prqsCrossProduct;
        var multiplierQS = pqCrossPr / prqsCrossProduct;

        // +- 0.01 is just to anticipate rounding errors
        if (multiplierPR < -0.01 || multiplierPR > 1.01 || multiplierQS < -0.01 || multiplierQS  > 1.01) {
            Debug.Assert(!checkIfTwoLineIntersect(p, p+pr, q, q+qs));
            return null;  // Intersects but not within the segments boundary
        }

        var result1 = p + multiplierPR * pr;
        var result2 = q + multiplierQS * qs;  // should be the same as result1
        Debug.Assert((result1 - result2).LengthSquared() < 0.001);
        return result1;
    }
}





public static class PointExtension
{
    public static Vector2 toVector2(this Point point) {
        return new Vector2(point.X, point.Y);
    }
}

public static class Vector2Extension
{
    public static float Cross(this Vector2 self, Vector2 other) {
        return self.X * other.Y - self.Y * other.X;
    }

    public static Vector2 getVectorOfSameDirection(this Vector2 self, float newLength) {
        return Vector2.Normalize(self) * newLength;
    }
}

public static class ContourListExtension
{
    public static List<ContourPoint> getIntersectionPoints(this ContourList contourList, Vector2 startLineB, Vector2 endLineB, float extensionLength=0.0f) {
        var ret = new List<ContourPoint>();

        foreach (var contourPoint in contourList.contours) {
            var startLineA = contourPoint.vector2;
            var endLineA = contourPoint.link?.vector2;
            if (endLineA == null)
                continue;

            var intersectionPoint = RayCastingUtility.getIntersectionPoint(startLineA, endLineA.Value,
                startLineB, endLineB, extensionLength);
            if (intersectionPoint != null)
                ret.Add(new ContourPoint(intersectionPoint.Value.X, intersectionPoint.Value.Y, -1, null));
        }
        return ret;
    }
    public static List<ContourPoint> getClosestIntersectionPointsTowardCircle(
        this ContourList contourList, Vector2 startLine, Vector2 endLine, float radius,
        float minimumIntersectionDistance, Func<ContourPoint, bool>? condition=null
    ) {
        condition ??= (_) => true;
        var ret = new List<ContourPoint>();

        foreach (var contourPoint in contourList.contours) {
            if (!condition.Invoke(contourPoint))
                continue;
            var circleCxCy = contourPoint.vector2;
            var intersectionPoint = CircleRadiusIntersection.ClosestIntersection(circleCxCy, radius, startLine, endLine);
            if (intersectionPoint != null && (intersectionPoint - startLine).Value.Length() >= minimumIntersectionDistance)
                ret.Add(new ContourPoint(intersectionPoint.Value.X, intersectionPoint.Value.Y, -1, null));
        }
        return ret;
    }
}


public static class CircleRadiusIntersection
{  // credits: https://stackoverflow.com/a/23017208/7069108


    //cx,cy is center point of the circle
    public static Vector2? ClosestIntersection(Vector2 cxcy, float radius,
                                      Vector2 lineStart, Vector2 lineEnd) {
        var cx = cxcy.X;
        var cy = cxcy.Y;
        Vector2 intersection1;
        Vector2 intersection2;
        int intersections = FindLineCircleIntersections(cx, cy, radius, lineStart, lineEnd, out intersection1, out intersection2);

        if (intersections == 1)
            return intersection1; // one intersection

        if (intersections == 2)
        {
            double dist1 = Vector2.Distance(intersection1, lineStart);
            double dist2 = Vector2.Distance(intersection2, lineStart);

            if (dist1 < dist2)
                return intersection1;
            else
                return intersection2;
        }

        return null; // no intersections at all
    }

    private static double Distance(PointF p1, PointF p2)
    {
        return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
    }

    // Find the points of intersection.
    private static int FindLineCircleIntersections(float cx, float cy, float radius,
                                            Vector2 point1, Vector2 point2, out Vector2 intersection1,
                                            out Vector2 intersection2)
    {
        float dx, dy, A, B, C, det, t;

        dx = point2.X - point1.X;
        dy = point2.Y - point1.Y;

        A = dx * dx + dy * dy;
        B = 2 * (dx * (point1.X - cx) + dy * (point1.Y - cy));
        C = (point1.X - cx) * (point1.X - cx) + (point1.Y - cy) * (point1.Y - cy) - radius * radius;

        det = B * B - 4 * A * C;
        if ((A <= 0.0000001) || (det < 0))
        {
            // No real solutions.
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if (det == 0)
        {
            // One solution.
            t = -B / (2 * A);
            intersection1 = new Vector2(point1.X + t * dx, point1.Y + t * dy);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 1;
        }
        else
        {
            // Two solutions.
            t = (float)((-B + Math.Sqrt(det)) / (2 * A));
            intersection1 = new Vector2(point1.X + t * dx, point1.Y + t * dy);
            t = (float)((-B - Math.Sqrt(det)) / (2 * A));
            intersection2 = new Vector2(point1.X + t * dx, point1.Y + t * dy);
            return 2;
        }
    }
}