using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using DatasetEditor.model;
using ImageProcessingLogic.TrainingDataExtractStrategy;
using Microsoft.Data.Analysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

    public List<PrimitiveDataFrameColumn<T>> getDataFrameColumnListFromType<T>(Type? labelType) where T : unmanaged {
        if (labelType == null) {
            return new List<PrimitiveDataFrameColumn<T>>();
        }

        var dfCol = new List<PrimitiveDataFrameColumn<T>>();
        var fieldsInfo = labelType.GetFields();
        foreach (var field in fieldsInfo) {
            if (field.FieldType != typeof(T))
                throw new ValidationException($"Invalid Type: {field.FieldType.Name}");
            dfCol.Add(new PrimitiveDataFrameColumn<T>(field.Name));
        }

        return dfCol;
    }

    public DataFrame getDataFrame(ContourList[] contourLists, DatasetImageLabel[]? labels=null) {
        var dfCol = getDataFrameColumnList();
        var labelCols = getDataFrameColumnListFromType<double>(
            labels == null ? null : typeof(DatasetImageLabel));

        for (var contourListIndex = 0; contourListIndex < contourLists.Length; contourListIndex++) {
            var contourList = contourLists[contourListIndex];
            var label = labels==null? null : labels[contourListIndex];

            var contourPoints = _orderingStrategy.getContourPointAsList(contourList);
            var numOfNonNullCol = Math.Min(numberOfFewFirstContourPoint, contourPoints.Count);
            var numOfNullCol = numberOfFewFirstContourPoint - numOfNonNullCol;

            copyAllContourListToDf(numOfNonNullCol, dfCol, contourList, contourPoints);
            paddingAllMissingContourPointToDf(numOfNullCol, numOfNonNullCol, dfCol);
            updateLabelCols(label, labelCols);
        }

        dfCol.AddRange(labelCols);
        var df = new DataFrame(dfCol);
        return df;
    }

    private void copyAllContourListToDf(int numOfNonNullCol, List<PrimitiveDataFrameColumn<double>> dfCol,
        ContourList contourList, List<ContourPoint> contourPoints
    ) {
        for (int i = 0; i < numOfNonNullCol; i++) {
            var offset = i * totalNumberColumnsGeneratedForEachContourPoint;

            var contourPoint = _transformationDecorator.applyTransformation(contourList, contourPoints[i]);
            Debug.Assert(contourPoint != null);
            var extractedData = extract(contourPoint);
            for (int j = 0; j < totalNumberColumnsGeneratedForEachContourPoint; j++) {
                dfCol[offset + j].Append(extractedData[j]);
            }
        }
    }

    private void paddingAllMissingContourPointToDf(int numOfNullCol, int numOfNonNullCol, List<PrimitiveDataFrameColumn<double>> dfCol) {
        for (int i = 0; i < numOfNullCol; i++) {
            var offset = (numOfNonNullCol + i) * totalNumberColumnsGeneratedForEachContourPoint;
            for (int j = 0; j < totalNumberColumnsGeneratedForEachContourPoint; j++) {
                dfCol[offset + j].Append(0);
            }
        }
    }

    private void updateLabelCols(DatasetImageLabel? label, List<PrimitiveDataFrameColumn<double>> labelCols) {
        if (label == null)
            return;
        var jObject = JObject.FromObject(label);
        var dictionary = jObject.ToObject<Dictionary<string, double>>();
        foreach (var labelCol in labelCols) {
            Debug.Assert(dictionary != null);
            labelCol.Append(dictionary[labelCol.Name]);
        }
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