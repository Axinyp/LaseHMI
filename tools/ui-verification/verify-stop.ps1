# Start a task, stop it mid-run, capture the result (UTF-8 with BOM required)
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

function Invoke-ById([string]$id) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $id)
    $el = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if ($null -eq $el) {
        Write-Output ("NOT FOUND ID: " + $id)
        return
    }
    $p = $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $p.Invoke()
    Write-Output ("Invoked by id: " + $id)
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

# 1) Workbench page, start task
Select-Nav 1
Start-Sleep -Milliseconds 700
Invoke-ByName "启动加工"
Start-Sleep -Milliseconds 900
Invoke-ById 'DialogConfirmButton'
Start-Sleep -Milliseconds 2200

# 2) Stop task mid-run and confirm quickly
Invoke-ByName "停止任务"
Start-Sleep -Milliseconds 700
Invoke-ById 'DialogConfirmButton'
Start-Sleep -Milliseconds 500

# 3) Capture result
& "$PSScriptRoot\capture-page.ps1" -OutPath "$PSScriptRoot\shots\p18-stop-actual.png"
