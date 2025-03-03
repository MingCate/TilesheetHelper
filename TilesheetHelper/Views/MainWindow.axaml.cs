using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
//using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TilesheetHelper.Converters;
using TilesheetHelper.ViewModels;
using Color = Avalonia.Media.Color;

namespace TilesheetHelper
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //CreatePalettePanel();
            Configuration.LoadSettings();
            //CreateSettingsWindow();

            AddHandler(DragDrop.DragEnterEvent, DimScreen);
            this.FindControl<Grid>("Overlay").AddHandler(DragDrop.DragLeaveEvent, UndimScreen);
            this.FindControl<Grid>("Overlay").AddHandler(DragDrop.DropEvent, UndimScreen);
            this.FindControl<Grid>("Overlay").AddHandler(DragDrop.DropEvent, TryLoadDroppedFile);

            //DataContext = this;


            //this.Resources.Add("ImageWidthConverter", new ImageWidthConverter());
            /*            // Create a dictionary of key/value pairs for the colors
            var colorDictionary = new Dictionary<string, Color>
            {
                { "BackgroundLight", Color.Parse("#4a4a4a") },
                { "BackgroundMedium", Color.Parse("#323236") },
                { "BackgroundDark", Color.Parse("#26272b") },
                { "TextLight", Color.Parse("#808080") }
            };

            // Add each color as a SolidColorBrush resource to the window's resources
            var resources = this.Resources;
            foreach (var kvp in colorDictionary)
            {
                var brush = new SolidColorBrush(kvp.Value);
                resources.Add(kvp.Key, brush);
            }*/

            //GenerateButton = this.FindControl<Border>("GenerateButton");

#if DEBUG
            this.AttachDevTools();
#endif
        }
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreatePalettePanel()
        {
            var paletteStackPanel = new StackPanel();
            paletteStackPanel.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            var resources = Application.Current.Resources;

            foreach (var resourceKey in resources.Keys)
            {
                if (resources[resourceKey] is SolidColorBrush brush)
                {
                    var textBlock = new TextBlock
                    {
                        Text = resourceKey.ToString(),
                        Foreground = new SolidColorBrush(Colors.White),
                        Background = brush,
                        FontSize = 12,
                        Padding = new Thickness(10),
                        Margin = new Thickness(2)
                    };

                    paletteStackPanel.Children.Add(textBlock);
                }
            }
            var paletteWindow = new Window
            {
                Title = "Palette Window",
                Width = 400,
                Height = 300,
                Content = paletteStackPanel,
                Background = App.GetColorBrush("BackgroundMedium"),
                Icon = this.Icon,
                Position = new PixelPoint(0, this.Position.Y),
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 200,
            };
            paletteWindow.Show();
        }
        private Border CreateSettingsWindow(/*object sender, PointerPressedEventArgs e*/)
        {
            var settingsPanel = new StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            List<SettingGroup> loadedSettingGroups = new List<SettingGroup>();
            var settingsByGroup = Configuration.SettingsList.Values.ToList().OrderBy(setting => setting.Group?.Index);
            foreach (var setting in settingsByGroup)
            {
                if (setting.Group is not null && !loadedSettingGroups.Contains(setting.Group))
                {
                    loadedSettingGroups.Add(setting.Group);
                    settingsPanel.Children.Add(new TextBlock() {
                        Text = setting.Group.Name,
                        Foreground = App.GetColorBrush("TextDark"),
                        FontSize = 15,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = Thickness.Parse("0, 10, 0, 2"),
                    });

                }
                //settingsPanel.Children.Add(textBlock);
                settingsPanel.Children.Add(setting.Control);
            }
            //var settingsWindow = new Window
            //{
            //    Title = "Palette Window",
            //    Width = 400,
            //    Height = 300,
            //    Content = settingsPanel,
            //    Background = App.GetColorBrush("BackgroundMedium"),
            //    Icon = this.Icon,
            //    Position = new PixelPoint(0, this.Position.Y),
            //    SizeToContent = SizeToContent.WidthAndHeight,
            //    MinWidth = 200,
            //};
            //settingsWindow.Show();

            var settingsBox = new Viewbox
            {
                Child = settingsPanel,
            };
            var settingsBorder = new Border
            {
                BoxShadow = BoxShadows.Parse("3 3 12 -2 #88000000"),
                CornerRadius = CornerRadius.Parse("10"),
                Name = "SettingsPanel",
                Opacity = 0,
                Width = 300,
                Height = 450,
                Child = settingsBox, //settingsPanel,//settingsBox,
                Background = App.GetColorBrush("BackgroundDark"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            };
            return settingsBorder;
        }
        public void ShowAlert(TimeSpan duration, params TextBlock[] textBlocks)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(8, 4, 8, 4),
            };

            foreach (var block in textBlocks)
            {
                stackPanel.Children.Add(block);
            }

            var viewBox = new Viewbox
            {
                Stretch = Stretch.Uniform,
                MinHeight = 30,
                Child = stackPanel,
            };
            var background = new Border
            {
                CornerRadius = CornerRadius.Parse("10,10,0,0"),
                Background = App.GetColorBrush("BackgroundLight"),
                Height = 30.0,
                Width = App.GetMainWindow().FindControl<DockPanel>("ExportPanel").Width,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Child = viewBox,
            };

            var heightAnim = App.CreateFadeInOut(HeightProperty, 0.0, background.Height, TimeSpan.FromMilliseconds(500), duration, new SineEaseOut());
            heightAnim.RunAsync(background, background.Clock);

            var fadeAnim = App.CreateFadeInOut(OpacityProperty, 0.0, 1.0, TimeSpan.FromMilliseconds(500), duration);
            fadeAnim.RunAsync(background, background.Clock);

            (Content as Panel).Children.Add(background);

            App.InvokeAfterDelay(duration, () =>
            {
                (Content as Panel).Children.Remove(background);
            });
        }
        public void DimScreen(object sender, DragEventArgs e)
        {
            var clear = new SolidColorBrush(Colors.Black, 0);
            var dim = new SolidColorBrush(Colors.Black, 0.5);
            var fadeAnim = App.CreateFade(BackgroundProperty, clear, dim, TimeSpan.FromMilliseconds(200), new SineEaseIn());
            var overlay = this.FindControl<Grid>("Overlay"); 
            fadeAnim.RunAsync(overlay, overlay.Clock);
            overlay.IsEnabled = true;

            this.FindControl<Border>("LoadBorder").Opacity = 1;
        }
        public void UndimScreen(object sender, RoutedEventArgs e)
        {
            var clear = new SolidColorBrush(Colors.Black, 0);
            var dim = new SolidColorBrush(Colors.Black, 0.5);
            var fadeAnim = App.CreateFade(BackgroundProperty, dim, clear, TimeSpan.FromMilliseconds(150), new SineEaseIn());
            var overlay = this.FindControl<Grid>("Overlay");
            fadeAnim.RunAsync(overlay, overlay.Clock);
            overlay.IsEnabled = false;

            this.FindControl<Border>("LoadBorder").Opacity = 0;
        }
        public void TryLoadDroppedFile(object sender, DragEventArgs e)
        {
            string droppedFile = e.Data.GetFileNames().First();
            Tilesheet? input = Program.TryGetTilesheetFromPath(droppedFile);
            if (input is null || input.type == TilesheetType.InvalidInput) return;

            Program.SelectedInput = input;
            try
            {
                var inputButton = MainWindowViewModel.InputTemplates.ButtonsExposed.First(b => b.Button.Type == input.type).Button;
                inputButton.Group.ButtonClicked(inputButton, true);
                inputButton.OnSelected();
            }
            catch (InvalidOperationException ex)
            {
                App.ShowError("Invalid input tilesheet", $"{ App.PrependAorAn(input.type.GetName()) } can't be used as an input.");
            } //TODO Come back when more tilesheet types are added and make sure this still works correctly
        }
        public void SetupInputTemplates(object sender, EventArgs e)
        {
            var panel = (sender as StackPanel);
            for (int i = 0; i < MainWindowViewModel.InputTemplates.Buttons.Count; i++)
            {
                BorderedImageButton? button = MainWindowViewModel.InputTemplates.Buttons[i]; //TODO do the same with output templates
                panel.Children.Add(button);
            }
        }
        public void LoadAnimationsHack(object sender, EventArgs e) //for some reason the first animation involving some kinds of controls will lag for some reason. this animates right after the program loads in a way that shouldn't be noticeable. i hate this
        {
            var anim = App.CreateFade(Border.OpacityProperty, 0.0001, 0.0, TimeSpan.FromMilliseconds(1));
            anim.RunAsync((sender as Animatable), (sender as Animatable).Clock);
        }
        public void SetupSettingsOpenButton(object sender, EventArgs e)
        {
            Border border = (sender as Border);
            Canvas gearIcon = App.DrawGear(App.GetColorBrush("TextDark"), App.GetColorBrush("BackgroundDark"), 25, 5);
            gearIcon.Name = "GearIcon";

            border.MakeReactiveToCursor(BackgroundProperty, App.GetColorBrush("BackgroundDark"), App.GetColorBrush("BackgroundDark").MultiplyLight(0.9), App.GetColorBrush("BackgroundExtraDark"), 0.5);
            border.AddTooltip("Hey how are you doing fella!", Dock.Right);
            foreach (var child in gearIcon.Children)
            {
                if (child is Shape shape)
                {
                    shape.MakeReactiveToCursor(Shape.StrokeProperty, App.GetColorBrush("TextDark"), App.GetColorBrush("TextMedium"), App.GetColorBrush("TextMedium").MultiplyLight(1.1), 0.5, border);
                }
            }
            border.Child = gearIcon;
            border.PointerPressed += OpenOrCloseSettingsMenu;
        }
        public void OpenOrCloseSettingsMenu(object sender, EventArgs e)
        {
            var parentPanel = (sender as IControl).Parent as Panel;

            Border? settingsPanel = null;
            foreach (var child in parentPanel.Children)
            {
                if (child is Border && child.Name == "SettingsPanel")
                {
                    settingsPanel = child as Border;
                    break;
                }
            };

            if (settingsPanel is null)
            {
                settingsPanel = CreateSettingsWindow();
                parentPanel.Children.Add(settingsPanel);
            }

            if (settingsPanel.Opacity == 0)
            {
                var fadeInAnim = App.CreateFade(OpacityProperty, 0.0, 1.0, TimeSpan.FromMilliseconds(200), new QuinticEaseOut());
                fadeInAnim.RunAsync(settingsPanel, settingsPanel.Clock);
                var heightInAnim = App.CreateFade(HeightProperty, settingsPanel.Height * 0.9, settingsPanel.Height, TimeSpan.FromMilliseconds(200), new QuinticEaseOut());
                heightInAnim.RunAsync(settingsPanel, settingsPanel.Clock);
                settingsPanel.IsEnabled = true;
            }
            else if (settingsPanel.Opacity == 1)
            {
                var fadeOutAnim = App.CreateFade(OpacityProperty, 1.0, 0.0, TimeSpan.FromMilliseconds(200), new QuinticEaseOut());
                fadeOutAnim.RunAsync(settingsPanel, settingsPanel.Clock);
                settingsPanel.IsEnabled = false;
                var heightOutAnim = App.CreateFade(HeightProperty, settingsPanel.Height, settingsPanel.Height * 0.9, TimeSpan.FromMilliseconds(200), new QuinticEaseOut());
                heightOutAnim.FillMode = FillMode.None;
                heightOutAnim.RunAsync(settingsPanel, settingsPanel.Clock);
                
            }
        }
    }
}
