using System;
using System.IO;
using System.Drawing;
using System.Runtime;
using System.Diagnostics;

namespace TilesheetHelper
{
    internal class Program
    {
        public const int basicSheetHeight = 45;
        public const int basicInputSheetWidth = 54;
        public const int splicedSheetWidth = 117;
        public const int fullSheetHeight = 135;
        public const int fullSheetWidth = 144;

        public static Stopwatch timer = new Stopwatch();
        public static List<TilesheetFile> selectedFiles = new List<TilesheetFile>(); 
        public static string[] fileSuffixes = new string[] { "BasicInput", "Spliced", "TileMergeBase", "TileMergeOverlay", "TileMergeLayered" };

        public static readonly bool saveProgressStages = true;
        static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Exception ex = new PlatformNotSupportedException();
                PrintConsoleError(ex);
                throw ex;
            }

            timer.Start();

            //string filePath;// = GetTileSheetPath(args);

            //TilesheetFile primaryTilesheet;

            //if (GetTilesheetFiles(args) is TilesheetFile file) primaryTilesheet = file;
            //else return;

            if (GetTilesheetFilesFromArgs(args) is not List<TilesheetFile> fileList) return;//primaryTilesheet = file;

            selectedFiles = fileList;
            TilesheetFile primaryTilesheet = selectedFiles[0];
            //else return;

            //Bitmap tilesheet = primaryTilesheet.originalBitmap; //Image.FromFile(primaryTilesheet.path);

            //selectedFiles = args;

            //TilesheetOperation[] operationOrder = new TilesheetOperation[] { GenerateSplicedTilesheet, GenerateTileMergeUnderlayTilesheet, GenerateTileMergeOverlayTilesheet };
            //merge should maybe not have an additional parameter at all? seems janky to have it check for a layer from within the option, but I'm not sure. Ideally yeah, feed it in, but idk.

            ///if 1 file, cvheck normally, if 2 files, separate check, if more, check to see if all are same type 

            //GetDefaultOperation(primaryTilesheet.type);

            //TilesheetOperation[] operations = operationOrder[(int)primaryTilesheet.type];


            //Logic flow here: have a process that generates a tilesheet from an inputted result state.
            //if a secondary tilesheet is provided, process that first, then process the primary tilesheet


            TilesheetType desiredResult = primaryTilesheet.type + 1;

            if (selectedFiles.Count > 1)
            {
                selectedFiles[1].GenerateTilesheet(selectedFiles[1], selectedFiles[1].GetOperationOrderForResultType(TilesheetType.TileOverlay));
                primaryTilesheet.GenerateTilesheet(primaryTilesheet, primaryTilesheet.GetOperationOrderForResultType(TilesheetType.TileUnderlay));
                primaryTilesheet.GenerateCombinedTileMergeTilesheet(primaryTilesheet, selectedFiles[1].currentBitmap);
                primaryTilesheet.SaveBitmap(fileSuffixes[(int)TilesheetType.MergedSheet]);
            }
            else
            {
                primaryTilesheet.GenerateTilesheet(primaryTilesheet, primaryTilesheet.GetOperationOrderForResultType(desiredResult)); //TilesheetFile.operationOrder[(int)primaryTilesheet.type]
                primaryTilesheet.SaveBitmap(fileSuffixes[(int)desiredResult]);
            }

            /*if (selectedFiles.Count > 1)
            {
                primaryTilesheet.GenerateDualTilesheet(primaryTilesheet, selectedFiles[1]);
            }
            else
            {
                primaryTilesheet.GenerateTilesheet(primaryTilesheet, primaryTilesheet.GetOperationOrderForResultType(desiredResult)); //TilesheetFile.operationOrder[(int)primaryTilesheet.type]
            }*/

            /*GenerateSplicedTilesheet(primaryTilesheet);

            GenerateTileMergeUnderlayTilesheet(primaryTilesheet);

            GenerateTileMergeOverlayTilesheet(primaryTilesheet);*/
            Thread.Sleep(1000);
        }

        /*static TilesheetFile GetTilesheetFiles(string[] args)
        {
            if (args.Any())
            { //check if .png
                try
                {
                    if (args.Length > 2) throw new ArgumentOutOfRangeException("TooManyArguments", "Only one primary tilesheet and one secondary overlay tilesheet may be input at once.");
                    foreach (string file in args)
                    {
                        //if (!args[0].EndsWith(".png")) throw;
                        TilesheetType type = GetTilesheetType(Image.FromFile(args[0]), out var res);
                        var tilesheetFile = new TilesheetFile(args[0], type, res);

                        if (type == TilesheetType.InvalidInput) throw new ArgumentOutOfRangeException();
                        else
                        {
                            selectedFiles.Add(tilesheetFile);
                        }
                    }
                    return tilesheetFile; //separate method for sorting the two?
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    //throw;
                }
            }
            return null;
        }*/
        static TilesheetFile GetTilesheetFile(string filepath, params TilesheetType[] allowedTypes)
        {
            try
            {
                TilesheetType type = DetermineTilesheetType(Image.FromFile(filepath), out var res);

                //if (type == TilesheetType.TileOverlay) 
                if (!allowedTypes.Contains(type))throw new ArgumentOutOfRangeException("Input Tilesheet", "The primary provided tilesheet is a Tilemerge Overlay. Tilemerge Overlays can only be added as a secondary tilesheet.");
                //else
                //{
                //    selectedFiles.Add(new TilesheetFile(filepath, type, res));
                //}
                return new TilesheetFile(filepath, type, res); //separate method for sorting the two?
            }
            catch (Exception ex)
            {
                PrintConsoleError(ex);
            }
            return null;
        }
        static List<TilesheetFile> GetTilesheetFilesFromArgs(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    if (args.Length > 2) throw new ArgumentOutOfRangeException("Input Tilesheets", "Only one tilesheet operation may be performed at once. Provide only one tilesheet, or two if you are including an overlay.");

                    var argFiles = new List<TilesheetFile>();
                    if (GetTilesheetFile(args[0], TilesheetType.BasicInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay) is TilesheetFile file) argFiles.Add(file); //Enum.GetValues(typeof(TilesheetType));
                    if (args.Length > 1 && GetTilesheetFile(args[1], TilesheetType.BasicInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay, TilesheetType.TileOverlay) is TilesheetFile overlayFile) argFiles.Add(overlayFile); //Enum.GetValues(typeof(TilesheetType));                                                                                                                                                                              //GetTilesheetFile(args[1], TilesheetType.BasicInput, TilesheetType.SplicedSheet, TilesheetType.TileUnderlay, TilesheetType.TileOverlay);                    return argFiles;
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

            if (!tilesheet.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png)) throw new ArgumentOutOfRangeException("Input Tilesheet", "The provided tilesheet is of the wrong file type. Sheet should be a .png file.");

            (int x, int y)[] dimensions = new[] { (basicInputSheetWidth, basicSheetHeight), (splicedSheetWidth, basicSheetHeight), (fullSheetWidth, fullSheetHeight) };

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

            if (type == TilesheetType.InvalidInput) throw new ArgumentOutOfRangeException("Input Tilesheet", "0801: The provided tilesheet has the wrong dimensions. Sheet should be 54x45 or 108x90. Reference templateTilesheet.png for an example sheet.");

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
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any key to close this window.");
            Console.ReadKey();
        }
    }
    /*public static class BitmapExtensions
    {
        static Bitmap GetUpscaledBitmap(this Bitmap bmp, int res)
        {
            if (!OperatingSystem.IsWindows()) return null;

            var upscaledBmp = new Bitmap(bmp.Width * 2, bmp.Height * 2);

            var canvas = Graphics.FromImage(upscaledBmp);
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        if (bmp.GetPixel(i, j).A != 0)
                        {
                            Color color = bmp.GetPixel(i, j);
                            upscaledBmp.SetPixel(i * res, j * res, color);
                            upscaledBmp.SetPixel(i * res + 1, j * res, color);
                            upscaledBmp.SetPixel(i * res, j * res + 1, color);
                            upscaledBmp.SetPixel(i * res + 1, j * res + 1, color);
                        }
                    }
                }
                canvas.Save();
            }
            return upscaledBmp;
        }
        public static Bitmap GetBaseResolutionBitmap(this Bitmap bmp)
        {

            var downscaledBmp = new Bitmap(bmp.Width / 2, bmp.Height / 2);

            var canvas = Graphics.FromImage(downscaledBmp);
            {
                for (int i = 0; i < downscaledBmp.Width; i++)
                {
                    for (int j = 0; j < downscaledBmp.Height; j++)
                    {
                        if (bmp.GetPixel(i * 2, j * 2).A != 0)
                        {
                            downscaledBmp.SetPixel(i, j, bmp.GetPixel(i * 2, j * 2));
                        }
                    }
                }
                canvas.Save();
            }
            return downscaledBmp;
        }
    }*/
}