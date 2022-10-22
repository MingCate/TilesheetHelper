﻿using System;
using System.IO;
using System.Drawing;
using System.Runtime;

namespace TilesheetHelper
{
    internal class Program
    {
        public const int inputWidth = 54;
        public const int inputHeight = 45;
        public const int tileSize = 8;
        public static int res = 1; //resolution

        public static Image tilesheet;
        public static Graphics canvas;
        public static Color internalOutlineColor; 
        static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Exception ex = new PlatformNotSupportedException();
                PrintConsoleError(ex);
                throw ex;
            }

            string filePath;// = GetTileSheetPath(args);
            if (GetTilesheetPath(args) is string path) filePath = path;
            else return;

            tilesheet = Image.FromFile(filePath);
            if (tilesheet.Width > 54) res = 2;

            internalOutlineColor = GetColors((Bitmap)tilesheet).ElementAtOrDefault(1); //the second least darkest color by luminance
            //internalOutlineColor = Color.Black;

            GenerateMergedTilesheet(tilesheet, filePath);
        }

        static string GetTilesheetPath(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    if (!CheckTilesheetDimensions(Image.FromFile(args[0]))) throw new ArgumentOutOfRangeException();
                    return args[0];
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    //throw;
                }
            }
            else
            {
                try
                {
                    return GetTilesheetPathFromFolder();
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    //throw;
                }
            }
            return null;
        }
        static string GetTilesheetPathFromFolder()
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
        }

        static bool CheckTilesheetDimensions(Image tilesheet)
        {
            int widthDiff = inputWidth - tilesheet.Width;
            int heightDiff = inputHeight - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 1 && heightDiff >= 0 && heightDiff <= 1)
            {
                return true;
            }
            widthDiff = inputWidth * 2 - tilesheet.Width;
            heightDiff = inputHeight * 2 - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 2 && heightDiff >= 0 && heightDiff <= 2)
            {
                return true;
            }
            return false;
        }
        static void PrintConsoleError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            if (ex is ArgumentOutOfRangeException) Console.WriteLine("The provided tilesheet has the wrong dimensions. Sheet should be 54x45 or 108x90. Reference defaulttilesheet.png for an example sheet.");
            else if (ex is FileNotFoundException) Console.WriteLine("No png file was found in the " + Path.GetFileName(Directory.GetCurrentDirectory()) + " folder.");
            else if (ex is PlatformNotSupportedException) Console.WriteLine("Sorry, but the image editing tools TilesheetHelper uses are only supported by Windows, so TilesheetHelper doesn't work on Mac or Linux.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Press any key to close this window.");
            Console.ReadKey();
        }

        static void GenerateMergedTilesheet(Image tilesheet, string filePath)
        {
            if (!OperatingSystem.IsWindows()) return;
            var bitmap = new Bitmap(117 * res, 45 * res); //144 135

            canvas = Graphics.FromImage(bitmap);
            {
                DrawRect(new Point(0, 0), new Point(0, 0), inputWidth, inputHeight); //copying original input

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


            bitmap.Save(Path.GetFileName(filePath).Replace(".png", "merged.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        static void DrawRect(Point srcPoint, Point destPoint, int width, int height)
        {
            canvas.DrawImage(tilesheet,
                new Rectangle(destPoint.X * res, destPoint.Y * res, width * res, height * res),
                new Rectangle(srcPoint.X * res, srcPoint.Y * res, width * res, height * res), GraphicsUnit.Pixel);
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
                canvas.DrawImage(pixel, new Rectangle(destPoint.X * res + (axis == Axis.X ? i * 9 * res : 0), destPoint.Y * res + (axis == Axis.Y ? i * 9 * res : 0), res, res), new Rectangle(0, 0, res, res), GraphicsUnit.Pixel);
                canvas.DrawImage(pixel, new Rectangle(nextPoint.X * res + (axis == Axis.X ? i * 9 * res : 0), nextPoint.Y * res + (axis == Axis.Y ? i * 9 * res : 0), res, res), new Rectangle(0, 0, res, res), GraphicsUnit.Pixel);
            }
        }
        static Color[] GetColors(Bitmap bmp)
        {
            List<Color> colors = new List<Color>();
            for (int x = 0; x < bmp.Width; x += res)
            {
                for (int y = 0; y < bmp.Height; y += res)
                {
                    Color pixColor = bmp.GetPixel(x, y);
                    if (!colors.Contains(pixColor) && pixColor.A > 0) colors.Add(pixColor);
                }
            }

            return colors.OrderBy(c =>
            {
                double luminance = c.R * 0.3 + c.G * 0.6 + c.B * 0.1;
                return luminance;
            }).ToArray();
        }
    }
}