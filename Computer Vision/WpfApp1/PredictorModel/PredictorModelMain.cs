using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataFrameGenerator;
using Microsoft.ML;
using PredictorModel;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

public class PredictorModelMain
{
    public static MLContext context = new MLContext();

    public static RegressionPredictor<DataModel, float> getPredictor(out Tuple<List<DataModel>, List<DataModel>> split, float testPercentage = 0.2f) {
        var data = csvDeserializer<DataModel>(File.ReadAllText("../../../../../../dataset/df.csv"));
        split = splitTrainTest(data, testPercentage);

        var predictor = new RegressionPredictor<DataModel, float>("angle", context: context);
        predictor.appendAll(split.Item1);
        predictor.fit();
        return predictor;
    }

    public static void Main(string[] args) {
        Tuple<List<DataModel>, List<DataModel>> split;
        var predictor = getPredictor(out split);
        var prediction = predictor.predict(split.Item2);
        var assessment = context.Regression.Evaluate(prediction, "angle");
        Console.WriteLine(assessment.RSquared);
        Console.WriteLine(assessment.MeanAbsoluteError);
    }

    public static Tuple<List<T>, List<T>> splitTrainTest<T>(List<T> initialArray, float testPercentage, Random? random=null) {
        if (testPercentage < 0 || testPercentage > 1)
            throw new ValidationException("trainPercentage cannot be lower than 0 and cannot be greater than 1");
        var numOfTestData = (int)(initialArray.Count * testPercentage);
        var numOfTrainingData = initialArray.Count - numOfTestData;

        var arrayShuffler = new ArrayShuffler();
        arrayShuffler.createRandomShuffle(random ?? new Random(), initialArray.Count);
        var shuffledArray = arrayShuffler.applyShuffle(initialArray);

        var training = shuffledArray.GetRange(0, numOfTrainingData);
        var testing = shuffledArray.GetRange(numOfTrainingData, numOfTestData);
        return new Tuple<List<T>, List<T>>(training, testing);
    }

    public static List<T> csvDeserializer<T>(string csvContent, string separator=";", string? newlineSeparator=null) {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = true,
            NewLine = newlineSeparator ?? Environment.NewLine,
            Delimiter = separator,
        };

        using (var textReader = new StringReader(csvContent))
        using (var csv = new CsvReader(textReader, config))
        {
            var records = csv.GetRecords<T>();
            return records.ToList();
        }
    }
}