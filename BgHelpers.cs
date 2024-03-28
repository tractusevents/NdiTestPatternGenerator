
/// <summary>
/// Helper methods for generating static backgrounds. This is used to pre-compute
/// backgrounds to be displayed instead of generating each background on the fly
/// (which takes HUGE amounts of CPU cycles in software).
/// 
/// On a Ryzen 7 3800X, trying to do 4k60 in software isn't doable with a pixel-by-pixel
/// approach. (This is slow AF anyway).
/// </summary>
public class BgHelpers
{
    public unsafe static void GenerateBlueBgFrames(LaunchOptions launchOptions, uint* videoPtr, int frameCount)
    {
        var frameSizeBytes = launchOptions.Width * launchOptions.Height;

        for (var i = 0; i < frameCount; i++)
        {
            var b = (int)(255 * (i / (float)frameCount));

            for (long offset = 0; offset < frameSizeBytes; offset++)
            {
                videoPtr[offset + (frameSizeBytes * i)] = 0xFF000000 | (uint)(0 << 16) | (uint)(0 << 8) | (uint)b;
            }
        }
    }

    public unsafe static void GenerateColorBarBgFrames(LaunchOptions launchOptions, uint* videoPtr)
    {
        var topTwoThird = launchOptions.Height / 3 * 2;
        var midThird = topTwoThird + (int)(launchOptions.Height / 3 * 0.25);
        var bottomThird = (int)(launchOptions.Height / 3 * 0.75);

        var colorBarLookupTop = new uint[launchOptions.Width];
        var colorBarLookupMid = new uint[launchOptions.Width];
        var colorBarLookupBottom = new uint[launchOptions.Width];

        var seventh = (int)Math.Ceiling(launchOptions.Width / 7.0);
        var eighteenth = (int)Math.Ceiling(launchOptions.Width / 18.0);

        var colorSwatch = 0xFF848484;

        // Top Third
        for (var i = 0; i < launchOptions.Width; i++)
        {
            colorBarLookupTop[i] = colorSwatch;
            if (i > 0 && i % seventh == 0)
            {
                // Switch color
                var colorSwatchNumber = i / seventh;

                switch (colorSwatchNumber)
                {
                    case 0:
                        colorSwatch = 0x848484;
                        break;
                    case 1:
                        colorSwatch = 0x848410;
                        break;
                    case 2:
                        colorSwatch = 0x108484;
                        break;
                    case 3:
                        colorSwatch = 0x108410;
                        break;
                    case 4:
                        colorSwatch = 0x841084;
                        break;
                    case 5:
                        colorSwatch = 0x841010;
                        break;
                    case 6:
                        colorSwatch = 0x101084;
                        break;
                    default:
                        break;
                }

                colorSwatch = colorSwatch | 0xFF000000;
            }
        }

        colorSwatch = 0xFF101084;
        // Mid third
        for (var i = 0; i < launchOptions.Width; i++)
        {
            colorBarLookupMid[i] = colorSwatch;
            if (i > 0 && i % seventh == 0)
            {
                // Switch color
                var colorSwatchNumber = i / seventh;

                switch (colorSwatchNumber)
                {
                    case 0:
                        colorSwatch = 0x101084;
                        break;
                    case 1:
                        colorSwatch = 0x101010;
                        break;
                    case 2:
                        colorSwatch = 0x841084;
                        break;
                    case 3:
                        colorSwatch = 0x101010;
                        break;
                    case 4:
                        colorSwatch = 0x108484;
                        break;
                    case 5:
                        colorSwatch = 0x101010;
                        break;
                    case 6:
                        colorSwatch = 0x848484;
                        break;
                    default:
                        break;
                }

                colorSwatch = colorSwatch | 0xFF000000;
            }
        }

        colorSwatch = 0xFF10466A;

        // Lower third
        for (var i = 0; i < launchOptions.Width; i++)
        {
            colorBarLookupBottom[i] = colorSwatch;
            if (i > 0 && i % eighteenth == 0)
            {
                // Switch color
                var colorSwatchNumber = i / eighteenth;

                switch (colorSwatchNumber)
                {
                    case 0:
                    case 1:
                    case 2:
                        colorSwatch = 0x10466A;
                        break;
                    case 3:
                    case 4:
                    case 5:
                        colorSwatch = 0xEBEBEB;
                        break;
                    case 6:
                    case 7:
                    case 8:
                        colorSwatch = 0x481076;
                        break;
                    case 9:
                    case 10:
                    case 11:
                        colorSwatch = 0x101010;
                        break;
                    case 12:
                        colorSwatch = 0x0;
                        break;
                    case 13:
                        colorSwatch = 0x101010;
                        break;
                    case 14:
                        colorSwatch = 0x1A1A1A;
                        break;
                    default:
                        colorSwatch = 0x101010;
                        break;
                }

                colorSwatch = colorSwatch | 0xFF000000;
            }
        }

        for (var y = 0; y < launchOptions.Height; y++)
        {
            for (var x = 0; x < launchOptions.Width; x++)
            {
                var offset = y * launchOptions.Width + x;

                // BGRA = ARGB (Endianness?)

                if (y <= topTwoThird)
                {
                    videoPtr[offset] = colorBarLookupTop[x];
                }
                else if (y <= midThird)
                {
                    videoPtr[offset] = colorBarLookupMid[x];
                }
                else
                {
                    videoPtr[offset] = colorBarLookupBottom[x];
                    // Bottom third.
                }
            }
        }

    }
}
