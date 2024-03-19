using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WpfApp1;

public class ContourDrawer
{
    private int thickness;
    private MCvScalar color;
    private LineType lineType;

    public ContourDrawer(int thickness, MCvScalar color, LineType lineType = LineType.FourConnected) {
        this.thickness = thickness;
        this.color = color;
        this.lineType = lineType;
    }

    public void drawContourPoints(ContourList contourList, Mat targetMat, int radius) {
        foreach (var contour in contourList.contours) {
            CvInvoke.Circle(targetMat, contour.point, radius, color, thickness, lineType);
        }
    }

    public void drawContourLinks(ContourList contourList, Mat targetMat, double tipLength) {
        foreach (var contour in contourList.contours) {
            if (contour.link != null)
                CvInvoke.ArrowedLine(targetMat, contour.point, contour.link.point, color, thickness, lineType, 0, tipLength);
        }
    }
    public void drawContourCalculationOrdering(ContourList contourList, Mat targetMat, double fontScale) {
        foreach (var contour in contourList.contours) {
            if (contour.order != null)
                CvInvoke.PutText(targetMat, $"{contour.order}", contour.point, FontFace.HersheyComplex, fontScale, new MCvScalar(255, 255, 0));
        }
    }
}