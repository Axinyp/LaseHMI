# Verify parameters page: change a slider value, apply, capture (UTF-8 with BOM required)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$root = [System.Windows.Automation.AutomationElement]::RootElement

function Invoke-ByName([string]$n) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $n)
    $items = $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    foreach ($el in $items) {
        if (-not $el.Current.IsEnabled) { continue }
        try {
            $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
            $p.Invoke()
            Write-Output ("Invoked: " + $n)
            return
        } catch { }
    }
    Write-Output ("NOT FOUND: " + $n)
}

function Get-MainWindow() {
    $proc = Get-Process LaserHmi.App -ErrorAction Stop | Select-Object -First 1
    $procCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    return $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $procCond)
}

function Select-Nav([int]$index) {
    $win = Get-MainWindow
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem)
    $items = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($index -ge $items.Count) {
        Write-Output "NAV OUT OF RANGE"
        return
    }
    $p = $items[$index].GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $p.Select()
    Write-Output ("Nav selected: " + $index)
}

# 1) Parameters page
Select-Nav 2
Start-Sleep -Milliseconds 800

# 2) Find sliders inside main window, set the first one (Laser power) to 75
$win = Get-MainWindow
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Slider)
$sliders = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
Write-Output ("Slider count: " + $sliders.Count)
if ($sliders.Count -gt 0) {
    $rvp = $sliders[0].GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
    Write-Output ("Slider0 before: " + $rvp.Current.Value)
    $rvp.SetValue(75)
    Start-Sleep -Milliseconds 400
    $rvp2 = $sliders[0].GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern)
    Write-Output ("Slider0 after: " + $rvp2.Current.Value)
}

# 3) Apply parameters
Invoke-ByName "应用参数"
Start-Sleep -Milliseconds 1000
& "$PSScriptRoot\capture-page.ps1" -OutPath "$PSScriptRoot\shots\p22-params-applied.png"
