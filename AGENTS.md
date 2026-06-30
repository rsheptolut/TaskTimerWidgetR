# Agent Handoff

A small WinUI 3 / .NET 8 Windows desktop app that docks to the right edge of the
screen as an always-visible task timer. This file captures only the things that
are **not obvious from the code** and will save you time or avoid breakage. General
C#/XAML/MVVM work needs no special notes.

## The one rule that will trip you up: how to build

**`dotnet build` / `dotnet publish` do NOT work here.** They fail with a missing
`Microsoft.Build.Packaging.Pri.Tasks.dll`. This WinUI 3 app needs the Appx/PRI
packaging tasks that ship only with **Visual Studio's MSBuild**.

Always build/publish via the helper, which locates VS MSBuild for you:

```powershell
.\build.ps1            # Release build
.\build.ps1 -Publish   # self-contained unpackaged publish -> .\local-publish\TaskTimerWidgetR.exe
```

To smoke-test, run the published `TaskTimerWidgetR.exe` and confirm it **stays
alive for >25s**. If `resources.pri` is missing from output, the app throws
`XamlParseException` and dies after ~30s — the csproj has custom targets that copy
the generated PRI to `resources.pri`; don't remove them.

## Dialogs: never use ContentDialog

The window is narrow and docked, so a `ContentDialog` renders **clipped** inside
that sliver and the user can't see the buttons. Use the standalone centered windows
instead:

- `Views/ConfirmationWindow.cs` — yes/no/cancel style prompts (`ShowAsync(...)`,
  returns `ConfirmationResult`). Buttons are optional; pass `null` to omit.
- `Views/TaskInfoWindow.cs` — read-only info popups.

Both are normal top-level windows centered on the active monitor. Model any new
dialog on these.

## Docking (handle with care)

`Helpers/AppBarDockManager.cs` uses the Win32 **AppBar API** (`SHAppBarMessage`) to
reserve a strip of desktop on the right edge. This affects the whole desktop, so
mistakes here can misposition the window or briefly freeze the shell on
start/stop/resolution-change. Lifecycle is wired in `MainWindow.xaml.cs`
(`_appBarDockManager`, around the show/close/size handlers). Test docking changes by
actually starting and stopping the app, not just compiling.

## Data model (normalized, day-based)

State is one JSON file: `%LOCALAPPDATA%\TaskTimerWidgetR\Data\tasks.json`
(`TaskStoreData`):

- `tasks` — task identities (name, id), keyed by Guid.
- `days[yyyy-MM-dd]` — per-day entries (`DayTaskEntry`: elapsed seconds, running,
  done, order) that reference a task by id.

A "day" rolls over at **4 AM local** (`Helpers/WorkdayClock.cs`), not midnight.
Opening a new day **clones the previous day's tasks** with timers reset
(`DayTaskEntry.CloneForNewDay`) — this continuity is intended; don't "fix" empty new
days by leaving them empty. History (past days) is read-only in the UI.

Flow: `MainViewModel` ⇄ `TaskService` ⇄ `StorageService`. Put persistence logic in
the services, day/selection logic in `MainViewModel`.

## UI structure

`Views/MainWindow.xaml.cs` is large and **code-behind heavy** by design: context
menus, drag-and-drop reordering, and the inline rename/change-time "cards" are all
built in C#, not XAML. There's a custom title bar (the standard one is suppressed).
When adding UI, search this file for an existing similar feature and follow its
pattern rather than introducing a new mechanism.

## Project facts / fork context

- This is a fork: `origin` → `rsheptolut/TaskTimerWidgetR`, `upstream` → the original
  `melihcelenk/TaskTimerWidget`. Keep the original author's attribution (README,
  LICENSE, About).
- Plain **unpackaged exe** — no Microsoft Store / MSIX. Don't reintroduce Store
  manifests, `apps.microsoft` links, or `dotnet`-based packaging.
- Output exe is `TaskTimerWidgetR.exe`, but the **csproj, sln, and root namespace
  stay `TaskTimerWidget`** (only `AssemblyName` was renamed). This split is
  intentional — don't "correct" it.
- `local-publish/` is gitignored. Commit convention: `[TTW-N] Subject` with a
  `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` trailer.
