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
using System.Linq;

namespace Picture_to_Continuous_Line
{
    class Program
    {
        public static int square = 11;
        public static int scaleMultiplier;
        public static int lineSkip;
        [STAThread]
        static void Main(string[] args)
        {           
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            string filePath = fd.FileName;
            Console.Write("Scale: ");
            scaleMultiplier = Convert.ToInt32(Console.ReadLine());
            Console.Write("\nLines per block row: ");
            lineSkip = Convert.ToInt32(Console.ReadLine());
            Image startImg = Image.FromFile(filePath);
            Image img = ResizeImage(startImg, GetNearestWholeMultiple(startImg.Width, square) * scaleMultiplier, GetNearestWholeMultiple(startImg.Height, square) * scaleMultiplier);
            Bitmap bitmapProcessing = new Bitmap(img);
            Bitmap bitmapResult = new Bitmap(img.Width, img.Height, img.PixelFormat);
            
            Graphics graphics = Graphics.FromImage(bitmapResult);

            //SolidBrush brush = new SolidBrush(Color.White);
            //Rectangle rect = new Rectangle(new Point(0, 0), new Size(img.Width, img.Height));
            //graphics.FillRectangle(brush, rect);

            bitmapResult.MakeTransparent();

            int height = bitmapProcessing.Height;
            int width = bitmapProcessing.Width;
            int[,,] pixelArr = new int[bitmapProcessing.Width, bitmapProcessing.Height, 3];
            int[,] avgArr = new int[bitmapProcessing.Width, bitmapProcessing.Height];
            int[,] argbArray = new int[bitmapProcessing.Width, bitmapProcessing.Height];
            Console.Title = "Spline Art";
            Console.WriteLine("File Read Successfully");
            Console.WriteLine("Pixel Matrix size: {0} x {1}", width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = bitmapProcessing.GetPixel(x, y);
                    argbArray[x, y] = color.ToArgb();
                    pixelArr[x, y, 0] = color.R;
                    pixelArr[x, y, 1] = color.G;
                    pixelArr[x, y, 2] = color.B;
                    //Luminocity
                    //avgArr[x, y] = (int)((0.21 * color.R) + (0.72 * color.G) + (0.07 * color.B));
                    //Average
                    avgArr[x, y] = (int)(color.R + color.G + color.B) / 3;
                    //Lightness
                    //avgArr[x, y] = (int)(Math.Max(color.R,Math.Max(color.G,color.B)) + 
                    //                     Math.Min(color.R, Math.Min(color.G, color.B)))/2;

                }
            }
            var pointMap = generatePointMap(avgArr);

            var whitePen = new Pen(Color.White, 1);
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
                
            }
            bitmapResult.Save(@"C:\Users\chris\Desktop\test.png");
        }

        static List<Point[]> generatePointMap (int[,] inputArr)
        {            
            int blocksWide = (int)Math.Floor((double)inputArr.GetLength(0) / square);
            int blocksTall = (int)Math.Floor((double)inputArr.GetLength(1) / square);

            int extraX = inputArr.GetLength(0) % (blocksWide * square);

            int totalBlocks = blocksWide * blocksTall;

            List<Point[]> resultList = new List<Point[]>();            

            for (int height = 0; height < blocksTall; height++) //Cycles through inputArr by height in blocks
            {
                int extraPixels = 0;
                Point[] resultArr = new Point[0];
                var rslt = new List<Point>();
                for (int width = 0; width < blocksWide; width++) //Cycles through inputArr by width in blocks
                {
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
            return resultList;
        }

        public static List<Point> generateSplineData(int average, int square, int blkWidth, int blkHeight)
        {
            int[] heightMap = { 5, 2, 1, 0, 0, 0, 0, 0 };

            int heightFreq = (((average - 0) * (heightMap.Length - 1 - 0)) / (255 - 0)) + 0;

            int height = heightMap[heightFreq];

            List<Point> pointMap = new List<Point>();
            if (!(average > 255 -50) && !(heightMap[heightFreq] == 0))
            {
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
