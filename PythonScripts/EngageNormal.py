import os
import sys
from PIL import Image

for src in sys.argv[1:]:
    outname = os.path.splitext(src)[0]
    try:
        img = Image.open(src).convert("RGBA")
        data = img.getdata()
        normalmap = [(d[3], d[1], d[0], 255) for d in data]
        img.putdata(normalmap)
        img.convert('RGB')
        img.save(outname + ".png", format="PNG")
    except IOError:
        pass