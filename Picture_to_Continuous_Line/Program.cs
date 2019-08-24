using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Picture_to_Continuous_Line
{
    class Program
    {
        public static int square = 11;
        public static int scaleMod = 1;
        public static int scaleMultiplier;
        public static int lineSkip;
        public static int pixelCount = 0;
        public static int blocksProc = 0;
        public static int blocks = 0;
        [STAThread]
        static void Main(string[] args)
        {           
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            string filePath = fd.FileName;
            Image startImg = Image.FromFile(filePath);
            Console.WriteLine("File Read Successfully");
            Console.WriteLine($"Dimensions {startImg.Width} x {startImg.Height}");
            Console.WriteLine($"Post-modifier {startImg.Width * scaleMod} x {startImg.Height * scaleMod}");
            Console.Write("Scale: ");
            scaleMultiplier = Convert.ToInt32(Console.ReadLine()) * scaleMod;
            Console.Write("Lines per block row: ");
            lineSkip = Convert.ToInt32(Console.ReadLine());            
            Image img = ResizeImage(startImg, GetNearestWholeMultiple(startImg.Width, square) * scaleMultiplier, GetNearestWholeMultiple(startImg.Height, square) * scaleMultiplier);
            Bitmap bitmapProcessing = new Bitmap(img);
            Bitmap bitmapResult = new Bitmap(img.Width, img.Height, img.PixelFormat);
            
            Graphics graphics = Graphics.FromImage(bitmapResult);

            SolidBrush brush = new SolidBrush(Color.White);
            Rectangle rect = new Rectangle(new Point(0, 0), new Size(img.Width, img.Height));
            graphics.FillRectangle(brush, rect);

            //bitmapResult.MakeTransparent();

            int height = bitmapProcessing.Height;
            int width = bitmapProcessing.Width;
            int[,] avgArr = new int[bitmapProcessing.Width, bitmapProcessing.Height];
            Console.Title = "Spline Art";
            Console.WriteLine("Pixel Matrix size: {0} x {1}", width, height);
            int area = width * height;
            blocks = area / (square * square);
            for (int x = 0; x < width; x++) // Generates the 
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = bitmapProcessing.GetPixel(x, y);
                    //Luminocity
                    //avgArr[x, y] = (int)((0.21 * color.R) + (0.72 * color.G) + (0.07 * color.B));
                    //Average
                    avgArr[x, y] = (int)(color.R + color.G + color.B) / 3;
                    //Lightness
                    //avgArr[x, y] = (int)(Math.Max(color.R,Math.Max(color.G,color.B)) + 
                    //                     Math.Min(color.R, Math.Min(color.G, color.B)))/2;

                    pixelCount++;
                    Console.Write($"\rPixels read: {pixelCount}/{area}  %{Math.Round((double)((double)pixelCount/(double)area * 100), 0)}");
                }
            }
            Console.WriteLine();
            var pointMap = generatePointMap(avgArr);
            var whitePen = new Pen(Color.Black, 1);
            for (int j = 0; j < pointMap.Count;)
            {
                Point[] p = pointMap.ElementAt(j);
                Point[] points = new Point[2];
                for(int i = 0; i < p.Length-3;)
                {
                    if((p.ElementAt(i).X + 1 == p.ElementAt(i+1).X) && (p.ElementAt(i).X != 0 & p.ElementAt(i).Y != 0))
                    {
                        points[0] = p.ElementAt(i);
                        points[1] = p.ElementAt(i+1);
                    }
                    else if((p.ElementAt(i).X == 0) && (p.ElementAt(i).Y == 0))
                    {
                        points[0] = p.ElementAt(i);
                        points[1] = p.ElementAt(i);
                    }
                    graphics.DrawCurve(whitePen, points, 0.5f);
                    i += 1;
                }
                j += lineSkip;
                Console.Write($"\rPopulating line segment {j}/{pointMap.Count}  %{Math.Round((double)((double)pointMap.Count / (double)j * 100), 0)}");
            }
            ResizeImage(bitmapResult, img.Width, img.Height);
            bitmapResult.Save(@"C:\Users\chris\Desktop\test4.png");
            Console.WriteLine("\n\nProcessing Complete");
            Console.Read();
        }

        static List<Point[]> generatePointMap (int[,] inputArr)
        {            
            int blocksWide = (int)Math.Floor((double)inputArr.GetLength(0) / square);
            int blocksTall = (int)Math.Floor((double)inputArr.GetLength(1) / square);

            List<Point[]> resultList = new List<Point[]>();            

            for (int height = 0; height < blocksTall; height++) //Cycles through inputArr by height in blocks
            {
                int extraPixels = 0;
                Point[] resultArr = new Point[0];
                var rslt = new List<Point>();
                for (int width = 0; width < blocksWide; width++) //Cycles through inputArr by width in blocks
                {
                    blocksProc++;
                    int[,] tempArr = new int[square, square];
                    for (int y = 0; y < square; y++)
                    {
                        for(int x = 0; x < square;x++)
                        {
                            tempArr[x, y] = inputArr[width * square + x, height * square + y];
                        }                        
                    }

                    int pixelAvg = 0;
                    foreach(int i in tempArr) //Cycles through pixels in {square} by {square} grid
                    {
                        pixelAvg += i;
                    }
                    pixelAvg = pixelAvg / (square * square);

                    Console.Write($"\rProcessing Block {blocksProc} of {blocks}  %{Math.Round((double)((double)blocksProc / (double)blocks * 100), 0)}");
                    rslt.AddRange(generateSplineData(pixelAvg, square, width, height));

                    for (int x = 0; x < rslt.Count; x++)
                    {
                        if (rslt.Count != 0)
                        {
                            resultArr.Append(rslt.ElementAt(x));
                            //resultArr[(width * square) + x + 1] = rslt[x];
                        }
                    }
                    extraPixels++;
                }
                if(rslt.ToArray().Length > 0)
                    resultList.Add(rslt.ToArray());
            }
            Console.WriteLine();
            return resultList;
        }

        public static List<Point> generateSplineData(int average, int square, int blkWidth, int blkHeight)
        {
            int[] heightMap = {11, 10, 9, 8, 7, 6, 5 ,4, 3, 2, 1, 0, 0};
            //int[] heightMap = { 6, 5, 4, 3, 2, 2, 1, 1, 1, 0 };
            //int[] heightMap = { 0, 0, 1, 1, 2, 4, 5 }; //Inverted

            int heightFreq = (((average - 0) * (heightMap.Length - 1 - 0)) / (255 - 0)) + 0;

            int height = heightMap[heightFreq];
            //int height = heightMap[heightMap.Length - heightFreq - 1]; //Inverted

            List<Point> pointMap = new List<Point>();
            //if (!(average > 255 -50) && !(heightMap[heightFreq] == 0))
            if (!(heightMap[heightFreq] == 0)) //Inverted
            {
                //pointMap.Add(new Point((blkWidth * square), (blkHeight * square)));
                for (int i = 0; i < square; i++)
                {
                    if (i % 2 == 0 || i == 0)
                    {
                        pointMap.Add(new Point(i + blkWidth * square, height + blkHeight * square));
                        pointMap.Add(new Point(i + blkWidth * square, +blkHeight * square));
                    }
                    else
                    {
                        pointMap.Add(new Point(i + blkWidth * square, height + blkHeight * square));
                        pointMap.Add(new Point(i + blkWidth * square, blkHeight * square));
                    }
                }
            }
            return pointMap;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                     graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        public static int GetNearestWholeMultiple(double input, double X)
        {
            var output = Math.Round(input / X);
            if (output == 0 && input > 0) output += 1;
            output *= X;

            return (int)output;
        }
    }
}
