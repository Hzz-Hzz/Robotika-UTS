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

    public static Vector2? getIntersectionPoint(Vector2 startLineA, Vector2 endLineA, Vector2 startLineB, Vector2 endLineB) {
        if (!checkIfTwoLineIntersect(startLineA, endLineA, startLineB, endLineB))
            return null;

        var p = startLineA;
        var pr = endLineA - startLineA;
        var q = startLineB;
        var qs = endLineB - startLineB;

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
}

public static class ContourListExtension
{
    public static List<ContourPoint> getIntersectionPoints(this ContourList contourList, Vector2 startLineB, Vector2 endLineB) {
        var ret = new List<ContourPoint>();

        foreach (var contourPoint in contourList.contours) {
            var startLineA = contourPoint.vector2;
            var endLineA = contourPoint.link?.vector2;
            if (endLineA == null)
                continue;

            var intersectionPoint = RayCastingUtility.getIntersectionPoint(startLineA, endLineA.Value, startLineB, endLineB);
            if (intersectionPoint != null)
                ret.Add(new ContourPoint(intersectionPoint.Value.X, intersectionPoint.Value.Y, -1, null));
        }
        return ret;
    }
}