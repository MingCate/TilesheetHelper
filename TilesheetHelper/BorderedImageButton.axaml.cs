using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TilesheetHelper.ViewModels;

namespace TilesheetHelper
{
    public partial class BorderedImageButton : UserControl
    {
        public BorderedImageButton()
        {
            InitializeComponent();
        }

        public Border border;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            border = this.FindControl<Border>("Border");
            Initialized += GetPropertiesFromTemplateIfFound;
            Initialized += SelectIfArgsMatch;
        }
        private void GetPropertiesFromTemplateIfFound(object? sender, EventArgs e)
        {
            if (Name is not null)
            {
                BorderedImageButton? templateButton = Group?.FindTemplateFromName(this.Name);
                if (templateButton == null) return;

                Uri = templateButton.Uri;
                InfoUri = templateButton.InfoUri;
                Dimensions = templateButton.Dimensions;
                Type = templateButton.Type;
                Description = templateButton.Description;
            }
        }
        private void SelectIfArgsMatch(object? sender, EventArgs e)
        {
            if (Program.SelectedInput is not null && Type == Program.SelectedInput.type)
            {
                Group.ButtonClicked(this, true);
                OnSelected();
            }
        }

        public static readonly StyledProperty<Bitmap> SourceProperty =
            AvaloniaProperty.Register<BorderedImageButton, Bitmap>(nameof(Source));
        public Bitmap Source
        {
            get { return GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly StyledProperty<ButtonGroup?> GroupProperty = AvaloniaProperty.Register<BorderedImageButton, ButtonGroup?>(nameof(GroupProperty));

        public ButtonGroup? Group
        {
            get => GetValue(GroupProperty);
            set => SetValue(GroupProperty, value);
        }

        public string Uri { get; set; }
        public string InfoUri { get; set; }
        public string Dimensions { get; set; }
        public string Description { get; set; }
        public TilesheetType Type { get; set; }

        public BorderedImageButton(string name, string infoUri, string uri, (int x, int y) dimensions, TilesheetType type, string description)
        {
            InitializeComponent();
            Name = name;
            Uri = uri;
            InfoUri = infoUri;
            Dimensions = $"{dimensions.x} x {dimensions.y} | {dimensions.x * 2} x {dimensions.y * 2}";
            Type = type;
            Description = description;
        }
        private bool IsSelected => (Group is not null && Group.SelectedButton == this);

        public static readonly StyledProperty<string> TemplateNameProperty =
            AvaloniaProperty.Register<BorderedImageButton, string>(nameof(TemplateName));

        public string TemplateName { get; set; }


        public async void OnBorderClicked(object sender, PointerPressedEventArgs e)
        {
            if (!IsSelected && Group == MainWindowViewModel.InputTemplates)
            {
                Tilesheet input = await PromptUserForFileOfSheetTypeAsync(Type);
                if (input is null || input.type == TilesheetType.InvalidInput) return;
                Program.SelectedInput = input;
            }
            Group.ButtonClicked(this);
            OnSelected();//could be where we check to see if user selected anything.
        }
        public async void OnSelected()
        {
            if (IsSelected) border.Background = App.GetColorBrush("BackgroundAccent");
            App.GetDataContext().TryUpdatePendingExport();
            App.GetDataContext().UpdateGridRowOpenedOrClosed(App.GetMainWindow().FindControl<Grid>("MainGrid"), 1, App.GetMainWindow().FindControl<Panel>("OutputTemplates"));
            App.GetDataContext().UpdateExportPanelOpenedOrClosed(App.GetMainWindow().FindControl<Panel>("ExportPanel"));
        }

        private void OnBorderHovered(object sender, PointerEventArgs e)
        {
            if (!IsSelected) border.Background = App.GetColorBrush("BackgroundLight");
            App.GetDataContext().HoveredButton = this;
            DockPanel infoBox = App.GetMainWindow().FindControl<DockPanel>("InfoBox");
            infoBox.Opacity = 1f;
        }
        private void OnBorderUnhovered(object sender, PointerEventArgs e)
        {
            if (!IsSelected) border.Background = App.GetColorBrush("BackgroundDark");
            //GetDataContext().HoveredButton = null;
            DockPanel infoBox = App.GetMainWindow().FindControl<DockPanel>("InfoBox");
            infoBox.Opacity = 0f;
        }
        internal void OnDeselected()
        {
            var backgroundColor = App.GetColorBrush("BackgroundDark");
            if (IsPointerOver) backgroundColor = App.GetColorBrush("BackgroundLight");
            border.Background = backgroundColor;
        }
        private static async Task<Tilesheet?> PromptUserForFileOfSheetTypeAsync(TilesheetType inputType)
        {
            var fileDialog = new OpenFileDialog();
            List<FileDialogFilter> Filters = new List<FileDialogFilter>();
            FileDialogFilter filter = new FileDialogFilter();
            filter.Extensions = new List<string> { "png", "gif", "jpg", "jpeg", "webp" };
            filter.Name = "Image Files";
            Filters.Add(filter);
            fileDialog.Filters = Filters;
            fileDialog.AllowMultiple = false;

            string selectedFile = (await fileDialog.ShowAsync(App.GetMainWindow()))?.First();
            return Program.TryGetTilesheetFromPath(selectedFile, inputType);
        }
    }
    public class ImageButtonExposer //you cant bind to controls lol
    {
        public BorderedImageButton Button { get; }
        public ImageButtonExposer(BorderedImageButton b)
        {
            Button = b;
        }

        public static explicit operator ImageButtonExposer(BorderedImageButton b)
        {
            return new ImageButtonExposer(b);
        }
    }
    public class ButtonGroup
    {
        public ObservableCollection<ImageButtonExposer> ButtonsExposed => new (Buttons.Select(b => (ImageButtonExposer)b));

        public ObservableCollection<BorderedImageButton> Buttons { get; } = new();
        public BorderedImageButton? SelectedButton { get; set; } = null;
         
        public ButtonGroup(params BorderedImageButton[] buttons)
        {
            foreach (var button in buttons)
            {
                Buttons.Add(button);
                button.Group = this;
            }
        }
        internal BorderedImageButton? FindTemplateFromName(string name)
        {
            foreach (var button in Buttons)
            {
                if (button.Name == name) return button;
            }
            return null;
        }

        public void ButtonClicked(BorderedImageButton button, bool doNotDeselect = false)
        {
            if (SelectedButton != button)
            {
                if (SelectedButton is not null) SelectedButton.OnDeselected();
                SelectedButton = button;
            }
            else if (!doNotDeselect)
            {
                SelectedButton.OnDeselected();
                SelectedButton = null;
            }
        }
    }
}

