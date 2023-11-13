using CommandLine;

public class CommandLineOptions
{
    public CommandLineOptions(int width, int height, string name, int frameRate, bool whiteLine, string mode)
    {
        this.Width = width;
        this.Height = height;
        this.Name = name;
        this.FrameRate = frameRate;
        this.WhiteLine = whiteLine;
        this.Mode = mode;
    }

    public CommandLineOptions()
    {
    }

    [Option('w', "width", Required = true, HelpText = "Horizontal resolution in pixels")]
    public int Width { get; set; }

    [Option('h', "height", Required = true, HelpText = "Vertical resolution in pixels")]
    public int Height { get; set; }

    [Option('n', "name", Required = true, HelpText = "Name of this NDI sender instance")]
    public string Name { get; set; }

    [Option('f', "fps", Required = false, HelpText = "Frames per second.", Default = 30)]
    public int FrameRate { get; set; }

    [Option(longName: "whiteline", Required = false, HelpText = "Display the sweeping white line", Default = false)]
    public bool WhiteLine { get; set; }

    [Option(longName: "mode", Required = false, HelpText = "Background mode. Expecting 'blue', 'noise'", Default = "blue")]
    public string Mode { get; set; }

}