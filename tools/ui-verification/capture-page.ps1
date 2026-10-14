# Click inside the LaserHmi window and capture it to PNG (ASCII-only script)
param(
    [int]$OffsetX = -1,
    [int]$OffsetY = -1,
    [string]$OutPath = 'C:\Users\Administrator\Documents\Qoder\2026-10-14\chat-1\shots\shot.png'
)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Page {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@

[Win32Page]::SetProcessDPIAware() | Out-Null

$proc = Get-Process LaserHmi.App -ErrorAction Stop | Select-Object -First 1
$handle = $proc.MainWindowHandle
if ($handle -eq [IntPtr]::Zero) { Write-Error 'No main window handle'; exit 1 }

[Win32Page]::SetForegroundWindow($handle) | Out-Null
Start-Sleep -Milliseconds 500

$rect = New-Object Win32Page+RECT
[Win32Page]::GetWindowRect($handle, [ref]$rect) | Out-Null

# Optional click at window-relative offset (physical pixels)
if ($OffsetX -ge 0 -and $OffsetY -ge 0) {
    $absX = $rect.Left + $OffsetX
    $absY = $rect.Top + $OffsetY
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($absX, $absY)
    Start-Sleep -Milliseconds 200
    [Win32Page]::mouse_event(0x02, 0, 0, 0, [UIntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 80
    [Win32Page]::mouse_event(0x04, 0, 0, 0, [UIntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 900
}

$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
$bmp = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bmp)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($width, $height)))
$dir = Split-Path -Parent $OutPath
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
$bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose()
$bmp.Dispose()
Write-Output ("Saved: " + $OutPath + " (" + $width + "x" + $height + ")")
