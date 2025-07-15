import os, sys
from PIL import Image

for src in sys.argv[1:]:
    outname = os.path.splitext(src)[0]
    try:
        img = Image.open(src).convert("RGBA")
        data = img.getdata()
        r = [(d[0], d[0], d[0]) for d in data]
        g = [(d[1], d[1], d[1]) for d in data]
        b = [(d[2], d[2], d[2]) for d in data]
        a = [(d[3], d[3], d[3]) for d in data]
        img.putdata(r)
        img.convert('RGB')
        img.save(outname + '_R.png', format="PNG")
        img.putdata(g)
        img.convert('RGB')
        img.save(outname + '_M.png', format="PNG")
        img.putdata(b)
        img.convert('RGB')
        img.save(outname + '_AO.png', format="PNG")
        img.putdata(a)
        img.convert('RGB')
        img.save(outname + '_Mask.png', format="PNG")
    except IOError:
        pass