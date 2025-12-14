


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

basepath = 'Tools/img/sgs/frontmountain'

test = iio.imread(basepath+".png")
out = np.zeros_like(test)
s = set()
print(out.shape)
for row in range(180):
  for col in range(320):
    s.add(tuple([x for x in test[row,col]]))
    c = test[row,col]
    o = np.zeros(4)
    if row>50:
      #back mountain
      if c[2] == 128:
        o = np.array([40,40,80   ,255])
      if c[2] == 135:
        o = np.array([40,60,90   ,255])
      if c[2] == 177:
        o = np.array([80,80,100   ,255])

      if c[2] == 153:
        #o = np.array([60,60,100   ,255])
        o = np.array([55,85,130   ,255])
      if c[2] == 189:
        o = np.array([55,85,130   ,255])
      if c[2] == 224:
        o = np.array([85,120,155   ,255])
      out[row-50,col] = o
print(s)

# for col in range(320):
#   f = -1
#   for row in range(180):
#     if f<0 and test[row+50,col,3] == 255 and test[row+50,col,2]==0:
#       f=0
#     elif f>=0:
#       f+=1
#       out[row,col] = np.array([f,0,0,255])

out[-50:,:] = out[-51].reshape(1,-1,4)

plt.imsave(basepath+"_out.png", out)
plt.show()
