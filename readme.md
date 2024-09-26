# Screen Recorder App

This is a simple screen recording application built using C#. It captures the screen at a fixed interval and saves the recorded frames as an animated GIF file. The recording starts when the user clicks a button, and it can be stopped using a global keyboard shortcut.

## Features

- Start recording by clicking the "Start Recording" button.
- Stop recording using the global hotkey `Ctrl + Shift + S`.
- The application window is hidden while recording to prevent it from being captured.
- The recorded frames are saved as a GIF file in the user's `Downloads` folder.
- A debug log is saved in the `Downloads` folder for troubleshooting.

## Requirements

- .NET 6 or later
- [ImageMagick](https://imagemagick.org/) for GIF creation (via the `Magick.NET` library)
- Windows OS (tested on Windows 10/11)

## How It Works

1. The application starts recording when you press the "Start Recording" button.
2. The application window is hidden, and the screen is captured in intervals of 200ms (5 frames per second).
3. You can stop the recording by pressing the global hotkey `Ctrl + Shift + S`. The application will be shown again, and the recording will be saved as a GIF.
