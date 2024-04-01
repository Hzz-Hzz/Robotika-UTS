using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using Emgu.CV;
using Emgu.CV.Util;

namespace WpfApp1;


public class ContourPoint
{
    private double _x;
    private double _y;
    private double _area;
    public int? order;

    private ContourPoint? _link;
    private ContourPoint? _backwardLink;

    public ContourPoint? link { // link to the next 'shortest' node
        get {
            return _link;
        }
        set {
            if (_link != null)
                _link._backwardLink = null;
            _link = value;
            if (value != null) {
                if (value._backwardLink != null)
                    value._backwardLink._link = null;
                value._backwardLink = this;
            }
        }
    }

    public ContourPoint? backwardLink { // link to which node refer this node as their shortest node
        get {
            return _backwardLink;
        }
        set {
            if (_backwardLink != null) {
                _backwardLink._link = null;
            }
            _backwardLink = value;
            if (value != null) {
                if (value._link != null)
                    value._link._backwardLink = null;
                value._link = this;
            }
        }
    }

    public double area => _area;
    public double X => _x;
    public double Y => _y;
    public Point point => new Point((int)X, (int)Y);
    public Vector2 vector2 => new Vector2((float) X, (float) Y);


    public double? getThisAngle() {
        if (link == null)
            return null;
        if (backwardLink == null)
            return null;

        return calculateAngle(backwardLink, this, link);
    }

    public bool isOutlier(double radiansThreshold) {
        var thisAngle = getThisAngle();
        return thisAngle < radiansThreshold;
    }


    public void deleteThis() {  // just like remove a linked list
        if (this.backwardLink != null)
            this.backwardLink.link = this.link;
        if (this.link != null)
            this.link.backwardLink = this.backwardLink;
        this.link = null;
        this.backwardLink = null;
    }


    public static ContourPoint? fromVectorOfPoint(VectorOfPoint rawContour, ContourPoint? link=null) {
        var res = CvInvoke.Moments(rawContour);
        var x = res.M10 / res.M00;
        var y = res.M01 / res.M00;
        if (Double.IsNaN(x) || Double.IsNaN(y))
            return null;
        var area = CvInvoke.ContourArea(rawContour);
        return new ContourPoint(x, y, area, link);
    }

    public ContourPoint(double x, double y, double area, ContourPoint? link) {
        if (Double.IsNaN(x)) {
            throw new InvalidDataException("x cannot be NaN");
        }
        if (Double.IsNaN(y)) {
            throw new InvalidDataException("y cannot be NaN");
        }

        this.link = link;
        _area = area;

        _x = x;
        _y = y;
    }


    public double distance(ContourPoint b, double weightX=1.0, double weightY=1.0) {
        return distance(this, b, weightX, weightY);
    }

    public static double distance(ContourPoint a, ContourPoint b, double weightX=1.0, double weightY=1.0) {
        var diffX = a._x - b._x;
        var diffY = a._y - b._y;
        return Math.Sqrt(weightX * diffX * diffX +  weightY * diffY * diffY);
    }

    // calculate angle (radians) made by 3 points
    public static double calculateAngle(ContourPoint p1, ContourPoint p2, ContourPoint p3) {
        var P12 = p1.distance(p2);
        var P23 = p2.distance(p3);
        var P13 = p1.distance(p3);
        var ret = Math.Acos((P12 * P12 + P23 * P23 - P13 * P13) / (2 * P12 * P23));
        if (Double.IsNaN(ret))  // for example if the three points form a single line
            return 0;
        return ret;
    }

    public static bool inTheSameLink(ContourPoint a, ContourPoint b) {
        if (a == null || b == null)
            return false;
        var currA = a;
        var currB = b;
        while (currA.link != null) {
            currA = currA.link;
        }
        while (currB.link != null) {
            currB = currB.link;
        }
        return currA == currB;
    }

    public List<ContourPoint> getThisAndAllNextLinks() {
        var ret = new List<ContourPoint>();
        var curr = this;
        while (curr != null) {
            ret.Add(curr);
            curr = curr.link;
        }
        return ret;
    }
}