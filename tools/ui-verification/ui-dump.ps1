# List UI Automation buttons/list items of the LaserHmi window (ASCII-only script)
param([string]$Type = 'Button')

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement
$proc = Get-Process LaserHmi.App -ErrorAction Stop | Select-Object -First 1

$procCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)

$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $procCond)
if ($null -eq $win) { Write-Error 'Window not found'; exit 1 }

$ctrlType = [System.Windows.Automation.ControlType]::$Type
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty, $ctrlType)

$items = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
Write-Output ("Count: " + $items.Count)
$i = 0
foreach ($item in $items) {
    $rect = $item.Current.BoundingRectangle
    Write-Output ("[" + $i + "] name='" + $item.Current.Name + "' enabled=" + $item.Current.IsEnabled +
        " rect=" + [int]$rect.X + "," + [int]$rect.Y + "," + [int]$rect.Width + "x" + [int]$rect.Height)
    $i++
}
