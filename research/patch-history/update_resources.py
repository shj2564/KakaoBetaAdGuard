from pathlib import Path
import shutil
root=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard')
proj=root/'KakaoTalkAdGuard_BetaFork'/'KakaoTalkAdGuard'
rc=proj/'KakaoTalkAdGuard.rc'
s=rc.read_text(encoding='utf-16')
s=s.replace('IDI_LOGO                ICON                    "KakaotalkADGuard.ico"',
            'IDI_LOGO                ICON                    "SoNG-AdGuard.ico"')
s=s.replace('IDS_APP_VERSION         "1.2.0.1"','IDS_APP_VERSION         "1.2.0.1-Qt6Beta"')
s=s.replace('FILEVERSION 1,2,0,1','FILEVERSION 1,2,6,0')
s=s.replace('PRODUCTVERSION 1,2,0,1','PRODUCTVERSION 1,2,6,0')
s=s.replace('VALUE "FileVersion", "1.2.0.1"','VALUE "FileVersion", "1.2.6.0-Qt6Beta"')
s=s.replace('VALUE "ProductVersion", "1.2.0.1"','VALUE "ProductVersion", "1.2.6.0-Qt6Beta"')
rc.write_text(s,encoding='utf-16')
shutil.copy2(root/'SoNG-KakaoBetaAdGuard.ico',proj/'SoNG-AdGuard.ico')
print('Updated original RC with custom SoNG icon and beta identification.')
