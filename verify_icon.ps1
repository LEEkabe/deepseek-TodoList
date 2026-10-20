$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = $PSScriptRoot
$exe  = Join-Path $root 'dist\BlueFishTodo.exe'
$out  = Join-Path $root 'build'
$py   = 'C:\Users\18384\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe'

Add-Type -ReferencedAssemblies System.Drawing, System.Windows.Forms -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public class U {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  public delegate bool EnumProc(IntPtr h, IntPtr l);

  public static IntPtr[] WindowsOf(int pid) {
    List<IntPtr> found = new List<IntPtr>();
    EnumWindows(delegate(IntPtr h, IntPtr l) {
      uint p; GetWindowThreadProcessId(h, out p);
      if ((int)p == pid && IsWindowVisible(h)) found.Add(h);
      return true;
    }, IntPtr.Zero);
    return found.ToArray();
  }

  public static string Shot(IntPtr h, string path) {
    RECT r; GetWindowRect(h, out r);
    int w = r.Right - r.Left, hh = r.Bottom - r.Top;
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

Get-Process -Name BlueFishTodo -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path (Join-Path $env:APPDATA 'BlueFatFishTodo')) {
    Remove-Item (Join-Path $env:APPDATA 'BlueFatFishTodo') -Recurse -Force -ErrorAction SilentlyContinue
}
& $py (Join-Path $root 'tools\seed.py')
Start-Sleep -Milliseconds 400

$p = Start-Process -FilePath $exe -PassThru
Start-Sleep -Milliseconds 2200
$p.Refresh()
$h = $p.MainWindowHandle
Write-Host ("main shot: " + [U]::Shot($h, (Join-Path $out 'i1_main.png')))

[void][U]::ShowWindow($h, 6)
Start-Sleep -Milliseconds 1500
$wins = [U]::WindowsOf($p.Id)
foreach ($w in $wins) {
    $r = New-Object U+RECT
    [void][U]::GetWindowRect($w, [ref]$r)
    $ww = $r.Right - $r.Left
    if ($ww -lt 420) {
        Write-Host ("mini shot: " + [U]::Shot($w, (Join-Path $out 'i2_mini.png')))
    }
}
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host 'done'
