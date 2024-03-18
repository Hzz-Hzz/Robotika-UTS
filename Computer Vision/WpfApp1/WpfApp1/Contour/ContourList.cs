using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Util;
using WpfApp1.ContourLinkScoreStrategy;

namespace WpfApp1;

public class ContourList
{
    private int sourceImageWidth;
    private int sourceImageHeight;
    private VectorOfVectorOfPoint rawContours;
    private List<ContourPoint> _contours;
    public List<ContourPoint> contours => _contours;
    public ILinkScoreCalculation linkScoreCalculation;

    private int halfWidth => sourceImageWidth / 2;

    public ContourList(VectorOfVectorOfPoint rawContours, int sourceImageWidth, int sourceImageHeight,
        ILinkScoreCalculation? linkScoreCalculation=null) {
        this.linkScoreCalculation = linkScoreCalculation ?? new ContourLinkBasedOnAreaAndDistance();
        this.rawContours = rawContours;
        this.sourceImageWidth = sourceImageWidth;
        this.sourceImageHeight = sourceImageHeight;
        initializeContourList();
        initializeContourLinks();
    }

    private void initializeContourList() {
        _contours = new List<ContourPoint>(rawContours.Size);
        for (int i = 0; i < this.rawContours.Size; i++) {
            var contour = ContourPoint.fromVectorOfPoint(rawContours[i]);
            if (contour == null)
                continue;
            _contours.Add(contour);
        }
    }

    private void initializeContourLinks() {
        foreach (var contour in _contours) {
            contour.link = getTwoClosesPoint(contour);
        }
    }

    private Tuple<ContourPoint?, ContourPoint?> getTwoClosesPoint(ContourPoint anchor) {
        var closest = new Tuple<double, ContourPoint?>(Double.PositiveInfinity, null);
        var secondClosest = new Tuple<double, ContourPoint?>(Double.PositiveInfinity, null);
        foreach (var contour in _contours) {
            if (contour == anchor)
                continue;

            var score = linkScoreCalculation.getScore(anchor, contour);
            if (score < closest.Item1) {
                secondClosest = closest;
                closest = new Tuple<double, ContourPoint?>(score, contour);
            } else if (score < secondClosest.Item1) {
                secondClosest = new Tuple<double, ContourPoint?>(score, contour);
            }
        }

        return new Tuple<ContourPoint?, ContourPoint?>(closest.Item2, secondClosest.Item2);
    }

}


public class ContourPoint
{
    private double _x;
    private double _y;
    private double _area;
    public Tuple<ContourPoint?, ContourPoint?> link = new Tuple<ContourPoint, ContourPoint>(null, null);

    public double area => _area;
    public double X => _x;
    public double Y => _y;
    public Point point => new Point((int) X, (int) Y);


    public static ContourPoint? fromVectorOfPoint(VectorOfPoint rawContour, Tuple<ContourPoint?, ContourPoint?>? link=null) {
        var res = CvInvoke.Moments(rawContour);
        var x = res.M10 / res.M00;
        var y = res.M01 / res.M00;
        if (Double.IsNaN(x) || Double.IsNaN(y))
            return null;
        var area = CvInvoke.ContourArea(rawContour);
        return new ContourPoint(x, y, area, link ?? new Tuple<ContourPoint?, ContourPoint?>(null, null));
    }

    public ContourPoint(double x, double y, double area, Tuple<ContourPoint?, ContourPoint?> link) {
        if (Double.IsNaN(x)) {
            throw new InvalidDataException("x cannot be NaN");
        }
        if (Double.IsNaN(y)) {
            throw new InvalidDataException("y cannot be NaN");
        }
        _area = area;
        this.link = link;

        _x = x;
        _y = y;
    }


    public double distance(ContourPoint b) {
        return distance(this, b);
    }

    public static double distance(ContourPoint a, ContourPoint b) {
        var diffX = a._x - b._x;
        var diffY = a._y - b._y;
        return Math.Sqrt(diffX * diffX + diffY * diffY);
    }
}