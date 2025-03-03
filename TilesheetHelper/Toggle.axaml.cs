using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;

namespace TilesheetHelper
{
    public partial class Toggle : UserControl
    {
        public Toggle()
        {
            InitializeComponent();
            //this.FindControl<Ellipse>("Circle").MakeReactiveToCursor(Shape.FillProperty, App.GetColorBrush("TextLight"), App.GetColorBrush("TextLight").MultiplyLight(1.1), App.GetColorBrush("TextLight").MultiplyLight(1.2));
        }
        public Toggle(Setting<bool> connectedSetting)
        {
            InitializeComponent();
            setting = connectedSetting;
            UpdateVisuals();
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
            //this.FindControl<Ellipse>("Circle").MakeReactiveToCursor(Shape.StrokeProperty, App.GetColorBrush("TextLight"), App.GetColorBrush("TextLight").MultiplyLight(1.1), App.GetColorBrush("TextLight").MultiplyLight(1.2));
        }
        Setting<bool> setting;
        bool Value => (bool)setting.Value;
        public async void OnClick(object sender, PointerPressedEventArgs e)
        {
            setting.Value = !(bool)setting.Value;
            UpdateVisuals();
        }
        public void UpdateVisuals()
        {
            if (Value == true)
            {
                this.FindControl<Ellipse>("Circle").Margin = new Avalonia.Thickness(16,0,0,0);
                //Canvas.SetRight(this.FindControl<Ellipse>("Circle"), 4);
                this.FindControl<Line>("Bar").Stroke = App.GetColorBrush("BackgroundAccent");
            }
            else
            {
                this.FindControl<Ellipse>("Circle").Margin = new Avalonia.Thickness(0, 0, 0, 0);
                //Canvas.SetRight(this.FindControl<Ellipse>("Circle"), 16);
                this.FindControl<Line>("Bar").Stroke = App.GetColorBrush("BackgroundDark");
            }
        }
    }
}
