# Verify E-STOP flow: trigger -> alarm page -> ack all -> reset (UTF-8 with BOM required)
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

function Select-Nav([int]$index) {
    $proc = Get-Process LaserHmi.App -ErrorAction Stop | Select-Object -First 1
    $procCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $procCond)
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

# 1) Device control page, trigger E-STOP
Select-Nav 3
Start-Sleep -Milliseconds 700
Invoke-ByName "紧急停止 E-STOP"
Start-Sleep -Milliseconds 1400
& "$PSScriptRoot\capture-page.ps1" -OutPath "$PSScriptRoot\shots\p19-estop.png"

# 2) Alarm page: S-9001 should appear as pending
Select-Nav 4
Start-Sleep -Milliseconds 900
& "$PSScriptRoot\capture-page.ps1" -OutPath "$PSScriptRoot\shots\p20-estop-alarm.png"

# 3) Acknowledge all alarms, then reset E-STOP
Invoke-ByName "全部确认"
Start-Sleep -Milliseconds 900
Select-Nav 3
Start-Sleep -Milliseconds 700
Invoke-ByName "复位急停"
Start-Sleep -Milliseconds 1200
& "$PSScriptRoot\capture-page.ps1" -OutPath "$PSScriptRoot\shots\p21-estop-reset.png"
