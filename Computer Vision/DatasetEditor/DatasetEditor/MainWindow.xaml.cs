using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();
            this.DataContext = viewModel;
        }

        private void saveDataLabel() {
            var result = Newtonsoft.Json.JsonConvert.SerializeObject(datasetLabelModel);
            File.WriteAllText(viewModel.datasetPath, result);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
            viewModel.nextDataset();
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
                viewModel.nextDataset();
                updateLabel();
                return;
            }
            updateLabel();
        }

        private void updateLabel() {
            datasetLabel.Text = $"angle={datasetLabelModel.angle} speed={datasetLabelModel.speed}";
            this.Title = $"{viewModel.datasetId}.png";
        }
    }
}