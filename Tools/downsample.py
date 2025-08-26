


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

basepath = 'Tools/img/vert'

test = iio.imread(basepath+".png").astype(np.float32)/255
out = test#np.clip(gaussianDs(test,2),0,1)[10:-18,14:-14]
for i in range(out.shape[0]):
   for j in range(out.shape[1]):
      if out[i,j,1]<0.2:
         out[i,j]=[0,0,0,0]
print(test.shape, out.shape)
plt.imsave(basepath+"_out.png", np.ascontiguousarray((np.rot90(np.flipud(out)))))
plt.show()
