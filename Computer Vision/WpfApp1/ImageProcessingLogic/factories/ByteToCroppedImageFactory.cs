using Emgu.CV;
using Emgu.CV.Structure;

namespace WpfApp1;

public class ByteToCroppedImageFactory: IByteToImageFactory
{
    public ByteToCroppedImageFactory(int percentage = 30) {
        this.percentage = percentage;
    }

    public int percentage { get; set; }

    public Image<Bgr, byte> convert(byte[] imageByte) {
        var image = ImageUtility.ConvertByteToImage(imageByte);
        return ImageUtility.cropUpperPart(image, percentage);
    }
}