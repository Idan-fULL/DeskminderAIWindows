# DeskminderAI for Windows

A reminder widget that stays on your screen and helps you manage your time efficiently.

## Features

- Create reminders that stay on your screen
- Customizable timer duration
- Name your reminders
- Drag the widget anywhere on your screen
- Minimize to system tray
- Start with Windows option
- Simple and intuitive interface

## System Requirements

- Windows 10 or later
- .NET 6.0 or later

## Installation

### From Windows Store (Recommended)

1. Search for "DeskminderAI" in the Microsoft Store
2. Click "Install" to download and install the application

### Manual Installation

1. Download the latest release from the Releases page
2. Extract the zip file to a location of your choice
3. Run the DeskminderAI.exe file

## Usage

1. Click the "+" button to create a new reminder
2. Drag the slider to set the desired duration for your reminder
3. Enter a name for your reminder and click "Add"
4. Your reminder will appear as a timer in the widget
5. Click the "×" button on a reminder to remove it
6. Click the settings button (⚙) to access application settings

## Settings

- **Start with Windows**: Automatically start the application when Windows starts
- **Start minimized to tray**: Start the application in the system tray instead of showing the window
- **Always on top**: Keep the widget above other windows

## Building from Source

1. Clone the repository
2. Open the solution in Visual Studio 2022 or later
3. Restore NuGet packages
4. Build the solution

### Requirements for Building

- Visual Studio 2022 or later
- .NET 6.0 SDK
- Windows 10 SDK

## Creating an Icon

If you want to generate the application icon from the SVG source:

1. Install Inkscape from [inkscape.org](https://inkscape.org/)
2. Install ImageMagick from [imagemagick.org](https://imagemagick.org/)
3. Run the `Tools/ConvertSvgToIco.ps1` PowerShell script

## Publishing to Windows Store

1. Create a developer account at [Partner Center](https://partner.microsoft.com/dashboard)
2. Create a new app submission
3. Build the package using the Visual Studio Store packaging tools
4. Upload the package to Partner Center
5. Fill in the required information and submit for certification

## License

This project is licensed under the MIT License - see the LICENSE file for details. 