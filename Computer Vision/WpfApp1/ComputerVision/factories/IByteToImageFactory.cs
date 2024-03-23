using Emgu.CV;
using Emgu.CV.Structure;

namespace WpfApp1;

public interface IByteToImageFactory
{
    public Image<Bgr, byte> convert(byte[] imageByte);
}