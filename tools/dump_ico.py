"""Render the frames inside an .ico to a single contact sheet."""
import sys
from PIL import Image

ico, out = sys.argv[1], sys.argv[2]
im = Image.open(ico)
sizes = sorted(im.info.get("sizes", []))

frames = []
for s in sizes:
    im.size = s
    fr = im.convert("RGBA")
    frames.append((s, fr.copy()))

# biggest frame big, small ones at real size
big = max(frames, key=lambda t: t[0][0])
cell = 272
canvas = Image.new("RGB", (cell + 340, cell), (128, 128, 128))
canvas.paste(big[1].resize((256, 256), Image.LANCZOS), (8, 8), big[1].resize((256, 256), Image.LANCZOS))

x = cell
y = 8
for s, fr in sorted(frames, key=lambda t: -t[0][0]):
    if s[0] == big[0][0]:
        continue
    canvas.paste(fr, (x, y), fr)
    y += s[1] + 8

canvas.save(out)
print("sizes in ico:", sizes, "->", out)
