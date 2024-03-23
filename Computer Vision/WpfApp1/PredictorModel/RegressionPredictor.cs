using System.Collections;
using System.ComponentModel.DataAnnotations;
using Emgu.CV.ML;
using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;

namespace PredictorModel;

public class RegressionPredictor<I, O>
    where I: class  // input type
{
    private bool updated = true;
    private int nextId;
    private Dictionary<int, I> data;

    private MLContext _context;
    private string[] _columns;
    private string _targetColumn;
    private EstimatorChain<RegressionPredictionTransformer<LightGbmRegressionModelParameters>> _pipeline;
    private TransformerChain<RegressionPredictionTransformer<LightGbmRegressionModelParameters>> _model;

    public RegressionPredictor(string targetColumn, Dictionary<int, I>? data=null,  MLContext? context=null, params string[] excludeColumn) {
        data = new Dictionary<int, I>();
        this.data = data;
        nextId = data.Count==0? 0 : data.Keys.Max() + 1;

        _context = context ?? new MLContext();

        _columns = typeof(I).GetProperties().Select(field => field.Name)
            .Concat(typeof(I).GetFields().Select(field => field.Name))
            .ToArray();
        if (!_columns.Contains(targetColumn))
            throw new ValidationException($"Field {targetColumn} is not found in class {typeof(I).Name}");
        _columns = _columns.Where(e => e != targetColumn && !excludeColumn.Contains(e)).ToArray();

        _pipeline = _context.Transforms.Concatenate("Features", _columns)
            .Append(_context.Regression.Trainers.LightGbm(targetColumn, "Features"));
    }

    /**
     * Don't forget to call fit() to reupdate the model
     */
    public void append(I newRow) {
        updated = true;
        data[nextId++] = newRow;
    }

    /**
     * Don't forget to call fit() to reupdate the model
     */
    public void appendAll(List<I> newRows) {
        updated = true;
        foreach (var row in newRows) {
            data[nextId++] = row;
        }
    }

    /**
     * Don't forget to call fit() to reupdate the model
     */
    public void remove(int index) {
        updated = true;
        data.Remove(index);
    }

    public EstimatorChain<RegressionPredictionTransformer<LightGbmRegressionModelParameters>> getModel() {
        return _pipeline;
    }

    public void fit() {
        updated = false;
        var trainingData = _context.Data.LoadFromEnumerable(data.Values);
        _model = _pipeline.Fit(trainingData);
    }


    public O predict(I input, bool ignoreUpdateStatus = false) {
        var result = predict(new[] { input }, ignoreUpdateStatus);
        return result.GetColumn<O>("Score").First();
    }

    public IDataView predict(IEnumerable<I> input, bool ignoreUpdateStatus=false) {
        IDataView dataView = _context.Data.LoadFromEnumerable(input);
        return predict(dataView, ignoreUpdateStatus);
    }

    public IDataView predict(IDataView input, bool ignoreUpdateStatus=false) {
        if (updated && !ignoreUpdateStatus)
            throw new ValidationException(
                "Training data is updated but the model is not updated. Forget to call fit()?");
        var result = _model.Transform(input);
        return result;
    }
}