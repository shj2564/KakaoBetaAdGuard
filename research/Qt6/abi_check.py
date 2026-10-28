import pefile,capstone
from pathlib import Path
p=pefile.PE(r'C:\Users\SoNG-MaIN\AppData\Local\Programs\Kakao\KakaoTalk\bin\Qt6Quick.dll')
p.parse_data_directories(directories=[pefile.DIRECTORY_ENTRY['IMAGE_DIRECTORY_ENTRY_EXPORT']])
names=[b'?childItems@QQuickItem@@QEBA?AV?$QList@PEAVQQuickItem@@@@XZ',b'?contentItem@QQuickWindow@@QEBAPEAVQQuickItem@@XZ']
for n in names:
 e=next((x for x in p.DIRECTORY_ENTRY_EXPORT.symbols if x.name==n),None)
 if not e:print('missing',n);continue
 blob=p.get_data(e.address,85)
 md=capstone.Cs(capstone.CS_ARCH_X86,capstone.CS_MODE_64)
 print('====',n.decode(),hex(e.address))
 for op in md.disasm(blob,e.address):
  print(hex(op.address),op.mnemonic,op.op_str)
  if op.address>=e.address+47:break
