


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

basepath = 'Tools/img/downleft'

test = iio.imread(basepath+".png")
out = np.zeros_like(test)

out[:,:,3]=255-np.clip((test[:,:,1].astype(np.float32)*255)/240,0,255).astype(np.uint8)
la = [0]*256
for x in out:
  for y in x:
    la[y[3]]+=1
print(la)

plt.imsave(basepath+"_out.png", out)
plt.show()
