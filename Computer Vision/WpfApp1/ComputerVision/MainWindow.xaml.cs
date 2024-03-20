using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModelVisualServer? viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModelVisualServer();
            this.DataContext = viewModel;
            viewModel.start();
        }

        private SettingsPersistenceLogic settingsPersistence = new("settings.xml");



        private void UIElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            var senderUiElement = sender as Image;
            if (senderUiElement == null)
                throw new Exception("Null UIElement UIElement_OnMouseMove");
            if (viewModel == null)
                throw new Exception("Null viewModel UIElement_OnMouseMove");
            var coord = GetImageCoordsAt(e, senderUiElement);
            var source = senderUiElement.Source as BitmapImage;
            if (source == null)
                throw new Exception("Null source UIElement_OnMouseMove");
            var color = getImageColor(source, coord);
            colorInformation.Text = $"({coord.X}, {coord.Y}) --> RGB({color.Red}, {color.Green}, {color.Blue})";
        }
        public BitmapImageUtility.PixelColor getImageColor(BitmapImage img, Point point) {
            return BitmapImageUtility.GetPixels(img)[(int) point.X, (int) point.Y];
        }

        public Point GetImageCoordsAt(MouseEventArgs e, UIElement targetElement) {
            if (targetElement.IsMouseOver)
            {
                var controlSpacePosition = e.GetPosition(targetElement);
                var imageControl = targetElement as Image;
                var mainViewModel = ((ViewModelVisualServer)base.DataContext);
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


        private void MainWindow_OnSourceInitialized(object? sender, EventArgs e)
        {
            this.Top = settingsPersistence.Settings.Top;
            this.Left = settingsPersistence.Settings.Left;
            this.Height = settingsPersistence.Settings.Height;
            this.Width = settingsPersistence.Settings.Width;
            if (settingsPersistence.Settings.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                // Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
                settingsPersistence.Settings.Top = RestoreBounds.Top;
                settingsPersistence.Settings.Left = RestoreBounds.Left;
                settingsPersistence.Settings.Height = RestoreBounds.Height;
                settingsPersistence.Settings.Width = RestoreBounds.Width;
                settingsPersistence.Settings.Maximized = true;
            }
            else
            {
                settingsPersistence.Settings.Top = this.Top;
                settingsPersistence.Settings.Left = this.Left;
                settingsPersistence.Settings.Height = this.Height;
                settingsPersistence.Settings.Width = this.Width;
                settingsPersistence.Settings.Maximized = false;
            }

            settingsPersistence.SaveUserSettings();
            System.Environment.Exit(0);
        }

    }
}