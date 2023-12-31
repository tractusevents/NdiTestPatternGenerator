﻿using CommandLine;
using NewTek;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SignalGeneratorCommandLineOptions))]
    private static void Main(string[] args)
    {
        var parserResult = Parser.Default.ParseArguments<SignalGeneratorCommandLineOptions>(args);

        if (parserResult.Errors.Any())
        {
            return;
        }

        var videoFrameWidth = parserResult.Value.Width;
        var videoFrameHeight = parserResult.Value.Height;

        // This reads in a 1-bit bitmap. This is so we don't have to deal with TTF nonsense.
        // Thanks, ChatGPT.

        var fileBytes = File.ReadAllBytes("font.bmp");
        var fileOffset = BitConverter.ToInt32(fileBytes, 10);
        var width = BitConverter.ToInt32(fileBytes, 18);
        var height = BitConverter.ToInt32(fileBytes, 22);
        // Calculate the row padding
        var rowSize = (int)Math.Ceiling(width / 8.0);
        var paddedRowSize = rowSize + 3 & ~3; // Rows are padded to multiples of 4 bytes

        var fontPixelData = new byte[width * height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < rowSize; x++)
            {
                var byteValue = fileBytes[fileOffset + y * paddedRowSize + x];
                for (var bit = 0; bit < 8; bit++)
                {
                    if (x * 8 + bit < width) // Avoid overflow on the last byte of the row
                    {
                        var index = (height - 1 - y) * width + x * 8 + bit; // Flip vertically as bitmap rows are stored bottom-to-top
                        fontPixelData[index] = (byte)(byteValue >> 7 - bit & 1);
                    }
                }
            }
        }

        // Font has 16 glyphs per row, 6 rows

        var pName = NewTek.NDI.UTF.StringToUtf8(parserResult.Value.Name);

        var senderSettings = new NDIlib.send_create_t
        {
            p_ndi_name = pName,
            clock_video = true
        };

        var senderPtr = NDIlib.send_create(ref senderSettings);

        Marshal.FreeHGlobal(pName);

        var videoData = Marshal.AllocHGlobal(4 * videoFrameWidth * videoFrameHeight);

        var averageRunTime = 0l;
        var maxRunTime = 0l;

        var stopwatch = new Stopwatch();

        var useNoise = parserResult.Value.Mode == "noise";
        var useBlue = parserResult.Value.Mode == "blue";
        var useColorBar = parserResult.Value.Mode == "colorbar";

        var displayWhiteLine = parserResult.Value.WhiteLine;

        // Calling Random.Next is sloooooooooooow. So we preallocate a random array of uint pixel data.
        var randomMaxIndex = 1024;
        var randomArray = Marshal.AllocHGlobal(randomMaxIndex * 4);
        var randomState = 0;

        unsafe
        {
            var randomNumber = new Random();
            var randomPixelData = (uint*)randomArray;
            for (var i = 0; i < randomMaxIndex; i++)
            {
                randomPixelData[i] = 0xFF000000 | (uint)randomNumber.Next(0xFFFFFF);
            }

            var videoPtr = (uint*)videoData;

            var r = 0;
            var g = 0;
            var b = 0;

            var whiteLineVerticalX = 0;
            var whiteLineVerticalXDirection = 1;

            var bDirection = 1;
            var frames = 0;

            var videoFrame = new NDIlib.video_frame_v2_t
            {
                FourCC = NDIlib.FourCC_type_e.FourCC_type_BGRA,
                frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                frame_rate_N = parserResult.Value.FrameRate,
                frame_rate_D = 1,
                xres = videoFrameWidth,
                yres = videoFrameHeight,
                picture_aspect_ratio = videoFrameWidth / (float)videoFrameHeight,
                line_stride_in_bytes = 4 * videoFrameWidth,
                p_data = videoData,
                timecode = NDIlib.send_timecode_synthesize,
                p_metadata = nint.Zero,
                timestamp = 0
            };

            Console.WriteLine($"NDI Signal Generator started. Sender name: {parserResult.Value.Name}.");
            Console.WriteLine("Created by Tractus Events - Grab the source code at https://github.com/tractusevents/NdiTestPatternGenerator\r\n");
            Console.WriteLine("Ctrl+C to exit.");

            var topTwoThird = videoFrameHeight / 3 * 2;
            var midThird = topTwoThird + (int)(videoFrameHeight / 3 * 0.25);
            var bottomThird = (int)(videoFrameHeight / 3 * 0.75);

            var colorBarLookupTop = new uint[videoFrameWidth];
            var colorBarLookupMid = new uint[videoFrameWidth];
            var colorBarLookupBottom = new uint[videoFrameWidth];

            var seventh = (int)Math.Ceiling(videoFrameWidth / 7.0);
            var eighteenth = (int)Math.Ceiling(videoFrameWidth / 18.0);

            var colorSwatch = 0xFF848484;

            // Top Third
            for (var i = 0; i < videoFrameWidth; i++)
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
            for (var i = 0; i < videoFrameWidth; i++)
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
            for (var i = 0; i < videoFrameWidth; i++)
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


            while (true)
            {
                stopwatch.Restart();
                frames++;

                var time
                    = "Time UTC: " + DateTime.UtcNow.ToString("HH:mm:ss.fff");

                b += bDirection;

                whiteLineVerticalX += whiteLineVerticalXDirection;

                if (whiteLineVerticalX >= videoFrameWidth - 16)
                {
                    whiteLineVerticalXDirection = -1;
                }
                else if (whiteLineVerticalX <= 0)
                {
                    whiteLineVerticalXDirection = 1;
                }


                if (b == 255)
                {
                    bDirection = -1;
                }
                else if (b == 0)
                {
                    bDirection = 1;
                }

                for (var y = 0; y < videoFrameHeight; y++)
                {
                    for (var x = 0; x < videoFrameWidth; x++)
                    {
                        randomState = (randomState + 1) % randomMaxIndex;

                        var offset = y * videoFrameWidth + x;

                        // BGRA = ARGB (Endianness?)

                        if (displayWhiteLine && x >= whiteLineVerticalX && x <= whiteLineVerticalX + 16)
                        {
                            videoPtr[offset] = 0xFFFFFFFF;
                        }
                        else if (useColorBar)
                        {
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
                        else if (useNoise)
                        {
                            videoPtr[offset] = randomPixelData[randomState];

                            // Xorshift algo on our random state (which is also the index).
                            if (offset % 8192 == 0)
                            {
                                randomState ^= randomState << 13;
                                randomState ^= randomState >> 17;
                                randomState ^= randomState << 5;
                            }
                        }
                        else
                        {
                            videoPtr[offset] = 0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b;
                        }
                    }

                    // Xorshift algo on our random state (which is also the index).
                    randomState ^= randomState << 13;
                    randomState ^= randomState >> 17;
                    randomState ^= randomState << 5;
                }



                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 32, time);
                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 48, $"Frame {frames}");
                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 80, $"NDI Sender Name: {parserResult.Value.Name}");
                stopwatch.Stop();

                averageRunTime += stopwatch.ElapsedMilliseconds;
                averageRunTime = averageRunTime / 2;

                maxRunTime = stopwatch.ElapsedMilliseconds > maxRunTime ? stopwatch.ElapsedMilliseconds : maxRunTime;

                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 96, $"Render time: {stopwatch.ElapsedMilliseconds} ms");
                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 96 + 16, $"Avg Render time: {averageRunTime} ms");
                RenderText(videoFrameWidth, width, fontPixelData, videoPtr, 32, 96 + 32, $"Max Render time: {maxRunTime} ms");

                NDIlib.send_send_video_v2(senderPtr, ref videoFrame);
            }
        }

        Marshal.FreeHGlobal(videoData);


        static unsafe void RenderText(
            int videoFrameWidth,
            int width,
            byte[] fontPixelData,
            uint* videoPtr,
            int textX,
            int textY,
            string toPrint)
        {

            for (var i = 0; i < toPrint.Length; i++)
            {
                var charOffset = toPrint[i] - 32;

                if (charOffset > 96 || charOffset < 0)
                {
                    continue;
                }

                var xPosition = textX + 16 * i;

                var lookupRow = charOffset / 16;
                var lookupCol = charOffset % 16;

                for (var yy = 0; yy < 16; yy++)
                {
                    for (var xx = 0; xx < 16; xx++)
                    {
                        var rowIndexDataOffset =
                            (lookupRow * 16 + yy) * width
                            + lookupCol * 16
                            + xx;

                        //rowIndexDataOffset += lookupCol + xx;

                        var pixelData = fontPixelData[rowIndexDataOffset];

                        if (pixelData != 0x1)
                        {
                            continue;
                        }

                        var drawColor = 0xFFFFFFFF;

                        var drawPositionOffset = (textY + yy) * videoFrameWidth + xx + i + textX + xPosition;

                        videoPtr[drawPositionOffset] = drawColor;
                    }
                }
            }
        }
    }
}