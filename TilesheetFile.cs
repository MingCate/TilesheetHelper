using System;
using System.IO;
using System.Drawing;
using System.Runtime;
using System.Diagnostics;
using static TilesheetHelper.Program;

namespace TilesheetHelper
{
    public enum TilesheetType
    {
        InvalidInput = -1,
        BasicInput,
        //StreamlinedInput,
        SplicedSheet,
        TileUnderlay, //Combine with MergedSheet? Think of tileunderlay as simply a mergedsheet with an invisible overlay. Its production could simply be the result of a mergedsheet with no tileoverlay selected, reducing outputs.
        TileOverlay,
        MergedSheet, //complete, will never be an input (?)
    }

    public class TilesheetFile
    {
        public string path;
        public TilesheetType type;
        public int res;
        public Bitmap originalBitmap;
        public Bitmap currentBitmap;
        public Graphics canvas;
        public Color internalOutlineColor;
        public TilesheetOperation[] operationOrder; //kind of in a weird place with this storing data about what tilesheetfile created it by not being static, despite the methods themselves being the same
        public TilesheetFile(string path, TilesheetType type, int res)
        {
            this.type = type;
            this.path = path;
            this.res = res;
            this.originalBitmap = (Bitmap)Image.FromFile(path);
            if (res == 2) originalBitmap = GetBaseResolutionBitmap(originalBitmap);
            this.currentBitmap = originalBitmap;

            if (Path.GetFileName(path) == "templateTilesheet.png") internalOutlineColor = Color.Black;
            else internalOutlineColor = GetColors(originalBitmap).ElementAtOrDefault(1); //the second least darkest color by luminance

            operationOrder = new TilesheetOperation[] { GenerateSplicedTilesheet, GenerateTileMergeUnderlayTilesheet, GenerateTileMergeOverlayTilesheet, };
        }

        public void GenerateTilesheet(TilesheetFile file, params TilesheetOperation[] operations)
        {
            foreach (var op in operations)
            {
                op(file);
            }
        }
        public void GenerateDualTilesheet(TilesheetFile primaryFile, TilesheetFile secondaryFile)
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
                op(primaryFile);
            }
            GenerateCombinedTileMergeTilesheet(primaryFile, secondaryFile.currentBitmap);
        }

        public delegate void TilesheetOperation(TilesheetFile file);

        public TilesheetOperation[] GetOperationOrderForResultType(TilesheetType resultType) 
        {
            if (type == TilesheetType.InvalidInput || type == TilesheetType.TileOverlay) return Array.Empty<TilesheetOperation>(); //uhhh probably we want better behavior if a tilesheet overlay is fed in rather than just doing nothing
            return operationOrder.Skip((int)type).SkipLast(operationOrder.Length - (int)resultType).ToArray();
        }
        void GenerateSplicedTilesheet(TilesheetFile file)
        {
            Console.WriteLine("spliced");
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

            file.currentBitmap = bitmap; // TODO :

            if (saveProgressStages) SaveBitmap(fileSuffixes[(int)TilesheetType.SplicedSheet]);//"Spliced");
        }


        void GenerateTileMergeUnderlayTilesheet(TilesheetFile file)
        {
            Console.WriteLine("mergeunderlay");
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

            file.currentBitmap = bitmap;

            if (saveProgressStages) SaveBitmap(fileSuffixes[(int)TilesheetType.TileUnderlay]);//"TileMerge");
        }

        void GenerateTileMergeOverlayTilesheet(TilesheetFile file)
        {
            Console.WriteLine("mergeoverlay");
            if (!OperatingSystem.IsWindows()) return;

            TilesheetFile overlayFile = file;

            var bitmap = new Bitmap(144, 135); //144 135

            var mergeShapeBitmap = Properties.Resources.DirtMerge; //Image.FromFile()
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirtMerge.png")))
            {
                var customMerge = (Bitmap)Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DirtMerge.png"));
                if (customMerge.Width == fullSheetWidth * 2 && customMerge.Height == fullSheetHeight * 2) customMerge = GetBaseResolutionBitmap(customMerge);

                if (customMerge.Width == fullSheetWidth && customMerge.Height == fullSheetHeight) mergeShapeBitmap = customMerge;
                else Console.WriteLine($"The provided 'DirtMerge.png' is of dimensions {customMerge.Width} x {customMerge.Height}. Please use a {fullSheetWidth} x {fullSheetHeight} image at 1x or 2x resolution. Continuing using default dirt merge tilesheet");
            }
            else Console.WriteLine($"No file named 'DirtMerge.png' found in {AppDomain.CurrentDomain.BaseDirectory}, using default dirt merge tilesheet");
            var mergeShapeOutline = GetColors(mergeShapeBitmap).ElementAtOrDefault(0);
            var mergeOverlayTexture = file.currentBitmap; // Properties.Resources.DirtMerge; 

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
                            //Console.WriteLine(j);
                            //Console.WriteLine(mergeOverlayTexture.Height);
                            if (mergeShapeBitmap.GetPixel(i, j) == mergeShapeOutline) bitmap.SetPixel(i, j, overlayOutlineColor);
                            else bitmap.SetPixel(i, j, mergeOverlayTexture.GetPixel(i, j)); // this breaks. all bitmap.widths/heights should be - 1 if the bitmap is set to have the transparent pixel gap ig?
                            //this doesnt check for IsInPixelGap, meaning outline pixels can be placed in the 8x8 grid, but non outline pixels cant. fix so it cant place if it's in the pixel gap.
                        }
                    }
                }
                canvas.Save();
            }

            file.currentBitmap = bitmap;

            if (saveProgressStages) SaveBitmap(fileSuffixes[(int)TilesheetType.TileOverlay]);//"TileMergeOverlay"); //this doesnt use overlayfile, so could be an issue

            //GenerateCombinedTileMergeTilesheet(file, bitmap);
        }

        public void GenerateCombinedTileMergeTilesheet(TilesheetFile file, Bitmap overlayTilesheet) //should this be public?
        {
            if (!OperatingSystem.IsWindows()) return;
            var underlayTilesheet = file.currentBitmap;

            var bitmap = new Bitmap(144, 135); //144 135

            //overlayTilesheet = Properties.Resources.DirtMerge;

            //var mergeShapeBitmap = Properties.Resources.DirtMerge; //Image.FromFile()
            var overlayOutlineColor = GetColors(overlayTilesheet).ElementAtOrDefault(0);
            //var mergeTextureBitmap = (Bitmap)tilesheet;

            canvas = Graphics.FromImage(bitmap);
            {
                canvas.DrawImage(underlayTilesheet,
                    new Rectangle(0, 0, 144, 135),
                    new Rectangle(0, 0, 144, 135), GraphicsUnit.Pixel);// these three lines were commented out? idk why
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
            if (saveProgressStages) SaveBitmap(fileSuffixes[(int)TilesheetType.MergedSheet]);//"TileMergeSheet");
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
        void DrawPixelPair(Point destPoint, Axis axis, int pairs)
        {
            var pixel = new Bitmap(2, 2);
            Graphics g = Graphics.FromImage(pixel);
            g.Clear(selectedFiles[0].internalOutlineColor); //sets the color of the pixel

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

        Bitmap GetUpscaledBitmap(Bitmap bmp)
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
        Bitmap GetBaseResolutionBitmap(Bitmap bmp)
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

        TilesheetOperation GetDefaultOperation(TilesheetType type, TilesheetType otherFileType) //selectedfiles
        {
            switch (type)
            {
                case TilesheetType.BasicInput: return GenerateSplicedTilesheet;
                case TilesheetType.SplicedSheet:
                    /*if (otherFileType == TilesheetType.TileOverlay) return GenerateCombinedTileMergeTilesheet;
                    else*/
                    return GenerateTileMergeUnderlayTilesheet;
                case TilesheetType.InvalidInput: case TilesheetType.MergedSheet: default: return null;
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
        public void SaveBitmap(string fileSuffix) //Bitmap bmp, string filePath
        {
            Bitmap bitmap = currentBitmap;
            if (res == 2) bitmap = GetUpscaledBitmap(bitmap);
            string path = Path.GetFileName(this.path);
            Array.ForEach(fileSuffixes, x => path = path.Replace(x, ""));
            path = path.Replace(".png", fileSuffix + ".png");
            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        }
        static double Luminance(Color c)
        {
            return c.R * 0.3 + c.G * 0.6 + c.B * 0.1;
        }
        static bool IsPixelInTileGridGap(int x, int y)
        {
            return (x + 1) % 9 == 0 || (y + 1) % 9 == 0;
        }
    }
}