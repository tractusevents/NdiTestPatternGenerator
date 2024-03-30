using NewTek;
using NewTek.NDI;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;

public class LaunchOptions
{
    public int Width;
    public int Height;
    public int FrameRate;
    public int AudioChannels;
    public int AudioRate;
    public string SourceName;
    public string MachineName;
    public string Mode;
    public bool WhiteBar;

}

internal class Program
{
    private static LaunchOptions GetLaunchOptions(string[] args)
    {
        var knownMachineName = args.Any(x => x.StartsWith("-m="))
            ? args.First(x => x.StartsWith("-m=")).Split('=')[1]
            : string.Empty;

        var knownSourceName = args.Any(x => x.StartsWith("-s="))
            ? args.First(x => x.StartsWith("-s=")).Split('=')[1]
            : string.Empty;

        var skipMachineName = args.Any(x => x == "-nm");

        var toReturn = new LaunchOptions()
        {
            MachineName = knownMachineName,
            SourceName = knownSourceName,
            AudioChannels = 2,
            AudioRate = 48000,
        };

        var preset = string.Empty;

        if (args.Any(x => x.StartsWith("-p=")))
        {
            preset = args.FirstOrDefault(x => x.StartsWith("-p=")).Split("=")[1];
        }
        else
        {
            Console.Write("Use preset (4k60, 1080p60, 1080p30, 720, or empty for none) >");
            preset = Console.ReadLine();
        }

        if (preset == "4k60")
        {
            toReturn.Width = 3840;
            toReturn.Height = 2160;
            toReturn.FrameRate = 60;
            toReturn.WhiteBar = true;
            toReturn.Mode = "colorbar";
        }
        else if (preset == "4k60b")
        {
            toReturn.Width = 3840;
            toReturn.Height = 2160;
            toReturn.FrameRate = 60;
            toReturn.WhiteBar = true;
            toReturn.Mode = "blue";
        }
        else if (preset == "1080p60")
        {
            toReturn.Width = 1920;
            toReturn.Height = 1080;
            toReturn.FrameRate = 60;
            toReturn.WhiteBar = true;
            toReturn.Mode = "colorbar";
        }
        else if (preset == "1080p30")
        {
            toReturn.Width = 1920;
            toReturn.Height = 1080;
            toReturn.FrameRate = 30;
            toReturn.WhiteBar = true;
            toReturn.Mode = "colorbar";
        }
        else if (preset == "720")
        {
            toReturn.Width = 1280;
            toReturn.Height = 720;
            toReturn.FrameRate = 60;
            toReturn.WhiteBar = true;
            toReturn.Mode = "colorbar";
        }
        else
        {
            var input = string.Empty;
            var videoFrameWidth = 0;
            var videoFrameHeight = 0;
            var frameRate = 0;

            while (!int.TryParse(input, out videoFrameWidth) && videoFrameWidth <= 0)
            {
                Console.Write("Horizontal Resolution > ");
                input = Console.ReadLine();
            }

            input = string.Empty;
            while (!int.TryParse(input, out videoFrameHeight) && videoFrameHeight <= 0)
            {
                Console.Write("Vertical Resolution > ");
                input = Console.ReadLine();
            }

            input = string.Empty;
            while (!int.TryParse(input, out frameRate) && frameRate <= 0)
            {
                Console.Write("FPS > ");
                input = Console.ReadLine();
            }


            input = string.Empty;
            do
            {
                Console.Write("Mode (blue, colorbar) > ");
                input = Console.ReadLine();
            } while (string.IsNullOrEmpty(input));

            var mode = input;
            input = string.Empty;


            Console.Write("White moving bar (Y/N) > ");
            input = Console.ReadLine();

            var movingBar = input?.Contains("y", StringComparison.InvariantCultureIgnoreCase) == true;

            toReturn.Width = videoFrameWidth;
            toReturn.Height = videoFrameHeight;
            toReturn.FrameRate = frameRate;
            toReturn.WhiteBar = movingBar;
            toReturn.Mode = mode;

            input = string.Empty;

            var audioSampleRate = 0;

            while(!int.TryParse(input, out audioSampleRate) && (audioSampleRate <= 11025 || audioSampleRate > 96000))
            {
                Console.Write("Audio sample rate in Hz (44100 to 96000) >");
                input = Console.ReadLine();
            }

            input = string.Empty;
            var channels = 0;

            while (!int.TryParse(input, out channels) && (channels <= 0))
            {
                Console.Write("Audio channel count (1 or more) >");
                input = Console.ReadLine();
            }

            toReturn.AudioChannels = channels;
            toReturn.AudioRate = audioSampleRate;
        }



        if (string.IsNullOrEmpty(knownMachineName) && !skipMachineName)
        {
            Console.Write("Custom Machine Name (Enter to skip) > ");
            var customMachineName = Console.ReadLine();

            if (!string.IsNullOrEmpty(customMachineName))
            {
                var envDetails = $$"""{"ndi": { "machinename": "{{customMachineName}}" } }""";
                File.WriteAllText("ndi-config.v1.json", envDetails);
                var pathToEnv = Path.Combine(Environment.CurrentDirectory, "ndi-config.v1.json");
                Environment.SetEnvironmentVariable("NDI_CONFIG_DIR", Environment.CurrentDirectory);

                Console.WriteLine($"WARNING: Machine name has been overridden to {customMachineName}");
            }

            knownMachineName = customMachineName;
        }


        if (string.IsNullOrEmpty(knownSourceName))
        {
            knownSourceName = string.Empty;
            do
            {
                Console.Write("NDI Source Name > ");
                knownSourceName = Console.ReadLine();
            } while (string.IsNullOrEmpty(knownSourceName));
        }

        toReturn.SourceName = knownSourceName;
        toReturn.MachineName = knownMachineName;

        return toReturn;
    }

    private unsafe static void Main(string[] args)
    {
        var launchOptions = GetLaunchOptions(args);

        var audioChannels = launchOptions.AudioChannels;
        var audioSampleRate = launchOptions.AudioRate;

        using var audioGenerator = new AudioGenerator(audioSampleRate, audioChannels, 440);

        NDIlib.initialize();


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

        var pName = NewTek.NDI.UTF.StringToUtf8(launchOptions.SourceName);

        var senderSettings = new NDIlib.send_create_t
        {
            p_ndi_name = pName,
            clock_video = true,
            clock_audio = false,
            //p_groups = UTF.StringToUtf8("hidden")
        };

        var senderPtr = NDIlib.send_create(ref senderSettings);

        Marshal.FreeHGlobal(pName);

        var useBlue = launchOptions.Mode == "blue";
        var useColorBar = launchOptions.Mode == "colorbar";

        var videoDataBuffer1 = Marshal.AllocHGlobal(4 * launchOptions.Width * launchOptions.Height);
        var videoDataBufferPtr1 = (uint*)videoDataBuffer1.ToPointer();


        var bgVideoFrames = useColorBar ? 1
            : useBlue
                ? 32
                : 4;

        var videoBgFrameBuffer = Marshal.AllocHGlobal(4 * launchOptions.Width * launchOptions.Height * bgVideoFrames);
        var videoBgFrameBufferPtr = (uint*)videoBgFrameBuffer.ToPointer();
        var videoBgFrameIndex = 0;

        if (useColorBar)
        {
            BgHelpers.GenerateColorBarBgFrames(launchOptions, videoBgFrameBufferPtr);
        }
        else if (useBlue)
        {
            BgHelpers.GenerateBlueBgFrames(launchOptions, videoBgFrameBufferPtr, bgVideoFrames);
        }

        NDIlib.video_frame_v2_t videoFrame1 = default(NDIlib.video_frame_v2_t);

        var averageRunTime = 0l;
        var maxRunTime = 0l;

        var stopwatch = new Stopwatch();
        var ndiSendStopWatch = new Stopwatch();
        var maxSendTime = 0l;
        var averageSendTime = 0l;

        var frameSizeBytes = launchOptions.Width * launchOptions.Height * 4;

        var videoPtr = (uint*)videoDataBufferPtr1;

        var r = 0;
        var g = 0;
        var b = 0;

        var whiteLineVerticalX = 0;
        var whiteLineVerticalXDirection = 1;

        var bDirection = 1;
        var frames = 0;

        videoFrame1 = new NDIlib.video_frame_v2_t
        {
            FourCC = NDIlib.FourCC_type_e.FourCC_type_BGRA,
            frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
            frame_rate_N = launchOptions.FrameRate,
            frame_rate_D = 1,
            xres = launchOptions.Width,
            yres = launchOptions.Height,
            picture_aspect_ratio = launchOptions.Width / (float)launchOptions.Height,
            line_stride_in_bytes = 4 * launchOptions.Width,
            p_data = videoDataBuffer1,
            timecode = NDIlib.send_timecode_synthesize,
            p_metadata = nint.Zero,
            timestamp = 0
        };

        var audioFrame = audioGenerator.SineFrame;

        Console.WriteLine($"NDI Signal Generator started. Sender name: {launchOptions.SourceName}.");
        Console.WriteLine($"v2024.3.26.1.");
        Console.WriteLine("Created by Tractus Events - Grab the source code at https://github.com/tractusevents/NdiTestPatternGenerator\r\n");
        Console.WriteLine("Ctrl+C to exit.");



        int videoFrameDirection = 1;
        int framesSentInLastSecond = 0;
        int framesSentInLastSecondReport = 0;
        var lastFrameLoopCalcTime = DateTime.UtcNow;



        while (true)
        {
            stopwatch.Restart();
            frames++;
            framesSentInLastSecond++;

            var now = DateTime.UtcNow;
            var time
                = "Time UTC: " + now.ToString("HH:mm:ss.fff");

            whiteLineVerticalX += whiteLineVerticalXDirection;

            if (whiteLineVerticalX >= launchOptions.Width - 16)
            {
                whiteLineVerticalXDirection = -1;
            }
            else if (whiteLineVerticalX <= 0)
            {
                whiteLineVerticalXDirection = 1;
            }

            Buffer.MemoryCopy(videoBgFrameBufferPtr + (videoBgFrameIndex * (frameSizeBytes / 4)), videoPtr, frameSizeBytes, frameSizeBytes);

            if (frames % 10 == 0 && bgVideoFrames > 1)
            {
                videoBgFrameIndex += videoFrameDirection;

                if (videoBgFrameIndex < 0)
                {
                    videoBgFrameIndex = 0;
                    videoFrameDirection = 1;
                }
                else if (videoBgFrameIndex >= bgVideoFrames)
                {
                    videoBgFrameIndex = bgVideoFrames - 1;
                    videoFrameDirection = -1;
                }

                videoBgFrameIndex %= bgVideoFrames;
            }

            if (launchOptions.WhiteBar)
            {
                for (var y = 0; y < launchOptions.Height; y++)
                {
                    var offset = y * launchOptions.Width + whiteLineVerticalX;

                    for (var x = 0; x < 16; x++)
                    {
                        videoPtr[offset + x] = 0xFFFFFFFF;
                    }
                }
            }



            //for (var y = 0; y < launchOptions.Height; y++)
            //{
            //    for (var x = 0; x < launchOptions.Width; x++)
            //    {
            //        randomState = (randomState + 1) % randomMaxIndex;

            //        var offset = y * launchOptions.Width + x;

            //        // BGRA = ARGB (Endianness?)

            //        if (launchOptions.WhiteBar && x >= whiteLineVerticalX && x <= whiteLineVerticalX + 16)
            //        {
            //            videoPtr[offset] = 0xFFFFFFFF;
            //        }
            //        else if (useNoise)
            //        {
            //            videoPtr[offset] = randomPixelData[randomState];

            //            // Xorshift algo on our random state (which is also the index).
            //            if (offset % 8192 == 0)
            //            {
            //                randomState ^= randomState << 13;
            //                randomState ^= randomState >> 17;
            //                randomState ^= randomState << 5;
            //            }
            //        }
            //        else
            //        {
            //            videoPtr[offset] = 0xFF000000 | (uint)(r << 16) | (uint)(g << 8) | (uint)b;
            //        }
            //    }

            //    // Xorshift algo on our random state (which is also the index).
            //    randomState ^= randomState << 13;
            //    randomState ^= randomState >> 17;
            //    randomState ^= randomState << 5;
            //}

            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 32, time);
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 48, $"Frame {frames}");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 80, $"NDI Sender Name: {launchOptions.SourceName}");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 96, $"Custom Machine Name: {launchOptions.MachineName}");
            stopwatch.Stop();

            averageRunTime += stopwatch.ElapsedMilliseconds;
            averageRunTime = averageRunTime / 2;

            maxRunTime = stopwatch.ElapsedMilliseconds > maxRunTime ? stopwatch.ElapsedMilliseconds : maxRunTime;

            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 128, $"Render time: {stopwatch.ElapsedMilliseconds} ms");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 160 + 16, $"Avg Render time: {averageRunTime} ms");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 192 + 32, $"Max Render time: {maxRunTime} ms");

            if(now.Subtract(lastFrameLoopCalcTime).TotalSeconds >= 1.0) 
            {
                lastFrameLoopCalcTime = now;
                framesSentInLastSecondReport = framesSentInLastSecond;
                framesSentInLastSecond = 0;
            }

            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 192 + 48, $"Frames Sent to NDI Last Second: {framesSentInLastSecondReport}");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 192 + 64, $"NDI Send Time: {ndiSendStopWatch.ElapsedMilliseconds} ms");
            RenderText(launchOptions.Width, width, fontPixelData, videoPtr, 32, 192 + 80, $"Avg: {averageSendTime} ms, Max: {maxSendTime} ms");

            ndiSendStopWatch.Restart();
            NDIlib.send_send_audio_v2(senderPtr, ref audioFrame);

            // This basically acts as our VSYNC as this is a blocking call. Fine.
            NDIlib.send_send_video_v2(senderPtr, ref videoFrame1);
            ndiSendStopWatch.Stop();
            
            averageSendTime += ndiSendStopWatch.ElapsedMilliseconds;
            averageSendTime /= 2;
            maxSendTime = ndiSendStopWatch.ElapsedMilliseconds > maxSendTime ? ndiSendStopWatch.ElapsedMilliseconds : maxSendTime;


        }


        Marshal.FreeHGlobal(videoDataBuffer1);


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