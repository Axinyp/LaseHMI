# Diagnose DPI coordinate consistency between Win32 and UIA (ASCII-only script)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Diag {
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT pt);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X; public int Y; }
}
"@

[Win32Diag]::SetProcessDPIAware() | Out-Null

$proc = Get-Process LaserHmi.App -ErrorAction Stop | Select-Object -First 1
$rect = New-Object Win32Diag+RECT
[Win32Diag]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
Write-Output ("Win32 window rect: " + $rect.Left + "," + $rect.Top + " size " + ($rect.Right - $rect.Left) + "x" + ($rect.Bottom - $rect.Top))

$pt = New-Object Win32Diag+POINT
[Win32Diag]::GetCursorPos([ref]$pt) | Out-Null
Write-Output ("Cursor pos now: " + $pt.X + "," + $pt.Y)

$root = [System.Windows.Automation.AutomationElement]::RootElement
$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $procCond)
$wrect = $win.Current.BoundingRectangle
Write-Output ("UIA window rect: " + [int]$wrect.X + "," + [int]$wrect.Y + " size " + [int]$wrect.Width + "x" + [int]$wrect.Height)
