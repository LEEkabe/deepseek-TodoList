"""Cut the character head out of a DARK background and produce the app icon + header sprite.

The background here is a flat dark gray, not white, so we flood fill from the
borders using a colour-distance tolerance instead of a whiteness test.
Flood fill (not a global "delete dark pixels") matters because the eyes are
near-black and must be kept.

Usage: make_head.py <src.png> <out_head.png> <out.ico> <preview.png>
"""
import sys
import numpy as np
from PIL import Image

TOL = 40.0          # Euclidean RGB distance from the sampled background colour


def dilate(mask):
    n = mask.copy()
    n[1:, :] |= mask[:-1, :]
    n[:-1, :] |= mask[1:, :]
    n[:, 1:] |= mask[:, :-1]
    n[:, :-1] |= mask[:, 1:]
    return n


def border_connected(cand):
    reach = np.zeros_like(cand)
    reach[0, :] = cand[0, :]
    reach[-1, :] = cand[-1, :]
    reach[:, 0] |= cand[:, 0]
    reach[:, -1] |= cand[:, -1]
    reach &= cand
    for _ in range(20000):
        nxt = dilate(reach) & cand
        if np.array_equal(nxt, reach):
            return reach
        reach = nxt
    raise RuntimeError("flood fill did not converge")


def main():
    src, out_png, out_ico, out_prev = sys.argv[1:5]

    im = Image.open(src).convert("RGB")
    W, H = im.size
    a = np.asarray(im).astype(np.int16)
    print("source %dx%d" % (W, H))

    ring = np.concatenate([
        a[0:4, :, :].reshape(-1, 3), a[-4:, :, :].reshape(-1, 3),
        a[:, 0:4, :].reshape(-1, 3), a[:, -4:, :].reshape(-1, 3),
    ])
    bg = np.median(ring, axis=0)
    print("background colour (median of border): %s" % bg)

    dist = np.sqrt(((a - bg) ** 2).sum(axis=2))
    cand = dist <= TOL
    print("background candidates: %d / %d" % (int(cand.sum()), W * H))

    mask = border_connected(cand)
    print("border-connected background: %d px" % int(mask.sum()))

    alpha = np.where(mask, 0, 255).astype(np.uint8)
    rgba = np.dstack([a.astype(np.uint8), alpha])
    img = Image.fromarray(rgba, "RGBA")

    ys, xs = np.nonzero(alpha > 0)
    if len(xs) == 0:
        raise RuntimeError("everything was classified as background")
    pad = 2
    x0 = max(0, int(xs.min()) - pad)
    y0 = max(0, int(ys.min()) - pad)
    x1 = min(W, int(xs.max()) + 1 + pad)
    y1 = min(H, int(ys.max()) + 1 + pad)
    img = img.crop((x0, y0, x1, y1))
    print("trimmed to %dx%d" % (img.width, img.height))

    # header sprite: cap the size so the embedded resource stays small
    head = img.copy()
    if max(head.size) > 512:
        r = 512.0 / max(head.size)
        head = head.resize((max(1, int(head.width * r)), max(1, int(head.height * r))),
                           Image.LANCZOS)
    head.save(out_png, "PNG", optimize=True)
    print("wrote %s (%dx%d)" % (out_png, head.width, head.height))

    # icon: square canvas with a little breathing room
    side = int(max(img.size) * 1.08)
    icon = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    icon.paste(img, ((side - img.width) // 2, (side - img.height) // 2), img)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    icon.resize((256, 256), Image.LANCZOS).save(
        out_ico, format="ICO", sizes=[(s, s) for s in sizes])
    print("wrote %s" % out_ico)

    # preview: magenta on the left, app navy on the right -> reveals any halo
    pv = Image.new("RGB", (img.width * 2 + 8, img.height), (255, 0, 255))
    pv.paste((12, 33, 53), (img.width + 8, 0, img.width * 2 + 8, img.height))
    pv.paste(img, (0, 0), img)
    pv.paste(img, (img.width + 8, 0), img)
    pv.thumbnail((880, 880), Image.LANCZOS)
    pv.save(out_prev, "PNG")
    print("wrote %s" % out_prev)


if __name__ == "__main__":
    main()
