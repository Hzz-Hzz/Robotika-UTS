using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.Util;

namespace WpfApp1.Emgucv_Wrapper;

public class GpuCpuMat :
    UnmanagedObject,
    IEquatable<Mat>,
    IEquatable<GpuMat>,
    IInputOutputArray,
    IInputArray,
    IDisposable,
    IOutputArray,
    IInputArrayOfArrays,
    IOutputArrayOfArrays,
    ISerializable
{
    public readonly static bool cudaAvailable = CudaInvoke.HasCuda && false;
    private bool gpuMatIsActive = false;
    public bool isGpu => gpuMatIsActive;
    private GpuMat? _gpuMat;
    private Mat? _cpuMat;

    public GpuCpuMat(GpuMat mat) {
        if (cudaAvailable) {
            _gpuMat = mat;
            gpuMatIsActive = true;
        }
        else {
            _cpuMat = new Mat();
            gpuMatIsActive = false;
            mat.Download(_cpuMat);
            mat.Dispose();
        }
    }

    public GpuCpuMat(Mat mat) {
        _cpuMat = mat;
        gpuMatIsActive = false;
    }

    public int NumberOfChannels {
        get {
            if (gpuMatIsActive)
                return _gpuMat.NumberOfChannels;
            return _cpuMat.NumberOfChannels;
        }
    }

    public void SetTo(MCvScalar value, IInputArray mask = null) {
        if (gpuMatIsActive)
            _gpuMat.SetTo(value, mask);
        else _cpuMat.SetTo(value, mask);
    }

    public void SetTo(IInputArray value, IInputArray mask = null) {
        if (gpuMatIsActive)
            throw new Exception("IInputArray is not an acceptable argument for GpuMat.SetTo()");
        else _cpuMat.SetTo(value, mask);
    }

    /**
     * Do not close resource you get from this
     */
    public Mat getLinkedMat() {
        if (!gpuMatIsActive || !cudaAvailable)
            return _cpuMat;
        _cpuMat ??= new Mat();
        _gpuMat.Download(_cpuMat);
        return _cpuMat;
    }

    public static GpuCpuMat fromImage(Image<Bgr, byte> image, bool tryGpu=false) {
        if (!tryGpu || !cudaAvailable) {
            var ret = new Mat();
            image.Mat.CopyTo(ret);
            return new GpuCpuMat(ret);
        }
        var gpuMat = new GpuMat();
        gpuMat.Upload(image.Mat);
        return new GpuCpuMat(gpuMat);
    }
    public static GpuCpuMat fromSolidColor(GpuCpuMat spec, MCvScalar mCvScalar, int channels=3){
        var ret = new GpuCpuMat(new Mat(spec.Size, spec.Depth, channels));
        ret.SetTo(mCvScalar);
        return ret;
    }
    public static GpuCpuMat fromSolidColor(GpuCpuMat spec, int scalar){
        return fromSolidColor(spec, new MCvScalar(scalar), channels:1);
    }

    public DepthType Depth {
        get {
            if (gpuMatIsActive)
                return _gpuMat.Depth;
            return _cpuMat.Depth;
        }
    }

    public Size Size {
        get {
            if (gpuMatIsActive)
                return _gpuMat.Size;
            return _cpuMat.Size;
        }
    }


    public void tryToGpu() {
        if (gpuMatIsActive || !cudaAvailable)
            return;
        _gpuMat ??= new GpuMat();
        _gpuMat.SetTo(new MCvScalar(0,0,0));
        _gpuMat.Upload(_cpuMat);
        gpuMatIsActive = true;
    }

    public void toCpuMat() {
        if (!gpuMatIsActive)
            return;
        _cpuMat ??= new Mat();
        _cpuMat.SetTo(new MCvScalar(0,0,0));
        _gpuMat.Download(_cpuMat);
        gpuMatIsActive = false;
    }

    public void CopyTo(IOutputArray dst, IInputArray mask = null, Stream stream = null) {
        if (gpuMatIsActive)
            _gpuMat.CopyTo(dst, mask, stream);
        else _cpuMat.CopyTo(dst, mask);
    }

    public InputArray GetInputArray() {
        if (gpuMatIsActive)
            return _gpuMat.GetInputArray();
        return _cpuMat.GetInputArray();
    }

    public OutputArray GetOutputArray() {
        if (gpuMatIsActive)
            return _gpuMat.GetOutputArray();
        return _cpuMat.GetOutputArray();
    }

    public InputOutputArray GetInputOutputArray() {
        if (gpuMatIsActive)
            return _gpuMat.GetInputOutputArray();
        return _cpuMat.GetInputOutputArray();
    }

    protected override void DisposeObject() {
        _gpuMat?.Dispose();
        _cpuMat?.Dispose();
        this.Dispose();
    }


    public void Dispose() {
        _gpuMat?.Dispose();
        _cpuMat?.Dispose();
        base.Dispose();
    }


    public bool Equals(Mat? other) {
        if (gpuMatIsActive)
            throw new Exception("GpuMat is active but being compared to other Mat");
        return _cpuMat.Equals(other);
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) {
        if (gpuMatIsActive)
            throw new Exception("GpuMat is not serializable");
        _cpuMat.GetObjectData(info, context);
    }

    public bool Equals(GpuMat? other) {
        if (!gpuMatIsActive)
            throw new Exception("CPU Mat is active but being compared to other GpuMat");
        return _gpuMat.Equals(other);
    }

    public Bitmap toBitmap() {
        if (gpuMatIsActive)
            return _gpuMat.ToBitmap();
        return _cpuMat.ToBitmap();
    }

    public GpuCpuMat[] Split(Stream stream = null) {
        if (gpuMatIsActive)
            return _gpuMat.Split(stream).Select(e=>new GpuCpuMat(e)).ToArray();
        return _cpuMat.Split().Select(e=>new GpuCpuMat(e)).ToArray();
    }

    public void MergeFrom(GpuCpuMat[] mats, Stream stream = null) {
        if (gpuMatIsActive) {
            AnyInvoke.tryToGpuMat(mats);
            _gpuMat.MergeFrom(mats.Select(e=>e._gpuMat).ToArray(), stream);
            return;
        }
        AnyInvoke.toCpuMat(mats);
        using (VectorOfMat srcArr = new (mats.Select(e=>e._cpuMat).ToArray()))
            CvInvoke.Merge(srcArr, _cpuMat);
    }
}