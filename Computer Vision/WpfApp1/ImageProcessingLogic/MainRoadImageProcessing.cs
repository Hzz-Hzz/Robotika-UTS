using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using WpfApp1.Emgucv_Wrapper;
using Image = System.Drawing.Image;
using Point = System.Windows.Point;

namespace WpfApp1;

public class MainRoadImageProcessing
{

    public List<Polygon>? resultingPolygons = null;

    public BitmapImage processImageAsBitmap(Image<Bgr, byte> image) {
        using (var gpuMat = imageToGpuMat(image))
            return ImageUtility.BitmapToImageSource(processImage(gpuMat).Item2.ToBitmap());
    }
    public Tuple<Mat, Mat> processImage(Image<Bgr, byte> image, bool returnMatOfBitmask=false) {
        using (var gpuMat = imageToGpuMat(image))
            return processImage(gpuMat, returnMatOfBitmask);
    }
    public Tuple<Mat, Mat> processImage(GpuCpuMat gpuMat, bool returnMatOfBitmask=false)
    {
        using (var newGpuMat = filterByDistanceWithTargetColor(gpuMat, 130, 130, 130, 230))
        using (var newGpuMat2 = filterByDistanceWithTargetColor(gpuMat, 205, 205, 205, 230))
        using (var newGpuMat3 = filterByDistanceWithTargetColor(gpuMat, 50, 60, 70, 230))
        {
            AnyInvoke.Max(newGpuMat, newGpuMat2, newGpuMat);
            AnyInvoke.Max(newGpuMat, newGpuMat3, newGpuMat);
            newGpuMat.toCpuMat();

            var ret1 = new Mat();
            newGpuMat.CopyTo(ret1);
            var ret2 = contourProcessor(newGpuMat);
            return new Tuple<Mat, Mat>(ret1, ret2);
        }
    }

    public Mat contourProcessor(GpuCpuMat mat) {
        mat.toCpuMat();
        Mat ret = new Mat(mat.Size, DepthType.Cv8U, 3);
        ret.SetTo(new MCvScalar(0, 0, 0));

        CvInvoke.CvtColor(mat, mat, ColorConversion.Bgr2Gray);

        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        CvInvoke.FindContours(mat, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        resultingPolygons ??= new();
        resultingPolygons.Clear();
        for (int i = 0; i < contours.Size; i++) {
            VectorOfPoint approx = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contours[i], approx, 6, true);
            resultingPolygons.Add(Polygon.fromVectorOfPoint(approx));
            CvInvoke.Polylines(ret, new VectorOfVectorOfPoint(approx), true, new MCvScalar(255, 255, 0), 2);
        }

        return ret;
    }

    private GpuCpuMat filterByDistanceWithTargetColor(GpuCpuMat gpuMat, int r, int g, int b, int threshold) {
        var ret = new GpuCpuMat(new Mat());
        ret.tryToGpu();
        gpuMat.CopyTo(ret);

        using (var solidGrey = GpuCpuMat.fromSolidColor(ret, new MCvScalar(b, g, r)))
        using (var solidWhite = GpuCpuMat.fromSolidColor(ret, new MCvScalar(255, 255, 255))) {
            AnyInvoke.Absdiff(ret, solidGrey, ret);
            AnyInvoke.Absdiff(ret, solidWhite, ret);
            removeRedObjects(gpuMat, 235, 10);

            AnyInvoke.CvtColor(ret, ret, ColorConversion.Bgr2Gray);
            AnyInvoke.Threshold(ret, ret, threshold, 255, ThresholdType.ToZero);
            applyErrosion(ret);
            applyErrosion(ret);
            AnyInvoke.CvtColor(ret, ret, ColorConversion.Gray2Bgr);
        }
        return ret;
    }

    private void removeRedObjects(GpuCpuMat gpuMat, int minimumRedForPenalty, int penaltyMultiplier) {
        var split = gpuMat.Split();  // bgr
        var oldRed = new GpuCpuMat(new Mat());
        oldRed.tryToGpu();
        split[2].CopyTo(oldRed);

        using (var penaltyMinimum = GpuCpuMat.fromSolidColor(gpuMat, minimumRedForPenalty))
        using (var penaltyMultiplierMat = GpuCpuMat.fromSolidColor(gpuMat, penaltyMultiplier))
        {
            AnyInvoke.Subtract(split[2], penaltyMinimum, split[2]);
            AnyInvoke.Multiply(split[2], penaltyMultiplierMat, split[2]);

            AnyInvoke.Subtract(split[0], split[2], split[0]);
            AnyInvoke.Subtract(split[1], split[2], split[1]);
            split[2] = oldRed;
        }
        gpuMat.MergeFrom(split);
    }



    private void applyErrosion(GpuCpuMat targetMat) {
        var splittedChannel = targetMat.Split();
        var singleChannel = splittedChannel[0];

        var anchor = new System.Drawing.Point(-1, -1);  // Point(-1, -1) is a special value means the center
        var kernelSize = new System.Drawing.Size(3, 3);
        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, kernelSize, anchor);

        // open fitler is doing Erode, then Dilatation
        AnyInvoke.MorphologyEx(singleChannel, singleChannel,MorphOp.Erode, kernel, anchor, 3,
            singleChannel.Depth, singleChannel.NumberOfChannels );
        for (int i = 0; i < targetMat.NumberOfChannels; i++) {
            splittedChannel[i] = singleChannel;
        }
        targetMat.MergeFrom(splittedChannel);
    }

    private Image<Bgr, byte> cropUpperPart(Image<Bgr, byte> originalImage, int percentage) {
        var oldRoi = originalImage.ROI;
        originalImage.ROI = new Rectangle(0, percentage * originalImage.Height / 100, originalImage.Width, originalImage.Height);
        var ret = originalImage.Copy();
        originalImage.ROI = oldRoi;
        return ret;
    }


    static GpuCpuMat imageToGpuMat(Image<Bgr, byte> image) {
        var ret = GpuCpuMat.fromImage(image);
        ret.tryToGpu();
        return ret;
    }



    static BitmapImage matToImageSource(Mat mat)
    {
        var bitmap = mat.ToImage<Bgr, Byte>().ToBitmap();
        return ImageUtility.BitmapToImageSource(bitmap);
    }

}