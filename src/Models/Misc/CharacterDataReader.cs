using SharpCompress.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class CharacterDataReader
{

    public async Task<string> ReadCharacterDataAsync(string base64string, string inputFormat = "png")
    {

        // Ensure the format is supported
        if (inputFormat.ToLower() != "png")
        {
            throw new NotSupportedException("Unsupported format");
        }

        byte[] imageBytes = Convert.FromBase64String(base64string);

        // Parse the character data from the image bytes
        return ParseCharacterData(imageBytes, inputFormat);
    }

    private string ParseCharacterData(byte[] imageData, string format)
    {
        if (format.ToLower() != "png")
        {
            throw new NotSupportedException("Unsupported format");
        }

        using (var image = Image.Load<Rgba32>(imageData))
        {
            var pngMetaData = image.Metadata.GetPngMetadata();

            foreach (var textChunk in pngMetaData.TextData)
            {
                if (textChunk.Keyword.Equals("ccv3", StringComparison.OrdinalIgnoreCase))
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(textChunk.Value));
                }
                else if (textChunk.Keyword.Equals("chara", StringComparison.OrdinalIgnoreCase))
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(textChunk.Value));
                }
            }
            throw new Exception("No PNG metadata.");
        }
    }
}