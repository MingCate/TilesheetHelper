using System;
using System.IO;
using System.Drawing;
using System.Runtime;

namespace TilesheetHelper 
{
    internal class Program
    {
        //public static readonly int outputWidth = 53;
        //public static readonly int outputHeight = 44;
        public const int inputWidth = 54;
        public const int inputHeight = 45;
        public const int tileSize = 8;
        public static int res = 1; //resolution
        static void Main(string[] args)
        {
            if (!OperatingSystem.IsWindows())
            {
                Exception ex = new PlatformNotSupportedException();
                PrintConsoleError(ex);
                throw ex;
                return;
            }

            string filePath;
            if (args.Any())
            {
                try
                {
                    filePath = args[0];
                    if (!CheckTileSheetDimensions(Image.FromFile(filePath), ref res)) throw new ArgumentOutOfRangeException();
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    return;
                }
            }
            else
            {
                try
                {
                    filePath = GetTileSheetPath();
                }
                catch (Exception ex)
                {
                    PrintConsoleError(ex);
                    return;
                }
            }
            Image tilesheet;

            tilesheet = Image.FromFile(filePath);

            using (tilesheet)
            {
                using (var bitmap = new Bitmap(inputWidth * res, inputHeight * res))
                {
                    using (var canvas = Graphics.FromImage(bitmap))
                    {
                        canvas.DrawImage(tilesheet,
                                         new Rectangle(0, 0, tilesheet.Width, tilesheet.Height),
                                         new Rectangle(0, 0, tilesheet.Width, tilesheet.Height), GraphicsUnit.Pixel);

                        canvas.DrawImage(tilesheet,
                                         new Rectangle(45 * res, 0, 8, 26 * res),
                                         new Rectangle(0, 0, 4 * res, 26 * res), GraphicsUnit.Pixel);

                        canvas.DrawImage(tilesheet,
                                         new Rectangle(49 * res, 0, 4 * res, 26 * res),
                                         new Rectangle(40 * res, 0, 4 * res, 26 * res), GraphicsUnit.Pixel);
                        canvas.Save();

                    }
                    try
                    {
                        bitmap.Save(Path.GetFileName(filePath).Replace(".png", "merged.png"),
                                    System.Drawing.Imaging.ImageFormat.Png);
                    }
                    catch (Exception ex) { }
                }
            }
        }
        static string GetTileSheetPath()
        {
            bool wrongSizeImageFound = false;
            foreach (var path in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png"))
            {
                Image image = Image.FromFile(path);
                if (CheckTileSheetDimensions(image, ref res))
                {
                    Console.WriteLine(path);
                    return path;
                }
                else wrongSizeImageFound = true;
            }
            if (wrongSizeImageFound) throw new ArgumentOutOfRangeException();
            throw new FileNotFoundException();
        }

        static bool CheckTileSheetDimensions(Image tilesheet, ref int resolution)
        {
            int widthDiff = inputWidth - tilesheet.Width;
            int heightDiff = inputHeight - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 1 && heightDiff >= 0 && heightDiff <= 1)
            {
                Console.WriteLine(widthDiff);
                resolution = 1;
                return true;
            }
            widthDiff = inputWidth * 2 - tilesheet.Width;
            heightDiff = inputHeight * 2 - tilesheet.Height;
            if (widthDiff >= 0 && widthDiff <= 2 && heightDiff >= 0 && heightDiff <= 2)
            {
                Console.WriteLine(widthDiff);
                resolution = 2;
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
    }
}