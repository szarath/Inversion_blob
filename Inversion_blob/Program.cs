using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
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
        List<(int, SixLabors.ImageSharp.Image)> imageList = new List<(int, SixLabors.ImageSharp.Image)>();

        for (int i = 1; i <= 1200; i++)
        {
            string localPath = Path.Combine(localDirectory, $"{i}.png");

            // Check if the image is already downloaded
            if (File.Exists(localPath))
            {
                SixLabors.ImageSharp.Image img = SixLabors.ImageSharp.Image.Load(localPath);
                imageList.Add((i, img));
                Console.WriteLine($"Loaded image {i} from local cache");
            }
            else
            {
                string url = $"{baseUrl}({i}).png";
                SixLabors.ImageSharp.Image img = DownloadImage(url);

                // Save the image locally
                img.Save(localPath);

                imageList.Add((i, img));
                Console.WriteLine($"Downloaded and cached image {i} of 1200");
            }
        }

        Console.WriteLine("Download and processing complete. Starting image reconstruction...");

        // Extract tile indices from filenames and sort the images
        imageList.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        // Assuming each tile has the same width and height
        int tileWidth = imageList[0].Item2.Width;
        int tileHeight = imageList[0].Item2.Height;

        // Calculate the dimensions of the original image including the black border
        int originalWidth = 40 * tileWidth;
        int originalHeight = 30 * tileHeight;

        // Stitch images together to reconstruct the original image
        SixLabors.ImageSharp.Image resultImage = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(originalWidth, originalHeight);

        for (int i = 0; i < imageList.Count; i++)
        {
            int x = i % 40;
            int y = i / 40;

            // Skip images with black border (assuming a 1-pixel border on each side)
            if (x == 0 || x == 39 || y == 0 || y == 29)
            {
                continue;
            }

            // Adjust the position by considering the black border
            int offsetX = (x - 1) * (tileWidth - 2); // Adjusted for the skipped border
            int offsetY = (y - 1) * (tileHeight - 2); // Adjusted for the skipped border

            resultImage.Mutate(ctx => ctx.DrawImage(imageList[i].Item2, new Point(offsetX, offsetY), 1f));
        }

        Console.WriteLine("Image reconstruction complete. Saving result image...");

        // Save or display the result image
        resultImage.Save("result.png");

        Console.WriteLine("Result image saved. Press any key to exit.");
        Console.ReadKey();
    }

    static SixLabors.ImageSharp.Image DownloadImage(string url)
    {
        using (var client = new HttpClient())
        {
            var bytes = client.GetByteArrayAsync(url).Result;
            using (var stream = new MemoryStream(bytes))
            {
                return SixLabors.ImageSharp.Image.Load(stream);
            }
        }
    }
}
