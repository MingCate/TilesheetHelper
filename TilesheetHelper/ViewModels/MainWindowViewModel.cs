using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using Bitmap = System.Drawing.Bitmap;
using Avalonia.Layout;

namespace TilesheetHelper.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static ButtonGroup inputTemplates = new(
            new BorderedImageButton("Simple Sheet", "templateSimple.png", "stoneSimple.png", (58, 58), TilesheetType.SimpleInput, "wip"),
            new BorderedImageButton("Base Sheet", "templateBase.png", "stoneBase.png", (53, 44), TilesheetType.BaseInput, "wip"),
            new BorderedImageButton("Standard Sheet", "templateSpliced.png", "stoneSpliced.png", (116, 44), TilesheetType.SplicedSheet, "The minimum required variations for a tilesheet in Terraria. Use this if you already have a finished tilesheet and just want to add or make a dirt merge.")
        );

        public static ButtonGroup InputTemplates => inputTemplates;

        private static ButtonGroup outputTemplates = new(
            new BorderedImageButton("Spliced Sheet", "templateSpliced.png", "stoneSpliced.png", (116, 44), TilesheetType.SplicedSheet, "wip"),
            new BorderedImageButton("Tile Merge Base Sheet", "templateTileMergeBase.png", "stoneTileMergeBase.png", (143, 134), TilesheetType.TileUnderlay, "wip"),
            new BorderedImageButton("Tile Merge Combined Sheet", "templateTileMergeCombined.png", "stoneTileMergeCombined.png", (143, 134), TilesheetType.MergedSheet, "wip")
        );

        public static ButtonGroup OutputTemplates => outputTemplates;
        public static ButtonGroup[] allTemplates = { inputTemplates, outputTemplates }; //TODO : KEEP UPDATED


        private BorderedImageButton hoveredButton;

        public BorderedImageButton HoveredButton
        {
            get => hoveredButton;
            set => this.RaiseAndSetIfChanged(ref hoveredButton, value);
        }

        public void UpdateGridRowOpenedOrClosed(Grid grid, int rowIndex, Panel rowPanel)
        {
            if (inputTemplates.SelectedButton is null) CloseGridRow(grid, rowIndex, rowPanel);
            else OpenGridRow(grid, rowIndex, rowPanel);
        }
        public void OpenGridRow(Grid grid, int rowIndex, Panel rowPanel)
        {
            rowPanel.Opacity = 1f;
            rowPanel.Classes.Add("opening");
            rowPanel.Classes.Remove("closing");
            ItemsControl control = rowPanel.FindControl<ItemsControl>("OutputTemplatesControl");
            control.Classes.Add("opening");
            control.Classes.Remove("closing");
        }
        public void CloseGridRow(Grid grid, int rowIndex, Panel rowPanel)
        {
            rowPanel.Opacity = 0f;
            rowPanel.Classes.Add("closing");
            rowPanel.Classes.Remove("opening");
            ItemsControl control = rowPanel.FindControl<ItemsControl>("OutputTemplatesControl");
            control.Classes.Add("closing");
            control.Classes.Remove("opening");
        }
        internal void UpdateExportPanelOpenedOrClosed(Panel panel)
        {
            if (inputTemplates.SelectedButton is not null && outputTemplates.SelectedButton is not null)
            {
                App.InvokeAfterCondition(() => !panel.IsAnimating(Panel.WidthProperty),
                    () => OpenExportPanel(panel));
            }
            else if (panel.Width > 0)
            {
                App.InvokeAfterCondition(() => !panel.IsAnimating(Panel.WidthProperty),
                    () => CloseExportPanel(panel));
            }
        }
        private static void OpenExportPanel(Panel panel)
        {
            var openAnim = App.CreateFade(Panel.WidthProperty, panel.Width, 340.0, TimeSpan.FromMilliseconds(350), new CubicEaseOut());
            openAnim.RunAsync(panel, panel.Clock);

            var fadeInAnim = App.CreateFade(Layoutable.OpacityProperty, panel.Children.First().Opacity, 1.0, TimeSpan.FromMilliseconds(400), new CubicEaseOut());
            fadeInAnim.Delay = TimeSpan.FromMilliseconds(80);
            foreach (Control child in panel.Children)
            {
                fadeInAnim.RunAsync(child, child.Clock);
            }
        }
        private void CloseExportPanel(Panel panel)
        {
            var closeAnim = App.CreateFade(Panel.WidthProperty, 340.0, 0.0, TimeSpan.FromMilliseconds(350), new SineEaseOut());
            closeAnim.RunAsync(panel, panel.Clock);

            var fadeOutAnim = App.CreateFade(Layoutable.OpacityProperty, panel.Children.First().Opacity, 0.0, TimeSpan.FromMilliseconds(150), new CubicEaseOut());
            foreach (Control child in panel.Children)
            {
                fadeOutAnim.RunAsync(child, child.Clock);
            }
        }

        public IBitmap TilesheetPreview
        {
            get => Program.pendingTilesheet.currentBitmap.ConvertToAvaloniaBitmap();
            //private set => this.RaiseAndSetIfChanged(ref Program.selectedInput, value);
        }
        public void TryUpdatePendingExport()
        {
            if (Program.SelectedInput is null) return;
            if (InputTemplates.SelectedButton is null || OutputTemplates.SelectedButton is null) return;
            TilesheetType resultType = OutputTemplates.SelectedButton.Type;
            Program.pendingTilesheet = Program.GenerateOutput(Program.SelectedInput, resultType);
            App.GetMainWindow().FindControl<Avalonia.Controls.Image>("PendingExport").Source = Tilesheet.GetUpscaledBitmap(Program.pendingTilesheet.currentBitmap).ConvertToAvaloniaBitmap();
        }

        public void ResetColorButtons()
        {
            App.GetMainWindow().FindControl<ItemsControl>("ImageColors").Items = App.GetDataContext().GetColorsAsBrushes(Program.SelectedInput.currentBitmap);
        }

        public List<ColorSelectButton> internalOutlineColorButtons = new();

        public Avalonia.Media.Color InternalOutlineColor
        {
            get
            {
                System.Drawing.Color color = Program.SelectedInput.internalOutlineColor;
                return Avalonia.Media.Color.FromRgb(color.R, color.G, color.B);
            }
            set
            {
                Program.SelectedInput.internalOutlineColor = System.Drawing.Color.FromArgb(value.R, value.G, value.B);
                TryUpdatePendingExport();
            }
        }
        public SolidColorBrush[] GetColorsAsBrushes(Bitmap bitmap)
        {
            return Tilesheet.GetColors(bitmap).Select(x => new SolidColorBrush(Avalonia.Media.Color.FromRgb(x.R, x.G, x.B))).ToArray();
        }
    }
}
