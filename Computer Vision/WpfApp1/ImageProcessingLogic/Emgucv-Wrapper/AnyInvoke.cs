using System.Drawing;
using Emgu.CV;
using Emgu.CV.Cuda;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace WpfApp1.Emgucv_Wrapper;

public static class AnyInvoke
{
    public readonly static bool cudaAvailable = GpuCpuMat.cudaAvailable;

    public static void Absdiff(GpuCpuMat input1, GpuCpuMat input2, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(input1, input2, outputMat);
        }
        if (input1.isGpu && input2.isGpu && outputMat.isGpu) {
            CudaInvoke.Absdiff(input1, input2, outputMat);
            return;
        }
        toCpuMat(input1, input2, outputMat);
        CvInvoke.AbsDiff(input1, input2, outputMat);
    }


    public static void Min(GpuCpuMat input1, GpuCpuMat input2, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(input1, input2, outputMat);
        }
        if (input1.isGpu && input2.isGpu && outputMat.isGpu) {
            CudaInvoke.Min(input1, input2, outputMat);
            return;
        }
        toCpuMat(input1, input2, outputMat);
        CvInvoke.Min(input1, input2, outputMat);
    }

    public static void Add(GpuCpuMat a, GpuCpuMat b, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(a, b, outputMat);
        }
        if (a.isGpu && b.isGpu && outputMat.isGpu) {
            CudaInvoke.Add(a, b, outputMat);
            return;
        }
        toCpuMat(a, b, outputMat);
        CvInvoke.Add(a, b, outputMat);
    }

    public static void Subtract(GpuCpuMat a, GpuCpuMat b, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(a, b, outputMat);
        }
        if (a.isGpu && b.isGpu && outputMat.isGpu) {
            CudaInvoke.Subtract(a, b, outputMat);
            return;
        }
        toCpuMat(a, b, outputMat);
        CvInvoke.Subtract(a, b, outputMat);
    }

    public static void Multiply(GpuCpuMat a, GpuCpuMat b, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(a, b, outputMat);
        }
        if (a.isGpu && b.isGpu && outputMat.isGpu) {
            CudaInvoke.Multiply(a, b, outputMat);
            return;
        }
        toCpuMat(a, b, outputMat);
        CvInvoke.Multiply(a, b, outputMat);
    }


    public static void Threshold(GpuCpuMat inputMat, GpuCpuMat outputMat, double threshold, double maxValue, ThresholdType thresholdType, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(inputMat, outputMat);
        }
        if (inputMat.isGpu && outputMat.isGpu) {
            CudaInvoke.Threshold(inputMat, outputMat, threshold, maxValue, thresholdType);
            return;
        }
        toCpuMat(inputMat, outputMat);
        CvInvoke.Threshold(inputMat, outputMat, threshold, maxValue, thresholdType);
    }

    public static void Max(GpuCpuMat input1, GpuCpuMat input2, GpuCpuMat outputMat, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(input1, input2, outputMat);
        }
        if (input1.isGpu && input2.isGpu && outputMat.isGpu) {
            CudaInvoke.Max(input1, input2, outputMat);
            return;
        }
        toCpuMat(input1, input2, outputMat);
        CvInvoke.Max(input1, input2, outputMat);
    }

    public static void MorphologyEx(GpuCpuMat input1, GpuCpuMat outputMat, MorphOp operation, Mat kernel, Point anchor,
        int iterations, DepthType depthType, int numOfSrcChannels,
        bool preferGpu=true
    ) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(input1, outputMat);
        }
        if (input1.isGpu && outputMat.isGpu) {
            var morphEx = new CudaMorphologyFilter(operation, depthType, numOfSrcChannels, kernel, anchor, iterations);
            morphEx.Apply(input1, outputMat);
            return;
        }
        toCpuMat(input1, outputMat);
        CvInvoke.MorphologyEx(input1, outputMat,
            operation, kernel, anchor, iterations, BorderType.Default, new MCvScalar());
    }

    public static void CvtColor(GpuCpuMat input1, GpuCpuMat outputMat, ColorConversion colorConversion, bool preferGpu=true) {
        if (preferGpu && cudaAvailable) {
            tryToGpuMat(input1, outputMat);
        }
        if (input1.isGpu && outputMat.isGpu) {
            CudaInvoke.CvtColor(input1, outputMat, colorConversion);
            return;
        }
        toCpuMat(input1, outputMat);
        CvInvoke.CvtColor(input1, outputMat, colorConversion);
    }



    public static void tryToGpuMat(params GpuCpuMat[] mats) {
        foreach (var mat in mats) {
            mat.tryToGpu();
        }
    }

    public static void toCpuMat(params GpuCpuMat[] mats) {
        foreach (var mat in mats) {
            mat.toCpuMat();
        }
    }
}