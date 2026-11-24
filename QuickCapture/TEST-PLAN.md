# Acceptance tests

## Tests actually performed in the authoring environment

- Reviewed tagged upstream source excerpts for the native region selection, recorder window, project frames, stop/close routing, tray startup and theme APIs.
- Exercised the equivalent FFmpeg CLI argument patterns with 12 synthetic RGB PNG frames at 321x181 pixels, 83 ms frame durations, plus a synthetic WAV tone. ffprobe confirmed H.264 at 322x182 and 30 fps for silent MP4; GIF at 321x181 and 15 fps; H.264 + AAC for audio MP4. These are command smoke tests only; Windows application code was not executed.
- JSON and archive integrity checked at packaging. PowerShell parsing, patch application to the complete upstream tree, C# compilation, and all Windows behaviors below remain unverified.

## Required Windows gates (all pending)

| Gate | Expected behavior |
|---|---|
| Clean build | Tagged checkout passes every patch anchor; restore, publish, and launch succeed. No separate ScreenToGif or .NET installation required on test machine. |
| First launch | One custom tray icon, no main window, startup registration created; relogin starts minimized. |
| Shortcut registration | Registration status shows success; test mode receives Ctrl+Alt+R without capture. Force collision from another app and confirm readable error; change key and retest. |
| Fresh region | Shortcut opens drag overlay even after previous region; mouse release starts without additional input; Esc cancels. |
| Display scaling | Verify 100%, 125%, 150%, 200%, negative-coordinate monitors, primary/secondary display; cross-monitor behavior may be limited by upstream selection. |
| Toolbar | Preset buttons and Edit sit outside captured area; check narrow regions, wide screens and near screen edges. Large/full-display region falls back to hotkeys if toolbar cannot fit. |
| MP4 | Record 10 seconds at default; click MP4, verify automatic unique file, H.264, 30 fps output, no audio track. |
| GIF | Record moving content; use GIF button, verify animation loops, 15 fps, original size. |
| Edit | Click Edit and confirm original editor loads complete source frames and can use normal exports. |
| Presets | Add/reorder/delete named presets, custom resolution/FPS/quality/folder; confirm after restart and matching export metadata. |
| More shortcuts | Add multiple start/export aliases; verify actual key reception and collision detection. |
| Clipboard | Paste into Explorer and an app supporting file paste, lock clipboard temporarily, ensure saved file is still valid on clipboard error. |
| Audio privacy | Silent start never opens microphone/loopback. Arm system and microphone separately; verify correct track and matching preset. Default MP4 remains silent with either source. Wrong source preset is disabled or rejected. |
| Audio timing | Compare visible/audio clock, silence then audio, long recording, device switch, denied microphone, sleep/resume; verify no capture after pause or exit. |
| Encode errors | Rename FFmpeg, fill disk, deny output folder, interrupt encode. Verify no final corrupt file, original project/editor fallback, useful log. |
| UI theme | Test light, dark, follow system, text contrast, native settings window and toolbar. |
| Portable path | Build and launch under path with spaces, non-ASCII and from writable external drive. Move while exited and relaunch; startup path updates. |
| Shutdown | During export ensure close/discard/exit guard works; verify after completion. Turn off startup; confirm no entry remains. |
| Security | Dependency vulnerabilities, FFmpeg provenance, Authenticode signature, EDR validation and corporate policy review. |

A failed build or test is a blocker to deploying this preview. File a reproducible issue with exact tag, Windows version, DPI, FFmpeg version, logs and steps (redact private paths/content).
