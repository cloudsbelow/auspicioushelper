import numpy as np
from PIL import Image

def makecg(cg_fn, filename="colorgrade.png"):
  width, height = 256, 16
  img = np.zeros((height, width, 3), dtype=np.uint8)

  for z in range(16):       # blue block row
    b = z/15.0
    for y in range(16):  # green within block
      g = y / 15.0
      for x in range(16):  # red within block
        r = x / 15.0

        out = cg_fn(r, g, b)
        out = np.clip(out, 0, 1) * 255

        px = z * 16 + x
        py = y
        img[py, px] = out.astype(np.uint8)

  Image.fromarray(img, "RGB").save("Tools/img/cgs/"+filename)
  print("done")

def dred(r, g, b):
    lum = 0.3*r + 0.6*g + 0.1*b
    
    boosted = lum**2  # <1 gamma brightens
    orig = np.array([r,g,b])
    n = np.array([boosted, 0.3 * boosted * g, 0.3 * boosted * b])

    return orig*0.15+n*0.6+0.1


if __name__ == "__main__":
    makecg(dred, "ld_dred.png")
