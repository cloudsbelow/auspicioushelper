import re

def transposeMask(s, off = True):
  arr = s.split('-')
  res = ""
  w = len(arr[0])
  h = len(arr)
  for i in range(w):
    if i!=0:
      res+="-"
    for j in range(h):
      res+=arr[h-j-1 if off else j][w-i-1 if off else i]
  return res


def transposeCoords(s, size=8, off=True):
  arr = [x.split(',') for x in s.split(';')]
  res = ""
  for i, sa in enumerate(arr):
    if i!=0:
      res+=";"
    if not off:
      res+=sa[1]+","+sa[0]
    else:
      res+=str(size-int(sa[1])-1)+","+str(size-int(sa[0])-1)
  return res

portion = """
    <set mask="000-111-x1x" tiles="3,0;4,0"/>
    <set mask="x1x-111-000" tiles="3,1;4,1"/>
"""

masks = re.findall(r'mask="([^"]+)"', portion)
tiles = re.findall(r'tiles="([^"]+)"', portion)
for m,t in zip(masks,tiles):
  a = transposeMask(m)
  b = transposeCoords(t,8,True)
  print("<set mask=\""+a+"\" tiles=\""+b+"\"/>")