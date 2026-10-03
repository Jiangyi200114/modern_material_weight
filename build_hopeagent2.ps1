$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "未找到 C# 编译器: $csc"
}

& $csc `
  /nologo `
  /target:winexe `
  /out:"$root\HopeAgent2.exe" `
  /win32icon:"$root\assets\hopeagent.ico" `
  /r:System.dll `
  /r:System.Core.dll `
  /r:System.Drawing.dll `
  /r:System.Windows.Forms.dll `
  "$root\HopeAgent2\HopeAgent2Core.cs" `
  "$root\HopeAgent2\HopeAgent2App.cs"
