# Changelog

## v1.0.2 — 2026-02-27
- Fixed GDI handle leak in programmatic tray icon
- Fixed race condition on concurrent update checks (volatile bool → Interlocked)
- Fixed unobserved exception crash in async timer callback
- Fixed mutex ReleaseMutex() called when not the mutex owner
- Fixed log file being read and rewritten on every single output line (now trimmed once per run)
- Added cancellation support — winget process is cancelled cleanly on app exit
- Improved error message when winget is not installed

## v1.0.1 — 2026-02-27
- Fixed garbled characters in log output (winget stdout now read as UTF-8)
- Filtered spinner frames and progress bar lines from the log

## v1.0.0 — 2026-02-27
- Initial release
