# 설치 · 원상복구 · 삭제

## 설치
GitHub 전체 저장소를 다운로드해 루트에서 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Install.ps1
```

EXE와 DLL은 반드시 같은 폴더에 두어야 합니다. 설치 스크립트는 현재 사용자의 `%LOCALAPPDATA%\SoNG-KakaoBetaAdGuard`에 배치하고, 해당 사용자 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`에 자동 실행 항목을 설정합니다.

## 원상복구 / 정상 종료

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\StopGuard.ps1
```

GUI 창에 `WM_CLOSE`를 보내 Qt6 광고 로더·구분선의 원래 표시 상태로 복원한 뒤 종료하게 합니다. **작업 관리자에서 강제 종료하면 복원 콜백이 실행되지 않을 수 있습니다.**

## 자동 시작 제거

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Uninstall.ps1
```

이 스크립트는 KakaoTalk 본체, 대화 데이터, 설치 폴더의 파일을 삭제하지 않습니다. 현재 사용자 자동 시작 값이 설치본 경로와 일치할 때만 제거합니다.

## 이전 빌드로 롤백

`archive/legacy/`에는 이전 Win32 기반 네이티브 EXE, v1~v4 C# 실험판 및 기존 메인 소스 백업이 있습니다. 대부분 최종 요구사항을 충족하지 못하거나 하단 테두리 문제가 있으므로 **최종 사용은 v1.3을 권장**합니다. 먼저 정상 종료한 뒤 필요한 파일을 수동 복원하세요.

## 장애 시

카카오톡 업데이트 이후 문제가 발생하면 최신 버전의 Qt6 객체 구조부터 검증해야 합니다. 다른 버전에서 검증 없이 파일을 교체하거나 기존 가드를 중복 실행하지 마세요.
