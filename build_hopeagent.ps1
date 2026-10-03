$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $csc)) { throw "未找到 C# 编译器：$csc" }
$outputDir = Join-Path $root 'build'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
& $csc /nologo /target:winexe /optimize+ /codepage:65001 /out:"$outputDir\HopeAgent.exe" /win32icon:"$root\assets\hopeagent.ico" /win32manifest:"$root\app.manifest" /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "$root\MaterialWeightApp.cs" "$root\OctgMappingForm.cs" "$root\OctgCatalog.cs" "$root\UiComponents.cs" "$root\FinishedPipeModel.cs" "$root\MainForm.Finished.cs"
if ($LASTEXITCODE -ne 0) { throw '编译失败' }
Write-Host "已编译：$outputDir\HopeAgent.exe"
