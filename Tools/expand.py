


import imageio.v3 as iio
import numpy as np
import matplotlib.pyplot as plt

basepath = 'Tools/img/templates/'
n=8
for str in ["tblk","tstat_legacy","tgroup","tgroupnode","tstat","ttrig","evilrooms","math"]:
  test = iio.imread(basepath+str+".png").astype(np.float32)/255
  plt.imsave(basepath+str+"_out.png", test.repeat(n,axis=0).repeat(n,axis=1))