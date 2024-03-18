
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

namespace WpfApp1;

public class ViewModel : INotifyPropertyChanged
{


    public event PropertyChangedEventHandler PropertyChanged;

    public ImageSource imageSourceOriginal {
        get { return _imageSourceOriginal; }
    }

    public ImageSource imageSource {
        get { return _imageSource; }
    }

    private BitmapImage ___imageSourceOriginal;
    private BitmapImage ___imageSource;

    public BitmapImage _imageSourceOriginal {
        get { return ___imageSourceOriginal; }
        set {
            ___imageSourceOriginal = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSourceOriginal"));
        }
    }

    public BitmapImage _imageSource {
        get { return ___imageSource; }
        set {
            ___imageSource = value;
            PropertyChanged(this, new PropertyChangedEventArgs("imageSource"));
        }
    }



    public String status {get;set;}
    private ImageProcessor _imageProcessor = new ImageProcessor();


    public ViewModel()
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

                    var image = blockingReadExactly(reader, size, stoppingCriteria);

                    var resultingTuple = _imageProcessor.processImage(image);
                    var origImage = resultingTuple.Item1;
                    var resultingImage = resultingTuple.Item2;
                    origImage.Freeze();
                    resultingImage.Freeze();
                    _imageSourceOriginal = origImage;
                    _imageSource = resultingImage;
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