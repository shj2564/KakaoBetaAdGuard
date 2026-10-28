import ctypes,time,collections
from ctypes import wintypes as W
k=ctypes.WinDLL('kernel32',use_last_error=True)
class MBI(ctypes.Structure):
 _fields_=[("BaseAddress",ctypes.c_void_p),("AllocationBase",ctypes.c_void_p),("AllocationProtect",W.DWORD),("PartitionId",W.WORD),("RegionSize",ctypes.c_size_t),("State",W.DWORD),("Protect",W.DWORD),("Type",W.DWORD)]
k.OpenProcess.argtypes=[W.DWORD,W.BOOL,W.DWORD];k.OpenProcess.restype=W.HANDLE
k.VirtualQueryEx.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.POINTER(MBI),ctypes.c_size_t];k.VirtualQueryEx.restype=ctypes.c_size_t
k.ReadProcessMemory.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_size_t,ctypes.POINTER(ctypes.c_size_t)];k.ReadProcessMemory.restype=W.BOOL
k.CloseHandle.argtypes=[W.HANDLE]
h=k.OpenProcess(0x1010,False,14472)
print('Attach query',bool(h),flush=True)
patterns={}
for name in ['AdBanner.qml','OnlineMainView.qml','ChatRoomList.qml','MainWindowContent.qml','appUI.ad.banner','appUI.onlineMain','qt_resource_data','AdBanner','ChatRoomList','MainWindowContent']:
 for enc in ['ascii','utf-16-be','utf-16-le']:
  patterns[(name,enc)]=name.encode(enc)
regions=[]
addr=0
while addr < 0x7FFFFFF00000:
 mb=MBI()
 if not k.VirtualQueryEx(h,ctypes.c_void_p(addr),ctypes.byref(mb),ctypes.sizeof(mb)):break
 if mb.RegionSize<=0:break
 if mb.State==0x1000 and mb.Type in (0x1000000,0x20000) and mb.Protect in (0x2,0x4,0x8,0x20,0x40,0x80) :
  if mb.RegionSize<60*1048576:regions.append((mb.BaseAddress,mb.RegionSize,mb.Type))
 addr=(mb.BaseAddress or 0)+mb.RegionSize
print('candidate regions',len(regions),'MB',round(sum(v[1] for v in regions)/1048576,1),flush=True)
found=collections.defaultdict(list);searched=0
buffer=ctypes.create_string_buffer(1024*1024)
start=time.monotonic()
for base,size,kind in regions:
 offset=0
 while offset<size:
  length=min(1024*1024,size-offset)
  actual=ctypes.c_size_t()
  ok=k.ReadProcessMemory(h,ctypes.c_void_p(base+offset),buffer,length,ctypes.byref(actual))
  if ok and actual.value:
   data=buffer.raw[:actual.value]
   searched+=len(data)
   for (name,enc),pat in patterns.items():
    if len(found[name])>25:continue
    at=0
    while True:
     at=data.find(pat,at)
     if at<0:break
     address=base+offset+at
     if (address,enc) not in found[name]:found[name].append((address,enc,hex(kind)))
     at+=len(pat)
  offset+=length
  if time.monotonic()-start>40:break
 if time.monotonic()-start>40:break
for name,places in found.items():
 print(name, 'count',len(places),'spots',[(hex(a),b,c) for a,b,c in places[:12]],flush=True)
print('scanned_MB',round(searched/1048576,1),'time',round(time.monotonic()-start,1),flush=True)
k.CloseHandle(h)
