using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Image = System.Drawing.Image;

namespace TilesheetHelper
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            /*Program.args = args;*/
            if (args.Length > 0 && TryGetTilesheetFromPath(args[0], TilesheetType.SimpleInput, TilesheetType.BaseInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay) is Tilesheet sheet)
            {
                selectedInput = sheet;
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();


        public const int BASIC_SHEET_HEIGHT = 45;
        public const int BASIC_INPUT_SHEET_WIDTH = 54;
        public const int SPLICED_SHEET_WIDTH = 117;
        public const int FULL_SHEET_HEIGHT = 135;
        public const int FULL_SHEET_WIDTH = 144;

        public const int SIMPLE_SHEET_WIDTH = 59;
        public const int SIMPLE_SHEET_HEIGHT = 59;

        //public static string[] args = { };
        public static Tilesheet? SelectedInput
        {
            get => selectedInput;
            set
            {
                selectedInput = value;
                App.GetDataContext().ResetColorButtons();
            }
        }
        protected static Tilesheet? selectedInput = null;

        public static Tilesheet pendingTilesheet;

        //public static List<Tilesheet> selectedFiles = new ();



        public const bool SAVE_PROGRESS_STAGES = false;

        //public static void TryParseArgs()
        //{
        //                /*if (!OperatingSystem.IsWindows())
        //    {
        //        Exception ex = new PlatformNotSupportedException();
        //        PrintConsoleError(ex);
        //        throw ex;
        //    }*/

        //    /*timer.Start();*/

        //    //string filePath;// = GetTileSheetPath(args);

        //    //TilesheetFile primaryTilesheet;

        //    //if (GetTilesheetFiles(args) is TilesheetFile file) primaryTilesheet = file;
        //    //else return;

        //    if (GetTilesheetsFromPaths(new string[] { selectedInput } ) is not List<Tilesheet> fileList) return;//primaryTilesheet = file;

        //    // List<TilesheetFile> fileList = new List<TilesheetFile>(new TilesheetFile());
        //    selectedFiles = fileList;
        //    Tilesheet primaryTilesheet = selectedFiles[0];
        //    //else return;

        //    //Bitmap tilesheet = primaryTilesheet.originalBitmap; //Image.FromFile(primaryTilesheet.path);

        //    //selectedFiles = args;

        //    //TilesheetOperation[] operationOrder = new TilesheetOperation[] { GenerateSplicedTilesheet, GenerateTileMergeUnderlayTilesheet, GenerateTileMergeOverlayTilesheet };
        //    //merge should maybe not have an additional parameter at all? seems janky to have it check for a layer from within the option, but I'm not sure. Ideally yeah, feed it in, but idk.

        //    ///if 1 file, cvheck normally, if 2 files, separate check, if more, check to see if all are same type 

        //    //GetDefaultOperation(primaryTilesheet.type);

        //    //TilesheetOperation[] operations = operationOrder[(int)primaryTilesheet.type];
        //}

        public static Tilesheet GenerateOutput(Tilesheet primaryInput, TilesheetType resultType, Tilesheet? secondaryInput = null)
        {
            Tilesheet input = new Tilesheet(primaryInput.path, primaryInput.type, primaryInput.originalResolution);
            input.internalOutlineColor = primaryInput.internalOutlineColor;

            //Logic flow here: have a process that generates a tilesheet from an inputted result state.
            //if a secondary tilesheet is provided, process that first, then process the primary tilesheet

            if (secondaryInput is null) secondaryInput = TryGetTilesheetFromPath(GetOrCreateTileMergePath("dirtMerge.png"), TilesheetType.TileOverlay);

            if (resultType == TilesheetType.MergedSheet)
            {
                //secondaryInput.GenerateResult(TilesheetType.TileOverlay);
                input.GenerateResult(TilesheetType.TileUnderlay);
                input.OverlayTileMergeOntoTilesheet(secondaryInput.currentBitmap); //make generatedualresult method and pass tilesheettype.MergedSheet
            }
            else
            {
                input.GenerateResult(resultType);
            }

            return input;
        }
        public static Tilesheet? TryGetTilesheetFromPath(string? filepath, params TilesheetType[] allowedTypes)
        {
            try
            {
                TilesheetType type = DetermineTilesheetType(Image.FromFile(filepath), out var res);

                //if (type == TilesheetType.TileOverlay) 
                if (allowedTypes.Any() && !allowedTypes.Contains(type))
                {
                    List<string> typeNames = allowedTypes.Select(x => x.GetName()).ToList();
                    App.ShowError("Tilesheet type mismatch", $"For this action, only { App.PrependAorAn(typeNames.JoinWithOxford("or"), false) } can be used.");
                    //throw new ArgumentOutOfRangeException("Input Tilesheet", "The primary provided tilesheet is a Tilemerge Overlay. Tilemerge Overlays can only be added as a secondary tilesheet."); //make this error message more robust and account for specific allowed types.
                }
                //else
                //{
                //    selectedFiles.Add(new TilesheetFile(filepath, type, res));
                //}
                return new Tilesheet(filepath, type, res); //separate method for sorting the two?
            }
            catch (Exception ex)
            {
                PrintConsoleError(ex);
            }
            return null;
        }
        static List<Tilesheet> GetTilesheetsFromPaths(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    if (args.Length > 2) throw new ArgumentOutOfRangeException("Input Tilesheets", "Only one tilesheet operation may be performed at once. Provide only one tilesheet, or two if you are including an overlay.");

                    var argFiles = new List<Tilesheet>();
                    if (TryGetTilesheetFromPath(args[0], TilesheetType.BaseInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay) is Tilesheet file) argFiles.Add(file); //Enum.GetValues(typeof(TilesheetType));
                    if (args.Length > 1 && TryGetTilesheetFromPath(args[1], TilesheetType.BaseInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay, TilesheetType.TileOverlay) is Tilesheet overlayFile) argFiles.Add(overlayFile); //Enum.GetValues(typeof(TilesheetType));                                                                                                                                                                              //GetTilesheetFile(args[1], TilesheetType.BasicInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay, TilesheetType.TileOverlay);                    return argFiles;
                    if (argFiles.Count > 0) return argFiles;
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                }
            }
            return null;
        }
        /*static string GetTilesheetPathFromFolder()
        {
            bool wrongSizeImageFound = false;
            foreach (var path in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png"))
            {
                Image image = Image.FromFile(path);
                if (CheckTilesheetDimensions(image))
                {
                    return path;
                }
                else wrongSizeImageFound = true;
            }
            if (wrongSizeImageFound) throw new ArgumentOutOfRangeException();
            throw new FileNotFoundException();
        }*/

        /*static bool CheckTilesheetDimensions(Image tilesheet)
        {
            int widthDiff = basicInputSheetWidth - tilesheet.Width;
            int heightDiff = basicSheetHeight - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 1 && heightDiff >= 0 && heightDiff <= 1)
            {
                return true;
            }
            widthDiff = basicInputSheetWidth * 2 - tilesheet.Width;
            heightDiff = basicSheetHeight * 2 - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 2 && heightDiff >= 0 && heightDiff <= 2)
            {
                return true;
            }
            return false;
        }*/
        static TilesheetType DetermineTilesheetType(Image tilesheet, out int res)
        {
            res = 1;
            TilesheetType type = TilesheetType.InvalidInput;

            if (!tilesheet.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png)) App.ShowError("Unsupported file format", "Only .png format files are supported."); //throw new ArgumentOutOfRangeException("Input Tilesheet", "The provided tilesheet is of the wrong file type. Sheet should be a .png file.");

            (int x, int y)[] dimensions = new[] { (SIMPLE_SHEET_WIDTH, SIMPLE_SHEET_HEIGHT), (BASIC_INPUT_SHEET_WIDTH, BASIC_SHEET_HEIGHT), (SPLICED_SHEET_WIDTH, BASIC_SHEET_HEIGHT), (FULL_SHEET_WIDTH, FULL_SHEET_HEIGHT) };
            //this dimensions array should honestly just be changed to a lookup and tilesheettype should be a more robust struct
            foreach (var dim in dimensions)
            {
                int widthDiff = dim.x - tilesheet.Width;
                int heightDiff = dim.y - tilesheet.Height;
                if (widthDiff >= 0 && widthDiff <= 1 && heightDiff >= 0 && heightDiff <= 1)
                {
                    type = (TilesheetType)Array.IndexOf(dimensions, dim);
                }
                widthDiff = dim.x * 2 - tilesheet.Width;
                heightDiff = dim.y * 2 - tilesheet.Height;
                if (widthDiff >= 0 && widthDiff <= 2 && heightDiff >= 0 && heightDiff <= 2)
                {
                    type = (TilesheetType)Array.IndexOf(dimensions, dim);
                    res = 2;
                }
            }

            if (type == TilesheetType.InvalidInput) App.ShowError("Unsupported image dimensions", $"The provided image is {tilesheet.Width}x{tilesheet.Height}. Make sure your input matches a template."); //throw new ArgumentOutOfRangeException("Input Tilesheet", "0801: The provided tilesheet has the wrong dimensions. Sheet should be 54x45 or 108x90. Reference templateTilesheet.png for an example sheet.");

            if (type <= TilesheetType.SplicedSheet) return type;

            var tilesheetBmp = (Bitmap)tilesheet;
            for (int i = 0; i < 8; i += res) //are there any pixels in the upper-leftmost 8x8 area
            {
                for (int j = 0; j < 8; j += res)
                {
                    if (tilesheetBmp.GetPixel(i, j).A == 255) return TilesheetType.TileUnderlay;
                }
            }
            return TilesheetType.TileOverlay;
        }
        static void PrintConsoleError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            //if (ex is ArgumentOutOfRangeException) Console.WriteLine("The provided tilesheet has the wrong dimensions. Sheet should be 54x45 or 108x90. Reference templateTilesheet.png for an example sheet.");
            //else if (ex is FileNotFoundException) Console.WriteLine("No png file was found in the " + Path.GetFileName(Directory.GetCurrentDirectory()) + " folder.");
            //else if (ex is PlatformNotSupportedException) Console.WriteLine("Sorry, but the image editing tools TilesheetHelper uses are only supported by Windows, so TilesheetHelper doesn't work on Mac or Linux.");
            if (ex.Message is not null) Console.WriteLine(ex.Message);
            if (ex.Message is not null) System.Diagnostics.Debug.WriteLine(ex.Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any key to close this window.");
            //Console.ReadKey();
        }
        public static Bitmap GetSystemBitmapFromAsset(string rawUri)
        {
            if (string.IsNullOrEmpty(rawUri))
                return null;

            Uri uri;

            string assemblyName = Assembly.GetEntryAssembly().GetName().Name;
            uri = new Uri($"avares://{assemblyName}/Assets/Images/{rawUri}");

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            System.IO.Stream asset;

            try
            {
                asset = assets.Open(uri);
            }
            catch
            {
                asset = assets.Open(new Uri($"avares://{Assembly.GetEntryAssembly().GetName().Name}/Assets/Images/trolled.png"));
            }

            return new System.Drawing.Bitmap(asset);
        }
        public static string GetOrCreateTileMergePath(string filename)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TileMerges");
            string filepath = Path.Combine(folder, filename);

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            if (!File.Exists(filepath)) GetSystemBitmapFromAsset("dirtMerge.png").Save(filepath);

            return filepath;
        }
        public static Bitmap GetTilemergeShape()
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirtMerge.png")))
            {
                var customMerge = (Bitmap)Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirtMerge.png"));
                if (customMerge.Width == FULL_SHEET_WIDTH * 2 && customMerge.Height == FULL_SHEET_HEIGHT * 2) customMerge = Tilesheet.GetDownscaledBitmap(customMerge);

                if (customMerge.Width == FULL_SHEET_WIDTH && customMerge.Height == FULL_SHEET_HEIGHT) return customMerge;
                else Console.WriteLine($"The provided 'DirtMerge.png' is of dimensions {customMerge.Width} x {customMerge.Height}. Please use a {FULL_SHEET_WIDTH} x {FULL_SHEET_HEIGHT} image at 1x or 2x resolution. Continuing using default dirt merge tilesheet");
            }
            else Console.WriteLine($"No file named 'DirtMerge.png' found in {AppDomain.CurrentDomain.BaseDirectory}, using default dirt merge tilesheet");

            return GetSystemBitmapFromAsset("dirtMerge.png");
        }
    }
}
