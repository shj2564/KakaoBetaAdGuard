import ctypes, collections
from ctypes import wintypes as W
k=ctypes.WinDLL('kernel32',use_last_error=True)
class MBI(ctypes.Structure):
 _fields_=[("BaseAddress",ctypes.c_void_p),("AllocationBase",ctypes.c_void_p),("AllocationProtect",W.DWORD),("PartitionId",W.WORD),("RegionSize",ctypes.c_size_t),("State",W.DWORD),("Protect",W.DWORD),("Type",W.DWORD)]
k.OpenProcess.argtypes=[W.DWORD,W.BOOL,W.DWORD];k.OpenProcess.restype=W.HANDLE
k.VirtualQueryEx.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.POINTER(MBI),ctypes.c_size_t];k.VirtualQueryEx.restype=ctypes.c_size_t
k.CloseHandle.argtypes=[W.HANDLE]
pid=14472
h=k.OpenProcess(0x1010,False,pid)
print('Open',bool(h),'error',ctypes.get_last_error(),'MBI struct',ctypes.sizeof(MBI))
if not h:quit()
addr=0;count=0;regions=collections.defaultdict(lambda:[0,0])
while addr < 0x7FFFFFF00000:
 info=MBI()
 r=k.VirtualQueryEx(h,ctypes.c_void_p(addr),ctypes.byref(info),ctypes.sizeof(info))
 if not r: break
 if info.RegionSize<1:break
 if info.State==0x1000:
  row=regions[(hex(info.Type),hex(info.Protect))]
  row[0]+=1;row[1]+=int(info.RegionSize)
 count+=1
 addr=(info.BaseAddress or addr)+info.RegionSize
print('Total regions',count,'committed bytes',sum(v[1] for v in regions.values()))
print(sorted([(k,v[0],round(v[1]/1048576,1)) for k,v in regions.items()],key=lambda x:-x[2])[:22])
k.CloseHandle(h)
