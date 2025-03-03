using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace TilesheetHelper
{
    public partial class ColorSelectButton : UserControl
    {
        public ColorSelectButton()
        {
            InitializeComponent();
            App.GetDataContext().internalOutlineColorButtons.Add(this);
            border = this.FindControl<Border>("Border");
            Initialized += (sender, e) =>
            {
                if (IsSelected) border.Background = App.GetColorBrush("BackgroundAccent");
            };
        }
        private Border border;

        public static readonly StyledProperty<Avalonia.Media.SolidColorBrush> FillProperty =
            AvaloniaProperty.Register<ColorSelectButton, Avalonia.Media.SolidColorBrush>(nameof(Fill));
        public Avalonia.Media.SolidColorBrush Fill
        {
            get { return GetValue(FillProperty); }
            set
            {
                SetValue(FillProperty, value);
            }
        }
        private bool IsSelected => (Fill.Color == App.GetDataContext().InternalOutlineColor);
        public async void OnBorderClicked(object sender, PointerPressedEventArgs e)
        {
            App.GetDataContext().InternalOutlineColor = Fill.Color;
            foreach (var button in App.GetDataContext().internalOutlineColorButtons)
            {
                button.border.Background = App.GetColorBrush("Clear");
            }
            border.Background = App.GetColorBrush("BackgroundAccent");
        }
        private void OnBorderHovered(object sender, PointerEventArgs e)
        {
            if (!IsSelected) border.Background = App.GetColorBrush("BackgroundLight");
        }
        private void OnBorderUnhovered(object sender, PointerEventArgs e)
        {
            if (!IsSelected) border.Background = App.GetColorBrush("Clear");
        }
    }
}
