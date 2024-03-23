

using System.Globalization;
using DataFrameGenerator;
using DataFrameGenerator.TrainingDataExtractStrategy;
using DatasetEditor.model;
using Microsoft.Data.Analysis;
using Microsoft.ML;
using Newtonsoft.Json;
using WpfApp1;


// TODO: Set fixed camera rect size gameObject.camera.pixelRect
// and re-gather data information for the cubic spline

class Program
{
    public static void Main(string[] args) {
        var viewModelDatasetEditor = new ViewModelDatasetEditor();
        var imagesFolder = ViewModelDatasetEditor.datasetFolder;

        var imageFileNames = Directory.GetFiles(imagesFolder, "*.png", SearchOption.AllDirectories);
        var labelFileNames = Directory.GetFiles(imagesFolder, "*.label.json", SearchOption.AllDirectories);


        // translate to cartesius, then rescale it
        var transformationDecorator = new TranslationToCartesiusDecorator(
            new CubicSplineInterpScalingDecorator());
        var orderingStrategy = new VerticalOrderingAscendingStrategy();
        var dataframeFactory = new DataFrameFactoryShufflerDecorator(new FewFirstContourPointInOneRowDataFrameFactory(
            5, transformationDecorator, orderingStrategy), new Random(11512));  // arbitrary random seed

        var byteToImageConverter = new ByteToCroppedImageFactory();
        var contourLists = new List<ContourList>();
        foreach (var imageFileName in imageFileNames) {
            Console.WriteLine($"Converting {imageFileName}...");
            var content = File.ReadAllBytes(imageFileName);
            var image = byteToImageConverter.convert(content);

            var imageEdgeProcessor = new RoadEdgeImageProcessing();
            var processingResult = imageEdgeProcessor.getContourList(image);
            var contourList = processingResult.Item1;
            contourLists.Add(contourList);
        }

        // var csvOptions = new CsvDataFrameFormatter.Options()
        // {
        //     CultureInfo = CultureInfo.InvariantCulture, // or specify your desired culture
        //     DecimalSeparator = '.' // or ',' depending on your needs
        // };
        var df = dataframeFactory.getDataFrame(contourLists.ToArray());
        DataFrame.SaveCsv(df, Path.Join(imagesFolder, "df.csv"),
            CultureInfo.CurrentCulture.TextInfo.ListSeparator[0]);
    }
}