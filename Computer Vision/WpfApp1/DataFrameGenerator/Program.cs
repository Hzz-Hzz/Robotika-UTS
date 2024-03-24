

using System.Globalization;
using DataFrameGenerator;
using DatasetEditor.model;
using ImageProcessingLogic.TrainingDataExtractStrategy;
using Microsoft.Data.Analysis;
using Microsoft.ML;
using Newtonsoft.Json;
using WpfApp1;


// TODO: Set fixed camera rect size gameObject.camera.pixelRect
// and re-gather data information for the cubic spline

class Program
{
    public static void Main(string[] args) {
        var imagesFolder = ViewModelDatasetEditor.datasetFolder;

        var imageFileNames = Directory.GetFiles(imagesFolder, "*.png", SearchOption.AllDirectories);
        var labelFileNames = Directory.GetFiles(imagesFolder, "*.label.json", SearchOption.AllDirectories);
        var labels = labelFileNames.Select(e => {
            DatasetImageLabel ret = JsonConvert.DeserializeObject<DatasetImageLabel>(
                File.ReadAllText(e)) ?? throw new InvalidOperationException();
            return ret;
        }).ToArray();


        // translate to cartesius, then rescale it
        var transformationDecorator = new TranslationToCartesiusDecorator(
            new CubicSplineInterpScalingDecorator());
        var orderingStrategy = new VerticalOrderingAscendingStrategy();
        var dataframeFactory = new DataFrameFactoryShufflerDecorator(new FewFirstContourPointInOneRowDataFrameFactory(
            5, transformationDecorator, orderingStrategy), new Random(11512));  // arbitrary random seed

        var byteToImageConverter = new ByteToCroppedImageFactory();
        var contourLists = new List<ContourList>();


        for (var i = 0; i < imageFileNames.Length; i++) {
            var imageFileName = imageFileNames[i];

            Console.WriteLine($"Converting {imageFileName}...");
            var content = File.ReadAllBytes(imageFileName);
            var image = byteToImageConverter.convert(content);

            var imageMainRoadProcessor = new MainRoadImageProcessing();
            imageMainRoadProcessor.processImage(image);  // to initialize the imageMainRoadProcessor.resultingPolygons

            var imageEdgeProcessor = new RoadEdgeImageProcessing();
            var processingResult = imageEdgeProcessor.getContourList(image, imageMainRoadProcessor.resultingPolygons);
            var contourList = processingResult.Item1;
            contourLists.Add(contourList);
        }


        var df = dataframeFactory.getDataFrame(contourLists.ToArray(), labels);
        DataFrame.SaveCsv(df, Path.Join(imagesFolder, "df.csv"),
            CultureInfo.CurrentCulture.TextInfo.ListSeparator[0], cultureInfo: new CultureInfo("en-US", false));
    }
}