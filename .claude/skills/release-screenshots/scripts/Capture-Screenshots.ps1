<#
.SYNOPSIS
    Recaptures docs/screenshots/*.png from a live run of the Widget app, seeded with the
    fixed demo dataset in docs/dummy notes/.

.DESCRIPTION
    This script is Windows-only and must be run on a machine with the repo checked out and
    the .NET SDK installed. It:
      1. Stops any running StickyDo.Widget process.
      2. Backs up the current %LocalAppData%\DefineStack\StickyDo.Debug\Data folder.
      3. Replaces it with the 8 items from docs/dummy notes/ (forcing IsOpened=false on all
         of them so the app boots to the main list view instead of restoring floating notes).
      4. Builds the Widget app (Debug).
      5. Launches it and drives it via Win32 window automation (no UI Automation framework
         needed - see winhelpers below) to reproduce each of the 10 marketing screenshots.
      6. Restores the original StickyDo.Debug data untouched, even if a step fails.

    Screen coordinates below are relative offsets discovered empirically against the app's
    actual XAML layout (see docs/screenshots regeneration session). They assume:
      - 100% display scaling (96 DPI). The script aborts early if that's not the case.
      - The main window is opened at a fixed 1200x900 size - matches the shipped screenshots.
      - The three "hero" notes (Wi-Fi & House Info / Weekly Groceries / Sprint Bug Fixes)
        keep the WindowLeft/WindowTop baked into their dummy-notes JSON (80,120 / 440,120 /
        800,120) - the hero shot crops screen region (0,0)-(1300,700) around them, so nothing
        else should be relying on that region of the screen while this runs.

    If the app's XAML layout changes (sidebar icon positions, card grid, footer toolbar),
    the click coordinates in this script will need updating to match.
#>

[CmdletBinding()]
param(
    [switch]$KeepAppOpen
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Limits
# ---------------------------------------------------------------------------
# Same 204,800-byte (200KB) ceiling MSBuild enforces (APPX3207) for the MSIX
# package's own tile images (src/StickyDo.Widget/StickyDo.Widget.Package/Images).
# These Store *listing* screenshots aren't part of that manifest and aren't
# actually checked by the build, but we hold them to the same cap so the whole
# image pipeline has one consistent ceiling rather than two silently different
# ones.
$MaxImageBytes = 204800

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
$DummyNotesDir = Join-Path $RepoRoot "docs\dummy notes"
$ScreenshotsDir = Join-Path $RepoRoot "docs\screenshots"
$WidgetProj = Join-Path $RepoRoot "src\StickyDo.Widget\StickyDo.Widget\StickyDo.Widget.csproj"
$WidgetBinRoot = Join-Path $RepoRoot "src\StickyDo.Widget\StickyDo.Widget\bin\Debug"
$DebugDataDir = Join-Path $env:LOCALAPPDATA "DefineStack\StickyDo.Debug\Data"

if (-not (Test-Path $DummyNotesDir)) {
    throw "Dummy notes folder not found: $DummyNotesDir"
}
if (-not (Test-Path $WidgetProj)) {
    throw "Widget project not found: $WidgetProj"
}

# ---------------------------------------------------------------------------
# Win32 / capture helpers
# ---------------------------------------------------------------------------
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class RelWin {
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr hWnd);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    [DllImport("user32.dll")] public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string lclassName, string windowTitle);
    public struct RECT { public int Left, Top, Right, Bottom; }
    public class WinInfo { public IntPtr Handle; public string Title; public string ClassName; public RECT Rect; }
    public static List<WinInfo> GetProcessWindows(uint pidFilter) {
        var results = new List<WinInfo>();
        EnumWindows((hWnd, lParam) => {
            uint pid; GetWindowThreadProcessId(hWnd, out pid);
            if (pid == pidFilter && IsWindowVisible(hWnd)) {
                int length = GetWindowTextLength(hWnd);
                var sb = new StringBuilder(length + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                var cls = new StringBuilder(256);
                GetClassName(hWnd, cls, cls.Capacity);
                RECT r; GetWindowRect(hWnd, out r);
                results.Add(new WinInfo { Handle = hWnd, Title = sb.ToString(), ClassName = cls.ToString(), Rect = r });
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }
    public static List<WinInfo> GetAllTopLevelWindows() {
        var results = new List<WinInfo>();
        EnumWindows((hWnd, lParam) => {
            if (IsWindowVisible(hWnd)) {
                int length = GetWindowTextLength(hWnd);
                if (length > 0) {
                    var sb = new StringBuilder(length + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    var cls = new StringBuilder(256);
                    GetClassName(hWnd, cls, cls.Capacity);
                    RECT r; GetWindowRect(hWnd, out r);
                    results.Add(new WinInfo { Handle = hWnd, Title = sb.ToString(), ClassName = cls.ToString(), Rect = r });
                }
            }
            return true;
        }, IntPtr.Zero);
        return results;
    }
    public static IntPtr GetDesktopIconsHandle() {
        IntPtr progman = FindWindow("Progman", "Program Manager");
        IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (defView == IntPtr.Zero) {
            IntPtr worker = IntPtr.Zero;
            do {
                worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null);
                if (worker != IntPtr.Zero) defView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
            } while (worker != IntPtr.Zero && defView == IntPtr.Zero);
        }
        if (defView == IntPtr.Zero) return IntPtr.Zero;
        return FindWindowEx(defView, IntPtr.Zero, "SysListView32", "FolderView");
    }
}
"@
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Move-AppWindow($hwnd, $x, $y, $w, $h) {
    # HWND_TOP (IntPtr 0), flags=SWP_SHOWWINDOW(0x40) so this both raises z-order and shows it -
    # SWP_NOZORDER must NOT be set here, or the window keeps whatever z-order it already had,
    # which is how earlier iterations of this script ended up capturing whatever else was on
    # screen (this app's own terminal window) instead of the sticky note.
    [RelWin]::ShowWindow([IntPtr]$hwnd, 9) | Out-Null  # SW_RESTORE
    [RelWin]::SetForegroundWindow([IntPtr]$hwnd) | Out-Null
    [RelWin]::SetWindowPos([IntPtr]$hwnd, [IntPtr]::Zero, $x, $y, $w, $h, 0x0040) | Out-Null
    Start-Sleep -Milliseconds 400
}

function Capture-Region($x, $y, $w, $h, $path) {
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($x, $y, 0, 0, (New-Object System.Drawing.Size $w, $h))
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
}

function Click-At($x, $y) {
    [RelWin]::SetCursorPos($x, $y) | Out-Null
    Start-Sleep -Milliseconds 100
    [RelWin]::mouse_event(0x0002, 0, 0, 0, [IntPtr]::Zero) # left down
    Start-Sleep -Milliseconds 60
    [RelWin]::mouse_event(0x0004, 0, 0, 0, [IntPtr]::Zero) # left up
    Start-Sleep -Milliseconds 300
}

function Type-Text($text) { [System.Windows.Forms.SendKeys]::SendWait($text) }
function Press-Key($key) { [System.Windows.Forms.SendKeys]::SendWait($key) }

function Get-ProcessWindows($procId) { [RelWin]::GetProcessWindows($procId) }

function Test-RectOverlap($rect, $x, $y, $w, $h) {
    $overlapsX = ($rect.Left -lt ($x + $w)) -and ($rect.Right -gt $x)
    $overlapsY = ($rect.Top -lt ($y + $h)) -and ($rect.Bottom -gt $y)
    return ($overlapsX -and $overlapsY)
}

function Hide-WindowsInRegion($x, $y, $w, $h, $excludeHandles) {
    # Minimizes (not closes) any *other* top-level window whose rect overlaps the given
    # screen region, so the hero screenshot's desktop backdrop isn't covered by whatever the
    # user happened to have open (File Explorer, a browser, etc.) at that screen location.
    # Returns the list of handles it minimized, so the caller can restore them afterward.
    $excludeSet = @($excludeHandles | ForEach-Object { $_.ToInt64() })
    $skipClasses = @("Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd", "Windows.UI.Core.CoreWindow")
    $all = [RelWin]::GetAllTopLevelWindows()
    $toMinimize = $all | Where-Object {
        ($excludeSet -notcontains $_.Handle.ToInt64()) -and
        ($skipClasses -notcontains $_.ClassName) -and
        (Test-RectOverlap $_.Rect $x $y $w $h)
    }
    foreach ($w2 in $toMinimize) {
        [RelWin]::ShowWindow($w2.Handle, 6) | Out-Null  # SW_MINIMIZE
    }
    Start-Sleep -Milliseconds 300
    return @($toMinimize | ForEach-Object { $_.Handle })
}

function Restore-Windows($handles) {
    foreach ($h in $handles) {
        [RelWin]::ShowWindow($h, 9) | Out-Null  # SW_RESTORE
    }
}

function Wait-ForNewNoteWindow($procId, $before, $timeoutMs) {
    # $before is a snapshot (List<WinInfo>) taken via Get-ProcessWindows before the click that
    # opens the note; compare by Handle value, not by object reference.
    $beforeHandles = @($before | ForEach-Object { $_.Handle.ToInt64() })
    $elapsed = 0
    while ($elapsed -lt $timeoutMs) {
        $after = Get-ProcessWindows $procId
        $new = $after | Where-Object { $_.Title -eq "Sticky Note" -and ($beforeHandles -notcontains $_.Handle.ToInt64()) }
        if ($new) { return $new[0] }
        Start-Sleep -Milliseconds 150
        $elapsed += 150
    }
    return $null
}

function Open-NoteCard($procId, $x, $y) {
    # Clicking a card's "open" chevron occasionally doesn't register (e.g. the card grid is
    # still settling right after a view switch) - retry once with a longer wait rather than
    # failing the whole run over one missed click.
    $before = Get-ProcessWindows $procId
    Click-At $x $y
    $win = Wait-ForNewNoteWindow $procId $before 3000
    if ($win) { return $win }
    Write-Host "  (no new note window yet, retrying click...)"
    Click-At $x $y
    $win = Wait-ForNewNoteWindow $procId $before 7000
    if (-not $win) { throw "Timed out waiting for a new Sticky Note window to appear after retry." }
    return $win
}

# ---------------------------------------------------------------------------
# DPI sanity check - the click coordinates below assume 100% scaling
# ---------------------------------------------------------------------------
$g = [System.Drawing.Graphics]::FromHwnd([System.IntPtr]::Zero)
$dpi = $g.DpiX
$g.Dispose()
if ($dpi -ne 96) {
    throw "Display scaling is $($dpi / 96 * 100)%, but this script's click coordinates assume 100% (96 DPI). Set display scaling to 100% and retry."
}

# ---------------------------------------------------------------------------
# 1. Stop any running instance
# ---------------------------------------------------------------------------
Write-Host "Stopping any running StickyDo.Widget process..."
Get-Process StickyDo.Widget -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

# ---------------------------------------------------------------------------
# 2. Backup current debug data
# ---------------------------------------------------------------------------
$BackupDir = Join-Path $env:TEMP "StickyDo.Debug.Data.Backup.$(Get-Date -Format yyyyMMddHHmmss)"
$didBackup = $false
if (Test-Path $DebugDataDir) {
    Write-Host "Backing up current debug data to $BackupDir"
    Copy-Item $DebugDataDir $BackupDir -Recurse
    $didBackup = $true
} else {
    New-Item -ItemType Directory -Path $DebugDataDir -Force | Out-Null
}

$minimizedWindows = @()

try {
    # -----------------------------------------------------------------------
    # 3. Seed with dummy notes, forcing IsOpened=false so the app boots to
    #    the main list view (floating windows are opened later via clicks).
    # -----------------------------------------------------------------------
    Write-Host "Seeding debug data from docs\dummy notes ..."
    Get-ChildItem $DebugDataDir -File | Remove-Item -Force
    Get-ChildItem $DummyNotesDir -Filter *.json | ForEach-Object {
        # Plain text substitution rather than ConvertFrom-Json/ConvertTo-Json: Windows
        # PowerShell 5.1 silently collapses single-element JSON arrays on round-trip, which
        # would corrupt a note's Tasks/Spans if one ever has exactly one entry.
        $raw = Get-Content $_.FullName -Raw
        $raw = $raw -replace '"IsOpened"\s*:\s*true', '"IsOpened": false'
        $destPath = Join-Path $DebugDataDir $_.Name
        Set-Content -Path $destPath -Value $raw -Encoding utf8 -NoNewline
    }

    # -----------------------------------------------------------------------
    # 4. Build
    # -----------------------------------------------------------------------
    Write-Host "Building StickyDo.Widget (Debug)..."
    & dotnet build $WidgetProj -c Debug | Write-Host
    if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

    $exe = Get-ChildItem $WidgetBinRoot -Recurse -Filter StickyDo.Widget.exe |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $exe) { throw "Could not find StickyDo.Widget.exe under $WidgetBinRoot" }

    # -----------------------------------------------------------------------
    # 5. Launch
    # -----------------------------------------------------------------------
    Write-Host "Launching $($exe.FullName) ..."
    $proc = Start-Process -FilePath $exe.FullName -PassThru -WorkingDirectory $exe.DirectoryName
    $mainHwnd = $null
    $elapsed = 0
    while ($elapsed -lt 10000) {
        $proc.Refresh()
        if ($proc.MainWindowHandle -ne 0 -and $proc.MainWindowTitle -eq "StickyDo") {
            $mainHwnd = $proc.MainWindowHandle
            break
        }
        Start-Sleep -Milliseconds 300
        $elapsed += 300
    }
    if (-not $mainHwnd) { throw "Timed out waiting for the main StickyDo window to appear." }

    if (-not (Test-Path $ScreenshotsDir)) { New-Item -ItemType Directory -Path $ScreenshotsDir -Force | Out-Null }

    # Fixed capture rect for the main window - matches the shipped screenshot dimensions (1200x900).
    $MX = 100; $MY = 100; $MW = 1200; $MH = 900

    # Sidebar icon offsets (local to main window top-left)
    $IconTodos = @(32, 111)
    $IconNotes = @(32, 161)
    $IconFavorites = @(32, 214)
    $IconSettings = @(32, 266)
    $SearchBox = @(594, 29)
    $SettingsCloseX = @(1150, 103)

    # Card "open" chevron offsets (local to main window), one per grid column
    $Card1 = @(198, 163)
    $Card2 = @(438, 163)
    $Card3 = @(678, 163)
    $Card4 = @(918, 163)

    function Screen($local) {
        # NOTE: do not collapse this to `return @($MX + $local[0], $MY + $local[1])` - PowerShell
        # mis-parses `+` combined with `,` inside an @() array subexpression (a known parser
        # quirk) and throws "System.Object[] does not contain a method named 'op_Addition'" at
        # runtime. Computing each element as its own statement first sidesteps it.
        $sx = $MX + $local[0]
        $sy = $MY + $local[1]
        return @($sx, $sy)
    }

    # -----------------------------------------------------------------------
    # 6. 01 - Todos list (default view on launch)
    # -----------------------------------------------------------------------
    Write-Host "Capturing 01-todos-list.png"
    Move-AppWindow $mainHwnd $MX $MY $MW $MH
    Capture-Region $MX $MY $MW $MH (Join-Path $ScreenshotsDir "01-todos-list.png")

    # -----------------------------------------------------------------------
    # 7. 02 - Notes list
    # -----------------------------------------------------------------------
    Write-Host "Capturing 02-notes-list-multicolor.png"
    $pt = Screen $IconNotes
    Click-At $pt[0] $pt[1]
    Capture-Region $MX $MY $MW $MH (Join-Path $ScreenshotsDir "02-notes-list-multicolor.png")

    # -----------------------------------------------------------------------
    # 8. 03 - Favorites
    # -----------------------------------------------------------------------
    Write-Host "Capturing 03-favorites.png"
    $pt = Screen $IconFavorites
    Click-At $pt[0] $pt[1]
    Capture-Region $MX $MY $MW $MH (Join-Path $ScreenshotsDir "03-favorites.png")

    # -----------------------------------------------------------------------
    # 9. 04 - Search ("trip") within Favorites
    # -----------------------------------------------------------------------
    Write-Host "Capturing 04-search.png"
    $pt = Screen $SearchBox
    Click-At $pt[0] $pt[1]
    Type-Text "trip"
    Start-Sleep -Milliseconds 400
    Capture-Region $MX $MY $MW $MH (Join-Path $ScreenshotsDir "04-search.png")

    # Clear the search box before moving on
    Press-Key "^a"
    Press-Key "{DEL}"
    Start-Sleep -Milliseconds 200

    # -----------------------------------------------------------------------
    # 10. 09 - Settings
    # -----------------------------------------------------------------------
    Write-Host "Capturing 09-settings.png"
    $pt = Screen $IconSettings
    Click-At $pt[0] $pt[1]
    Start-Sleep -Milliseconds 400
    Capture-Region $MX $MY $MW $MH (Join-Path $ScreenshotsDir "09-settings.png")

    # Close settings overlay
    $pt = Screen $SettingsCloseX
    Click-At $pt[0] $pt[1]
    Start-Sleep -Milliseconds 300

    # -----------------------------------------------------------------------
    # 11. 05 - Wi-Fi & House Info floating note (formatted text)
    # -----------------------------------------------------------------------
    Write-Host "Capturing 05-floating-note-formatted.png"
    $pt = Screen $IconNotes
    Click-At $pt[0] $pt[1]
    Start-Sleep -Milliseconds 300
    $pt = Screen $Card1  # Wi-Fi & House Info is the first card in Notes view
    $wifiWin = Open-NoteCard $proc.Id $pt[0] $pt[1]
    Move-AppWindow $wifiWin.Handle 80 120 320 400
    Capture-Region 80 120 320 400 (Join-Path $ScreenshotsDir "05-floating-note-formatted.png")

    # -----------------------------------------------------------------------
    # 12. 06 - Weekly Groceries floating todo
    # -----------------------------------------------------------------------
    Write-Host "Capturing 06-floating-todo.png"
    Move-AppWindow $mainHwnd $MX $MY $MW $MH
    $pt = Screen $IconTodos
    Click-At $pt[0] $pt[1]
    Start-Sleep -Milliseconds 300
    $pt = Screen $Card4  # Weekly Groceries is the last card in Todos view
    $groceriesWin = Open-NoteCard $proc.Id $pt[0] $pt[1]
    Move-AppWindow $groceriesWin.Handle 440 120 320 400
    Capture-Region 440 120 320 400 (Join-Path $ScreenshotsDir "06-floating-todo.png")

    # -----------------------------------------------------------------------
    # 13. 07 - Sprint Bug Fixes floating todo (favorited)
    # -----------------------------------------------------------------------
    Write-Host "Capturing 07-floating-todo-favorite.png"
    Move-AppWindow $mainHwnd $MX $MY $MW $MH
    $pt = Screen $Card1  # Sprint Bug Fixes is the first card in Todos view
    $sprintWin = Open-NoteCard $proc.Id $pt[0] $pt[1]
    Move-AppWindow $sprintWin.Handle 800 120 320 400
    Capture-Region 800 120 320 400 (Join-Path $ScreenshotsDir "07-floating-todo-favorite.png")

    # -----------------------------------------------------------------------
    # 14. 08 - Color picker open on Sprint Bug Fixes
    # -----------------------------------------------------------------------
    Write-Host "Capturing 08-color-picker.png"
    Move-AppWindow $sprintWin.Handle 800 120 320 400
    Click-At 1062 500  # palette icon in the note footer toolbar (local 262,380)
    Start-Sleep -Milliseconds 500
    Capture-Region 800 120 320 400 (Join-Path $ScreenshotsDir "08-color-picker.png")
    Press-Key "{ESC}"
    Start-Sleep -Milliseconds 300

    # -----------------------------------------------------------------------
    # 15. 10 - Desktop hero: all three notes over the wallpaper, no desktop
    #     icons, no main window.
    # -----------------------------------------------------------------------
    Write-Host "Capturing 10-desktop-hero.png"
    [RelWin]::ShowWindow([IntPtr]$mainHwnd, 6) | Out-Null  # SW_MINIMIZE
    Start-Sleep -Milliseconds 300
    $iconsHandle = [RelWin]::GetDesktopIconsHandle()
    if ($iconsHandle -ne [IntPtr]::Zero) { [RelWin]::ShowWindow($iconsHandle, 0) | Out-Null } # SW_HIDE
    # Anything else (File Explorer, a browser, ...) sitting in the hero crop region would show
    # through the gaps between/around the three note windows, so clear it out first.
    $minimizedWindows = Hide-WindowsInRegion 0 0 1300 700 @($wifiWin.Handle, $groceriesWin.Handle, $sprintWin.Handle, $mainHwnd)
    Move-AppWindow $wifiWin.Handle 80 120 320 400
    Move-AppWindow $groceriesWin.Handle 440 120 320 400
    Move-AppWindow $sprintWin.Handle 800 120 320 400
    Capture-Region 0 0 1300 700 (Join-Path $ScreenshotsDir "10-desktop-hero.png")
    if ($iconsHandle -ne [IntPtr]::Zero) { [RelWin]::ShowWindow($iconsHandle, 5) | Out-Null } # SW_SHOW
    Restore-Windows $minimizedWindows
    $minimizedWindows = @()

    Write-Host "All 10 screenshots captured into $ScreenshotsDir"

    # -----------------------------------------------------------------------
    # 17. Size check - flag anything over the shared 200KB ceiling so it gets
    #     recompressed before committing, rather than discovered later.
    # -----------------------------------------------------------------------
    Write-Host ""
    Write-Host "Checking file sizes against the $MaxImageBytes byte limit..."
    $oversized = @()
    Get-ChildItem $ScreenshotsDir -Filter *.png | Sort-Object Name | ForEach-Object {
        $status = if ($_.Length -gt $MaxImageBytes) { "OVER LIMIT" } else { "ok" }
        Write-Host ("  {0,-32} {1,10:N0} bytes  [{2}]" -f $_.Name, $_.Length, $status)
        if ($_.Length -gt $MaxImageBytes) { $oversized += $_.Name }
    }
    if ($oversized.Count -gt 0) {
        Write-Warning "$($oversized.Count) screenshot(s) exceed the $MaxImageBytes byte limit: $($oversized -join ', ')`nRecompress these losslessly (same dimensions/alpha) before committing - see SKILL.md."
    }
}
finally {
    # -----------------------------------------------------------------------
    # 16. Cleanup: always stop the app, un-minimize anything step 15 minimized,
    #     and restore the user's real debug data - even if a step above threw.
    # -----------------------------------------------------------------------
    if ($minimizedWindows -and $minimizedWindows.Count -gt 0) {
        Restore-Windows $minimizedWindows
    }
    if (-not $KeepAppOpen) {
        Get-Process StickyDo.Widget -ErrorAction SilentlyContinue | Stop-Process -Force
    }
    if ($didBackup) {
        Write-Host "Restoring original debug data from backup..."
        Get-ChildItem $DebugDataDir -File | Remove-Item -Force
        Copy-Item (Join-Path $BackupDir "*") $DebugDataDir -Recurse -Force
        Remove-Item $BackupDir -Recurse -Force
    }
}
