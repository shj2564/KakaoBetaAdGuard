import ctypes,struct
from ctypes import wintypes as W
k=ctypes.WinDLL('kernel32',use_last_error=True)
k.OpenProcess.argtypes=[W.DWORD,W.BOOL,W.DWORD];k.OpenProcess.restype=W.HANDLE
k.ReadProcessMemory.argtypes=[W.HANDLE,ctypes.c_void_p,ctypes.c_void_p,ctypes.c_size_t,ctypes.POINTER(ctypes.c_size_t)];k.ReadProcessMemory.restype=W.BOOL
h=k.OpenProcess(0x1010,False,14472)
def read(addr,n):
 b=ctypes.create_string_buffer(n);a=ctypes.c_size_t()
 if not k.ReadProcessMemory(h,ctypes.c_void_p(addr),b,n,ctypes.byref(a)):return b''
 return b.raw[:a.value]
for name,adr in [('AdBanner.qml',0xff96c3f),('OnlineMainView.qml',0xff97d41),('ChatRoomList.qml',0x1002e18f),('AdBanner.qml second',0x10062abf)]:
 b=read(adr-24,150)
 print('====',name,hex(adr),'read',len(b))
 print('prefix_bytes',b[:32].hex(' '))
 for pos in range(0,30):
  n=struct.unpack('>H',b[pos:pos+2])[0]
  if 5<=n<=60:
   string=b[pos+6:pos+6+n*2].decode('utf-16-be',errors='ignore')
   if '.qml' in string:
    print('QT_NAME offset',pos-24,'length',n,'hash',b[pos+2:pos+6].hex(),'decoded',repr(string[:70]))
 print('utf16_be_match',b.find(name.split(' ')[0].encode('utf-16-be'))-24)
k.CloseHandle(h)
