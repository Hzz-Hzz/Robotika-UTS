using System;
using System.ComponentModel;
using System.Diagnostics;
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

public class RoadEdgeImageProcessing
{
    private MainRoadImageProcessing _mainRoadImageProcessing = new ();
    private ContourDrawer contourPointDrawer = new (3, new MCvScalar(0, 255, 0), LineType.Filled);
    private ContourDrawer contourArrowDrawer = new (2, new MCvScalar(0, 0, 255), LineType.FourConnected);


    public BitmapImage processImageAsBitmap(Image<Bgr, byte> image) {
        var mainRoadMat = _mainRoadImageProcessing.processImage(image).Item2;
        var contourListInfo = getContourList(image, _mainRoadImageProcessing.resultingPolygons,
            true);
        try {
            return getImageFromContourInformation(contourListInfo, mainRoadMat);
        }
        finally {
            contourListInfo.Item2.Dispose();
        }
    }

    public BitmapImage getImageFromContourInformation(Tuple<ContourList, Mat?> contourAndMat, Mat mainRoadMat) {
        var resultingContourListAndMat = contourAndMat;
        var contourList = resultingContourListAndMat.Item1;
        Debug.Assert(resultingContourListAndMat.Item2 != null, "resultingContourListAndMat.Item2 != null");
        Mat resultingMat = resultingContourListAndMat.Item2;

        contourPointDrawer.drawContourPoints(contourList, resultingMat, 3);
        contourArrowDrawer.drawContourLinks(contourList, resultingMat, 0.12);
        contourArrowDrawer.drawContourCalculationOrdering(contourList, resultingMat, 1);

        CvInvoke.Add(resultingMat, mainRoadMat, resultingMat);
        return matToImageSource(resultingMat);
    }

    public Tuple<ContourList, Mat?> getContourList(Image<Bgr, byte> image, List<Polygon>? boundariesNotToIntersectWith, bool returnMat = false) {
        using (var gpuMat = imageToGpuMat(image)) {
            var resultingMat = new Mat();

            substractRedChannelFromMaxOfBlueAndGreen(gpuMat);
            applyMorphologyEx(gpuMat);
            gpuMat.Download(resultingMat);

            var contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(resultingMat, contours, null,
                RetrType.List, ChainApproxMethod.ChainApproxSimple);

            CvInvoke.CvtColor(resultingMat, resultingMat, ColorConversion.Gray2Bgr);
            CvInvoke.DrawContours(resultingMat, contours, -1, new MCvScalar(255, 0 ,0));

            var contourList = new ContourList(contours, resultingMat.Width, resultingMat.Height);
            contourList.boundariesNotToIntersectWith = boundariesNotToIntersectWith ?? new List<Polygon>();
            contourList.initializeContourLinks();
            contourList.removeOutliers();
            if (!returnMat) {
                resultingMat.Dispose();
                resultingMat = null;
            }
            return new Tuple<ContourList, Mat>(contourList, resultingMat);
        }
    }



    private void applyMorphologyEx(GpuMat targetMat) {
        CudaInvoke.CvtColor(targetMat, targetMat, ColorConversion.Bgr2Gray);
        var splittedChannel = targetMat.Split();
        var singleChannel = splittedChannel[0];

        var anchor = new System.Drawing.Point(-1, -1);  // Point(-1, -1) is a special value means the center
        var kernelSize = new System.Drawing.Size(1, 3);
        var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, kernelSize, anchor);

        // open fitler is doing Erode, then Dilatation
        var openFilter = new CudaMorphologyFilter(MorphOp.Open, singleChannel.Depth, singleChannel.NumberOfChannels, kernel, anchor, 1);

        kernelSize = new System.Drawing.Size(5, 1);
        kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, kernelSize, anchor);
        var openFilter2 = new CudaMorphologyFilter(MorphOp.Open, singleChannel.Depth, singleChannel.NumberOfChannels, kernel, anchor, 1);

        openFilter.Apply(singleChannel, singleChannel);
        openFilter2.Apply(singleChannel, singleChannel);

        for (int i = 0; i < targetMat.NumberOfChannels; i++) {
            splittedChannel[i] = singleChannel;
        }
        targetMat.MergeFrom(splittedChannel);
    }

    private void substractRedChannelFromMaxOfBlueAndGreen(GpuMat targetMat)
    {
        using (var max = new GpuMat())
        using (var identity = solidColorGpuMat(targetMat, 1))
        {
            var split = targetMat.Split();  // BGR

            CudaInvoke.Max(split[0], split[1], max);
            CudaInvoke.Subtract(split[2], max, split[2]);

            CudaInvoke.Threshold(split[2], split[2], 30, 255, ThresholdType.ToZero);
            split[0] = split[2];
            split[1] = split[2];
            targetMat.MergeFrom(split);
        }
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

    static BitmapImage GpuMatToImageSource(GpuMat gpuMat)
    {
        var image = new Image<Bgr, Byte>(gpuMat.Size.Width, gpuMat.Size.Height);
        gpuMat.Download(image);
        var bitmap = image.ToBitmap();
        return ImageUtility.BitmapToImageSource(bitmap);
    }




    public static byte[] ConvertImageToByte(Image<Bgr, byte> My_Image)
    {
        MemoryStream m1 = new MemoryStream();
        new Bitmap(My_Image.ToBitmap()).Save(m1, System.Drawing.Imaging.ImageFormat.Jpeg);
        byte[] header = new byte[] { 255, 216 };
        header = m1.ToArray();
        return (header);
    }


}