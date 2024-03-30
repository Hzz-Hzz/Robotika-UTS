using System.Drawing;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using WpfApp1.Emgucv_Wrapper;

namespace WpfApp1;
/**
 * Cannot isolate light reflections, still fail to prevent reflections from being captured as "obstacle",
 * so we will abandon this method for now.
 */
public class ObstacleImageProcessing
{
    public BitmapImage processImageAsBitmap(Image<Bgr, byte> image) {
        using (GpuCpuMat img = GpuCpuMat.fromImage(image))
        using (GpuCpuMat white = GpuCpuMat.fromSolidColor(img, new MCvScalar(255, 255, 255)))
        using (GpuCpuMat obstacle1 = GpuCpuMat.fromSolidColor(img, new MCvScalar(102, 89, 80)))
        using (GpuCpuMat obstacle2 = GpuCpuMat.fromSolidColor(img, new MCvScalar(230, 251, 255)))
        using (GpuCpuMat obstacle3 = GpuCpuMat.fromSolidColor(img, new MCvScalar(173, 184, 182)))

        using (GpuCpuMat obstacle4 = GpuCpuMat.fromSolidColor(img, new MCvScalar(235, 255, 255)))
        using (GpuCpuMat obstacle5 = GpuCpuMat.fromSolidColor(img, new MCvScalar(192, 206, 210)))
        {
            filterObstacleBasedOnColorSimilarity(img, obstacle1, 220);
            filterObstacleBasedOnColorSimilarity(img, obstacle2);
            filterObstacleBasedOnColorSimilarity(img, obstacle3, 245);
            filterObstacleBasedOnColorSimilarity(img, obstacle4, 230);
            filterObstacleBasedOnColorSimilarity(img, obstacle5, 240);

            AnyInvoke.Max(obstacle1, obstacle2, obstacle1);
            AnyInvoke.Max(obstacle1, obstacle3, obstacle1);
            AnyInvoke.Max(obstacle1, obstacle4, obstacle1);
            AnyInvoke.Max(obstacle1, obstacle5, obstacle1);
            AnyInvoke.CvtColor(obstacle1, obstacle1, ColorConversion.Bgr2Gray);

            Point anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(7, 7), anchor);
            AnyInvoke.MorphologyEx(obstacle1, obstacle1, MorphOp.Erode, kernel, anchor, 3, DepthType.Cv8U, 1);
            AnyInvoke.MorphologyEx(obstacle1, obstacle1, MorphOp.Dilate, kernel, anchor, 2, DepthType.Cv8U, 1);
            AnyInvoke.CvtColor(obstacle1, obstacle1, ColorConversion.Gray2Bgr);

            // var contours = new VectorOfVectorOfPoint();
            // img.toCpuMat();
            // CvInvoke.GaussianBlur(img.getLinkedMat(), img.getLinkedMat(), new Size(15,15), 3.0, 3.0);
            // CvInvoke.CvtColor(img, img, ColorConversion.Bgr2Gray);
            // AnyInvoke.Threshold(img, img, 200.0, 255.0, ThresholdType.Otsu, false);
            // CvInvoke.FindContours(img, contours, null,
                // RetrType.List, ChainApproxMethod.ChainApproxSimple);
            // CvInvoke.CvtColor(img, img, ColorConversion.Gray2Bgr);
            // CvInvoke.DrawContours(img, contours, -1, new MCvScalar(255, 0 ,0));


            return ImageUtility.BitmapToImageSource(obstacle1.toBitmap());
        }
    }
    public void filterObstacleBasedOnColorSimilarity(GpuCpuMat img, GpuCpuMat targetMat, double threshold=250.0) {
        using (GpuCpuMat white = GpuCpuMat.fromSolidColor(img, new MCvScalar(255, 255, 255))) {
            AnyInvoke.Absdiff(img, targetMat, targetMat);
            sumAllChannels(targetMat);
            AnyInvoke.Absdiff(targetMat, white, targetMat);
            AnyInvoke.Threshold(targetMat, targetMat, threshold, 255.0, ThresholdType.ToZero);
        }
    }

    public void sumAllChannels(GpuCpuMat mat) {
        var channels = mat.Split();
        for (int i = 1; i < channels.Length; i++) {
            AnyInvoke.Add(channels[0], channels[i], channels[0]);
        }
        for (int i = 1; i < channels.Length; i++) {
            channels[0].CopyTo(channels[i]);
        }
        mat.MergeFrom(channels);
    }
}