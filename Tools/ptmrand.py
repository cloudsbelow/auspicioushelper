



import numpy as np

n=6
a = np.array([[(i+j)%n for i in range(n)] for j in range(n)], dtype=np.int8)

def score(a):
  res = -np.ones((n,n),dtype=np.int8)
  np.add.at(res,(a[1:,:],a[:-1,:]),1)
  return np.sum(np.max(res,0))
print(a, score(a))
def scramble(arr, temp=0.8):
  a=arr.copy()
  while True:
    i = np.random.randint(0,n)
    j = np.random.randint(0,n)
    t=a[i,:].copy()
    a[i,:]=a[j,:]
    a[j,:]=t
    if np.random.rand()>temp:
      return a
  
bestScore = score(a)
bestarr = a
cscore = score(a)
for iter in range(1000000):
  narr = scramble(a)
  nscore = score(narr)
  if nscore<bestScore:
    bestScore=nscore
    bestarr=narr
    print(iter, bestScore)
    if bestScore == 0:
      break
  if nscore<=cscore or np.random.rand()<0.6:
    a = narr
    cscore = nscore

print(bestarr)




'''
[[ 9  7  4 13  3  6  2 12 11  8  0 15 10 14  5  1]
 [11  9  6 15  5  8  4 14 13 10  2  1 12  0  7  3]
 [14 12  9  2  8 11  7  1  0 13  5  4 15  3 10  6]
 [13 11  8  1  7 10  6  0 15 12  4  3 14  2  9  5]
 [ 6  4  1 10  0  3 15  9  8  5 13 12  7 11  2 14]
 [ 4  2 15  8 14  1 13  7  6  3 11 10  5  9  0 12]
 [ 5  3  0  9 15  2 14  8  7  4 12 11  6 10  1 13]
 [ 2  0 13  6 12 15 11  5  4  1  9  8  3  7 14 10]
 [ 8  6  3 12  2  5  1 11 10  7 15 14  9 13  4  0]
 [ 3  1 14  7 13  0 12  6  5  2 10  9  4  8 15 11]
 [15 13 10  3  9 12  8  2  1 14  6  5  0  4 11  7]
 [ 7  5  2 11  1  4  0 10  9  6 14 13  8 12  3 15]
 [12 10  7  0  6  9  5 15 14 11  3  2 13  1  8  4]
 [ 0 14 11  4 10 13  9  3  2 15  7  6  1  5 12  8]
 [10  8  5 14  4  7  3 13 12  9  1  0 11 15  6  2]
 [ 1 15 12  5 11 14 10  4  3  0  8  7  2  6 13  9]]
'''