
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.XPath;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.Structure;
using Emgu.Util;

namespace WpfApp1;

public class ViewModelDatasetEditor : INotifyPropertyChanged
{
    public int datasetId = 0;
    public string datasetIdLabel => $"{datasetId}";
    public static readonly string datasetFolder = "H:\\01_Kuliah\\01_Dokumen\\22 - Robotika\\02 Tugas Unity\\10 Persiapan UTS\\UTS template\\dataset";
    public int maxFileNo;


    public string datasetPath => Path.Join(datasetFolder, datasetFilename);
    public string datasetFilename => $"{datasetId}.{LABEL_FILE_EXTENSION}";

    public const string LABEL_FILE_EXTENSION = "label.json";


    public event PropertyChangedEventHandler PropertyChanged;

    public ImageSource imageSourceOriginal {
        get { return ImageSourceOriginal; }
    }

    public ImageSource imageSourceRoadEdge {
        get { return ImageSourceRoadEdge; }
    }
    public ImageSource imageSourceRoadMain {
        get { return ImageSourceRoadMain; }
    }

    private BitmapImage ___imageSourceOriginal;
    private BitmapImage ___imageSourceRoadEdge;
    private BitmapImage ___imageSourceRoadMain;

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



    public String status {get;set;}
    private ObstacleImageProcessing _obstacleImageProcessing = new ();
    private RoadEdgeImageProcessing _roadEdgeImageProcessing = new ();
    private MainRoadImageProcessing _mainRoadImageProcessing = new ();


    public ViewModelDatasetEditor() {
        maxFileNo = Int32.Parse(File.ReadAllText(Path.Join(datasetFolder, "-fileno")));
        if (File.Exists(Path.Join(datasetFolder, "-lastfileno")))
            datasetId = Int32.Parse(File.ReadAllText(Path.Join(datasetFolder, "-lastfileno")));
    }

    public void prevDataset() {
        var prevId = datasetId;
        while (true) {
            datasetId--;
            datasetId = (datasetId + maxFileNo) % maxFileNo;
            if (File.Exists(Path.Join(datasetFolder, $"{datasetId}.png"))) {
                break;
            }
            if (datasetId == prevId)
                break;
        }
        File.WriteAllText(Path.Join(datasetFolder, "-lastfileno"), datasetId.ToString());
        processDataset(datasetId);
        PropertyChanged(this, new PropertyChangedEventArgs("datasetIdLabel"));
    }

    public void nextDataset(int increment = 1, bool skipAlreadyDefinedLabel=true) {
        var prevDatasetId = datasetId;
        datasetId += increment;
        datasetId %= maxFileNo;
        while (true) {
            bool datasetIdChanged = false;
            while (!File.Exists(Path.Join(datasetFolder, $"{datasetId}.png")) && datasetId+1 <= maxFileNo) {
                datasetId++;
                datasetIdChanged = true;
            }
            while (skipAlreadyDefinedLabel && File.Exists(Path.Join(datasetFolder, datasetFilename)) && datasetId+1 <= maxFileNo) {
                datasetId++;
                datasetIdChanged = true;
            }

            if (datasetId == prevDatasetId) {
                MessageBox.Show("End of dataset");
                break;
            }
            if (!datasetIdChanged) break;
        }
        File.WriteAllText(Path.Join(datasetFolder, "-lastfileno"), datasetId.ToString());
        processDataset(datasetId);
        PropertyChanged(this, new PropertyChangedEventArgs("datasetIdLabel"));
    }

    private void processDataset(int datasetId) {
        byte[] imageByte = File.ReadAllBytes( Path.Join(datasetFolder, $"{datasetId}.png"));
        var image = ConvertByteToImage(imageByte);
        image = cropUpperPart(image, 30);

        try {
            var origImage = ImageUtility.BitmapToImageSource(image.ToBitmap());
            // var resultingRoadEdgeImage = _roadEdgeImageProcessing.processImageAsBitmap(image);
            var resultingRoadEdgeImage = _roadEdgeImageProcessing.processImageAsBitmap(image);

            origImage.Freeze();
            resultingRoadEdgeImage.Freeze();
            // resultingMainRoadImage.Freeze();
            ImageSourceOriginal = origImage;
            ImageSourceRoadEdge = resultingRoadEdgeImage;
            // ImageSourceRoadMain = resultingMainRoadImage;
        }
        catch (ArgumentException e) {
            if (e.Message.Contains("Parameter is not valid")) {
                Trace.TraceWarning(e.Message);
                return;
            }
            throw;
        }
    }

    private Image<Bgr, byte> cropUpperPart(Image<Bgr, byte> originalImage, int percentage) {
        var oldRoi = originalImage.ROI;
        originalImage.ROI = new Rectangle(0, percentage * originalImage.Height / 100, originalImage.Width, originalImage.Height);
        var ret = originalImage.Copy();
        originalImage.ROI = oldRoi;
        return ret;
    }


    public static Image<Bgr, byte> ConvertByteToImage(byte[] bytes)
    {
        return new Bitmap(Image.FromStream(new MemoryStream(bytes), true, true)).ToImage<Bgr, byte>();
    }


    private void OnPropertyChanged(string propertyName){
        var handler = PropertyChanged;
        if (handler != null)
            handler(this, new PropertyChangedEventArgs(propertyName));
    }
}