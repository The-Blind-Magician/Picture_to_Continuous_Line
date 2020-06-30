using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace Picture_to_Continuous_Line
{
    class Program
    {
        public static int square = 11;
        public static int scaleMod = 1;
        public static int scaleMultiplier;
        public static int pixelCount = 0;
        public static int blocksProc = 0;
        public static int blocks = 0;
        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            string filePath = fd.FileName;            
            
            Console.WriteLine("File Read Successfully");
            Console.WriteLine($"Dimensions {Image.FromFile(filePath).Width} x {Image.FromFile(filePath).Height}");
            Console.Write("Scale: ");
            scaleMultiplier = Convert.ToInt32(Console.ReadLine()) * scaleMod;
            Bitmap bitmapProcessing = new Bitmap(ResizeImage(Image.FromFile(filePath), GetNearestWholeMultiple(Image.FromFile(filePath).Width, square) * scaleMultiplier, GetNearestWholeMultiple(Image.FromFile(filePath).Height, square) * scaleMultiplier));
            
            Bitmap bitmapResult = new Bitmap(bitmapProcessing.Width, bitmapProcessing.Height, bitmapProcessing.PixelFormat);

            Graphics graphics = Graphics.FromImage(bitmapResult);

            SolidBrush brush = new SolidBrush(Color.White);
            Rectangle rect = new Rectangle(new Point(0, 0), new Size(bitmapProcessing.Width, bitmapProcessing.Height));
            graphics.FillRectangle(brush, rect);

            //bitmapResult.MakeTransparent();

            int height = bitmapProcessing.Height;
            int width = bitmapProcessing.Width;

            Console.Title = "Spline Art";
            Console.WriteLine($"Pixel Matrix size: {width} x {height}");
            int area = width * height;
            blocks = area / (square * square);

            Point[] pointMap;
            Pen whitePen = new Pen(Color.Black, 1);

            int widthProg = width / square;
            int heightProg = height / square;

            Console.WriteLine();
            for (int y = 0; y < heightProg; y++)
            {
                for (int x = 0; x < widthProg; x++)
                {
                    int avg = 0;
                    for(int i = 0; i < square; i++)
                    {
                        for(int j = 0; j < square; j++)
                        {
                            double pixel = (int)(bitmapProcessing.GetPixel(j + (square * x), i + (square * y)).GetBrightness() * 255);
                            avg += (int)Math.Round(pixel);
                        }
                    }

                    avg /= (square * square);

                    bool bitches = true;
                    pointMap = generatePointMap(avg, x, y);
                    
                    foreach(Point bitch in pointMap)
                    {
                        if(bitch == new Point(0,0))
                        {
                            bitches = false;
                            break;
                        }
                    }

                    if(bitches)
                        graphics.DrawCurve(whitePen, pointMap, 1.0f);
                    Console.Write("\rComplete: %{0}", Math.Round((((double)((y*widthProg) + x) / (widthProg * heightProg)) * 100.0)));
                }
            }
            ResizeImage(bitmapResult, Image.FromFile(filePath).Width, Image.FromFile(filePath).Height);
            bitmapResult.Save(@"C:\Users\chris\Desktop\test4.png");
            Console.WriteLine("\n\nProcessing Complete");
            Console.Read();
        }

        static Point[] generatePointMap(int avg, int x, int y)
        {
            List<Point> resultList = new List<Point>();

            x *= square;
            y *= square;
            int pixelAvg = avg;

            resultList.AddRange(generateSplineData(pixelAvg, square, x, y));

            //Console.Write($"\rProcessing Blocks: %{Math.Round((double)((double)height / (double)blocksTall * 100), 0)}");

            Point[] points = resultList.ToArray();
            return points;
        }




        public static List<Point> generateSplineData(int average, int square, int x, int y)
        {
            int[] heightMap = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1,  0,  0 };
            //int[] heightMap = { 6, 5, 4, 3, 2, 1, 0};
            //int[] heightMap = { 9, 7, 4, 1, 0, 0 };
            //int[] heightMap = {  0,  0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            //int[] heightMap = { 6, 5, 4, 3, 2, 2, 1, 1, 1, 0 };
            //int[] heightMap = { 0, 0, 1, 1, 2, 4, 5 }; //Inverted

            int heightFreq = (((average - 0) * (heightMap.Length - 1 - 0)) / (255 - 0)) + 0;

            int height = heightMap[heightFreq];// * scaleMultiplier;
            //int height = heightMap[heightMap.Length - heightFreq - 1]; //Inverted

            List<Point> pointMap = new List<Point>();
            //if (!(average > 255 -50) && !(heightMap[heightFreq] == 0))
            if ((heightMap[heightFreq] == 0)) //Inverted
            {
                pointMap.Add(Point.Empty);
                return pointMap;
            }
            else
            {
                //pointMap.Add(new Point((blkWidth * square), (blkHeight * square)));
                for (int i = 0; i < square; i++)
                {
                    //pointMap.Add(new Point(i + x, y - height));
                    //pointMap.Add(new Point(i + x, height + y));
                    pointMap.Add(new Point(i + x, height + y));
                    pointMap.Add(new Point(i + x, y));
                }
            }
            return pointMap;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        public static int GetNearestWholeMultiple(double input, double X)
        {
            double output = Math.Round(input / X);
            if (output == 0 && input > 0)
            {
                output += 1;
            }

            output *= X;

            return (int)output;
        }
    }
}