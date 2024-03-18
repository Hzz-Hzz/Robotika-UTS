using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WpfApp1;

public class ContourDrawer
{
    private int radius;
    private int thickness;
    private MCvScalar color;
    private LineType lineType;

    public ContourDrawer(int radius, int thickness, MCvScalar color, LineType lineType = LineType.Filled) {
        this.radius = radius;
        this.thickness = thickness;
        this.color = color;
        this.lineType = lineType;
    }

    public void drawContourPoints(ContourList contourList, Mat targetMat) {
        foreach (var contour in contourList.contours) {
            CvInvoke.Circle(targetMat, contour.point, radius, color, thickness, lineType);
        }
    }

    public void drawContourLinks(ContourList contourList, Mat targetMat) {
        foreach (var contour in contourList.contours) {
            if (contour.link.Item1 != null)
                CvInvoke.Line(targetMat, contour.link.Item1.point, contour.point, color, thickness, lineType);
            if (contour.link.Item2 != null)
                CvInvoke.Line(targetMat, contour.link.Item2.point, contour.point, color, thickness, lineType);
        }
    }
}