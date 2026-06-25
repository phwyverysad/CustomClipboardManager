# CustomClipboardManager

CustomClipboardManager is a WPF clipboard history app for Windows. It monitors clipboard changes, keeps a searchable history of text, images, files, audio, and other clipboard data, and lets you paste or copy entries back with hotkeys and mouse actions.

## Features

- Clipboard history for text, images, file drops, audio, and other data
- Search and category filters
- Pin items to keep them from being cleared
- Quick paste and copy actions
- Light and dark themes
- Optional startup registration

## Requirements

- Windows 10 or Windows 11
- .NET 10 SDK

## Build

From the project directory:

```powershell
dotnet restore
dotnet build -c Debug
dotnet build -c Release
```

## Run

```powershell
dotnet run -c Debug
```

Or run the built executable directly:

```powershell
.\bin\Debug\net10.0-windows\CustomClipboardManager.exe
```

## Notes

- The app uses a global hotkey to show the clipboard window.
- History data is stored in the user's local application data folder.
- Build outputs under `bin/` and `obj/` are ignored by git.
