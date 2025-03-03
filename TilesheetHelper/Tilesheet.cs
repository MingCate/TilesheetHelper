using System;
using System.IO;
using System.Drawing;
using System.Runtime;
using System.Diagnostics;
using System.Linq;
using static TilesheetHelper.Program;
using static TilesheetHelper.BitmapExtensions;
using Avalonia;
using Avalonia.Platform;
using System.Reflection;
using Point = System.Drawing.Point;
using System.Collections.Generic;

namespace TilesheetHelper
{
    public enum TilesheetType
    {
        InvalidInput = -1,
        SimpleInput,
        BaseInput,
        //StreamlinedInput,
        SplicedSheet,
        TileUnderlay, //Combine with MergedSheet? Think of tileunderlay as simply a mergedsheet with an invisible overlay. Its production could simply be the result of a mergedsheet with no tileoverlay selected, reducing outputs.
        TileOverlay,
        MergedSheet, //complete, will never be an input (?)
    }
    public static class TilesheetTypeExtensions
    {
        public static string GetSuffix(this TilesheetType type)
        {
            return Tilesheet.fileSuffixes[(int)type];
        }
        public static string GetName(this TilesheetType type)
        {
            foreach (var templateGroup in ViewModels.MainWindowViewModel.allTemplates)
            {
                foreach (var template in templateGroup.Buttons)
                {
                    if (template.Type == type) return template.Name;
                }
            }
            return "unknown type of sheet";
        }
        //public static string GetCustomName(TilesheetType tilesheetType)
        //{
        //    Dictionary<TilesheetType, string> customNames = new Dictionary<TilesheetType, string>
        //    {
        //        { TilesheetType.InvalidInput, "Invalid Input" },
        //        { TilesheetType.SimpleInput, "Simple Sheet" },
        //        { TilesheetType.BaseInput, "Base Sheet" },
        //        { TilesheetType.SplicedSheet, "Standard Sheet" },
        //        { TilesheetType.TileUnderlay, "Tile Merge Base Sheet" },
        //        { TilesheetType.TileOverlay, "Tile Overlay" },
        //        { TilesheetType.MergedSheet, "Tile Merge Combined Sheet" }
        //    };

        //    if (customNames.ContainsKey(tilesheetType))
        //    {
        //        return customNames[tilesheetType];
        //    }
        //    else
        //    {
        //        return "Unknown";
        //    }
        //}
    }

    public class Tilesheet
    {
        public string path;
        public TilesheetType type;
        public int originalResolution;
        public Bitmap currentBitmap;
        public Graphics canvas;
        public Color internalOutlineColor;
        public Dictionary<TilesheetType, (TilesheetOperation operation, TilesheetType inputType)> OperationsForResults;
        public Tilesheet(string path, TilesheetType type, int originalResolution)
        {
            this.type = type;
            this.path = path;
            this.originalResolution = originalResolution;
            currentBitmap = (Bitmap)Image.FromFile(path);
            if (originalResolution == 2) currentBitmap = GetDownscaledBitmap(currentBitmap);

            if (Path.GetFileName(path) == "templateTilesheet.png") internalOutlineColor = Color.Black; //delete this
            else internalOutlineColor = GetColors(currentBitmap).ElementAtOrDefault(1); //the second darkest color by luminance

            OperationsForResults = new() {
                { TilesheetType.BaseInput, (GenerateBaseTilesheet, TilesheetType.SimpleInput) },
                { TilesheetType.SplicedSheet, (GenerateSplicedTilesheet, TilesheetType.BaseInput) },
                { TilesheetType.TileUnderlay, (GenerateTileMergeUnderlayTilesheet, TilesheetType.SplicedSheet) },
                { TilesheetType.TileOverlay, (GenerateTileMergeOverlayTilesheet, TilesheetType.TileUnderlay) },
            };
        }

        public static string[] fileSuffixes = {
                "SimpleInput", //should be unused
                "BasicInput",
                "Spliced",
                "TileMergeBase",
                "TileMergeOverlay",
                "TileMergeLayered"
            };

        public void GenerateResult(TilesheetType resultType)
        {
            Stack<TilesheetOperation> operations = new();
            TilesheetType inputType = resultType;
            while (inputType != type) //get the operation necessary to make the result type, then check the input type. if the needed input type is the current type, perform the operation. otherwise, keeping adding operations
            {
                operations.Push(OperationsForResults[inputType].operation);
                inputType = OperationsForResults[inputType].inputType;
            }
            while (operations.Count > 0)
            {
                operations.Pop()(this);
            }
        }
        /*
        public void GenerateResultType(params TilesheetOperation[] operations)
        {
            foreach (var op in operations)
            {
                op(this);
            }
        }
        */
        /*public void GenerateDualTilesheet(Tilesheet secondaryFile)
        {
            TilesheetOperation[] secondaryOperations = GetOperationOrderForResultType(TilesheetType.TileOverlay); //operationOrder.Skip((int)secondaryFile.type).ToArray();
            foreach (var op in secondaryOperations)
            {
                Console.WriteLine(secondaryFile.currentBitmap.Height);
                Console.WriteLine(op.Method);
                op(secondaryFile);
            }
            TilesheetOperation[] primaryOperations = GetOperationOrderForResultType(TilesheetType.TileUnderlay); //operationOrder.SkipLast(1).Skip((int)primaryFile.type).ToArray(); //possible out of index errors with wrong types?
            foreach (var op in primaryOperations)
            {
                op(this);
            }
            GenerateCombinedTileMergeTilesheet(secondaryFile.currentBitmap);
        }*/

        public delegate void TilesheetOperation(Tilesheet file);

        void GenerateBaseTilesheet(Tilesheet file)
        {
            var bitmap = new Bitmap(BASIC_INPUT_SHEET_WIDTH, BASIC_SHEET_HEIGHT); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                for (int chunk = 0; chunk < 3; chunk++) //the 2x2 tile chunk variants
                {
                    DrawRect(new Point(0 + chunk * 9, 0), new Point(0 + chunk * 18, 27), 8, 8);
                    DrawRect(new Point(32 + chunk * 9, 0), new Point(9 + chunk * 18, 27), 8, 8);
                    DrawRect(new Point(0 + chunk * 9, 50), new Point(0 + chunk * 18, 36), 8, 8);
                    DrawRect(new Point(32 + chunk * 9, 50), new Point(9 + chunk * 18, 36), 8, 8);
                }
                for (int strip = 0; strip < 2; strip++) //the left and right vertical strips
                {
                    for (int tile = 0; tile < 3; tile++)
                    {
                        DrawRect(new Point(9 + 32 * strip, 17 + tile * 8), new Point(0 + 36 * strip, 0 + tile * 9), 8, 8);
                    }
                }
                for (int strip = 0; strip < 3; strip++) //the top, middle, and bottom horizontal strips
                {
                    for (int tile = 0; tile < 3; tile++)
                    {
                        DrawRect(new Point(17 + tile * 8, 9 + 16 * strip), new Point(9 + tile * 9, 0 + 9 * strip), 8, 8);
                    }
                }
                //we use chunk or tile * 8 (or 16) for the simple sheet because there are no gaps between tiles, and * 9 (or 18) for the base sheet because there's a pixel gap

                canvas.Save();
            }

            file.currentBitmap = bitmap; // TODO :
            file.type = TilesheetType.BaseInput;

            if (SAVE_PROGRESS_STAGES) SaveBitmap();//"Spliced");
        }
        void GenerateSplicedTilesheet(Tilesheet file)
        {
            var bitmap = new Bitmap(SPLICED_SHEET_WIDTH, BASIC_SHEET_HEIGHT); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                DrawRect(new Point(0, 0), new Point(0, 0), BASIC_INPUT_SHEET_WIDTH, BASIC_SHEET_HEIGHT); //copying original input

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

                DrawPixelPairs(new Point(54, 9), Axis.X, 3); //internal outline pixels for center intersection block corners
                DrawPixelPairs(new Point(54, 25), Axis.X, 3);

                DrawPixelPairs(new Point(90, 0), Axis.Y, 3);
                DrawPixelPairs(new Point(106, 0), Axis.Y, 3);

                void DrawPixelPairs(Point destPoint, Axis axis, int pairs)
                {
                    Point nextPoint = destPoint;
                    if (axis == Axis.X) nextPoint.X += 7;
                    else nextPoint.Y += 7;

                    for (int i = 0; i < pairs; i++)
                    {
                        bitmap.CheckSetPixelToInternalOutline(destPoint.X + (axis == Axis.X ? i * 9 : 0), destPoint.Y + (axis == Axis.Y ? i * 9 : 0), internalOutlineColor);
                        bitmap.CheckSetPixelToInternalOutline(nextPoint.X + (axis == Axis.X ? i * 9 : 0), nextPoint.Y + (axis == Axis.Y ? i * 9 : 0), internalOutlineColor);
                    }
                }

                canvas.Save();
            }

            file.currentBitmap = bitmap; // TODO :
            file.type = TilesheetType.SplicedSheet;

            if (SAVE_PROGRESS_STAGES) SaveBitmap();//"Spliced");
        }


        void GenerateTileMergeUnderlayTilesheet(Tilesheet file)
        {
            var bitmap = new Bitmap(FULL_SHEET_WIDTH, FULL_SHEET_HEIGHT); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                DrawRect(new Point(0, 0), new Point(0, 0), SPLICED_SHEET_WIDTH, BASIC_SHEET_HEIGHT); //copying original input

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

            file.currentBitmap = bitmap;
            file.type = TilesheetType.TileUnderlay;

            if (SAVE_PROGRESS_STAGES) SaveBitmap();//"TileMerge");
        }

        void GenerateTileMergeOverlayTilesheet(Tilesheet file)
        {
            var bitmap = new Bitmap(FULL_SHEET_WIDTH, FULL_SHEET_HEIGHT);

            Bitmap mergeShape = GetTilemergeShape();
            Color mergeShapeOutlineColor = GetColors(mergeShape).ElementAtOrDefault(0);

            Color overlayOutlineColor = GetColors(file.currentBitmap).ElementAtOrDefault(1); //only necessary because its not necessarily the same type as the main tile

            canvas = Graphics.FromImage(bitmap);

            ForEachPixel(bitmap, (i, j) =>
            {
                if (mergeShape.GetPixel(i, j).A == 0) return;

                if (mergeShape.GetPixel(i, j) == mergeShapeOutlineColor) bitmap.SetPixel(i, j, overlayOutlineColor);
                else bitmap.SetPixel(i, j, file.currentBitmap.GetPixel(i, j)); // this breaks. all bitmap.widths/heights should be - 1 if the bitmap is set to have the transparent pixel gap ig?
                                                                               //this doesnt check for IsInPixelGap, meaning outline pixels can be placed in the 8x8 grid, but non outline pixels cant. fix so it cant place if it's in the pixel gap.
            });

            canvas.Save();

            file.currentBitmap = bitmap; //necessary? doubt
            file.type = TilesheetType.TileOverlay;

            if (SAVE_PROGRESS_STAGES) SaveBitmap();//"TileMergeOverlay"); //this doesnt use overlayfile, so could be an issue
        }

        public void OverlayTileMergeOntoTilesheet(Bitmap overlayTilesheet)
        {
            var underlayTilesheet = currentBitmap;
            Color[] underlayColors = GetColors(underlayTilesheet);
            Color overlayOutlineColor = GetColors(overlayTilesheet).ElementAtOrDefault(0);

            HashSet<(int, int)> internalOutlinePixels = new HashSet<(int, int)>(); //used to avoid duplicate operations on the same pixel

            canvas = Graphics.FromImage(underlayTilesheet);
            canvas.DrawImage(overlayTilesheet,
                new Rectangle(0, 0, FULL_SHEET_WIDTH, FULL_SHEET_HEIGHT),
                new Rectangle(0, 0, FULL_SHEET_WIDTH, FULL_SHEET_HEIGHT),
                GraphicsUnit.Pixel);

            ForEachPixel(currentBitmap, (i, j) =>
            {
                bool IsOutlinePixel = overlayTilesheet.GetPixel(i, j) == overlayOutlineColor;

                if (!IsOutlinePixel)
                    return;

                foreach (Point point in GetCardinalAdjacentPoints(i, j))
                {
                    if (IsPixelInTileGridGap(point.X, point.Y))
                        continue;
                    if (overlayTilesheet.GetPixel(point.X, point.Y).A != 0) //can't draw over other pixels on the overlay
                        continue;
                    if (underlayTilesheet.GetPixel(point.X, point.Y).A == 0)
                        continue;
                    internalOutlinePixels.Add((point.X, point.Y));
                    //if (Luminance(internalOutlineColor) < Luminance(underlayTilesheet.GetPixel(point.X, point.Y)))
                    //    currentBitmap.SetPixel(point.X, point.Y, Color.Red);

                    //else currentBitmap.SetPixel(point.X, point.Y, internalOutlineColor);
                }
            });

            foreach ((int X, int Y) point in internalOutlinePixels)
            {
                currentBitmap.CheckSetPixelToInternalOutline(point.X, point.Y, internalOutlineColor);
                //if (Luminance(internalOutlineColor) >= Luminance(underlayTilesheet.GetPixel(point.X, point.Y)))
                //    continue; //currentBitmap.SetPixel(point.X, point.Y, TryGetDarkerColorInPalette(underlayColors, underlayTilesheet.GetPixel(point.X, point.Y))); //tries to find a color to darken pixels of internal outlines that are already dark

                //else currentBitmap.SetPixel(point.X, point.Y, internalOutlineColor);
            }

            canvas.Save();
            type = TilesheetType.MergedSheet;

            if (SAVE_PROGRESS_STAGES) SaveBitmap();//"TileMergeSheet");
        }
        void DrawRect(Point srcPoint, Point destPoint, int width, int height)
        {
            canvas.DrawImage(currentBitmap,
                new Rectangle(destPoint.X, destPoint.Y, width, height),
                new Rectangle(srcPoint.X, srcPoint.Y, width, height), GraphicsUnit.Pixel);
        }
        enum Axis
        {
            X,
            Y,
        }
        //void DrawPixelPair(Point destPoint, Axis axis, int pairs)
        //{
        //    //var pixel = new Bitmap(2, 2);
        //    //Graphics g = Graphics.FromImage(pixel);
        //    //g.Clear(internalOutlineColor); //sets the color of the pixel
        //    Point nextPoint = destPoint;
        //    if (axis == Axis.X) nextPoint.X += 7;
        //    else nextPoint.Y += 7;

        //    for (int i = 0; i < pairs; i++)
        //    {
        //        currentBitmap.SetPixel(destPoint.X + (axis == Axis.X ? i * 9 : 0), destPoint.Y + (axis == Axis.Y ? i * 9 : 0), internalOutlineColor);
        //        currentBitmap.SetPixel(nextPoint.X + (axis == Axis.X ? i * 9 : 0), nextPoint.Y + (axis == Axis.Y ? i * 9 : 0), internalOutlineColor);

        //        //canvas.DrawImage(pixel, new Rectangle(destPoint.X + (axis == Axis.X ? i * 9 : 0), destPoint.Y + (axis == Axis.Y ? i * 9 : 0), 1, 1), new Rectangle(0, 0, 1, 1), GraphicsUnit.Pixel);
        //        //canvas.DrawImage(pixel, new Rectangle(nextPoint.X + (axis == Axis.X ? i * 9 : 0), nextPoint.Y + (axis == Axis.Y ? i * 9 : 0), 1, 1), new Rectangle(0, 0, 1, 1), GraphicsUnit.Pixel);
        //    }
        //}
        public static Color[] GetColors(Bitmap bmp)
        {
            HashSet<Color> colors = new();
            ForEachPixel(bmp, (i, j) =>
            {
                Color pixColor = bmp.GetPixel(i, j);
                if (pixColor.A == 0) return;
                colors.Add(pixColor);
            });

            return colors.OrderBy(c => c.Luminance()).ToArray();
        }
        static Color TryGetDarkerColorInPalette(Color[] paletteByLuminance, Color originalColor)
        {
            int index = Array.IndexOf(paletteByLuminance, originalColor);

            if (index == -1 || index == 0) return originalColor;

            return paletteByLuminance[Array.IndexOf(paletteByLuminance, originalColor) - 1];
        }

        public static Bitmap GetUpscaledBitmap(Bitmap bmp)
        {
            var upscaledBmp = new Bitmap(bmp.Width * 2, bmp.Height * 2);

            Graphics newCanvas = Graphics.FromImage(upscaledBmp);

            ForEachPixel(bmp, (i, j) =>
            {
                if (bmp.GetPixel(i, j).A == 0) return;

                Color color = bmp.GetPixel(i, j);

                upscaledBmp.SetPixel(i * 2, j * 2, color);
                upscaledBmp.SetPixel(i * 2 + 1, j * 2, color);
                upscaledBmp.SetPixel(i * 2, j * 2 + 1, color);
                upscaledBmp.SetPixel(i * 2 + 1, j * 2 + 1, color);
            });

            newCanvas.Save();

            return upscaledBmp;
        }
        public static Bitmap GetDownscaledBitmap(Bitmap bmp, int divisor = 2)
        {
            var downscaledBmp = new Bitmap(bmp.Width / divisor, bmp.Height / divisor);

            ForEachPixel(downscaledBmp, (i, j) =>
            {
                if (bmp.GetPixel(i * divisor, j * divisor).A == 0) return;
                downscaledBmp.SetPixel(i, j, bmp.GetPixel(i * divisor, j * divisor));
            });
            //removed a canvas save here
            return downscaledBmp;
        }

        TilesheetOperation GetDefaultOperation(TilesheetType type, TilesheetType otherFileType) //selectedfiles
        {
            switch (type)
            {
                case TilesheetType.BaseInput:
                    return GenerateSplicedTilesheet;
                case TilesheetType.SplicedSheet:
                    /*if (otherFileType == TilesheetType.TileOverlay) return GenerateCombinedTileMergeTilesheet;
                    else*/
                    return GenerateTileMergeUnderlayTilesheet;
                case TilesheetType.InvalidInput:
                case TilesheetType.MergedSheet:
                default:
                    return null;
            }
        }
        /*void SaveBitmap(Bitmap bitmap, TilesheetFile originFile, string fileSuffix) //Bitmap bmp, string filePath
        {
            if (originFile.res == 2) bitmap = GetUpscaledBitmap(bitmap);
            string path = Path.GetFileName(originFile.path);
            Array.ForEach(fileSuffixes, x => path = path.Replace(x, ""));
            path = path.Replace(".png", fileSuffix + ".png");
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }*/
        public void SaveBitmap() //Bitmap bmp, string filePath
        {
            Bitmap bitmap = currentBitmap;
            if (originalResolution == 2) bitmap = GetUpscaledBitmap(bitmap);
            string newPath = GetSaveName(path);
            bitmap.Save(newPath, System.Drawing.Imaging.ImageFormat.Png);
        }
        public string GetSaveName(string path)
        {
            path = Path.GetFileName(path);
            Array.ForEach(fileSuffixes, x => path = path.Replace(x, "")); //remove other filesuffixes
            return path.Replace(".png", type + ".png");
        }

        static bool IsPixelInTileGridGap(int x, int y)
        {
            return (x + 1) % 9 == 0 || (y + 1) % 9 == 0;
        }
        public static Point[] GetCardinalAdjacentPoints(int x, int y)
        {
            return new[] { new Point(-1 + x, 0 + y), new Point(0 + x, -1 + y), new Point(1 + x, 0 + y), new Point(0 + x, 1 + y) };
        }
        public delegate void PixelOperation(int x, int y);
        public static void ForEachPixel(Bitmap bitmap, PixelOperation func)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    func(x, y);
                }
            }
        }
    }
    public static class BitmapExtensions
    {
        public static double Luminance(this Color c)
        {
            return c.R * 0.3 + c.G * 0.6 + c.B * 0.1;
        }
        public static void CheckSetPixelToInternalOutline(this Bitmap currentBitmap, int pixelX, int pixelY, Color internalOutlineColor)
        {
            if ((bool)Configuration.onlyReplaceLighterPixels.Value && Luminance(internalOutlineColor) >= Luminance(currentBitmap.GetPixel(pixelX, pixelY))) return;
            else currentBitmap.SetPixel(pixelX, pixelY, internalOutlineColor);
        }
    }
}