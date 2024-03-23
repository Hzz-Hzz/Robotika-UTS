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
using Image = System.Drawing.Image;
using Point = System.Windows.Point;

namespace WpfApp1;

public class MainRoadImageProcessing
{

    public List<Polygon>? resultingPolygons = null;

    public BitmapImage processImageAsBitmap(Image<Bgr, byte> image) {
        using (var gpuMat = imageToGpuMat(image))
            return ImageUtility.BitmapToImageSource(processImage(gpuMat).ToBitmap());
    }
    public Mat processImage(Image<Bgr, byte> image) {
        using (var gpuMat = imageToGpuMat(image))
            return processImage(gpuMat);
    }
    public Mat processImage(GpuMat gpuMat)
    {
        using (var newGpuMat = filterByDistanceWithTargetColor(gpuMat, 130, 130, 130, 200))
        using (var newGpuMat2 = filterByDistanceWithTargetColor(gpuMat, 205, 205, 205, 220))
        using (var newGpuMat3 = filterByDistanceWithTargetColor(gpuMat, 50, 60, 70, 220))
        using (var resultingMat = new Mat()) {
            CudaInvoke.Max(newGpuMat, newGpuMat2, newGpuMat);
            CudaInvoke.Max(newGpuMat, newGpuMat3, newGpuMat);
            newGpuMat.Download(resultingMat);

            return contourProcessor(resultingMat);
        }
    }

    public Mat contourProcessor(Mat mat) {
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

    private GpuMat filterByDistanceWithTargetColor(GpuMat gpuMat, int r, int g, int b, int threshold) {
        var ret = new GpuMat();
        gpuMat.CopyTo(ret);

        using (var solidGrey = solidColorGpuMat(ret, r, g, b))
        using (var solidWhite = solidColorGpuMat(ret, 255, 255, 255)) {
            CudaInvoke.Absdiff(ret, solidGrey, ret);
            CudaInvoke.Absdiff(ret, solidWhite, ret);
            removeRedObjects(gpuMat, 235, 10);

            CudaInvoke.CvtColor(ret, ret, ColorConversion.Bgr2Gray);
            CudaInvoke.Threshold(ret, ret, threshold, 255, ThresholdType.ToZero);
            applyErrosion(ret);
            applyErrosion(ret);
            CudaInvoke.CvtColor(ret, ret, ColorConversion.Gray2Bgr);
        }
        return ret;
    }

    private void removeRedObjects(GpuMat gpuMat, int minimumRedForPenalty, int penaltyMultiplier) {
        var split = gpuMat.Split();  // bgr
        var oldRed = new GpuMat();
        split[2].CopyTo(oldRed);

        using (var penaltyMinimum = solidColorGpuMat(gpuMat, minimumRedForPenalty))
        using (var penaltyMultiplierMat = solidColorGpuMat(gpuMat, penaltyMultiplier))
        {
            CudaInvoke.Subtract(split[2], penaltyMinimum, split[2]);
            CudaInvoke.Multiply(split[2], penaltyMultiplierMat, split[2]);

            CudaInvoke.Subtract(split[0], split[2], split[0]);
            CudaInvoke.Subtract(split[1], split[2], split[1]);
            split[2] = oldRed;
        }
        gpuMat.MergeFrom(split);
    }



    private void applyErrosion(GpuMat targetMat) {
        var splittedChannel = targetMat.Split();
        var singleChannel = splittedChannel[0];

        var anchor = new System.Drawing.Point(-1, -1);  // Point(-1, -1) is a special value means the center
        var kernelSize = new System.Drawing.Size(3, 3);
        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, kernelSize, anchor);

        // open fitler is doing Erode, then Dilatation
        var openFilter = new CudaMorphologyFilter(MorphOp.Erode, singleChannel.Depth, singleChannel.NumberOfChannels, kernel, anchor, 3);


        openFilter.Apply(singleChannel, singleChannel);
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


    static GpuMat imageToGpuMat(Image<Bgr, byte> image)
    {
        var gpuMat = new GpuMat();
        gpuMat.Upload(image);
        return gpuMat;
    }


    static GpuMat solidColorGpuMat(GpuMat gpuMatSpec, int r, int g, int b)
    {
        MCvScalar color = new MCvScalar(b, g, r);
        Mat cpuMat = new Mat(gpuMatSpec.Size, gpuMatSpec.Depth, 3);
        cpuMat.SetTo(color);

        GpuMat gpuMat = new GpuMat();
        gpuMat.Upload(cpuMat);

        cpuMat.Dispose();
        return gpuMat;
    }
    static GpuMat solidColorGpuMat(GpuMat gpuMatSpec, int value)
    {
        MCvScalar color = new MCvScalar(value);
        Mat cpuMat = new Mat(gpuMatSpec.Size, gpuMatSpec.Depth, 1);
        cpuMat.SetTo(color);

        GpuMat gpuMat = new GpuMat();
        gpuMat.Upload(cpuMat);

        cpuMat.Dispose();
        return gpuMat;
    }


    static BitmapImage matToImageSource(Mat mat)
    {
        var bitmap = mat.ToImage<Bgr, Byte>().ToBitmap();
        return ImageUtility.BitmapToImageSource(bitmap);
    }

}