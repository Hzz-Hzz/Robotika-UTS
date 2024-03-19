using System.Collections.Generic;
using System.Drawing;
using Emgu.CV.Util;

namespace WpfApp1;

public class Polygon
{
    private List<Point> _points = new();

    public static Polygon fromVectorOfPoint(VectorOfPoint vectorOfPoint) {
        var ret = new Polygon();
        for (int i = 0; i < vectorOfPoint.Size; i++) {
            var point = vectorOfPoint[i];
            ret.addPoint(point);
        }

        return ret;
    }

    public Polygon(){}

    public void addPoint(Point point) {
        _points.Add(point);
    }

    public bool checkIfIntersect(Point lineStart, Point lineEnd) {
        for (int i = 0; i < _points.Count; i++) {
            var currentPoint = _points[i];
            var nextPoint = _points[(i + 1) % _points.Count];
            if (checkIfTwoLineIntersect(currentPoint, nextPoint, lineStart, lineEnd))
                return true;
        }
        return false;
    }

    public static bool isCounterClockwise(Point a, Point b, Point c) {  // check if A -> B -> C counter-clockwise
        return (c.Y-a.Y) * (b.X-a.X) > (b.Y-a.Y) * (c.X-a.X);
    }

    public static bool checkIfTwoLineIntersect(Point startLineA, Point endLineA, Point startLineB, Point endLineB) {
        return isCounterClockwise(startLineA, startLineB, endLineB) != isCounterClockwise(endLineA, startLineB, endLineB)
               && isCounterClockwise(startLineA, endLineA, startLineB) != isCounterClockwise(startLineA, endLineA, endLineB);
    }



    public static bool checkIfIntersectWithAnyPolygon(List<Polygon> polygons, Point lineStart, Point lineEnd) {
        foreach (var polygon in polygons) {
            if (polygon.checkIfIntersect(lineStart, lineEnd))
                return true;
        }

        return false;
    }
}