import ctypes,time
from ctypes import wintypes as W
u=ctypes.windll.user32
class R(ctypes.Structure):
 _fields_=[('left',ctypes.c_int),('top',ctypes.c_int),('right',ctypes.c_int),('bottom',ctypes.c_int)]
h=526190
r=R()
if not u.GetWindowRect(h,ctypes.byref(r)):raise Exception('Qt main not found')
w=r.right-r.left;ht=r.bottom-r.top
print('old geometry',r.left,r.top,w,ht)
flags=0x0001|0x0004|0x0010|0x0200
try:
 # DISPLAY4 working area: -1080,-430 through 0,1442
 # Intentional near-left and near-taskbar position (+12,+11px from desired snap)
 u.SetWindowPos(h,0,-1076,769,0,0,flags)
 time.sleep(0.65)
 s=R();u.GetWindowRect(h,ctypes.byref(s))
 print('magnet result',s.left,s.top,'size',s.right-s.left,s.bottom-s.top)
 print('expected',-1088,780,'matched',s.left==-1088 and s.top==780)
finally:
 u.SetWindowPos(h,0,r.left,r.top,0,0,flags)
