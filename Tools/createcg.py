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
    
    lcol =  np.array([lum,lum,lum])
    rcol = np.array([r,0,0])
    redness = max([min([r-g,r-b]),0])
    return rcol*redness+lcol*(1-redness)


if __name__ == "__main__":
    makecg(dred, "redonly.png")
