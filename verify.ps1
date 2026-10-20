$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = $PSScriptRoot
$exe  = Join-Path $root 'dist\BlueFishTodo.exe'
$out  = Join-Path $root 'build'
$py   = 'C:\Users\18384\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe'
$dataDir  = Join-Path $env:APPDATA 'BlueFatFishTodo'
$dataFile = Join-Path $dataDir 'tasks.dat'

Add-Type -ReferencedAssemblies System.Drawing, System.Windows.Forms -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public class U {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  public const uint DOWN = 0x0002, UP = 0x0004;

  public static readonly IntPtr TOPMOST = new IntPtr(-1);
  public static readonly IntPtr NOTOPMOST = new IntPtr(-2);

  // raise the window so clicks land on it instead of whatever is on top
  public static void Raise(IntPtr h) {
    SetWindowPos(h, TOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0040); // NOSIZE|NOMOVE|SHOWWINDOW
    SetWindowPos(h, NOTOPMOST, 0, 0, 0, 0, 0x0001 | 0x0002);
    SetForegroundWindow(h);
  }

  public static IntPtr[] WindowsOf(int pid) {
    List<IntPtr> found = new List<IntPtr>();
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      uint p; GetWindowThreadProcessId(h, out p);
      if ((int)p == pid && IsWindowVisible(h)) found.Add(h);
      return true;
    }, IntPtr.Zero);
    return found.ToArray();
  }

  // 直接渲染窗口内容，不依赖屏幕坐标，天然免疫 DPI / 窗口遮挡问题
  public static string Shot(IntPtr h, string path) {
    RECT r; GetWindowRect(h, out r);
    int w = r.Right - r.Left, hh = r.Bottom - r.Top;
    if (w <= 0 || hh <= 0) return "bad rect " + w + "x" + hh;
    using (Bitmap bmp = new Bitmap(w, hh))
    using (Graphics g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc();
      PrintWindow(h, hdc, 2);
      g.ReleaseHdc(hdc);
      bmp.Save(path, ImageFormat.Png);
    }
    return w + "x" + hh;
  }
}
'@

# Make PowerShell DPI aware too, otherwise ClientToScreen and the real pixel
# coordinates disagree and clicks land in the wrong place.
[void][U]::SetProcessDPIAware()

function ClickAt([int]$x, [int]$y, [IntPtr]$h) {
    [U]::Raise($h)
    Start-Sleep -Milliseconds 180
    $pt = New-Object U+POINT
    $pt.X = $x; $pt.Y = $y
    [void][U]::ClientToScreen($h, [ref]$pt)
    [void][U]::SetCursorPos($pt.X, $pt.Y)
    Start-Sleep -Milliseconds 150
    [U]::mouse_event([U]::DOWN, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 50
    [U]::mouse_event([U]::UP, 0, 0, 0, [IntPtr]::Zero)
}

function Dump {
    if (-not (Test-Path $dataFile)) { Write-Host '  (no data file)'; return }
    $i = 0
    foreach ($ln in [IO.File]::ReadAllLines($dataFile)) {
        if ($ln.Trim().Length -eq 0) { continue }
        $f = $ln.Split("`t")
        if ($f.Length -lt 4) { continue }
        $i++
        $cat = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($f[2]))
        $txt = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($f[3]))
        Write-Host ("  {0}. done={1} cat={2} text={3}" -f $i, $f[1], $cat, $txt)
    }
}

Get-Process -Name BlueFishTodo -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path $dataDir) { Remove-Item $dataDir -Recurse -Force -ErrorAction SilentlyContinue }
& $py (Join-Path $root 'tools\seed.py')
Start-Sleep -Milliseconds 400

$p = Start-Process -FilePath $exe -PassThru
Start-Sleep -Milliseconds 2200
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { throw 'main window not found' }
Write-Host ("pid={0} hwnd={1}" -f $p.Id, $h)

$cr = New-Object U+RECT
[void][U]::GetClientRect($h, [ref]$cr)
Write-Host ("client {0}x{1}" -f $cr.Right, $cr.Bottom)
[void][U]::SetForegroundWindow($h)
Start-Sleep -Milliseconds 400
Write-Host ("shot x1_main  " + [U]::Shot($h, (Join-Path $out 'x1_main.png')))

$rowTop = 202
$pitch = 58
Write-Host '--- click row 1 text (toggle) ---'
ClickAt 120 ($rowTop + 25) $h
Start-Sleep -Milliseconds 700
[void][U]::Shot($h, (Join-Path $out 'x2_toggled.png'))
Dump

Write-Host '--- click delete on row 6 ---'
ClickAt 500 ($rowTop + 5 * $pitch + 25) $h
Start-Sleep -Milliseconds 800
[void][U]::Shot($h, (Join-Path $out 'x3_deleted.png'))
Dump

Write-Host '--- minimize ---'
[void][U]::ShowWindow($h, 6)
Start-Sleep -Milliseconds 1500
$wins = [U]::WindowsOf($p.Id)
Write-Host ("visible windows for pid: " + $wins.Count)
for ($i = 0; $i -lt $wins.Count; $i++) {
    $r = New-Object U+RECT
    [void][U]::GetWindowRect($wins[$i], [ref]$r)
    Write-Host ("  win{0} {1}x{2}" -f $i, ($r.Right - $r.Left), ($r.Bottom - $r.Top))
    [void][U]::Shot($wins[$i], (Join-Path $out ("x4_mini_" + $i + ".png")))
}

Write-Host '--- restore ---'
[void][U]::ShowWindow($h, 9)
Start-Sleep -Milliseconds 1000
Write-Host ("shot x5_restored  " + [U]::Shot($h, (Join-Path $out 'x5_restored.png')))

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host 'verify done'
