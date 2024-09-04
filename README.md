# NDI Test Pattern Generator

Generates either a set of color bars, or a blue fading signal at any resolution and frame rate (limited by your PC's capabilities).

Also sends out a 440Hz tone across an arbitrary number of audio channels at a user-selectable sample rate.

Used for testing NDI feeds.

At startup, you can either specify a preset with the `-p=` parameter, or specify custom settings.

#### Built-In Presets

Preset Flag|Details
---|----
4k60|3840x2160 @ 60 FPS, 2 audio channels, 48kHz audio - color bars
4k60b|3840x2160 @ 60 FPS, 2 audio channels, 48kHz audio - blue background
1080p60|1920x1080 @ 60 FPS, 2 audio channels, 48kHz audio - color bars
1080p59|1920x1080 @ 59.94 FPS, 2 audio channels, 48kHz audio - color bars
1080p30|1920x1080 @ 30 FPS, 2 audio channels, 48kHz audio - color bars
1080p29|1920x1080 @ 29.97 FPS, 2 audio channels, 48kHz audio - color bars
720|1280x720 @ 60 FPS, 2 audio channels, 48kHz audio - color bars

### How to Use

Download the latest release from the [releases page](https://github.com/tractusevents/NdiTestPatternGenerator/releases/). Extract the ZIP file, then run `NdiTestPatternGenerator.exe`.
