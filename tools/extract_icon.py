"""Extract the icon embedded in an exe and render it large, to verify /win32icon."""
import sys
from PIL import Image
import ctypes
from ctypes import wintypes

exe, out = sys.argv[1], sys.argv[2]

# Ask the shell for the exe icon at several sizes via ExtractIconExW
user32 = ctypes.windll.user32
shell32 = ctypes.windll.shell32

large = (wintypes.HICON * 1)()
small = (wintypes.HICON * 1)()
count = shell32.ExtractIconExW(exe, 0, large, small, 1)
print("icons extracted:", count)
if count == 0:
    raise SystemExit("no icon found in exe")

# Use IShellItemImageFactory-free approach: render the large HICON with GDI
import struct

gdi32 = ctypes.windll.gdi32
u32 = ctypes.windll.user32

hicon = large[0]

class ICONINFO(ctypes.Structure):
    _fields_ = [("fIcon", wintypes.BOOL),
                ("xHotspot", wintypes.DWORD),
                ("yHotspot", wintypes.DWORD),
                ("hbmMask", wintypes.HBITMAP),
                ("hbmColor", wintypes.HBITMAP)]

info = ICONINFO()
if not u32.GetIconInfo(hicon, ctypes.byref(info)):
    raise SystemExit("GetIconInfo failed")

# Draw into a 32-bit DIB via DrawIconEx
SIZE = 256
hdc = gdi32.CreateCompatibleDC(0)


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", wintypes.DWORD), ("biWidth", wintypes.LONG),
                ("biHeight", wintypes.LONG), ("biPlanes", wintypes.WORD),
                ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", wintypes.LONG),
                ("biYPelsPerMeter", wintypes.LONG), ("biClrUsed", wintypes.DWORD),
                ("biClrImportant", wintypes.DWORD)]

bmi = BITMAPINFOHEADER()
bmi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
bmi.biWidth = SIZE
bmi.biHeight = -SIZE
bmi.biPlanes = 1
bmi.biBitCount = 32
bmi.biCompression = 0

bits = ctypes.c_void_p()
hbm = gdi32.CreateDIBSection(hdc, ctypes.byref(bmi), 0, ctypes.byref(bits), 0, 0)
old = gdi32.SelectObject(hdc, hbm)
u32.DrawIconEx(hdc, 0, 0, hicon, SIZE, SIZE, 0, 0, 3)  # DI_NORMAL
gdi32.SelectObject(hdc, old)

buf = ctypes.string_at(bits, SIZE * SIZE * 4)
img = Image.frombuffer("RGBA", (SIZE, SIZE), buf, "raw", "BGRA", 0, 1)

# on a mid gray so both dark and light fringes show
canvas = Image.new("RGB", (SIZE, SIZE), (128, 128, 128))
canvas.paste(img, (0, 0), img)
canvas.save(out)
print("wrote", out)
