using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TilesheetHelper.ViewModels;
using Avalonia.Animation.Easings;
using System.Reactive.Linq;

namespace TilesheetHelper
{
    public partial class GenerationButton : UserControl
    {
        public GenerationButton()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            PointerPressed += OnBorderClicked;
            this.MakeReactiveToCursor(BackgroundProperty, App.GetColorBrush("BackgroundAccent"), App.GetColorBrush("BackgroundAccent").MultiplyLight(1.2), App.GetColorBrush("BackgroundAccent").MultiplyLight(1.5));
            this.MakeReactiveToCursor(WidthProperty, 180.0, 180.0 * 1.06, 180.0 * 1.2);
            //this.MakeReactiveToCursor(HeightProperty, 40.0, 40.0 * 1.06, 40.0 * 1.2);
        }
        public bool InputAndOutputSelected => (MainWindowViewModel.InputTemplates.SelectedButton is not null && MainWindowViewModel.OutputTemplates.SelectedButton is not null);
        private async void OnBorderClicked(object sender, PointerPressedEventArgs e)
        {
            if (!InputAndOutputSelected) return;

            PromptUserToSaveTilesheetAsync();

            //var glowAnim = App.CreateFadeInOut(BackgroundProperty, App.GetColorBrush("BackgroundAccent").MultiplyLight(1.2), App.GetColorBrush("BackgroundAccent").MultiplyLight(1.5), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400));
            //var unhoverAnim = App.CreateFade(BackgroundProperty, Background, App.GetColorBrush("BackgroundAccent"), TimeSpan.FromMilliseconds(100));
            //this.QueueAnimations(Clock, glowAnim, unhoverAnim);
        }
        private async void PromptUserToSaveTilesheetAsync()
        {
            Tilesheet sheet = Program.pendingTilesheet;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save As";
            dialog.InitialFileName = sheet.GetSaveName(sheet.path);
            dialog.Directory = Path.GetDirectoryName(sheet.path);
            dialog.DefaultExtension = "png";

            FileDialogFilter filter = new FileDialogFilter();
            filter.Extensions.Add("png");
            filter.Name = "PNG Files";
            dialog.Filters.Add(filter);

            string? fileSavePath = await dialog.ShowAsync(App.GetMainWindow());
            if (fileSavePath is null) return;
  
            var bitmap = sheet.currentBitmap;
            if (sheet.originalResolution == 2 || (bool)Configuration.alwaysExportDouble.Value) bitmap = Tilesheet.GetUpscaledBitmap(bitmap);

            bitmap.Save(fileSavePath, System.Drawing.Imaging.ImageFormat.Png);
            App.GetMainWindow().ShowAlert(TimeSpan.FromSeconds(5), App.MakeTextBlock("Saved at "), App.MakeLink(fileSavePath));
        }
        //private void OnBorderHovered(object sender, PointerEventArgs e)
        //{
        //    //IsEnabled = InputAndOutputSelected;
        //    if (!InputAndOutputSelected) return;

        //    //var hoverAnim = App.CreateFade(BackgroundProperty, Background, App.GetColorBrush("BackgroundAccent").MultiplyLight(1.2), TimeSpan.FromMilliseconds(100));
        //    //hoverAnim.RunAsync(this, Clock);

        //    //Background = App.GetColorBrush("BackgroundAccent").MultiplyLight(1.2);
        //}

        //private void OnBorderUnhovered(object sender, PointerEventArgs e)
        //{
        //    //since clicking opens a window, it will count as unhovering the border and interrupt the glow animation. this queues it until after the animation finishes
        //    //App.InvokeAfterCondition(
        //    //    () => !IsAnimating(BackgroundProperty),
        //    //    () => { Background = App.GetColorBrush("BackgroundAccent"); }
        //    //    );

        //    //unhoverAnim.Apply(this, Clock, Observable.Return(IsAnimating(BackgroundProperty)), () => { });

        //    //var unhoverAnim = App.CreateFade(BackgroundProperty, Background, App.GetColorBrush("BackgroundAccent"), TimeSpan.FromMilliseconds(100));
        //    //unhoverAnim.RunAsync(this, Clock);

        //}
    }
}
