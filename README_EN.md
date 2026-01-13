# imgany

[中文](./README.md)

A Windows system tray clipboard image processing tool with auto-save, image hosting upload, and favorite path management.

## Features

### Core Features
- **Clipboard Image Saving**: Copy an image, press `Ctrl+V` in Explorer to save as PNG
- **Image URL Download**: Copy an image URL and paste to download and save automatically
- **Auto Mode**: Monitor clipboard changes and automatically save images to a specified directory

### Image Hosting
- **Lsky Pro Support**: Integrated Lsky Pro API with login authentication and guest upload
- **Upload Only Mode**: Option to upload without saving locally
- **Auto Copy Link**: Image URL is copied to clipboard after successful upload

### Convenience Features
- **Favorite Paths**: Save frequently used directories, quick switch via tray menu
- **Startup on Boot**: Implemented via Windows Task Scheduler
- **Portable Mode**: Config file stored in program directory for USB portability

## System Requirements

- Windows 10/11
- .NET 10.0 Desktop Runtime or higher

> **⚠️ Please install .NET Runtime before first run**  
> Download: [.NET 10.0 Desktop Runtime (Windows x64)](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation

1. Download the latest Release or build from source
2. Extract to any directory
3. Run `imgany.exe`

## Usage

### Basic Operation
1. Program runs in system tray after launch
2. Copy an image or image URL
3. Press `Ctrl+V` in Explorer to save

### Auto Mode
1. Right-click tray icon → Settings → Enable Auto Mode
2. Configure save path
3. Copied images are automatically saved to the specified directory

### Image Host Configuration
1. Check "Enable Image Hosting"
2. Enter host URL (e.g., `https://img.example.com`)
3. Choose authentication method:
   - Guest Upload: No account required (server must support)
   - Account Login: Enter email and password

## Configuration

Program settings are stored in `config.json` in the same directory as the executable.

```json
{
  "FilePrefix": "Img",
  "AutoSave": false,
  "AutoSavePath": "",
  "FavoritePaths": {},
  "UploadToHost": false,
  "UploadHostUrl": "",
  "EnableUploadNotification": true
}
```

## Build

```bash
dotnet build -c Release
```

## Tech Stack

- C# / .NET 10.0
- Windows Forms
- Windows API (Clipboard, Shell)

## License

MIT License
