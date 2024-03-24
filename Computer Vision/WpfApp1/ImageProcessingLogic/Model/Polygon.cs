using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using Emgu.CV.Util;

namespace WpfApp1;

public class Polygon
{
    private List<Vector2> _points = new();

    public static Polygon fromVectorOfPoint(VectorOfPoint vectorOfPoint) {
        var ret = new Polygon();
        for (int i = 0; i < vectorOfPoint.Size; i++) {
            var point = vectorOfPoint[i];
            ret.addPoint(point.toVector2());
        }

        return ret;
    }

    public Polygon(){}

    public void addPoint(Vector2 point) {
        _points.Add(point);
    }

    public bool checkIfIntersect(Vector2 lineStart, Vector2 lineEnd) {
        for (int i = 0; i < _points.Count; i++) {
            var currentPoint = _points[i];
            var nextPoint = _points[(i + 1) % _points.Count];
            if (RayCastingUtility.checkIfTwoLineIntersect(currentPoint, nextPoint, lineStart, lineEnd))
                return true;
        }
        return false;
    }

    public bool getAllIntersectionPoints(List<Polygon> polygons, Vector2 lineStart, Vector2 lineEnd) {
        foreach (var polygon in polygons) {
            if (!polygon.checkIfIntersect(lineStart, lineEnd))
                return true;
        }
        return false;
    }


    public static bool checkIfIntersectWithAnyPolygon(List<Polygon> polygons, Vector2 lineStart, Vector2 lineEnd) {
        foreach (var polygon in polygons) {
            if (polygon.checkIfIntersect(lineStart, lineEnd))
                return true;
        }
        return false;
    }

    // credits: Gareth Rees, https://stackoverflow.com/a/565282/7069108
}
