# Task Timer Widget R

<p align="center">
  <img src="assets/icons/app-icon-256.png" alt="Task Timer Widget R Logo" width="128" height="128">
</p>

<p align="center">
  <a href="https://opensource.org/licenses/MIT"><img src="https://img.shields.io/badge/License-MIT-yellow.svg" alt="License: MIT"></a>
  <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/platform-Windows%2011-blue.svg" alt="Platform"></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-8.0-purple.svg" alt=".NET"></a>
  <a href="https://microsoft.github.io/microsoft-ui-xaml/"><img src="https://img.shields.io/badge/WinUI-3-brightgreen.svg" alt="WinUI"></a>
</p>

<p align="center">
  <strong>A lightweight, minimal task timer widget for Windows 11.</strong><br>
  Manage your tasks and track time with a simple, distraction-free interface.
</p>

<p align="center">
  <em>A fork of <a href="https://github.com/melihcelenk/TaskTimerWidget">Task Timer Widget</a> by Melih Çelenk, with right-edge AppBar docking and day-based history. Ships as a plain unpackaged Windows executable.</em>
</p>

<p align="center">
  <a href="#-getting-started">Download</a> •
  <a href="#-features">Features</a> •
  <a href="docs/general/DEVELOPMENT.md">Documentation</a> •
  <a href="https://rsheptolut.github.io/TaskTimerWidgetR/PRIVACY_POLICY.html">Privacy Policy</a> •
  <a href="https://github.com/rsheptolut/TaskTimerWidgetR">Repository</a>
</p>

---

## 📸 Screenshots

<p align="center">
  <img src="assets/screenshots/screenshot-1.png" alt="Normal Mode" width="45%">
  <img src="assets/screenshots/screenshot-2.png" alt="Compact Mode" width="45%">
</p>

<p align="center">
  <img src="assets/screenshots/screenshot-3.png" alt="Rename Feature" width="45%">
</p>

<p align="center">
  <em>Left: Normal mode with multiple tasks • Right: Compact mode showing active task only • Bottom: Inline rename feature</em>
</p>

## 🎯 Features

- **Minimal Design**: Widget-sized window that doesn't take up much space
- **Task Management**: Create, delete, and manage multiple tasks
- **Built-in Timer**: Track elapsed time for each task
- **Day Snapshots**: Navigate day-by-day history with a 4:00 AM local day boundary
- **Manual Time Adjustment**: Right-click a task to add or subtract time (-1h, -5m, +5m, +1h)
- **Done Workflow**: Mark tasks done, move them to the bottom, and keep historical continuity
- **Active Task Highlighting**: Yellow highlight shows which task is currently active
- **Pause & Resume**: Click to pause/resume timer on any task
- **Inline Rename**: Right-click to rename tasks in-place
- **Drag & Drop Reorder**: Drag tasks to change their order
- **Compact Mode**: Show only the active task for minimal screen usage
- **Right Edge Docking (AppBar)**: Reserves a right-side workspace strip so maximized windows stay clear of the widget
- **Persistent Storage**: Tasks are saved locally
- **Help & About**: Built-in tips and app information
- **Lightweight**: Minimal resource usage

## 🚀 Getting Started

### Prerequisites
- Windows 10/11 (Build 17763 or later)
- .NET 8.0 Runtime

### Installation

1. Clone or download the project
2. From the repository root, run `.\build.ps1 -Publish` (requires Visual Studio with the .NET desktop / WinUI workload)
3. Launch `.\local-publish\TaskTimerWidgetR.exe`

Alternatively, open `src/TaskTimerWidget/TaskTimerWidget.csproj` in Visual Studio and run with F5.

### Usage

1. **Add Task**: Click the `+` button to add a new task
2. **Name Task**: Enter the task name and press Enter (or click elsewhere to save)
3. **Start Timer**: Click on a task to activate it and start the timer
4. **Pause Timer**: Click the active task again to pause the timer
5. **Switch Tasks**: Click another task to pause the current one and activate the new one
6. **Delete Task**: Click the `✕` button on a task to remove it
7. **Task Actions**: Use right-click or kebab menu (`⋮`) for Rename, Change Time, Mark done, and Delete actions
8. **Navigate Days**: Use bottom arrows to browse previous days and return to today
9. **Add to Today**: From historical days, use context menu action "Add to today"
10. **Reorder Tasks**: Drag tasks up or down to change their order
11. **Compact Mode**: Click the `◧` button in the title bar to show only the active task
12. **Docking**: The widget automatically docks to the right edge and reserves that screen area

## 📁 Project Structure

```
TaskTimerWidget/
├── src/
│   └── TaskTimerWidget/
│       ├── Models/              # Data models (Task, etc.)
│       ├── ViewModels/          # MVVM ViewModels
│       ├── Services/            # Business logic services
│       ├── Helpers/             # Utility classes and converters
│       ├── Views/               # XAML UI files
│       ├── Assets/              # Images and resources
│       ├── App.xaml(.cs)        # Application entry point
│       └── TaskTimerWidget.csproj
├── docs/
│   ├── general/                 # General documentation
│   │   ├── DEVELOPMENT.md       # Development guide
│   │   └── PRIVACY_POLICY.md    # Privacy policy
│   └── WINUI3_WINDOW_DRAGGING.md # WinUI 3 guide
├── legacy/                      # Old prototypes
├── CLAUDE.md                    # Code standards and guidelines
├── LICENSE                      # MIT License
└── README.md                    # This file
```

## 🏗️ Architecture

This project follows the **MVVM (Model-View-ViewModel)** pattern:

- **Models**: Pure data objects (Task.cs)
- **ViewModels**: Business logic and UI state management
- **Views**: XAML UI and code-behind
- **Services**: Data persistence and task management

## 🔧 Technology Stack

- **Framework**: .NET 8.0
- **UI**: WinUI 3
- **Logging**: Serilog
- **DI**: Microsoft.Extensions.DependencyInjection
- **Serialization**: System.Text.Json

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

### Code Standards

Please refer to [CLAUDE.md](./CLAUDE.md) for:
- Naming conventions
- Code style guidelines
- MVVM patterns
- Error handling standards
- Documentation requirements

### Development Guides and Resources

See [docs/general/DEVELOPMENT.md](./docs/general/DEVELOPMENT.md) for detailed development setup and guides, including:
- Setup instructions
- Project structure
- Development workflow
- **[WinUI 3 Custom Window Dragging Guide](./docs/WINUI3_WINDOW_DRAGGING.md)** - Applicable to all C# WinUI 3 projects

## 🎯 Roadmap

### v1.0 ✅
- [x] Core timer functionality
- [x] Task management (create, delete, rename, reorder)
- [x] Persistent storage
- [x] Compact mode
- [x] Always-on-top widget
- [x] Custom fonts and styling
- [x] Time percentage display
- [x] Drag-and-drop reordering

### Fork (Task Timer Widget R) ✅
- [x] Right-edge AppBar docking (reserves a workspace strip)
- [x] Day-based task history with 4:00 AM day boundary
- [x] Stable task IDs, done-state workflow, kebab/context menu
- [x] Daily total time row
- [x] Converted to a plain unpackaged executable (no Microsoft Store / MSIX)
- [x] Local state stored under `%LOCALAPPDATA%\TaskTimerWidgetR`

### v1.1 (Current) ✅
- [x] Manual time adjustment (Change Time)
- [x] Inline Change Time card (same UX as Rename)
- [x] LostFocus save for rename and new task
- [x] Help & About buttons with flyout panels
- [x] Bug fixes (compact mode + change time interaction)

### v2.0 (Planned)
- [ ] Cloud synchronization (OneDrive)
- [ ] Advanced statistics & reporting
- [ ] Multiple themes
- [ ] System tray integration
- [ ] Windows startup option
- [ ] Premium tier

### v3.0+ (Future)
- [ ] Team collaboration features
- [ ] Cross-platform (mobile apps)
- [ ] Integration ecosystem
- [ ] Enterprise features

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
The original work is Copyright (c) 2025 Melih Çelenk; fork changes are Copyright (c) 2026 Task Timer Widget R contributors.

**Why MIT?** This license allows you to:
- ✅ Use this software for any purpose (personal, commercial, educational)
- ✅ Modify and distribute the code
- ✅ Incorporate it into your own projects
- ✅ Sell software that includes this code

The only requirement is to include the copyright notice and license text.

## 👥 Contributing

Contributions are welcome! Please:
1. Follow the code standards in CLAUDE.md
2. Create a feature branch
3. Submit a pull request with description

## 📞 Support

For issues, questions, or suggestions, please:
1. Check existing issues
2. Create a new issue with detailed description
3. Include screenshots/logs if applicable

## 🙏 Acknowledgments

- **Original author**: This project is a fork of [Task Timer Widget](https://github.com/melihcelenk/TaskTimerWidget) by [Melih Çelenk](https://github.com/melihcelenk). Huge thanks for the original app and for releasing it under MIT.
- Built with WinUI 3 and .NET 8.0
- Inspired by Toggl and other time-tracking tools
- Designed for productivity enthusiasts

---

**Version**: 1.1.0
**Last Updated**: June 22, 2026
**Status**: Public Release (fork)
**License**: MIT
