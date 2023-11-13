# NDI Test Pattern Generator

Generates either pseudorandom noise, or a blue fading signal.

Used for testing NDI feeds.

```
NdiTestPatternGenerator 2023.11.13.1
Copyright (C) 2023 Tractus Events by Northern HCI Solutions Inc.

Command Line Options:
  -w, --width     Required. Horizontal resolution in pixels

  -h, --height    Required. Vertical resolution in pixels

  -n, --name      Required. Name of this NDI sender instance

  -f, --fps       (Default: 30) Frames per second.

  --whiteline     (Default: false) Display the sweeping white line

  --mode          (Default: blue) Background mode. Expecting 'blue', 'noise'

  --help          Display this help screen.

  --version       Display version information.

```

### Example Usage

#### Display a fading blue background at 1920x1080

`.\NdiTestPatternGenerator.exe -w 1920 -h 1080 -n "Sender 1"`

#### Display a scrolling white line at 1920x1080

`.\NdiTestPatternGenerator.exe -w 1920 -h 1080 -n "Sender 1" --whiteline`

#### Display a scrolling white line at 1920x1080 with random noise

`.\NdiTestPatternGenerator.exe -w 1920 -h 1080 -n "Sender 1" --whiteline --mode noise`
