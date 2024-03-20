

using DatasetEditor.model;
using Microsoft.ML;
using Newtonsoft.Json;
using WpfApp1;

class Main
{
    public static void main() {
        var viewModelDatasetEditor = new ViewModelDatasetEditor();
        var imagesFolder = ViewModelDatasetEditor.datasetFolder;

        var imageFileNames = Directory.GetFiles(imagesFolder, "*.png", SearchOption.AllDirectories);
        var labelFileNames = Directory.GetFiles(imagesFolder, "*.label.json", SearchOption.AllDirectories);

        var labels = labelFileNames.Select(fileName =>
            JsonConvert.DeserializeObject<DatasetImageLabel>(File.ReadAllText(fileName)));




        var context = new MLContext();
        context.Data.LoadFromEnumerable(labels);
    }
}