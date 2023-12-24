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
        List<(int, Image<Bgra32>)> imageList = new List<(int, Image<Bgra32>)>();

        for (int i = 1; i <= 1200; i++)
        {
            string localPath = Path.Combine(localDirectory, $"{i}.png");

            // Check if the image is already downloaded
            if (File.Exists(localPath))
            {
                Image<Bgra32> img = Image.Load<Bgra32>(localPath);
                imageList.Add((i, img));
                Console.WriteLine($"Loaded image {i} from local cache");
            }
            else
            {
                string url = $"{baseUrl}({i}).png";
                Image<Bgra32> img = DownloadImage(url);

                // Save the image locally
                img.Save(localPath);

                imageList.Add((i, img));
                Console.WriteLine($"Downloaded and cached image {i} of 1200");
            }
        }

        Console.WriteLine("Download and processing complete. Starting image reconstruction...");

        Image<Bgra32> referenceTile = imageList[0].Item2;

        // Sort the images based on similarity to the reference tile
        imageList.Sort((a, b) => CompareImages(a.Item2, referenceTile).CompareTo(CompareImages(b.Item2, referenceTile)));

        // Continue with the rest of your code

        // Assuming each tile has the same width and height
        int tileWidth = imageList[0].Item2.Width;
        int tileHeight = imageList[0].Item2.Height;

        // Calculate the dimensions of the original image including the black border
        int originalWidth = 40 * tileWidth;
        int originalHeight = 30 * tileHeight;

        // Stitch images together to reconstruct the original image
        Image<Bgra32> resultImage = new Image<Bgra32>(originalWidth, originalHeight);

        for (int i = 0; i < imageList.Count; i++)
        {
            int x = i % 40;
            int y = i / 40;

            // Adjust the position by considering the black border
            int offsetX = x * (tileWidth - 2); // Adjusted for the skipped border
            int offsetY = y * (tileHeight - 2); // Adjusted for the skipped border

            resultImage.Mutate(ctx => ctx.DrawImage(imageList[i].Item2, new Point(offsetX, offsetY), 1f));
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
        return grayscaleImage.CloneAs<L8>();
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


    static double CalculateHistogramCorrelation(int[] histogram1, int[] histogram2)
    {
        // Calculate correlation coefficient
        double mean1 = histogram1.Average();
        double mean2 = histogram2.Average();

        double numerator = 0.0;
        double denominator1 = 0.0;
        double denominator2 = 0.0;

        for (int i = 0; i < histogram1.Length; i++)
        {
            double diff1 = histogram1[i] - mean1;
            double diff2 = histogram2[i] - mean2;

            numerator += diff1 * diff2;
            denominator1 += diff1 * diff1;
            denominator2 += diff2 * diff2;
        }

        double correlation = numerator / Math.Sqrt(denominator1 * denominator2);

        return correlation;
    }
}
