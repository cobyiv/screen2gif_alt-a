# ScreenToGif | Quick Capture — integrated source preview

Upstream 2.43.2, commit `a4d0a67c2131cd048ceec86cd40afc2f1a06f2fd`. Microsoft Public License; see original LICENSE.txt. Windows x64, .NET 9 self-contained publish, FFmpeg 8.1.2 pinned. This is integrated **source**, not a verified EXE. No Windows compilation or UI tests have yet run. The CI workflow is configured to build and run encoder smoke tests, not interactive desktop acceptance tests.

## Install a successful CI artifact

Extract `QuickCapture-win-x64.zip` into a writable folder. Launch `ScreenToGif.exe`. The application starts minimized to a new system-tray icon; double-click the icon for settings. It is unsigned; follow your organization's software approval process. No admin rights or separate stock ScreenToGif installation needed for normal use. Keep all files in the same folder. The original executable name is intentionally preserved to avoid changing upstream assembly/resource references.

## Use

- Ctrl+Alt+R: draw region (minimum 30 DIPs per side), release mouse to start automatically. Escape cancels. Drag must stay within one monitor; the upstream selector is per-monitor.
- MP4 / Ctrl+Alt+M: stop and export silent H.264 CRF 23. GIF / Ctrl+Alt+G: stop and export a 15fps animated GIF. Edit / Ctrl+Alt+E: open original editor. Files save to `%USERPROFILE%\Videos\Captures`, timestamped; file-drop clipboard copying allows paste into compatible apps.
- Recorder shows export presets below native controls when it fits outside the capture region. If it would overlap, toolbar is hidden; use an export hotkey or tray Stop and export (tray interaction may appear in a full-screen capture). No extra countdown in Quick Capture.
- Settings: add/reorder presets (label, format, fps, CRF, max width, GIF colors, audio, shortcut, folder override) and multiple capture hotkeys. Save & register reports conflicts. Test shortcuts checks actual registered keystrokes without recording; end test to resume. Startup is opt-in/out in settings (default on); HKCU Run key points to current EXE path, so rerun after moving the folder. Remove-Startup.cmd unregisters it without deleting captures.
- Audio is not recorded by default. For audio presets, start with tray system-audio/microphone option or an additional capture shortcut configured for that source. The original editor receives image frames, not a synchronized audio track. Audio capture is experimental, separate WASAPI recording; pause invalidates audio export. Audio-enabled output requires continuous matching audio source.
- Light/dark/system theme; base recorder/editor styling remains upstream. Original editor and options accessible from tray. Stock global shortcuts are disabled to avoid competition; native window-local commands remain. Full editor functionality retained, except upstream auto-update/feedback disabled. The custom edition uses a dedicated temp directory and separate appdata settings namespace. Settings.xaml and quickcapture.json travel with the package. No background/concurrent export in this preview.
- Failed encoding falls back to original editor and keeps source frames; a clipboard failure does not delete a successfully saved file. Logs under application folder; source image/audio temporarily stored under `%TEMP%\ScreenToGif-QuickCapture`.

## Build locally on Windows

Install Git and .NET 9 SDK. From repository root in PowerShell 7: `./Build-QuickCapture.ps1`. If local script policy blocks execution, use CI rather than weakening company policy. Build downloads a pinned FFmpeg archive from Gyan's GitHub release, verifies SHA256 against Microsoft WinGet manifest, publishes a self-contained app, retains notices, and runs synthetic video/GIF/audio smoke tests. Output `dist/QuickCapture-win-x64`. A fresh output directory is required.

## GitHub CI and upstream maintenance

Fork https://github.com/NickeManarin/ScreenToGif using your personal or approved organization GitHub account. Clone your fork, create a `quickcapture` branch at the baseline commit, apply the separately delivered git patch, commit and push. See the external setup guide for exact commands. Enable Actions if GitHub disables workflows in forks. The push workflow runs on Windows, publishes the app, runs smoke tests, and uploads a ZIP and SHA256 (14 days). Review logs and manual TEST-PLAN.md before deployment. No automated publishing or signing. For upstream updates fetch upstream, create a review branch, merge or rebase, resolve conflicts, rerun CI and test manually. Do not merge blindly. For public redistribution of FFmpeg and other dependencies, review corresponding-source and notice requirements; included source notice is not a complete legal compliance package. Review .NET 9 support status and upgrade with upstream as needed.
