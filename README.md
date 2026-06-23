## Intro

Simple task manager and tracker that's always on your screen.

Fork of [Task Timer Widget](https://github.com/melihcelenk/TaskTimerWidget) by [Melih Çelenk](https://github.com/melihcelenk).

## Features
- **Task Management**: Create, delete, and manage multiple tasks
- **Built-in Timer**: Track elapsed time for each task
- **Day Snapshots**: Navigate day-by-day history
- **Manual Time Adjustment**: Right-click a task to add or subtract time
- **Done Workflow**: Mark tasks done, move them to the bottom, and keep historical continuity
- **Right Edge Docking (AppBar)**: Reserves a right-side workspace strip so maximized windows stay clear of the widget

## 🚀 Getting Started

### Prerequisites
- Windows 10/11 (Build 17763 or later)
- .NET 8.0 Runtime

### Installation

1. Clone or download the project
2. From the repository root, run `.\build.ps1 -Publish` (requires Visual Studio with the .NET desktop / WinUI workload)
3. Launch `.\local-publish\TaskTimerWidgetR.exe`

Alternatively, open `src/TaskTimerWidget/TaskTimerWidget.csproj` in Visual Studio and run with F5.

## 📋 Development

### Building

This is a WinUI 3 desktop app, which requires Visual Studio's MSBuild (the bare
`dotnet` CLI lacks the Appx/PRI packaging tooling). Use the provided helper:

```powershell
# Release build
.\build.ps1

# Self-contained, unpackaged publish to .\local-publish
.\build.ps1 -Publish
```

### Running

Run the published executable directly:

```powershell
.\local-publish\TaskTimerWidgetR.exe
```

Or launch from Visual Studio with F5 (the "TaskTimerWidget (Unpackaged)" profile).

## 🎯 Roadmap

### Fork (Task Timer Widget R) ✅
- [x] Right-edge AppBar docking (reserves a workspace strip)
- [x] Day-based task history
- [x] Stable task IDs, done-state workflow, kebab/context menu
- [x] Daily total time row
- [x] Converted to a plain unpackaged executable (instead of Microsoft Store)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
The original work is Copyright (c) 2025 Melih Çelenk; fork changes are Copyright (c) 2026 Task Timer Widget R contributors.

**Why MIT?** This license allows you to:
- ✅ Use this software for any purpose (personal, commercial, educational)
- ✅ Modify and distribute the code
- ✅ Incorporate it into your own projects
- ✅ Sell software that includes this code

The only requirement is to include the copyright notice and license text.

## 🙏 Acknowledgments

- **Original author**: This project is a fork of [Task Timer Widget](https://github.com/melihcelenk/TaskTimerWidget) by [Melih Çelenk](https://github.com/melihcelenk) for personal use. Huge thanks for the original app and for releasing it under MIT.