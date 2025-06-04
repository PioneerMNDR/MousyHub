
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
    public string ParseCharacterDataDirect(byte[] imageData)
    {
        using var image = Image.Load<Rgba32>(imageData);
        var pngMetaData = image.Metadata.GetPngMetadata();

        foreach (var textChunk in pngMetaData.TextData)
        {
            if (textChunk.Keyword.Equals("ccv3", StringComparison.OrdinalIgnoreCase) ||
                textChunk.Keyword.Equals("chara", StringComparison.OrdinalIgnoreCase))
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(textChunk.Value));
            }
        }
        throw new Exception("Character data not found in image metadata");
    }

 
}