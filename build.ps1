$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$csc  = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$py   = 'C:\Users\18384\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe'

$build = Join-Path $root 'build'
$dist  = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $build, $dist | Out-Null

$headPng = Join-Path $build 'head.png'
$headIco = Join-Path $build 'head.ico'
$headPrev = Join-Path $build 'head_preview.png'
$headSrc = Join-Path $root 'tools\head-source.png'

Write-Host '--- [1/2] character head + icon ---'
& $py (Join-Path $root 'tools\make_head.py') $headSrc $headPng $headIco $headPrev
if ($LASTEXITCODE -ne 0) { throw "head step failed" }

Write-Host '--- [2/2] compile ---'
$exe = Join-Path $dist 'BlueFishTodo.exe'
Get-Process -Name BlueFishTodo -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 300
if (Test-Path $exe) { Remove-Item $exe -Force -ErrorAction SilentlyContinue }

& $csc /nologo /target:winexe /platform:anycpu /optimize+ `
    "/win32icon:$headIco" `
    "/resource:$headPng,head.png" `
    "/out:$exe" `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    (Join-Path $root 'src\Todo.cs')
if ($LASTEXITCODE -ne 0) { throw "compile failed ($LASTEXITCODE)" }

$fi = Get-Item $exe
Write-Host ("EXE: {0}  ({1:N0} bytes)" -f $fi.FullName, $fi.Length)
