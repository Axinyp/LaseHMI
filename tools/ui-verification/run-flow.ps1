param([string]$Step)

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
        try {
            $p = $el.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
            $p.Select()
            Write-Output ("Selected: " + $n)
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

switch ($Step) {
    'connect' { Invoke-ByName "连接设备"; Start-Sleep -Milliseconds 2600 }
    'home' { Invoke-ByName "回零"; Start-Sleep -Milliseconds 4200 }
    'start' { Invoke-ByName "启动加工"; Start-Sleep -Milliseconds 1200 }
    'confirm' { Invoke-ById 'DialogConfirmButton'; Start-Sleep -Milliseconds 2500 }
    'pause' { Invoke-ByName "暂停"; Start-Sleep -Milliseconds 900 }
    'resume' { Invoke-ByName "继续"; Start-Sleep -Milliseconds 900 }
    'stop' { Invoke-ByName "停止任务"; Start-Sleep -Milliseconds 1000 }
    'estop' { Invoke-ByName "紧急停止 E-STOP"; Start-Sleep -Milliseconds 1200 }
    'reset-estop' { Invoke-ByName "复位急停"; Start-Sleep -Milliseconds 1200 }
    'apply-params' { Invoke-ByName "应用参数"; Start-Sleep -Milliseconds 900 }
    'apply-preset' { Invoke-ByName "应用预设"; Start-Sleep -Milliseconds 900 }
    'nav0' { Select-Nav 0; Start-Sleep -Milliseconds 800 }
    'nav1' { Select-Nav 1; Start-Sleep -Milliseconds 800 }
    'nav2' { Select-Nav 2; Start-Sleep -Milliseconds 800 }
    'nav3' { Select-Nav 3; Start-Sleep -Milliseconds 800 }
    'nav4' { Select-Nav 4; Start-Sleep -Milliseconds 800 }
    default { Write-Output ("Unknown step: " + $Step) }
}
