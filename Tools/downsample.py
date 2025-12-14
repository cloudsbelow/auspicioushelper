


import numpy as np
import imageio.v3 as iio
import matplotlib.pyplot as plt

def gaussianMask(shape,sigma):
  h, w = shape
  y = np.fft.fftfreq(h).reshape(-1, 1)
  x = np.fft.fftfreq(w).reshape(1, -1)
  radius_squared = (x ** 2 + y ** 2)
  res = np.exp(-2 * (np.pi ** 2) * sigma ** 2 * radius_squared)
  return np.fft.fftshift(res)

def gaussianDs(image, fac, sigma = None):
  if sigma==None:
    sigma = fac/2
  if image.ndim == 3:
      channels = []
      for c in range(image.shape[2]):
          channels.append(gaussianDs(image[:, :, c], fac, sigma))
      return np.stack(channels, axis=2)
  freq = np.fft.fftshift(np.fft.fft2(image))
  h, w = image.shape
  lp = gaussianMask((h, w), sigma)
  filtered_freq = freq * lp
  return np.fft.ifft2(np.fft.ifftshift(filtered_freq)).real[fac//2::fac,fac//2::fac]

basepath = 'Tools/img/rain'

test = iio.imread(basepath+".png")
out = test
for x in range(16):
   for y in range(16):
      if test[x,y,0] == 37:
         out[x,y,0:3] = np.array([65,104,142])
      if test[x,y,0] == 51:
         out[x,y,0:3] = np.array([100,200,255])
print(out[8,:])

print(test.shape, out.shape)
plt.imsave(basepath+"_out2.png", test)
plt.show()
