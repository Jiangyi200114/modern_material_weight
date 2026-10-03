param([switch]$Dpi96)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
Push-Location $root
try {
    & $csc /nologo /target:exe /main:SmokeTests /codepage:65001 /out:tests\SmokeTests.exe /win32icon:assets\hopeagent.ico /win32manifest:app.manifest /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll MaterialWeightApp.cs OctgMappingForm.cs OctgCatalog.cs UiComponents.cs FinishedPipeModel.cs MainForm.Finished.cs tests\SmokeTests.cs
    if ($LASTEXITCODE -ne 0) { throw '测试程序编译失败' }
    if ($Dpi96) { & .\tests\SmokeTests.exe --dpi96 } else { & .\tests\SmokeTests.exe }
    if ($LASTEXITCODE -ne 0) { throw '回归检查失败' }
}
finally { Pop-Location }
