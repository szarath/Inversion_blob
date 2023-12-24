using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

class Program
{
    static void Main()
    {
        Console.WriteLine("Downloading and processing images...");

        // Set your base URL
        string baseUrl = "https://inversionrecruitment.blob.core.windows.net/find-the-code/";

        // Local directory to store downloaded images
        string localDirectory = "DownloadedImages";

        // Create the directory if it doesn't exist
        if (!Directory.Exists(localDirectory))
        {
            Directory.CreateDirectory(localDirectory);
        }

        // Download and load images
        List<(int, Image<Bgra32>, bool, bool, bool, bool)> imageList = new List<(int, Image<Bgra32>, bool, bool, bool, bool)>();

        // Assuming the first image is the reference tile
        Image<Bgra32> referenceTile = null;

        for (int i = 1; i <= 1200; i++)
        {
            string localPath = Path.Combine(localDirectory, $"{i}.png");

            // Check if the image is already downloaded
            if (File.Exists(localPath))
            {
                Image<Bgra32> img = Image.Load<Bgra32>(localPath);
                bool hasLeftBorder = HasLeftBorder(img);
                bool hasRightBorder = HasRightBorder(img);
                bool hasTopBorder = HasTopBorder(img);
                bool hasBottomBorder = HasBottomBorder(img);

                // Set the reference tile if not set
                if (referenceTile == null)
                {
                    referenceTile = img;
                }
              
                imageList.Add((i, img, hasLeftBorder, hasRightBorder, hasTopBorder, hasBottomBorder));
                Console.WriteLine($"Loaded image {i} from local cache");
            }
            else
            {
                string url = $"{baseUrl}({i}).png";
                Image<Bgra32> img = DownloadImage(url);
                bool hasLeftBorder = HasLeftBorder(img);
                bool hasRightBorder = HasRightBorder(img);
                bool hasTopBorder = HasTopBorder(img);
                bool hasBottomBorder = HasBottomBorder(img);

                // Set the reference tile if not set
                if (referenceTile == null)
                {
                    referenceTile = img;
                }

                // Save the image locally
                img.Save(localPath);

                imageList.Add((i, img, hasLeftBorder, hasRightBorder, hasTopBorder, hasBottomBorder));
                Console.WriteLine($"Downloaded and cached image {i} of 1200");
            }
        }

        Console.WriteLine("Download and processing complete. Starting image reconstruction...");


        // Sort the images, placing tiles with borders at the end
        //imageList.Sort((a, b) => CompareImages(b.Item2, referenceTile).CompareTo(CompareImages(a.Item2, referenceTile)));
        //imageList.Sort((a, b) => CompareColorHistograms(a.Item2, referenceTile).CompareTo(CompareColorHistograms(b.Item2, referenceTile)));
        // Assuming each tile has the same width and height
        int tileWidth = imageList[0].Item2.Width;
        int tileHeight = imageList[0].Item2.Height;

        // Calculate the dimensions of the original image including the black border
        int originalWidth = 38 * (tileWidth);
        int originalHeight = 28 * (tileHeight);

        // Stitch images together to reconstruct the original image
        Image<Bgra32> resultImage = new Image<Bgra32>(originalWidth, originalHeight);
        imageList.RemoveAll(x => (x.Item3 == true) || (x.Item4 == true) || (x.Item5 == true) || (x.Item6 == true));
        // Initialize counters for row and column
        int currentRow = 0;
        int currentColumn = 0;

        foreach (var imageInfo in imageList)
        {
            // Calculate the base position
            int offsetX = currentColumn * tileWidth;
            int offsetY = currentRow * tileHeight;

            // Draw the current tile onto the result image
            resultImage.Mutate(ctx => ctx.DrawImage(imageInfo.Item2, new Point(offsetX, offsetY), 1f));

            // Update counters
            currentColumn++;

            // Move to the next row if we've reached the end of a row
            if (currentColumn == 40)
            {
                currentColumn = 0;
                currentRow++;
            }
        }




        Console.WriteLine("Image reconstruction complete. Saving result image...");

        // Save or display the result image
        resultImage.Save("result.png");

        Console.WriteLine("Result image saved. Press any key to exit.");
        Console.ReadKey();
    }

    static Image<Bgra32> DownloadImage(string url)
    {
        using (var client = new HttpClient())
        {
            var bytes = client.GetByteArrayAsync(url).Result;
            using (var stream = new MemoryStream(bytes))
            {
                return Image.Load<Bgra32>(stream);
            }
        }
    }

    static double CompareImages(Image<Bgra32> image1, Image<Bgra32> image2)
    {
        // Convert Image<Bgra32> to grayscale
        var grayscale1 = ConvertToGrayscale(image1);
        var grayscale2 = ConvertToGrayscale(image2);

        // Calculate histogram for grayscale images
        var histogram1 = CalculateHistogram(grayscale1);
        var histogram2 = CalculateHistogram(grayscale2);

        // Use histogram comparison (you can choose a different method based on your needs)
        double correlation = CalculateHistogramCorrelation(histogram1, histogram2);

        return correlation;
    }

    static Image<L8> ConvertToGrayscale(Image<Bgra32> image)
    {
        // Use ImageSharp processors to convert to grayscale
        var grayscaleImage = image.Clone(x => x.Grayscale());

        // Convert to L8 format
        var grayscale = grayscaleImage.CloneAs<L8>();

        return grayscale;
    }

    static int[] CalculateHistogram(Image<L8> image)
    {
        // Get the histogram
        var histogram = new int[256];

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                histogram[image[x, y].PackedValue]++;
            }
        }

        return histogram;
    }
    static double CompareColorHistograms(Image<Bgra32> image1, Image<Bgra32> image2)
    {
        // Calculate color histograms for the images
        var histogram1 = CalculateColorHistogram(image1);
        var histogram2 = CalculateColorHistogram(image2);

        // Use histogram comparison (you can choose a different method based on your needs)
        double euclideanDistance = Accord.Math.Distance.Euclidean(histogram1, histogram2);

        // You may need to normalize the distance based on your requirements
        // For example, you can divide by the length of the histograms.
        double normalizedDistance = euclideanDistance / histogram1.Length;

        return normalizedDistance;
    }

    static double[] CalculateColorHistogram(Image<Bgra32> image)
    {
        // Get the color histogram
        var histogram = new double[256 * 3]; // 256 bins for each channel (R, G, B)

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                histogram[image[x, y].R]++;
                histogram[image[x, y].G + 256]++;
                histogram[image[x, y].B + 512]++;
            }
        }

        return histogram;
    }
    static double CalculateHistogramCorrelation(int[] histogram1, int[] histogram2)
    {
        // Convert int[] arrays to double[] arrays
        double[] doubleHistogram1 = Array.ConvertAll(histogram1, x => (double)x);
        double[] doubleHistogram2 = Array.ConvertAll(histogram2, x => (double)x);

        // Use histogram comparison (you can choose a different method based on your needs)
        double euclideanDistance = Accord.Math.Distance.Euclidean(doubleHistogram1, doubleHistogram2);

        // You may need to normalize the distance based on your requirements
        // For example, you can divide by the length of the histograms.
        double normalizedDistance = euclideanDistance / histogram1.Length;

        return normalizedDistance;
    }

    static bool HasLeftBorder(Image<Bgra32> image)
    {
        // Check if there is a black line on the left side
        return Enumerable.Range(0, image.Height).Any(y => image[0, y].A > 0 && image[0, y].R < 30 && image[0, y].G < 30 && image[0, y].B < 30);
    }

    static bool HasRightBorder(Image<Bgra32> image)
    {
        // Check if there is a black line on the right side
        return Enumerable.Range(0, image.Height).Any(y => image[image.Width - 1, y].A > 0 && image[image.Width - 1, y].R < 30 && image[image.Width - 1, y].G < 30 && image[image.Width - 1, y].B < 30);
    }

    static bool HasTopBorder(Image<Bgra32> image)
    {
        // Check if there is a black line on the top side
        return Enumerable.Range(0, image.Width).Any(x => image[x, 0].A > 0 && image[x, 0].R < 30 && image[x, 0].G < 30 && image[x, 0].B < 30);
    }

    static bool HasBottomBorder(Image<Bgra32> image)
    {
        // Check if there is a black line on the bottom side
        return Enumerable.Range(0, image.Width).Any(x => image[x, image.Height - 1].A > 0 && image[x, image.Height - 1].R < 30 && image[x, image.Height - 1].G < 30 && image[x, image.Height - 1].B < 30);
    }

}
