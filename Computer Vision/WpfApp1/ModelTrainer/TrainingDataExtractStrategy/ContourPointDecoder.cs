using System.Numerics;
using WpfApp1;

namespace ModelTrainer.TrainingDataExtractStrategy;

public class ContourPointDecoder
{
    public ContourPointDecoder() { }

    public double[] extract(ContourPoint point) {
        var originVector = point.vector2;
        var targetVector = point.link?.vector2;
        var directionVector = new Vector2(0, 0);

        if (targetVector != null) {
            directionVector = targetVector.Value - originVector;
            directionVector = Vector2.Normalize(directionVector);
        }

        return new []{point.X, directionVector.X, point.Y, directionVector.Y};
    }
}