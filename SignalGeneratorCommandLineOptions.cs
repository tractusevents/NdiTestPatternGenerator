using CommandLine;

public class SignalGeneratorCommandLineOptions(int width, int height, string? name, int frameRate, bool whiteLine, string? mode)
{
    [Option('w', "width", Required = true, HelpText = "Horizontal resolution in pixels")]
    public int Width { get; } = width;

    [Option('h', "height", Required = true, HelpText = "Vertical resolution in pixels")]
    public int Height { get; } = height;

    [Option('n', "name", Required = true, HelpText = "Name of this NDI sender instance")]
    public string? Name { get; } = name;

    [Option('f', "fps", Required = false, HelpText = "Frames per second.", Default = 30)]
    public int FrameRate { get; } = frameRate;

    [Option(longName: "whiteline", Required = false, HelpText = "Display the sweeping white line", Default = false)]
    public bool WhiteLine { get; } = whiteLine;

    [Option(longName: "mode", Required = false, HelpText = "Background mode. Expecting 'blue', 'noise', 'colorbar'", Default = "blue")]
    public string? Mode { get; } = mode;

}