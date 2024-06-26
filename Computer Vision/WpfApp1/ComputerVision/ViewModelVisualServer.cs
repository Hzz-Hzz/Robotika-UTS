
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.XPath;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using PredictorModel;

namespace WpfApp1;

public class ViewModelVisualServer : INotifyPropertyChanged
{


    public event PropertyChangedEventHandler? PropertyChanged;

    public ImageSource imageSourceOriginal {
        get { return ImageSourceOriginal; }
    }

    public ImageSource imageSourceRoadEdge {
        get { return ImageSourceRoadEdge; }
    }
    public ImageSource imageSourceRoadMain {
        get { return ImageSourceRoadMain; }
    }
    public ImageSource imageSourceSurroundingMap {
        get { return ImageSourceSurroundingMap; }
    }


    public string anglePrediction { get; set; }

    private BitmapImage ___imageSourceOriginal;
    private BitmapImage ___imageSourceRoadEdge;
    private BitmapImage ___imageSourceRoadMain;
    private BitmapImage ___imageSourceSurroundingMap;

    public BitmapImage ImageSourceOriginal {
        get { return ___imageSourceOriginal; }
        set {
            ___imageSourceOriginal = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSourceOriginal"));
        }
    }

    public BitmapImage ImageSourceRoadEdge {
        get { return ___imageSourceRoadEdge; }
        set {
            value.Freeze();
            ___imageSourceRoadEdge = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSourceRoadEdge"));
        }
    }
    public BitmapImage ImageSourceRoadMain {
        get { return ___imageSourceRoadMain; }
        set {
            ___imageSourceRoadMain = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSourceRoadMain"));
        }
    }
    public BitmapImage ImageSourceSurroundingMap {
        get { return ___imageSourceSurroundingMap; }
        set {
            ___imageSourceSurroundingMap = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSourceSurroundingMap"));
        }
    }



    public String status {get;set;}
    private RoadEdgeImageProcessing _roadEdgeImageProcessing = new ();
    private MainRoadImageProcessing _mainRoadImageProcessing = new ();


    public ViewModelVisualServer() {}



    private RegressionPredictor<DataModel, float> _predictor;



    /**
     * This returns tuples of (distance, recommended angle in rads)
     */
    public AngleRecommendationsReturnType? processImage(byte[] imageByte, Obstacles obstacles) {
        var converter = new ByteToCroppedImageFactory();
        var image = converter.convert(imageByte);

        try {
            updateOriginalImage(image);
            var contourList = updateProcessedImageAndGetContourList(image);
            return updateSurrondingMap(contourList, image.Height, image.Width,
                obstaclesToContourList(obstacles, image.Width, image.Height));
        }
        catch (ArgumentException e) {
            if (e.Message.Contains("Parameter is not valid")) {
                Trace.TraceWarning(e.Message);
                return null;
            }
            throw;
        }
    }

    private ContourList obstaclesToContourList(Obstacles obstacles, int sourceImageWidth, int sourceImageHeight) {
        var list = new List<ContourPoint>();
        foreach (var obstacle in obstacles) {
            var end = ContourPoint.fromVector2(obstacle.Item2);
            var start = ContourPoint.fromVector2(obstacle.Item1, end);
            list.Add(start);
            list.Add(end);
        }

        return new ContourList(list, sourceImageWidth, sourceImageHeight);
    }

    private void updateOriginalImage(Image<Bgr, byte> image) {
        var origImage = ImageUtility.BitmapToImageSource(image.ToBitmap());
        origImage.Freeze();
        ImageSourceOriginal = origImage;
    }

    private ContourList updateProcessedImageAndGetContourList(Image<Bgr, byte> image) {
        var resultingMainRoadImage = _mainRoadImageProcessing.processImage(image, false);
        var mainRoadBitmap = ImageUtility.BitmapToImageSource(resultingMainRoadImage.Item1.ToBitmap());
        mainRoadBitmap.Freeze();
        ImageSourceRoadMain = mainRoadBitmap;

        var contourInformation = _roadEdgeImageProcessing.getContourList(image,
            _mainRoadImageProcessing.resultingPolygons, true);
        var resultingRoadEdgeImage = _roadEdgeImageProcessing.getImageFromContourInformation(contourInformation, resultingMainRoadImage.Item2);
        contourInformation.Item2!.Dispose();

        resultingRoadEdgeImage.Freeze();
        ImageSourceRoadEdge = resultingRoadEdgeImage;

        return contourInformation.Item1;
    }

    /**
     * This returns tuples of (distance, recommended angle in rads)
     */
    private AngleRecommendationsReturnType updateSurrondingMap(ContourList contourList, int rows, int cols,
        ContourList obstacles) {
        prevSurroundingMap = SurroundingMap.fromCameraContourList(contourList);
        prevSurroundingMap.updateIntersectionPoints(obstacles, extensionLength: 0.1f);

        using (var mat = new Mat(rows, cols, DepthType.Cv8U, 3)) {
            mat.SetTo(new MCvScalar(0, 0, 0));
            prevSurroundingMap.drawOnMat(mat);

            var bitmap = ImageUtility.BitmapToImageSource(mat.ToBitmap());
            bitmap.Freeze();
            ImageSourceSurroundingMap = bitmap;
        }

        return prevSurroundingMap.getMostRecommendedIntersectionPoints();
    }

    public SurroundingMap? prevSurroundingMap { get; set; }


    public void setStatusToWaitingForClient()
    {
        status = "Status: Waiting for client (Unity project) to connect";
        OnPropertyChanged("status");
    }
    public void setStatusToDisconnected()
    {
        status = "Status: Disconnected";
        OnPropertyChanged("status");
    }
    public void setStatusToClientConnected()
    {
        status = "Status: client connected";
        OnPropertyChanged("status");
    }


    private void OnPropertyChanged(string propertyName){
        var handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}