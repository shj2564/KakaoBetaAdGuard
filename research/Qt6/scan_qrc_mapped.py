import ctypes,time,collections
from ctypes import wintypes as W
k=ctypes.WinDLL('kernel32',use_last_error=True)
class MBI(ctypes.Structure):
 _fields_=[('BaseAddress',ctypes.c_void_p),('AllocationBase',ctypes.c_void_p),('AllocationProtect',W.DWORD),('PartitionId',W.WORD),('RegionSize',ctypes.c_size_t),('State',W.DWORD),('Protect',W.DWORD),('Type',W.DWORD)]
k.OpenProcess.argtypes=[W.DWORD,W.BOOL,W.DWORD];k.OpenProcess.restype=W.HANDLE
k.VirtualQueryEx.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.POINTER(MBI),ctypes.c_size_t];k.VirtualQueryEx.restype=ctypes.c_size_t
k.ReadProcessMemory.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_size_t,ctypes.POINTER(ctypes.c_size_t)];k.ReadProcessMemory.restype=W.BOOL
h=k.OpenProcess(0x1010,False,14472); a=0;regions=[]
while a<0x7fffffff0000:
 r=MBI()
 if not k.VirtualQueryEx(h,ctypes.c_void_p(a),ctypes.byref(r),ctypes.sizeof(r)):break
 if r.RegionSize<1:break
 if r.State==0x1000 and r.Type==0x40000 and r.Protect in (0x2,0x4,0x8,0x20,0x40) and r.RegionSize<160*1048576:regions.append((r.BaseAddress,r.RegionSize))
 a=(r.BaseAddress or 0)+r.RegionSize
print('MEM_MAPPED regions',len(regions),'MB',round(sum(x[1] for x in regions)/1048576,2),flush=True)
patterns={'name16be':'AdBanner.qml'.encode('utf-16-be'),'nameascii':b'AdBanner.qml','rcc':b'qres\x00\x00\x00\x03'}
matches=collections.defaultdict(list);buf=ctypes.create_string_buffer(1048576);scanned=0;t=time.monotonic()
for base,size in regions:
 offset=0
 while offset<size:
  length=min(1048576,size-offset);got=ctypes.c_size_t()
  if k.ReadProcessMemory(h,ctypes.c_void_p(base+offset),buf,length,ctypes.byref(got)) and got.value:
   data=buf.raw[:got.value];scanned+=got.value
   for name,pat in patterns.items():
    if len(matches[name])<100:
     start=0
     while True:
      i=data.find(pat,start)
      if i<0:break
      matches[name].append(hex(base+offset+i))
      start=i+len(pat)
  offset+=length
  if time.monotonic()-t>38:break
 if time.monotonic()-t>38:break
for name,spots in matches.items():print(name,len(spots),spots[:35],flush=True)
print('scanned',round(scanned/1048576,2),'MB in',round(time.monotonic()-t,1),'sec',flush=True)
