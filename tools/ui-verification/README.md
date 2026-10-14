# UI 自动化验证脚本（开发期工具）

本目录包含原型开发过程中使用的 Windows UI Automation（UIA）验证脚本：以脚本驱动真实运行的应用完成全流程操作并截图，用于核心交互与状态机的回归验证。

## 使用前提

1. 应用已启动：`dotnet run --project src/LaserHmi.App`
2. Windows PowerShell 5.1+（脚本依赖 UIAutomationClient/UIAutomationTypes 程序集）
3. 运行期间不要移动/最小化应用窗口

## 脚本说明

| 脚本 | 用途 |
| --- | --- |
| `run-flow.ps1 -Step <步骤>` | 单步操作：`connect` / `home` / `start` / `confirm` / `pause` / `resume` / `stop` / `estop` / `reset-estop` / `apply-params` / `nav0`~`nav4` |
| `verify-stop.ps1` | 组合验证：启动任务 → 加工中停止 → 确认 → 截图（`shots/p18-stop-actual.png`） |
| `verify-estop.ps1` | 组合验证：急停 → 报警页截图 → 全部确认 → 复位急停 → 截图 |
| `verify-params.ps1` | 组合验证：功率滑块调至 75% → 应用参数 → 截图 |
| `capture-page.ps1 -OutPath <路径>` | 将应用窗口截图保存为 PNG（可选 `-OffsetX/-OffsetY` 先点击窗口内相对坐标） |
| `ui-dump.ps1 -Type <控件类型>` | 列出应用窗口内指定类型（如 Button）的 UIA 元素名称/可用性/位置 |
| `diag-dpi.ps1` | 对比 Win32 与 UIA 坐标系，排查 DPI 缩放导致的点击偏移 |

## 示例

```powershell
# 启动应用后，验证急停全流程
powershell -ExecutionPolicy Bypass -File verify-estop.ps1

# 由脚本分步驱动（每一步之间可人工观察界面）
powershell -ExecutionPolicy Bypass -File run-flow.ps1 -Step connect
powershell -ExecutionPolicy Bypass -File run-flow.ps1 -Step home
powershell -ExecutionPolicy Bypass -File run-flow.ps1 -Step nav1
powershell -ExecutionPolicy Bypass -File run-flow.ps1 -Step start
powershell -ExecutionPolicy Bypass -File capture-page.ps1 -OutPath shots/step-start.png
```

> 注：脚本内包含中文按钮名称，文件必须保存为 **UTF-8 with BOM**，否则 Windows PowerShell 5.1 会按 ANSI 解析导致乱码匹配失败。
