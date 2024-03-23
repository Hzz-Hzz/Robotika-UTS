using System.Numerics;
using DataFrameGenerator.TrainingDataExtractStrategy;
using Microsoft.Data.Analysis;
using WpfApp1;

namespace DataFrameGenerator;

public class FewFirstContourPointInOneRowDataFrameFactory : IDataFrameFromContourListFactory
{
    int numberOfFewFirstContourPoint;
    private IContourPointTransformationDecorator _transformationDecorator;
    private IContourPointOrderingStrategy _orderingStrategy;

    public FewFirstContourPointInOneRowDataFrameFactory(
        int numberOfFewFirstContourPoint, IContourPointTransformationDecorator transformationDecorator, IContourPointOrderingStrategy orderingStrategy
    ) {
        this.numberOfFewFirstContourPoint = numberOfFewFirstContourPoint;
        _transformationDecorator = transformationDecorator;
        _orderingStrategy = orderingStrategy;
    }

    public List<PrimitiveDataFrameColumn<double>> getDataFrameColumnList() {
        var dfCol = new List<PrimitiveDataFrameColumn<double>>();

        for (int i = 0; i < numberOfFewFirstContourPoint; i++) {
            dfCol.Add(new PrimitiveDataFrameColumn<double>(xName(i)));
            dfCol.Add(new PrimitiveDataFrameColumn<double>(yName(i)));
            dfCol.Add(new PrimitiveDataFrameColumn<double>(xDirName(i)));
            dfCol.Add(new PrimitiveDataFrameColumn<double>(yDirName(i)));
            dfCol.Add(new PrimitiveDataFrameColumn<double>(lengthName(i)));
        }

        return dfCol;
    }

    public DataFrame getDataFrame(params ContourList[] contourLists) {
        var dfCol = getDataFrameColumnList();
        foreach (var contourList in contourLists) {
            var contourPoints = _orderingStrategy.getContourPointAsList(contourList);
            var numOfNonNullCol = Math.Min(numberOfFewFirstContourPoint, contourPoints.Count);
            var numOfNullCol = numberOfFewFirstContourPoint - numOfNonNullCol;

            for (int i = 0; i < numOfNonNullCol; i++) {
                var offset = i * totalNumberColumnsGeneratedForEachContourPoint;

                var contourPoint = _transformationDecorator.applyTransformation(contourList, contourPoints[i]);
                var extractedData = extract(contourPoint);
                for (int j = 0; j < totalNumberColumnsGeneratedForEachContourPoint; j++) {
                    dfCol[offset + j].Append(extractedData[j]);
                }
            }
            for (int i = 0; i < numOfNullCol; i++) {
                var offset = (numOfNonNullCol + i) * totalNumberColumnsGeneratedForEachContourPoint;
                for (int j = 0; j < totalNumberColumnsGeneratedForEachContourPoint; j++) {
                    var x = dfCol[offset + j];

                }
            }
        }

        var df = new DataFrame(dfCol);
        return df;
    }


    private const int totalNumberColumnsGeneratedForEachContourPoint = 5;
    private string xName(int i) {
        return $"x_{i}";
    }
    private string yName(int i) {
        return $"y_{i}";
    }
    private string xDirName(int i) {
        return $"xdir_{i}";
    }
    private string yDirName(int i) {
        return $"ydir_{i}";
    }
    private string lengthName(int i) {
        return $"len_{i}";
    }


    // for graph and experiments, see /development-purpose-only/Visualize-cubic-spline.dib
    // and /development-purpose-only/patokan-interpolasi.png
    public double[] extract(ContourPoint point) {
        var originVector = point.vector2;
        var targetVector = point.link?.vector2;
        var directionVector = new Vector2(0, 0);
        var directionLength = 0.0;

        if (targetVector != null) {
            directionVector = targetVector.Value - originVector;
            directionLength = directionVector.LengthSquared();

            directionVector = Vector2.Normalize(directionVector);
        }

        return new []{point.X, point.Y, directionVector.X, directionVector.Y, directionLength};
    }

}