# SoNG Kakao Beta AdGuard v1.4.0 — 단일 실행파일
제작자: **SoNG** / 기존 원본: loopback.kr (MIT 라이선스 유지)
검증: 2026-11-11 Windows 64비트 / KakaoTalk Beta 26.9.2.447 (Qt 6.10.3)

## 배포
- `release/single-exe/SoNG-KakaoBetaAdGuard_v1.4.0_Win64.exe`: 설치 없이 EXE 하나로 실행.
- `release/single-exe/SoNG-KakaoBetaAdGuard_v1.4.0_Win64.zip`: 단일 EXE 파일만 들어 있는 ZIP.
- EXE SHA-256: `BF8DEB32A73D385B9B54D3D32FDCC5C0070EEC9394C01AE1CB05781651DB1EAF`

## 트레이 우클릭 메뉴
- **광고차단 시작 ON/OFF**: 비활성화 즉시 Qt6 광고 영역 원복, 활성화 시 다시 차단
- **Windows 자동실행 ON/OFF**: 현재 사용자(HKCU) 로그인 자동 실행 등록/해제
- **트레이 아이콘 표시 ON/OFF**: OFF 후 EXE를 재실행하면 ON 복원. 숨기기 전 안내창 제공.
- **종료**: 원래 Qt6 광고 레이아웃 정상 복원 시도
- 제작자는 SoNG으로, 원저작자는 loopback.kr (MIT)으로 표기

## 단일 EXE 작동 방식
Qt6 레이아웃 제어에는 Windows WH_GETMESSAGE 후크용 보조 DLL이 필수입니다.
v1.4는 **검증된 v1.3 DLL 그대로** EXE 리소스에 내장합니다.
실행 중 필요한 경우 `%LOCALAPPDATA%\SoNG-KakaoBetaAdGuard\Runtime\v1.4.0\SoNGQt6Layout.dll`로 자동 추출합니다.
따라서 사용자에게 배포할 파일은 EXE 하나지만, 운영 중에는 추출된 DLL이 존재합니다.
추출할 때 리소스와 기존 파일의 바이트 단위 일치 여부를 검사합니다.

## 기존 v1.3 사용자
기존 `KakaoTalkAdGuard-Qt6Beta.exe`를 **트레이에서 먼저 종료한 다음** 새 EXE를 실행합니다.
중복 차단으로 인한 Qt6 레이아웃 충돌을 막기 위해 v1.4는 기존 가드가 실행 중일 때 안내하고 종료합니다.
기존 설치·자동실행은 자동 삭제하지 않습니다. 신버전에서 자동실행을 ON 하면 기존 실행경로를 신버전 경로로 대체합니다.
이전 v1.3 파일/압축본은 삭제하지 않았습니다.

## 검증
- x64 PE, 버전 메타데이터 회사명 SoNG, 아이콘 리소스 포함
- 내장 DLL SHA-256: `4BAD72BBABA132B67605DFEC206DD3381DA1D2AD264295E255C7A98467336FCC` (원본 성공본과 일치)
- SoNG-HoME 실제 KakaoTalk UI 동작 중 새 실행파일 시작 및 Qt6 DLL 추출 확인
- 광고차단 ON/OFF 토글, 자동실행 ON/OFF 레지스트리 반영, 트레이 숨김/재실행 복원 시험 통과
- 시험 뒤 기존 v1.3 프로그램 재실행 및 설정 원상 복구
- 새 버전은 Windows 전체 재부팅 자동실행, 다른 모니터/DPI 및 향후 KakaoTalk 버전까지 테스트한 것은 아님.

기존 KakaoTalk 실행 파일이나 채팅 DB를 수정하지 않습니다.
