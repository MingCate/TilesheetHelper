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
        //public const int tileSize = 8;

        //public static int res = 1; //resolution

        public static Image tilesheet; //would like to replace but drawrect stuff... sighs... params[] for drawrect?. YES get rid of and also check getbaseresolution instances to remove. and make the overlay load in at the start.
        public static Graphics canvas;
        public static Color internalOutlineColor;
        public static Stopwatch timer = new Stopwatch();
        //public static string[] selectedFiles = new string[0]; //array of a custom type that contains res data, filepath, tilesheet type?
        public static List<TilesheetFile> selectedFiles = new List<TilesheetFile>();
        public class TilesheetFile
        {
            public string path;
            public TilesheetType type;
            public int res;
            public Bitmap originalBitmap;

            public TilesheetFile(string path, TilesheetType type, int res)
            {
                this.type = type;
                this.path = path;
                this.res = res;
                this.originalBitmap = (Bitmap)Image.FromFile(path);
                if (res == 2) originalBitmap = GetBaseResolutionBitmap(originalBitmap);
            }
        }

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
            TilesheetFile primaryTilesheet;
            if (GetTilesheetFile(args) is TilesheetFile file) primaryTilesheet = file;
            else return;

            tilesheet = primaryTilesheet.originalBitmap; //Image.FromFile(primaryTilesheet.path);

            if (Path.GetFileName(primaryTilesheet.path) == "templateTilesheet.png") internalOutlineColor = Color.Black;
            else internalOutlineColor = GetColors((Bitmap)tilesheet).ElementAtOrDefault(1); //the second least darkest color by luminance

            //selectedFiles = args;

            GenerateSplicedTilesheet(primaryTilesheet);

            GenerateTileMergeUnderlayTilesheet(primaryTilesheet);

            GenerateTileMergeOverlayTilesheet(primaryTilesheet);

        }

        static TilesheetFile GetTilesheetFile(string[] args)
        {
            if (args.Any())
            { //check if .png
                try
                {
                    //if (!args[0].EndsWith(".png")) throw;
                    TilesheetType type = GetTilesheetType(Image.FromFile(args[0]), out var res);
                    var tilesheetFile = new TilesheetFile(args[0], type, res);

                    if (type == TilesheetType.InvalidInput) throw new ArgumentOutOfRangeException();
                    else selectedFiles.Add(tilesheetFile);

                    return tilesheetFile;
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    //throw;
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
        public enum TilesheetType
        {
            InvalidInput = -1,
            BasicInput,
            //StreamlinedInput,
            SplicedSheet,
            TileUnderlay,
            TileOverlay,
        }
        static TilesheetType GetTilesheetType(Image tilesheet, out int res)
        {
            res = 1;
            TilesheetType type = TilesheetType.InvalidInput;

            (int x, int y)[] dimensions = new[] { (basicInputSheetWidth, basicSheetHeight), (splicedSheetWidth, basicSheetHeight), (fullSheetWidth, fullSheetHeight) };

            foreach (var dim in dimensions)
            {
                int widthDiff = dim.x - tilesheet.Width;
                int heightDiff = dim.y - tilesheet.Height;
                if (widthDiff >= 0 && widthDiff <= 1 && heightDiff >= 0 && heightDiff <= 1)
                {
                    type = (TilesheetType)Array.IndexOf(dimensions, dim);
                }
                widthDiff = basicInputSheetWidth * 2 - tilesheet.Width;
                heightDiff = basicSheetHeight * 2 - tilesheet.Height;
                if (widthDiff >= 0 && widthDiff <= 2 && heightDiff >= 0 && heightDiff <= 2)
                {
                    type = (TilesheetType)Array.IndexOf(dimensions, dim);
                    res = 2;
                }
            }

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
            if (ex is ArgumentOutOfRangeException) Console.WriteLine("The provided tilesheet has the wrong dimensions. Sheet should be 54x45 or 108x90. Reference templateTilesheet.png for an example sheet.");
            else if (ex is FileNotFoundException) Console.WriteLine("No png file was found in the " + Path.GetFileName(Directory.GetCurrentDirectory()) + " folder.");
            else if (ex is PlatformNotSupportedException) Console.WriteLine("Sorry, but the image editing tools TilesheetHelper uses are only supported by Windows, so TilesheetHelper doesn't work on Mac or Linux.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any key to close this window.");
            Console.ReadKey();
        }

        static void GenerateSplicedTilesheet(TilesheetFile file)
        {
            if (!OperatingSystem.IsWindows()) return;
            var bitmap = new Bitmap(splicedSheetWidth, basicSheetHeight); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                DrawRect(new Point(0, 0), new Point(0, 0), basicInputSheetWidth, basicSheetHeight); //copying original input

                DrawRect(new Point(0, 0), new Point(45, 0), 4, 26); //left-right border blend
                DrawRect(new Point(40, 0), new Point(49, 0), 4, 26);

                DrawRect(new Point(9, 0), new Point(54, 36), 26, 4); //top-bottom border blend
                DrawRect(new Point(9, 22), new Point(54, 40), 26, 4);

                for (int i = 0; i < 3; i++) //the 2x2 tile chunk variants
                {
                    DrawRect(new Point(0 + i * 18, 27), new Point(54 + i * 9, 0), 4, 8); //vertical strips
                    DrawRect(new Point(13 + i * 18, 27), new Point(58 + i * 9, 0), 4, 8);
                    DrawRect(new Point(0 + i * 18, 36), new Point(54 + i * 9, 27), 4, 8);
                    DrawRect(new Point(13 + i * 18, 36), new Point(58 + i * 9, 27), 4, 8);

                    DrawRect(new Point(0 + i * 18, 27), new Point(81, 0 + i * 9), 8, 4); //horizontal strips
                    DrawRect(new Point(9 + i * 18, 27), new Point(108, 0 + i * 9), 8, 4);
                    DrawRect(new Point(0 + i * 18, 40), new Point(81, 4 + i * 9), 8, 4);
                    DrawRect(new Point(9 + i * 18, 40), new Point(108, 4 + i * 9), 8, 4);

                    DrawRect(new Point(0 + i * 18, 27), new Point(81 + i * 9, 27), 4, 4); //1x1 chunks
                    DrawRect(new Point(13 + i * 18, 27), new Point(85 + i * 9, 27), 4, 4);
                    DrawRect(new Point(0 + i * 18, 40), new Point(81 + i * 9, 31), 4, 4);
                    DrawRect(new Point(13 + i * 18, 40), new Point(85 + i * 9, 31), 4, 4);
                }

                DrawRect(new Point(9, 9), new Point(54, 9), 26, 8); //horizontal strip intersection
                DrawRect(new Point(9, 9), new Point(54, 18), 26, 8);

                for (int i = 0; i < 3; i++) //vertical strip intersection
                {
                    DrawRect(new Point(9 + i * 9, 9), new Point(90, 0 + i * 9), 8, 8);
                    DrawRect(new Point(9 + i * 9, 9), new Point(99, 0 + i * 9), 8, 8);
                }

                DrawPixelPair(new Point(54, 9), Axis.X, 3); //internal outline pixels for center intersection block corners
                DrawPixelPair(new Point(54, 25), Axis.X, 3);

                DrawPixelPair(new Point(90, 0), Axis.Y, 3);
                DrawPixelPair(new Point(106, 0), Axis.Y, 3);

                canvas.Save();
            }

            tilesheet = bitmap; // TODO :

            SaveBitmap(bitmap, file, "Spliced");
        }


        static void GenerateTileMergeUnderlayTilesheet(TilesheetFile file)
        {
            if (!OperatingSystem.IsWindows()) return;
            var bitmap = new Bitmap(fullSheetWidth, fullSheetHeight); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                DrawRect(new Point(0, 0), new Point(0, 0), splicedSheetWidth, basicSheetHeight); //copying original input

                DrawRect(new Point(9, 0), new Point(117, 0), 26, 8); //top-bottom borders
                DrawRect(new Point(9, 18), new Point(117, 9), 26, 8);
                for (int i = 0; i < 2; i++)
                {
                    DrawRect(new Point(9, 0), new Point(0 + 27 * i, 99), 26, 8);
                    DrawRect(new Point(9, 18), new Point(0 + 27 * i, 108), 26, 8);
                }

                DrawRect(new Point(54, 36), new Point(81, 99), 26, 8);  //top-bottom border blends
                DrawRect(new Point(54, 36), new Point(0, 126), 26, 8);
                DrawRect(new Point(54, 36), new Point(27, 126), 26, 8);

                for (int i = 0; i < 3; i++) //left-right borders
                {
                    DrawRect(new Point(0, 0 + 9 * i), new Point(117 + 9 * i, 18), 8, 8);
                    DrawRect(new Point(36, 0 + 9 * i), new Point(117 + 9 * i, 27), 8, 8);
                }
                for (int i = 0; i < 2; i++)
                {
                    DrawRect(new Point(0, 0), new Point(36, 45 + 27 * i), 8, 26);
                    DrawRect(new Point(36, 0), new Point(45, 45 + 27 * i), 8, 26);
                    DrawRect(new Point(45, 0), new Point(63, 45 + 27 * i), 8, 26);  //left-right border blends
                }
                DrawRect(new Point(45, 0), new Point(54, 108), 8, 26);

                for (int i = 0; i < 3; i++) //center tiles
                {
                    for (int k = 0; k < 4; k++)
                    {
                        DrawRect(new Point(9 + 9 * i, 9), new Point(0 + 9 * k, 45 + 18 * i), 8, 8);
                        DrawRect(new Point(9 + 9 * i, 9), new Point(0 + 9 * k, 54 + 18 * i), 8, 8);
                    }
                }

                DrawRect(new Point(9, 9), new Point(72, 45), 26, 8);
                DrawRect(new Point(9, 9), new Point(72, 54), 26, 8);

                for (int i = 0; i < 3; i++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        DrawRect(new Point(9 + 9 * i, 9), new Point(72 + k * 9, 63 + i * 9), 8, 8);
                    }
                }

                DrawRect(new Point(9, 9), new Point(72, 90), 26, 8);
                DrawRect(new Point(9, 9), new Point(54, 99), 26, 8);

                for (int i = 0; i < 3; i++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        DrawRect(new Point(9 + 9 * i, 9), new Point(99 + k * 9, 45 + i * 9), 8, 8);
                        DrawRect(new Point(9 + 9 * i, 9), new Point(99 + k * 9, 72 + i * 9), 8, 8);
                    }
                }

                for (int i = 0; i < 3; i++) //horizontal strips
                {
                    DrawRect(new Point(54 + 9 * i, 0), new Point(54, 45 + i * 9), 8, 8);
                    DrawRect(new Point(54 + 9 * i, 27), new Point(54, 72 + i * 9), 8, 8);
                    DrawRect(new Point(81, 0 + 9 * i), new Point(27 + i * 9, 117), 8, 8);//vertical strips
                    DrawRect(new Point(108, 0 + 9 * i), new Point(0 + i * 9, 117), 8, 8);
                }

                canvas.Save();
            }

            tilesheet = bitmap;

            SaveBitmap(bitmap, file, "TileMerge");
        }

        static void GenerateTileMergeOverlayTilesheet(TilesheetFile file)
        {
            if (!OperatingSystem.IsWindows()) return;

            TilesheetFile overlayFile = file;

            var bitmap = new Bitmap(144, 135); //144 135

            var mergeShapeBitmap = Properties.Resources.DirtMerge; //Image.FromFile()
            var mergeShapeOutline = GetColors(mergeShapeBitmap).ElementAtOrDefault(0);
            var mergeOverlayTexture = (Bitmap)tilesheet; // Properties.Resources.DirtMerge; 

            if (selectedFiles.Count > 1)
            {
                mergeOverlayTexture = (Bitmap)Image.FromFile(selectedFiles[1].path);
                overlayFile = selectedFiles[1];
            }//!!!

            if (mergeOverlayTexture.Width > 144) mergeOverlayTexture = GetBaseResolutionBitmap(mergeOverlayTexture); //super duper hacky

            Color overlayOutlineColor = GetColors(mergeOverlayTexture).ElementAtOrDefault(1); //only necessary because its not necessarily the same type as the main tile


            canvas = Graphics.FromImage(bitmap);
            {
                //DrawRect(new Point(0, 0), new Point(0, 0), splicedSheetInputWidth, inputHeight); //copying original input

                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        if (mergeShapeBitmap.GetPixel(i, j).A != 0)
                        {
                            if (mergeShapeBitmap.GetPixel(i, j) == mergeShapeOutline) bitmap.SetPixel(i, j, overlayOutlineColor);
                            else bitmap.SetPixel(i, j, mergeOverlayTexture.GetPixel(i, j));
                        }
                    }
                }
                canvas.Save();
            }

            SaveBitmap(bitmap, overlayFile, "TileMergeOverlay");

            GenerateCombinedTileMergeTilesheet(file, bitmap);
        }

        static void GenerateCombinedTileMergeTilesheet(TilesheetFile file, Bitmap overlayTilesheet)
        {
            if (!OperatingSystem.IsWindows()) return;
            var underlayTilesheet = (Bitmap)tilesheet;

            var bitmap = new Bitmap(144, 135); //144 135

            //overlayTilesheet = Properties.Resources.DirtMerge;

            //var mergeShapeBitmap = Properties.Resources.DirtMerge; //Image.FromFile()
            var overlayOutlineColor = GetColors(overlayTilesheet).ElementAtOrDefault(0);
            //var mergeTextureBitmap = (Bitmap)tilesheet;

            canvas = Graphics.FromImage(bitmap);
            {
                //canvas.DrawImage(underlayTilesheet,
                //    new Rectangle(0, 0, 144, 135),
                //    new Rectangle(0, 0, 144, 135), GraphicsUnit.Pixel);
                canvas.DrawImage(overlayTilesheet,
                    new Rectangle(0, 0, 144, 135),
                    new Rectangle(0, 0, 144, 135), GraphicsUnit.Pixel);

                Color underlayOutlineColor = GetColors(underlayTilesheet).ElementAtOrDefault(1);
                //underlayOutlineColor = Color.Red;
                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        if (overlayTilesheet.GetPixel(i, j).A != 0) //refactor into method probably
                        {
                            if (overlayTilesheet.GetPixel(i, j) == overlayOutlineColor)
                            {
                                Point[] cardinalAdjacentPixels = new[] { new Point(-1, 0), new Point(0, -1), new Point(1, 0), new Point(0, 1) };
                                foreach (Point point in cardinalAdjacentPixels)
                                {
                                    if (!IsPixelInTileGridGap(point.X + i, point.Y + j) && overlayTilesheet.GetPixel(point.X + i, point.Y + j).A == 0
                                        && Luminance(underlayTilesheet.GetPixel(point.X + i, point.Y + j)) > Luminance(underlayOutlineColor))
                                        bitmap.SetPixel(point.X + i, point.Y + j, underlayOutlineColor); //this can replace darker pixels. shouldnt be possible. could it always go one darker maybe?
                                }
                            }
                        }
                    }
                }

                canvas.Save();
            }
            SaveBitmap(bitmap, file, "TileMergeSheet");
        }
        static void DrawRect(Point srcPoint, Point destPoint, int width, int height)
        {
            canvas.DrawImage(tilesheet,
                new Rectangle(destPoint.X, destPoint.Y, width, height),
                new Rectangle(srcPoint.X, srcPoint.Y, width, height), GraphicsUnit.Pixel);
        }
        enum Axis
        {
            X,
            Y,
        }
        static void DrawPixelPair(Point destPoint, Axis axis, int pairs)
        {
            var pixel = new Bitmap(2, 2);
            Graphics g = Graphics.FromImage(pixel);
            g.Clear(internalOutlineColor); //sets the color of the pixel

            Point nextPoint = destPoint;
            if (axis == Axis.X) nextPoint.X += 7;
            else nextPoint.Y += 7;

            for (int i = 0; i < pairs; i++)
            {
                canvas.DrawImage(pixel, new Rectangle(destPoint.X + (axis == Axis.X ? i * 9 : 0), destPoint.Y + (axis == Axis.Y ? i * 9 : 0), 1, 1), new Rectangle(0, 0, 1, 1), GraphicsUnit.Pixel);
                canvas.DrawImage(pixel, new Rectangle(nextPoint.X + (axis == Axis.X ? i * 9 : 0), nextPoint.Y + (axis == Axis.Y ? i * 9 : 0), 1, 1), new Rectangle(0, 0, 1, 1), GraphicsUnit.Pixel);
            }
        }
        static Color[] GetColors(Bitmap bmp, TilesheetFile file = null)
        {
            int res = 1;
            if (file is not null) res = 2;

            List<Color> colors = new List<Color>();
            for (int x = 0; x < bmp.Width; x += res)
            {
                for (int y = 0; y < bmp.Height; y += res)
                {
                    Color pixColor = bmp.GetPixel(x, y);
                    if (!colors.Contains(pixColor) && pixColor.A > 0) colors.Add(pixColor);
                }
            }

            return colors.OrderBy(c => Luminance(c)).ToArray();
        }

        static double Luminance(Color c)
        {
            return c.R * 0.3 + c.G * 0.6 + c.B * 0.1;
        }
        static Bitmap GetUpscaledBitmap(Bitmap bmp)
        {
            if (!OperatingSystem.IsWindows()) return null;
            //if (!OperatingSystem.IsWindows()) return null;

            var upscaledBmp = new Bitmap(bmp.Width * 2, bmp.Height * 2);

            canvas = Graphics.FromImage(upscaledBmp);
            {
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        if (bmp.GetPixel(i, j).A != 0)
                        {
                            Color color = bmp.GetPixel(i, j);
                            upscaledBmp.SetPixel(i * 2, j * 2, color);
                            upscaledBmp.SetPixel(i * 2 + 1, j * 2, color);
                            upscaledBmp.SetPixel(i * 2, j * 2 + 1, color);
                            upscaledBmp.SetPixel(i * 2 + 1, j * 2 + 1, color);
                        }
                    }
                }
                canvas.Save();
            }
            return upscaledBmp;
        }
        static Bitmap GetBaseResolutionBitmap(Bitmap bmp)
        {
            //if (res == 1) return bmp; this could be an error spot

            var downscaledBmp = new Bitmap(bmp.Width / 2, bmp.Height / 2);

            canvas = Graphics.FromImage(downscaledBmp);
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
        static void SaveBitmap(Bitmap bitmap, TilesheetFile originFile, string fileSuffix) //Bitmap bmp, string filePath
        {
            if (originFile.res == 2) bitmap = GetUpscaledBitmap(bitmap);
            bitmap.Save(Path.GetFileName(originFile.path).Replace(".png", fileSuffix + ".png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        static bool IsPixelInTileGridGap(int x, int y)
        {
            return (x + 1) % 9 == 0 || (y + 1) % 9 == 0;
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