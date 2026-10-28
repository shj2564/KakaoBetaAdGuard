import ctypes,struct,time
from ctypes import wintypes as W
k=ctypes.WinDLL('kernel32',use_last_error=True)
class MBI(ctypes.Structure):
 _fields_=[('BaseAddress',ctypes.c_void_p),('AllocationBase',ctypes.c_void_p),('AllocationProtect',W.DWORD),('PartitionId',W.WORD),('RegionSize',ctypes.c_size_t),('State',W.DWORD),('Protect',W.DWORD),('Type',W.DWORD)]
k.OpenProcess.argtypes=[W.DWORD,W.BOOL,W.DWORD];k.OpenProcess.restype=W.HANDLE
k.VirtualQueryEx.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.POINTER(MBI),ctypes.c_size_t];k.VirtualQueryEx.restype=ctypes.c_size_t
k.ReadProcessMemory.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_size_t,ctypes.POINTER(ctypes.c_size_t)];k.ReadProcessMemory.restype=W.BOOL
h=k.OpenProcess(0x1010,False,14472)
regions=[];a=0
while a<0x7fffffff0000:
 x=MBI()
 if not k.VirtualQueryEx(h,ctypes.c_void_p(a),ctypes.byref(x),ctypes.sizeof(x)):break
 if x.RegionSize<1:break
 if x.State==4096 and x.Type in (0x20000,0x1000000) and x.Protect in (2,4,8,32,64,128) and x.RegionSize<65*1048576:
  regions.append((x.BaseAddress,x.RegionSize))
 a=(x.BaseAddress or 0)+x.RegionSize
pattern=b'\x00\x00\x00\x00\x00\x02'
buffer=ctypes.create_string_buffer(1048576)
roots=[];scanned=0;start=time.monotonic()
for base,length in regions:
 offset=0
 while offset<length:
  n=min(1048576,length-offset)
  got=ctypes.c_size_t()
  if k.ReadProcessMemory(h,ctypes.c_void_p(base+offset),buffer,n,ctypes.byref(got)) and got.value:
   data=buffer.raw[:got.value];scanned+=got.value
   at=0
   while True:
    i=data.find(pattern,at)
    if i<0:break
    if i+14<len(data):
     ch=struct.unpack_from('>I',data,i+6)[0]
     first=struct.unpack_from('>I',data,i+10)[0]
     if first==1 and 1<=ch<=3000:
      roots.append((base+offset+i,ch))
    at=i+2
  offset+=n
 if time.monotonic()-start>35:break
print('found root candidates',len(roots),'after MB',round(scanned/1048576,1),'secs',round(time.monotonic()-start,1))
print('first candidates',[(hex(address),n) for address,n in roots[:20]])
valid=[]
for addr,ch in roots:
 n=min(ch+1,16)
 block=ctypes.create_string_buffer(22*n)
 got=ctypes.c_size_t()
 if not k.ReadProcessMemory(h,ctypes.c_void_p(addr),block,22*n,ctypes.byref(got)) or got.value<22*n:continue
 data=block.raw
 good=0
 for index in range(1,n):
  pos=index*22
  nameoff=struct.unpack_from('>I',data,pos)[0]
  flags=struct.unpack_from('>H',data,pos+4)[0]
  if nameoff<0x04000000 and flags in (0,1,2,4,5):good+=1
 if good>=n-2 and n>=5:valid.append((hex(addr),ch,good,n-1,[(hex(struct.unpack_from('>I',data,i*22)[0]),struct.unpack_from('>H',data,i*22+4)[0]) for i in range(1,min(n,6))]))
print('validated candidates',len(valid))
for a in valid[:35]:print(a)
