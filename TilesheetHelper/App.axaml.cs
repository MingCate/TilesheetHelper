using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using TilesheetHelper.ViewModels;

namespace TilesheetHelper
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
        public static SolidColorBrush GetColorBrush(string brushName)
        {
            return Application.Current.Resources[brushName] as SolidColorBrush;
        }
        public static MainWindow? GetMainWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return (MainWindow?)desktop.MainWindow;
            }
            return null;
        }
        public static MainWindowViewModel? GetDataContext()
        {
            return (MainWindowViewModel?)GetMainWindow().DataContext;
        }
        public static double Lerp(double start, double end, double t)
        {
            return start + (end - start) * t;
        }
        //public static async Task InvokeDelayedMethod(TimeSpan delay, EventHandler method)
        //{
        //    await Task.Delay(delay);
        //    Avalonia.Threading.DispatcherTimer timer = new Avalonia.Threading.DispatcherTimer();
        //    timer.Interval = delay;
        //    timer.Tick += method;
        //    timer.Tick += (sender, e) =>
        //    { 
        //        timer.Stop();
        //    };
        //    timer.Start();
        //}
        //public static async Task NextFrame(EventHandler method)
        //{
        //    InvokeDelayedMethod(TimeSpan.FromMilliseconds(1), method);
        //}
        public static async Task InvokeAfterDelay(TimeSpan delay, Action action)
        {
            await Task.Delay(delay);
            action.Invoke();
        }
        public static async void InvokeAfterCondition(Func<bool> condition, Action action) //for some reason has more reliable timing than using await Task.Delay, and is less buggy
        {
            Avalonia.Threading.DispatcherTimer timer = new();
            timer.Interval = TimeSpan.FromMilliseconds(1);
            EventHandler stop = (sender, e) =>
            {
                if (condition())
                {
                    action();
                    timer.Stop();
                }
            };
            timer.Tick += stop;

            timer.Start();
        }
        public static async Task NextFrame(Action action)
        {
            await Task.Delay(1);
            action.Invoke();
        }
        public static string? GetFolderName(string filePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(filePath));
        }
        public static TextBlock MakeTextBlock(string text)
        {
            var block = new TextBlock
            {
                Text = text,
                Foreground = App.GetColorBrush("TextLight"),
            };
            return block;
        }
        public static TextBlock MakeLink(string filePath)
        {
            var linkBlock = new TextBlock
            {
                Text = Path.Join(App.GetFolderName(filePath), Path.GetFileName(filePath)),
                //FontFamily = Application.Current.Resources["AndyBold"] as FontFamily,                
                Foreground = App.GetColorBrush("BackgroundAccent"),
                TextDecorations = TextDecorationCollection.Parse("Underline"),
            };
            linkBlock.PointerPressed += OpenFolderOfFile(filePath);
            linkBlock.Cursor = new Cursor(StandardCursorType.Hand);

            linkBlock.MakeReactiveToCursor(TextBlock.ForegroundProperty,
                GetColorBrush("BackgroundAccent"), GetColorBrush("BackgroundAccent").MultiplyLight(1.2), GetColorBrush("BackgroundAccent").MultiplyLight(1.5), 0.5);

            return linkBlock; 
        }
        public static void ShowError(string errorName, string errorDetails)
        {
            InvokeAfterCondition(() => GetMainWindow() is not null,
                () =>
                {
                    double windowWidth = 800;
                    double windowHeight = 350;

                    var headerBlock = new TextBlock
                    {
                        Text = errorName,
                        FontWeight = FontWeight.SemiBold,
                        FontSize = 25,
                        Foreground = App.GetColorBrush("TextMedium"),
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    };
                    var detailsBlock = new TextBlock
                    {
                        Text = errorDetails,
                        FontWeight = FontWeight.Medium,
                        Foreground = App.GetColorBrush("TextDark"),
                        Background = GetColorBrush("BackgroundDark"),
                        Padding = new Thickness(4, 2, 4, 2),
                    };
                    var errorText = new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Vertical,
                        ZIndex = 1,
                    };
                    errorText.Children.Add(headerBlock);
                    errorText.Children.Add(detailsBlock);

                    var greeble = new TextBlock
                    {
                        Text = "WARNINGWARNINGWARNINGWARNINGWARNINGWARNING",
                        FontWeight = FontWeight.Heavy,
                        Foreground = App.GetColorBrush("BackgroundLight").MultiplyLight(0.9),
                        TextDecorations = TextDecorationCollection.Parse("Underline"),
                    };
                    var greeble2 = new TextBlock
                    {
                        Text = "WARNINGWARNINGWARNINGWARNINGWARNINGWARNING",
                        FontWeight = FontWeight.Heavy,
                        Foreground = App.GetColorBrush("BackgroundLight").MultiplyLight(0.9),
                        TextDecorations = TextDecorationCollection.Parse("Overline"),

                    };
                    var pulseAnim = App.CreateFadeInOut(TextBlock.ForegroundProperty, App.GetColorBrush("BackgroundLight").MultiplyLight(0.9), App.GetColorBrush("BackgroundDark").MultiplyLight(1.05), TimeSpan.FromMilliseconds(625), TimeSpan.FromMilliseconds(1250), new SineEaseIn());
                    pulseAnim.IterationCount = IterationCount.Infinite;
                    pulseAnim.Apply(greeble, greeble.Clock, Observable.Return(true), () => { });
                    pulseAnim.Apply(greeble2, greeble2.Clock, Observable.Return(true), () => { });

                    var viewBox = new Viewbox
                    {
                        Stretch = Stretch.Fill,
                        Height = 30,
                        Width = windowWidth * 2,
                        Child = greeble,
                    };
                    var viewBox2 = new Viewbox
                    {
                        Stretch = Stretch.Fill,
                        Height = 30,
                        Width = windowWidth * 2,
                        Child = greeble2,
                    };

                    var slideAnim = CreateFade(Viewbox.MarginProperty, new Thickness(0, 0, 0, 0), new Thickness(-800, 0, 0, 0), TimeSpan.FromSeconds(5));
                    slideAnim.IterationCount = IterationCount.Infinite;
                    slideAnim.Apply(viewBox, viewBox.Clock, Observable.Return(true), () => { });
                    var slideAnimRev = CreateFade(Viewbox.MarginProperty, new Thickness(-800, 0, 0, 0), new Thickness(0, 0, 0, 0), TimeSpan.FromSeconds(5));
                    slideAnimRev.IterationCount = IterationCount.Infinite;
                    slideAnimRev.Apply(viewBox2, viewBox2.Clock, Observable.Return(true), () => { });

                    var dismissButton = new Border
                    {
                        Width = 180,
                        Height = 40,
                        Background = GetColorBrush("BackgroundMedium"),
                        CornerRadius = CornerRadius.Parse("10"),
                    };
                    var dismissText = new TextBlock
                    {
                        Text = "OK",//"Dismiss",
                        Foreground = GetColorBrush("TextDark"),
                        FontWeight = FontWeight.Heavy,
                        FontSize = 20,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    };
                    dismissButton.Child = dismissText;
                    dismissButton.Cursor = new Cursor(StandardCursorType.Hand);
                    dismissButton.MakeReactiveToCursor(Border.BackgroundProperty,
                        GetColorBrush("BackgroundMedium"), GetColorBrush("BackgroundMedium").MultiplyLight(1.025), GetColorBrush("BackgroundLight").MultiplyLight(1.5), 0.5);

                    var canvas = new Canvas
                    {
                    };
                    canvas.Children.Add(viewBox);
                    canvas.Children.Add(viewBox2);
                    Canvas.SetBottom(viewBox2, 0);

                    var caution = DrawCautionSign(App.GetColorBrush("BackgroundLight").MultiplyLight(0.8), 150, 15);
                    Canvas.SetTop(caution, windowHeight / 4);
                    Canvas.SetLeft(caution, 100);
                    canvas.Children.Add(caution);

                    Canvas.SetLeft(errorText, windowWidth / 4);
                    Canvas.SetBottom(errorText, 140);
                    canvas.Children.Add(errorText);

                    Canvas.SetLeft(dismissButton, 400 - dismissButton.Width / 2);
                    Canvas.SetBottom(dismissButton, windowHeight / 4);
                    canvas.Children.Add(dismissButton);

                    var settingsWindow = new Window
                    {
                        Width = windowWidth,
                        Height = windowHeight,
                        Content = canvas,
                        Background = GetColorBrush("BackgroundDark"),
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Title = "Error",
                        Icon = Application.Current.Resources["ErrorIcon"] as WindowIcon,
                        CanResize = false,
                    };
                    settingsWindow.ShowDialog(GetMainWindow());
                    dismissButton.PointerPressed += (sender, e) => settingsWindow.Close();
                });
        }
        private static EventHandler<PointerPressedEventArgs> OpenFolderOfFile(string filePath)
        {
            return (sender, e) =>
            {
                if (File.Exists(filePath))
                {
                    Process.Start("explorer.exe", "/select, " + filePath);
                }
            };
        }
        /// <summary>
        /// Transitions a property between two values. Fails silently if the property type does not match the start or ending values.
        /// </summary>>
        public static Animation CreateFade(AvaloniaProperty property, object startValue, object endValue, TimeSpan duration, Easing? easing = null)
        {
            //if (startValue.GetType().IsAssignableTo(property.PropertyType)) throw new ArgumentException();

            if (easing is null) easing = new LinearEasing();

            var frame1 = new KeyFrame()
            {
                Cue = Cue.Parse("0%", CultureInfo.CurrentCulture),
                Setters = { new Setter(property, startValue), },
            };
            var frame2 = new KeyFrame()
            {
                Cue = Cue.Parse("100%", CultureInfo.CurrentCulture),
                Setters = { new Setter(property, endValue), },
            };
            var anim = new Animation()
            {
                Duration = duration,
                IterationCount = IterationCount.Parse("1"),
                Children = { frame1, frame2 },
                PlaybackDirection = PlaybackDirection.Normal,
                Easing = easing,
                FillMode = FillMode.Forward,
            };
            return anim;
        }
        /// <summary>
        /// Transitions a property between two values, then reverses the transition after an optional delay. Fails silently if the property type does not match the start or ending values.
        /// </summary>>
        public static Animation CreateFadeInOut(AvaloniaProperty property, object startValue, object endValue, TimeSpan oneWayDuration, TimeSpan totalDuration, Easing? easing = null)
        {
            //if (startValue.GetType().IsAssignableTo(property.PropertyType)) throw new ArgumentException();
            if (totalDuration / 2 < oneWayDuration) throw new ArgumentException("The total duration must be at least twice as long as the one way duration.");

            Animation anim = CreateFade(property, startValue, endValue, oneWayDuration, easing);
            anim.IterationCount = IterationCount.Parse("2");
            anim.PlaybackDirection = PlaybackDirection.Alternate;
            anim.DelayBetweenIterations = totalDuration - oneWayDuration * 2;

            return anim;
        }
        public static Avalonia.Media.Color AvaloniaColor(System.Drawing.Color input)
        {
            return Avalonia.Media.Color.FromArgb(input.A, input.R, input.G, input.B);
        }
        public static System.Drawing.Color SystemColor(Avalonia.Media.Color input)
        {
            return System.Drawing.Color.FromArgb(input.A, input.R, input.G, input.B);
        }
        public static Canvas DrawCautionSign(IBrush brushColor, double size, double strokeThickness)
        {
            Point point1 = new Point(0, Math.Sqrt(3) / 2 * size);
            Point point2 = new Point(1 * size, Math.Sqrt(3) / 2 * size);
            Point point3 = new Point(0.5 * size, 0);
            Point linePoint1 = new Point(0.5 * size, 3f / 10f * size);
            Point linePoint2 = new Point(0.5 * size, 5.5f / 10f * size);
            Point dotPoint = new Point(0.5 * size, 7f / 10f * size);

            var canvas = new Canvas()
            {

            };

            var line1 = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = point1,
                EndPoint = point2,
            };

            var line2 = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = point2,
                EndPoint = point3,
            };

            var line3 = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = point3,
                EndPoint = point1,
            };

            var line4 = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = linePoint1,
                EndPoint = linePoint2,
            };

            var line5 = new Avalonia.Controls.Shapes.Line
            {
                StartPoint = dotPoint,
                EndPoint = dotPoint,
            };

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
            canvas.Children.Add(line3);
            canvas.Children.Add(line4);
            canvas.Children.Add(line5);
            foreach (var child in canvas.Children)
            {
                if (child is Avalonia.Controls.Shapes.Line line)
                {
                    line.StrokeLineCap = PenLineCap.Round;
                    line.Stroke = brushColor;
                    line.StrokeThickness = strokeThickness;
                }
            }
            return canvas;
        }
        public static Canvas DrawGear(SolidColorBrush brushColor, IBrush backgroundColor, double size, double strokeThickness)
        {
            Point point1 = new Point(0, 0);
            Point point2 = new Point(size, size);
            Point center = new Point(size * 0.5, size * 0.5);

            var canvas = new Canvas()
            {

            };

            Point[] points = GetPointsOnCirclePerimeter(8, size);
            for (int i = 0; i < points.Length / 2; i++)
            {
                Point point = points[i];
                var line = new Avalonia.Controls.Shapes.Line
                {
                    StartPoint = point,
                    EndPoint = points[i + points.Length / 2],
                };
                canvas.Children.Add(line);
            }

            var ellipse = new Avalonia.Controls.Shapes.Ellipse
            {
                Width = size,
                Height = size,
                Stroke = brushColor,
                Fill = backgroundColor,
            };
            canvas.Children.Add(ellipse);
            ellipse.StrokeThickness = strokeThickness;

            foreach (var child in canvas.Children)
            {
                if (child is Avalonia.Controls.Shapes.Line line)
                {
                    line.StrokeLineCap = PenLineCap.Round;
                    line.Stroke = brushColor;
                    line.StrokeThickness = strokeThickness * 1.25;
                }
            }
            return canvas;
        }
        public static Point[] GetPointsOnCirclePerimeter(int numPoints, double size)
        {
            double halfSize = size / 2;
            List<Point> points = new();

            float angleStep = 360f / numPoints;

            for (int i = 0; i < numPoints; i++)
            {
                double angle = i * angleStep;
                double radians = angle * (Math.PI / 180);

                // Calculate x and y coordinates
                double x = halfSize + halfSize * (float)Math.Cos(radians);
                double y = halfSize + halfSize * (float)Math.Sin(radians);

                points.Add(new Point(x, y));
            }

            return points.ToArray();
        }

        public static Point GetOppositeOfPoint(Point point, double size)
        {
            double x = ((point.X - size / 2) * -1) + size * 2;
            double y = ((point.Y - size / 2) * -1) + size * 2;
            return new Point(x, y);
        }
        public static string PrependAorAn(string input, bool uppercase = true)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string a = uppercase ? "A" : "a";
            string an = uppercase ? "An" : "an";

            char inputFirstLetter = input.TrimStart().ToLower()[0];
            char[] vowels = { 'a', 'e', 'i', 'o', 'u' }; //wouldn't work on words like yttrium or honor
            foreach (char vowel in vowels)
            {
                if (inputFirstLetter == vowel) return an + " " + input;
            }
            return a + " " + input;
        }
    }
    public static class ClassExtensions
    {
        /// <summary>
        /// Applies a class, removing it if the element already has this class. Allows for reapplying animations easily.
        /// </summary>>
        public static void Apply(this Classes classes, string className)
        {
            classes.Remove(className);
            classes.Add(className);
        }
        public static Bitmap ConvertToAvaloniaBitmap(this System.Drawing.Image bitmap)
        {
            if (bitmap == null)
                return null;
            System.Drawing.Bitmap bitmapTmp = new System.Drawing.Bitmap(bitmap);
            var bitmapdata = bitmapTmp.LockBits(new System.Drawing.Rectangle(0, 0, bitmapTmp.Width, bitmapTmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Bitmap bitmap1 = new Bitmap(Avalonia.Platform.PixelFormat.Bgra8888, Avalonia.Platform.AlphaFormat.Premul,
                bitmapdata.Scan0,
                new Avalonia.PixelSize(bitmapdata.Width, bitmapdata.Height),
                new Avalonia.Vector(96, 96),
                bitmapdata.Stride);
            bitmapTmp.UnlockBits(bitmapdata);
            bitmapTmp.Dispose();
            return bitmap1;
        }
        public static Panel FindClosestPanelParent(this Control control)
        {
            var parent = control.Parent;

            while (parent != null)
            {
                if (parent is Panel panel)
                {
                    return panel;
                }

                parent = parent.Parent;
            }
            return null;
        }
        private static void FindColorSelectButtonsRecursive(this Panel panel, List<ColorSelectButton> buttons)
        {
            foreach (var child in panel.Children)
            {
                if (child is ColorSelectButton button)
                {
                    buttons.Add(button);
                }

                if (child is Panel childPanel)
                {
                    FindColorSelectButtonsRecursive(childPanel, buttons);
                }
            }
        }
        public static SolidColorBrush MultiplyLight(this SolidColorBrush brush, double brightnessFactor)
        {
            double factor = Math.Clamp(brightnessFactor, 0, 2); // Clamp brightness adjustment value between 0 and 2

            byte endValue = 0;
            if (factor > 1)
            {
                endValue = 255;
                factor -= 1;
            }
            else factor = (1 - factor);

            Color color = brush.Color;

            Color lerpedColor = Color.FromRgb((byte)App.Lerp(color.R, endValue, factor), (byte)App.Lerp(color.G, endValue, factor), (byte)App.Lerp(color.B, endValue, factor));

            return new SolidColorBrush(lerpedColor);
        }

        //public static IDisposable Apply(this Animation animation, Animatable control, IClock clock, Action onComplete = null, IObservable<bool> match = null)
        //{
        //    if (onComplete is null) onComplete = () => { };
        //    if (match is null) match = Observable.Return(value: true);
        //    return animation.Apply(control, clock, match, onComplete);
        //}
        public static void QueueAnimations(this Animatable control, IClock clock, params Animation[] anims)
        {
            anims = anims.Reverse().ToArray();
            Action queue = null;

            foreach (var anim in anims) //builds a chain of animations using oncomplete, like matryoshka dolls
            {
                Action onComplete = () => { };
                if (queue is not null) onComplete = queue;

                Action animChain = () => anim.Apply(control, clock, Observable.Return(true), onComplete);
                
                queue = animChain;
            }
            queue.Invoke();
        }
        //public static void QueueAnimation(this Animatable animatable, Animation anim)
        //{
        //    anim.RunAsync(animatable, animatable.Clock);
        //}
        /// <summary>
        /// Makes a property of a button automatically react to cursor actions. Fails silently if the property type does not match any of the value types, or if any of the values are taken from unfinalized properties.
        /// </summary>>
        public static void MakeReactiveToCursor(this InputElement button, AvaloniaProperty property, object baseValue, object hoverValue, object clickValue, double speedFactor = 1.0, InputElement? listener = null) //may need to set this up to target child properties
        {
            if (listener is null) listener = button;

            var hoverAnim = App.CreateFade(property, baseValue, hoverValue, TimeSpan.FromMilliseconds(100));
            var unhoverAnim = App.CreateFade(property, hoverValue, baseValue, TimeSpan.FromMilliseconds(100));
            var clickAnim = App.CreateFadeInOut(property, hoverValue, clickValue, TimeSpan.FromMilliseconds(200 * speedFactor), TimeSpan.FromMilliseconds(400 * speedFactor));

            EventHandler<PointerEventArgs> hoverEffect = (sender, e) =>
            {
                hoverAnim.RunAsync(button, button.Clock);
            };
            EventHandler<PointerEventArgs> unhoverEffect = (sender, e) =>
            {
                unhoverAnim.RunAsync(button, button.Clock);
            };
            EventHandler<PointerPressedEventArgs> clickEffect = (sender, e) =>
            {
                button.QueueAnimations(button.Clock, clickAnim, unhoverAnim);
            }; //TODO: have a bool for animations where the cursor will be hovering when done vs when it wont be and needs to fade completely back to defauult

            listener.PointerEnter += hoverEffect;
            listener.PointerLeave += unhoverEffect;
            listener.PointerPressed += clickEffect;
        }
        public static void AddTooltip(this InputElement parent, string tooltipText, Dock attachAt)
        {
            double padding = 8;
            TextBlock text = App.MakeTextBlock(tooltipText);
            text.Foreground = App.GetColorBrush("TextDark");
            text.FontWeight = FontWeight.Medium;
            //text.Width = 200;
            //text.Height = 30;
            text.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            text.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            //Border tooltip = new Border
            //{
            //    Background = Brushes.Black,
            //    CornerRadius = new CornerRadius(10),
            //    Child = text,
            //    Width = text.Width + 10,
            //    Height = text.Height,
            //};
            Border tooltipBorder = new Border
            {
                Background = Brushes.Black,
                CornerRadius = new CornerRadius(10),
                Child = text,
                Padding = new Thickness(4),
            };
            Viewbox tooltip = new Viewbox
            {
                Child = tooltipBorder,
                Width = 400,
                Height = 30,
            };
            tooltip.ZIndex = 100;
            tooltip.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
            tooltip.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;

            tooltip.Opacity = 0;
            var fade = new DoubleTransition
            {
                Easing = new CubicEaseIn(),
                Property = Visual.OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(200),
            };
            tooltip.Transitions = new Transitions();
            tooltip.Transitions.Add(fade);

            EventHandler<PointerEventArgs> hoverEffect = (sender, e) =>
            {
                if (tooltip.Parent is not null) return;

                App.NextFrame(() => { SetPositionRelativeToParent(); });

                (App.GetMainWindow().Content as Panel).Children.Add(tooltip);
                tooltip.Opacity = 1.0;
            };
            EventHandler<PointerEventArgs> unhoverEffect = (sender, e) =>
            {
                tooltip.Opacity = 0.0;
                App.InvokeAfterDelay(fade.Duration, () =>
                {
                    (App.GetMainWindow().Content as Panel).Children.Remove(tooltip);
                });
            };

            void SetPositionRelativeToParent()
            {
                (double x, double y) position = (parent.Bounds.Center.X, parent.Bounds.Center.X);
                switch (attachAt)
                {
                    case Dock.Left:
                        position.x -= parent.Bounds.Width / 2 + tooltip.Bounds.Width + padding;
                        position.y -= tooltip.Bounds.Height / 2;
                        break;
                    case Dock.Right:
                        position.x += parent.Bounds.Width / 2 + padding;
                        position.y -= tooltip.Bounds.Height / 2;
                        break;
                    case Dock.Top:
                        position.x -= tooltip.Bounds.Width / 2;
                        position.y -= parent.Bounds.Height / 2 + tooltip.Bounds.Height / 2 + padding;
                        break;
                    case Dock.Bottom:
                        position.x -= tooltip.Bounds.Width / 2;
                        position.y += parent.Bounds.Height / 2 + padding;
                        break;
                }
                if (position.x < padding) position.x = padding;
                if (position.y < padding) position.y = padding;

                tooltip.Margin = new Thickness(position.x, position.y, 0, 0);
            }

            parent.PointerEnter += hoverEffect;
            parent.PointerLeave += unhoverEffect;
        }
        public static string SubstringBetween(this string input, string start, string end)
        {
            int startIndex = input.IndexOf(start) + start.Length;
            if (startIndex - start.Length >= 0)
            {
                int endIndex = input.LastIndexOf(end) - end.Length;
                if (endIndex > startIndex)
                {
                    return input.Substring(startIndex, endIndex + 1 - startIndex);
                }
            }
            return string.Empty;
        }
        public static string JoinWithOxford(this List<string> strings, string conjunction = "and")
        {
            string result = string.Empty;

            for (int i = 0; i < strings.Count(); i++)
            {
                result += strings[i];
                if (i < strings.Count - 1 && strings.Count > 2) result += ",";
                if (i < strings.Count - 1) result += " ";
                if (i == strings.Count - 2) //penultimate item
                {
                    result += conjunction + " ";
                }
            }
            return result;
        }
    }
    //public class ControlExtensions
    //{
    //    //    public static readonly AttachedProperty<bool> TemplateNameProperty =
    //    //AvaloniaProperty.RegisterAttached<BorderedImageButton, bool>(nameof(TemplateName));
    //    public static readonly AttachedProperty<bool> IsClickableProperty =
    //        AvaloniaProperty.RegisterAttached<Control, bool>(
    //            nameof(IsClickable),
    //            typeof(ControlExtensions),
    //            false,
    //            inherits: true);
    //    private bool IsClickable => (Control.PointerPressedEvent)

    //    public static bool GetIsClickable(Control control)
    //    {
    //        return control.GetValue(IsClickableProperty);
    //    }
    //    public static void SetIsClickable(Control control, bool value)
    //    {
    //        control.SetValue(IsClickableProperty, value);
    //    }
    //}
}
