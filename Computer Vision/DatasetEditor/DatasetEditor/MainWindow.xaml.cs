using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DatasetEditor.model;
using WpfApp1;

namespace DatasetEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModelDatasetEditor _viewModelDatasetEditor;

        public MainWindow()
        {
            InitializeComponent();
            _viewModelDatasetEditor = new ViewModelDatasetEditor();
            this.DataContext = _viewModelDatasetEditor;
        }

        private void saveDataLabel() {
            var result = Newtonsoft.Json.JsonConvert.SerializeObject(datasetLabelModel);
            File.WriteAllText(_viewModelDatasetEditor.datasetPath, result);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            _viewModelDatasetEditor.prevDataset();
            _viewModelDatasetEditor.nextDataset();
            updateLabel();
        }


        private DatasetImageLabel datasetLabelModel = new DatasetImageLabel(0, 4);

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            if (Key.D0 <= e.Key && e.Key <= Key.D9) {
                var key = e.Key - Key.D0;
                datasetLabelModel.angle = key - 4;
            }
            switch (e.Key) {
                case Key.Q:
                    datasetLabelModel.speed = 1;
                    break;
                case Key.W:
                    datasetLabelModel.speed = 2;
                    break;
                case Key.E:
                    datasetLabelModel.speed = 3;
                    break;
                case Key.R:
                    datasetLabelModel.speed = 4;
                    break;
            }

            if (e.Key == Key.Enter) {
                saveDataLabel();
                _viewModelDatasetEditor.nextDataset();
                updateLabel();
                return;
            }
            updateLabel();
        }

        private void updateLabel() {
            datasetLabel.Text = $"angle={datasetLabelModel.angle} speed={datasetLabelModel.speed}";
            this.Title = $"{_viewModelDatasetEditor.datasetId}.png";
        }


        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            var senderUiElement = sender as Image;
            if (senderUiElement == null)
                throw new Exception("Null UIElement UIElement_OnMouseMove");
            var coord = GetImageCoordsAt(e, senderUiElement);
            var source = senderUiElement.Source as BitmapImage;
            if (source == null)
                throw new Exception("Null source UIElement_OnMouseMove");
            var color = getImageColor(source, coord);
            colorInformation.Text = $"({coord.X}, {coord.Y}) --> RGB({color.Red}, {color.Green}, {color.Blue})";
        }
        public ImageUtility.PixelColor getImageColor(BitmapImage img, Point point) {
            return ImageUtility.GetPixels(img)[(int) point.X, (int) point.Y];
        }
        public Point GetImageCoordsAt(MouseEventArgs e, UIElement targetElement) {
            if (targetElement.IsMouseOver)
            {
                var controlSpacePosition = e.GetPosition(targetElement);
                var imageControl = targetElement as Image;
                if (imageControl != null && imageControl.Source != null)
                {
                    // Convert from control space to image space
                    var x = Math.Floor(controlSpacePosition.X * imageControl.Source.Width / imageControl.ActualWidth);
                    var y = Math.Floor(controlSpacePosition.Y * imageControl.Source.Height / imageControl.ActualHeight);

                    return new Point(x, y);
                }
            }
            return new Point(-1, -1);
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Q)
                _viewModelDatasetEditor.prevDataset();
            if (e.Key == Key.E)
                _viewModelDatasetEditor.nextDataset(skipAlreadyDefinedLabel: false);
        }
    }
}