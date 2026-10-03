from PIL import Image, ImageDraw, ImageFilter


SIZE = 1024
RADIUS = 230


def lerp(a, b, t):
    return int(a + (b - a) * t)


def gradient_background(size):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    c1 = (8, 12, 24)
    c2 = (18, 24, 44)
    c3 = (7, 44, 66)
    for y in range(size):
        for x in range(size):
            tx = x / (size - 1)
            ty = y / (size - 1)
            a = tuple(lerp(c1[i], c2[i], tx) for i in range(3))
            b = tuple(lerp(c2[i], c3[i], tx) for i in range(3))
            rgb = tuple(lerp(a[i], b[i], ty) for i in range(3))
            px[x, y] = rgb + (255,)
    return img


def rounded_mask(size, radius):
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((28, 28, size - 28, size - 28), radius=radius, fill=255)
    return mask


def glow_layer(size, center, radius, color, alpha):
    layer = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    x, y = center
    draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=color + (alpha,))
    return layer.filter(ImageFilter.GaussianBlur(radius // 2))


def make_polygon_mask(size, points):
    mask = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(mask)
    draw.polygon(points, fill=255)
    return mask


def gradient_fill(size, top_color, bottom_color):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    for y in range(size):
        t = y / (size - 1)
        c = tuple(lerp(top_color[i], bottom_color[i], t) for i in range(3))
        for x in range(size):
            px[x, y] = c + (255,)
    return img


def add_stroke(draw, points, fill, width):
    draw.line(points, fill=fill, width=width, joint="curve")


def main():
    base = gradient_background(SIZE)
    mask = rounded_mask(SIZE, RADIUS)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    canvas.paste(base, (0, 0), mask)

    canvas = Image.alpha_composite(canvas, glow_layer(SIZE, (290, 260), 220, (52, 211, 255), 120))
    canvas = Image.alpha_composite(canvas, glow_layer(SIZE, (760, 760), 250, (126, 87, 255), 115))
    canvas = Image.alpha_composite(canvas, glow_layer(SIZE, (760, 240), 180, (44, 194, 255), 55))

    vignette = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    vd = ImageDraw.Draw(vignette)
    vd.rounded_rectangle((28, 28, SIZE - 28, SIZE - 28), radius=RADIUS, outline=(255, 255, 255, 34), width=5)
    vd.rounded_rectangle((52, 52, SIZE - 52, SIZE - 52), radius=RADIUS - 24, outline=(255, 255, 255, 14), width=2)
    canvas = Image.alpha_composite(canvas, vignette)

    # Stylized H inspired by modern AI tooling, but drawn as an original monogram.
    left = [(256, 232), (368, 232), (446, 360), (446, 674), (340, 806), (228, 806), (322, 658), (322, 382)]
    right = [(766, 232), (654, 232), (576, 360), (576, 674), (682, 806), (794, 806), (700, 658), (700, 382)]
    bridge = [(408, 452), (618, 452), (682, 516), (618, 580), (408, 580), (344, 516)]

    mark_grad = gradient_fill(SIZE, (241, 250, 255), (154, 244, 255))
    left_mask = make_polygon_mask(SIZE, left)
    right_mask = make_polygon_mask(SIZE, right)
    bridge_mask = make_polygon_mask(SIZE, bridge)

    mark_layer = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    for m in (left_mask, right_mask, bridge_mask):
        mark_layer = Image.alpha_composite(mark_layer, Image.composite(mark_grad, Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0)), m))

    # Internal glow and diagonal energy slash.
    slash = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    sd = ImageDraw.Draw(slash)
    sd.polygon([(356, 742), (444, 742), (676, 280), (588, 280)], fill=(74, 212, 255, 120))
    slash = slash.filter(ImageFilter.GaussianBlur(8))
    mark_layer = Image.alpha_composite(mark_layer, slash)

    # Notch creates a sharper futuristic center cut.
    notch_mask = Image.new("L", (SIZE, SIZE), 0)
    nd = ImageDraw.Draw(notch_mask)
    nd.polygon([(472, 484), (552, 484), (588, 516), (552, 548), (472, 548), (436, 516)], fill=255)
    notch_fill = Image.new("RGBA", (SIZE, SIZE), (8, 13, 23, 255))
    mark_layer = Image.composite(notch_fill, mark_layer, notch_mask)

    # Subtle edge highlights.
    highlight = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    hd = ImageDraw.Draw(highlight)
    add_stroke(hd, [(274, 256), (364, 256), (420, 346)], (255, 255, 255, 90), 18)
    add_stroke(hd, [(748, 256), (658, 256), (602, 346)], (255, 255, 255, 90), 18)
    add_stroke(hd, [(394, 470), (620, 470)], (255, 255, 255, 70), 16)
    highlight = highlight.filter(ImageFilter.GaussianBlur(2))
    mark_layer = Image.alpha_composite(mark_layer, highlight)

    # Outer glow for the mark.
    glow = mark_layer.copy().filter(ImageFilter.GaussianBlur(24))
    glow = Image.blend(Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0)), glow, 0.55)
    canvas = Image.alpha_composite(canvas, glow)
    canvas = Image.alpha_composite(canvas, mark_layer)

    # Tiny accent dot for "agent pulse".
    pulse = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    pd = ImageDraw.Draw(pulse)
    pd.ellipse((724, 204, 790, 270), fill=(155, 245, 255, 255))
    pulse = Image.alpha_composite(pulse, glow_layer(SIZE, (757, 237), 55, (155, 245, 255), 140))
    canvas = Image.alpha_composite(canvas, pulse)

    # Final clip to rounded square.
    final = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    final.paste(canvas, (0, 0), mask)

    png_path = "assets/hopeagent.png"
    ico_path = "assets/hopeagent.ico"
    final.resize((512, 512), Image.LANCZOS).save(png_path)
    final.save(ico_path, sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])


if __name__ == "__main__":
    main()
