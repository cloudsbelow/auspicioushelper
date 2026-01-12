


import numpy as np
import imageio.v3 as iio
import matplotlib.pyplot as plt

# def gaussianMask(shape,sigma):
#   h, w = shape
#   y = np.fft.fftfreq(h).reshape(-1, 1)
#   x = np.fft.fftfreq(w).reshape(1, -1)
#   radius_squared = (x ** 2 + y ** 2)
#   res = np.exp(-2 * (np.pi ** 2) * sigma ** 2 * radius_squared)
#   return np.fft.fftshift(res)

# def gaussianDs(image, fac, sigma = None):
#   if sigma==None:
#     sigma = fac/2
#   if image.ndim == 3:
#       channels = []
#       for c in range(image.shape[2]):
#           channels.append(gaussianDs(image[:, :, c], fac, sigma))
#       return np.stack(channels, axis=2)
#   freq = np.fft.fftshift(np.fft.fft2(image))
#   h, w = image.shape
#   lp = gaussianMask((h, w), sigma)
#   filtered_freq = freq * lp
#   return np.fft.ifft2(np.fft.ifftshift(filtered_freq)).real[fac//2::fac,fac//2::fac]

basepath = 'Tools/img/raads'

test = iio.imread(basepath+".png")
out = np.zeros_like(test)
s = set()

print(out.shape)
center = (109.5, 116.5)
for row in range(test.shape[0]):
  for col in range(test.shape[1]):
    s.add(tuple([x for x in test[row,col]]))
    c = test[row,col]
    o = np.zeros(4)
    dy = row-center[0]
    dx = col-center[1]
    r = np.sqrt(dx*dx+dy*dy)

    if c[0]==255 and c[1]==255 and c[2]==255 and r>104:
      continue

    if  r>=105:
      pass
    elif r>=103.5:
      opacity = 1-(r-103.5)/1.5
      out[row,col]=opacity*c
    else:
      out[row,col]=c
      continue
    
    if row>105 and row<158 and col<30 and r>103.5:
      out[row,col]=o
      opacity = 0
      if c[0]>c[1] and c[0]>c[2]:
        opacity = 1-(c[0]-237)/(255-237)
      if c[0]<c[1] and c[0]<c[2]:
        opacity = 1-(c[1]-241)/(255-241)
      if opacity != 0:
        out[row,col] = c*opacity

print("fish")
print("fish")
print("fish")
for i in range(17,24):
  out[154,i] = test[154,i]

plt.imsave(basepath+"_out.png", out)
plt.show()
