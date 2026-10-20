"""Upscale small icon regions from a screenshot so quality can be judged."""
import sys
from PIL import Image

src, out = sys.argv[1], sys.argv[2]
im = Image.open(src)

hdr = im.crop((14, 58, 96, 132))
hdr = hdr.resize((hdr.width * 6, hdr.height * 6), Image.NEAREST)

tb = im.crop((0, 0, 34, 28))
tb = tb.resize((tb.width * 8, tb.height * 8), Image.NEAREST)

canvas = Image.new("RGB", (max(hdr.width, tb.width), hdr.height + tb.height + 12), (255, 255, 255))
canvas.paste(tb, (0, 0))
canvas.paste(hdr, (0, tb.height + 12))
canvas.save(out)
print("wrote", out, canvas.size)
