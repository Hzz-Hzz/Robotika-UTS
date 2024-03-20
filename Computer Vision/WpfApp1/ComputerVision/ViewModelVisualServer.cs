
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
using Emgu.CV.Structure;

namespace WpfApp1;

public class ViewModelVisualServer : INotifyPropertyChanged
{


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
    private RoadEdgeImageProcessing _roadEdgeImageProcessing = new ();
    private MainRoadImageProcessing _mainRoadImageProcessing = new ();


    public ViewModelVisualServer()
    {
    }




    public void start()
    {
        Thread thread = new Thread(ServerThread_Read);
        thread.Start();
    }

    private void ServerThread_Read()
    {
        NamedPipeServerStream namedPipeServerStream =
            new NamedPipeServerStream("RobotikaNuelValen", PipeDirection.InOut);
        var stoppingCriteria = () => !namedPipeServerStream.IsConnected;

        while (true)
        {
            try
            {
                setStatusToWaitingForClient();
                namedPipeServerStream.WaitForConnection();
                setStatusToClientConnected();

                var reader = new BinaryReader(namedPipeServerStream);
                // var writer = new StreamWriter(namedPipeServerStream); ;
                while (true)
                {
                    var sizeRaw = blockingReadExactly(reader, 4, stoppingCriteria);
                    // namedPipeServerStream.Read(sizeRaw);
                    var size = BitConverter.ToInt32(sizeRaw);

                    var imageByte = blockingReadExactly(reader, size, stoppingCriteria);
                    var image = ConvertByteToImage(imageByte);
                    image = cropUpperPart(image, 30);


                    try {
                        var origImage = BitmapImageUtility.BitmapToImageSource(image.ToBitmap());
                        var resultingRoadEdgeImage = _roadEdgeImageProcessing.processImageAsBitmap(image);
                        // var resultingMainRoadImage = _mainRoadImageProcessing.processImage(image);


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
            }
            catch (OperationCanceledException e)
            {
                setStatusToWaitingForClient();
            }
            catch (IOException e)
            {
                setStatusToWaitingForClient();
                namedPipeServerStream.Disconnect();
            }
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

    private byte[] blockingReadExactly(BinaryReader streamReader, int bytesCount, Func<bool> stopFlag)
    {
        List<byte[]> resultingArrays = new();

        while (bytesCount > 0)
        {
            var result = streamReader.ReadBytes(bytesCount);
            bytesCount -= result.Length;
            if (result.Length != 0)
                resultingArrays.Add(result);
            if (stopFlag.Invoke())
                throw new OperationCanceledException();
            Thread.Sleep(50);
        }
        if (resultingArrays.Count == 1)
            return resultingArrays[0];
        return ConcatArrays(resultingArrays);
    }

    public static T[] ConcatArrays<T>(List<T[]> p)
    {
        var position = 0;
        var outputArray = new T[p.Sum(a => a.Length)];
        foreach (var curr in p)
        {
            Array.Copy(curr, 0, outputArray, position, curr.Length);
            position += curr.Length;
        }
        return outputArray;
    }





    void setStatusToWaitingForClient()
    {
        status = "Status: Waiting for client (Unity project) to connect";
        OnPropertyChanged("status");
    }
    void setStatusToClientConnected()
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