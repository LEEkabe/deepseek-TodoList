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
using System.Text;
public class U {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref POINT p);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr p, EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
  [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetDlgCtrlID(IntPtr h);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, string l);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  public const uint DOWN = 0x0002, UP = 0x0004;

  static string ClassOf(IntPtr h) {
    StringBuilder sb = new StringBuilder(256);
    GetClassName(h, sb, 256);
    return sb.ToString();
  }

  public static IntPtr[] Combos(IntPtr p) {
    List<IntPtr> list = new List<IntPtr>();
    EnumChildWindows(p, delegate(IntPtr h, IntPtr l) {
      if (ClassOf(h).ToUpperInvariant().Contains("COMBOBOX")) list.Add(h);
      return true;
    }, IntPtr.Zero);
    return list.ToArray();
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

  public static void SetText(IntPtr hEdit, string s) { SendMessage(hEdit, 0x000C, IntPtr.Zero, s); }

  public static void PostKey(IntPtr h, int vk, int scan) {
    uint down = (((uint)scan) << 16) | 0x0001u;
    uint up = (((uint)scan) << 16) | 0xC0000001u;
    PostMessage(h, 0x0100, (IntPtr)vk, (IntPtr)unchecked((int)down));
    PostMessage(h, 0x0101, (IntPtr)vk, (IntPtr)unchecked((int)up));
  }

  // select a combo item natively and tell WinForms it changed
  public static void SelectCombo(IntPtr combo, int index) {
    SendMessage(combo, 0x014E, (IntPtr)index, IntPtr.Zero);       // CB_SETCURSEL
    IntPtr parent = GetParent(combo);
    int id = GetDlgCtrlID(combo);
    IntPtr w = (IntPtr)((id & 0xFFFF) | (1 << 16));               // CBN_SELCHANGE = 1
    SendMessage(parent, 0x0111, w, combo);                        // WM_COMMAND
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

[void][U]::SetProcessDPIAware()

function Dump([string]$tag) {
    Write-Host ("  [{0}]" -f $tag)
    $i = 0
    foreach ($ln in [IO.File]::ReadAllLines($dataFile)) {
        if ($ln.Trim().Length -eq 0) { continue }
        $f = $ln.Split("`t")
        if ($f.Length -lt 4) { continue }
        $i++
        $cat = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($f[2]))
        $txt = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($f[3]))
        Write-Host ("    {0}. done={1} cat={2} text={3}" -f $i, $f[1], $cat, $txt)
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

$combos = [U]::Combos($h)
Write-Host ("combos found: {0}" -f $combos.Count)

Write-Host '--- 1. filter combo -> index 1 (first category) ---'
if ($combos.Count -ge 2) {
    [U]::SelectCombo($combos[$combos.Count - 1], 1)
    Start-Sleep -Milliseconds 900
    [void][U]::Shot($h, (Join-Path $out 'v1_filter_cat1.png'))
    Write-Host '  shot v1_filter_cat1'
} else {
    Write-Host '  not enough combos'
}

Write-Host '--- 2. filter combo -> index 0 (all) ---'
if ($combos.Count -ge 2) {
    [U]::SelectCombo($combos[$combos.Count - 1], 0)
    Start-Sleep -Milliseconds 900
    [void][U]::Shot($h, (Join-Path $out 'v2_filter_all.png'))
}

Write-Host '--- 3. clear completed (confirm dialog) ---'
$pt = New-Object U+POINT
$pt.X = 484; $pt.Y = 700
[void][U]::ClientToScreen($h, [ref]$pt)
[void][U]::SetCursorPos($pt.X, $pt.Y)
Start-Sleep -Milliseconds 200
[U]::mouse_event([U]::DOWN, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 50
[U]::mouse_event([U]::UP, 0, 0, 0, [IntPtr]::Zero)
Start-Sleep -Milliseconds 1400

$wins = [U]::WindowsOf($p.Id)
Write-Host ("  windows: {0}" -f $wins.Count)
$dlg = [IntPtr]::Zero
foreach ($w in $wins) {
    $r = New-Object U+RECT
    [void][U]::GetWindowRect($w, [ref]$r)
    $ww = $r.Right - $r.Left
    Write-Host ("    {0}x{1} at {2},{3}" -f $ww, ($r.Bottom - $r.Top), $r.Left, $r.Top)
    if ($ww -lt 420) { $dlg = $w }
}
if ($dlg -ne [IntPtr]::Zero) {
    Write-Host '  -> confirming dialog with Enter'
    [U]::PostKey($dlg, 0x0D, 0x1C)
    Start-Sleep -Milliseconds 1500
} else {
    Write-Host '  -> no dialog found'
}
Dump 'after clear completed'
[void][U]::Shot($h, (Join-Path $out 'v3_cleared.png'))

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Host 'done'
