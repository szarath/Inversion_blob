using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

class Program
{
    static void Main()
    {
        Console.WriteLine("Downloading and processing images...");

        // Set your base URL
        string baseUrl = "https://inversionrecruitment.blob.core.windows.net/find-the-code/";

        // Download and load images
        List<(int, SixLabors.ImageSharp.Image)> imageList = new List<(int, SixLabors.ImageSharp.Image)>();

        for (int i = 1; i <= 1200; i++)
        {
            string url = $"{baseUrl}({i}).png";
            SixLabors.ImageSharp.Image img = DownloadImage(url);
            imageList.Add((i, img));
            Console.WriteLine($"Downloaded image {i} of 1200");
        }

        Console.WriteLine("Download and processing complete. Starting image reconstruction...");

        // Sort images based on filename or any other logic
        imageList.Sort((x, y) => x.Item1.CompareTo(y.Item1));

        // Assuming each tile has the same width and height
        int tileWidth = imageList[0].Item2.Width;
        int tileHeight = imageList[0].Item2.Height;

        // Calculate the dimensions of the original image
        int originalWidth = 40;
        int originalHeight = 30;

        // Stitch images together to reconstruct the original image
        SixLabors.ImageSharp.Image resultImage = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(originalWidth * tileWidth, originalHeight * tileHeight);

        for (int i = 0; i < imageList.Count; i++)
        {
            int x = i % originalWidth;
            int y = i / originalWidth;

            resultImage.Mutate(ctx => ctx.DrawImage(imageList[i].Item2, new Point(x * tileWidth, y * tileHeight), 1f));
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
