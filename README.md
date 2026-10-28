# KakaoBetaAdGuard — Qt6 Beta Full-Height AdGuard

> **Windows x64 / KakaoTalk Beta 26.9.2.447 / Qt 6.10.3**  
> User-tested successful build: **v1.3.0.0**, 2026-10-28 (KST).
>
> **Unofficial community project.** Not affiliated with Kakao. Derived from [loopback-kr/KakaoTalkAdGuard](https://github.com/loopback-kr/KakaoTalkAdGuard) (MIT license; original copyright retained).

## 핵심 기능

기존 Windows용 KakaoTalkAdGuard를 Qt6 베타 구조에 맞게 확장했습니다. 광고 웹뷰만 숨기는 데 그치지 않고, Qt6 `QQuickColumnLayout` 안의 **광고 `QQuickLoader`(100px) + 구분선(1px)**을 비활성화합니다. 그 결과 실제 **채팅/친구 화면 `StackView`가 101px 늘어나며**, 창의 물리적 크기는 변경되지 않습니다.

- ✅ 광고 영역 및 구분선을 숨기고 실제 채팅 목록 높이를 확장
- ✅ 기존 창 크기, 기본 Windows 프레임, 드래그 기능 유지
- ✅ 모니터 가장자리·작업표시줄 경계 자석 정렬 유지
- ✅ 가드 정상 종료 시 원래 Qt6 레이아웃 복원
- ✅ 현재 사용자 Windows 로그인 자동 실행 등록 가능
- ✅ 원본 KakaoTalk 실행 파일, DLL, 대화 데이터 수정 불필요

**주의:** 이후 KakaoTalk Beta 업데이트에서는 Qt6 구성요소 명칭이나 배치가 달라질 수 있습니다. 테스트 기준 버전 이외의 호환성은 확인되지 않았습니다.

## 다운로드

| 파일 | 용도 |
| --- | --- |
| [KakaoTalkAdGuard-Qt6Beta.exe](release/windows-x64/KakaoTalkAdGuard-Qt6Beta.exe) | 가드 실행 파일 v1.3.0.0 |
| [SoNGQt6Layout.dll](release/windows-x64/SoNGQt6Layout.dll) | Qt6 내부 레이아웃 조정 (필수) |
| [SoNG-KakaoBetaAdGuard.ico](release/windows-x64/SoNG-KakaoBetaAdGuard.ico) | 별도 아이콘 |
| [설치 스크립트](scripts/Install.ps1) | 현재 사용자 대상 설치·자동 시작 등록 |
| [중지/원복](scripts/StopGuard.ps1) | 가드 종료 및 원래 레이아웃 복원 |
| [자동 실행 해제](scripts/Uninstall.ps1) | 가드 중지 및 자동 시작 레지스트리 삭제 |

**EXE와 DLL은 항상 같은 폴더에 위치해야 합니다.** DLL의 GUI 스레드 연결 동작은 보안 프로그램에서 주입 동작으로 인식될 수 있습니다. 사용자는 배포 파일 해시를 확인해 무결성을 검증할 수 있습니다.

## SHA-256 (실제 PC 최종 배포본)

```text
KakaoTalkAdGuard-Qt6Beta.exe  08D77F8E90E22A7337F04B2BE382C5F1DB8AFE2801C885D41AAD801BD90DAFD9
SoNGQt6Layout.dll            4BAD72BBABA132B67605DFEC206DD3381DA1D2AD264295E255C7A98467336FCC
```

Windows PowerShell 검증 예:
```powershell
Get-FileHash .\KakaoTalkAdGuard-Qt6Beta.exe -Algorithm SHA256
Get-FileHash .\SoNGQt6Layout.dll -Algorithm SHA256
```

## 설치 방법

1. `release/windows-x64/`의 EXE, DLL을 **같은 폴더**에 내려받습니다.
2. 두 파일의 SHA-256이 위 값과 일치하는지 확인합니다.
3. `KakaoTalkAdGuard-Qt6Beta.exe`를 실행합니다. 이전 시험판 가드가 실행 중이면 먼저 종료합니다.
4. 자동 실행을 원하면 [scripts/Install.ps1](scripts/Install.ps1)을 참조합니다. 관리 권한은 필요하지 않습니다.

배포 스크립트는 저장소를 다운로드하여 **`scripts/`와 `release/windows-x64/`을 포함한 폴더 구조**에서 실행하는 것을 전제로 합니다. 실제 사용 중인 개인 PC에는 이미 가드가 설치되어 있으므로 설치 스크립트를 다시 실행할 필요가 없습니다.

## 개발·이력

- [빌드 및 개발 환경](docs/BUILD.md)
- [Qt6 레이아웃 원리와 검증 수치](docs/ARCHITECTURE.md)
- [개발 이력·실패한 접근](docs/CHANGELOG.md)
- [테스트와 한계](docs/TESTING.md)
- [복원·삭제·자동 시작](docs/RECOVERY.md)
- [전체 파일 안내](docs/INVENTORY.md)
- [성공본 당시 PC 진단 메모](docs/README.installed.txt)
- [이전 시험판 백업](archive/legacy/)
- [Qt6 분석 코드](research/Qt6/)

## 기여·저작권

원본 Windows 가드는 **Hyunseok Lim, 2024**, MIT 라이선스입니다. 원저작자 표시 및 라이선스를 [LICENSE](LICENSE)에 보존했습니다. KakaoTalk 브랜드 및 프로그램 자체는 각각의 권리자에게 속합니다.

Qt6 베타 구조 비교 참고: [blurfx/KakaoTalkAdBlock PR #107](https://github.com/blurfx/KakaoTalkAdBlock/pull/107). 기존 공개 프로젝트를 참고했지만 해당 저장소의 파일을 그대로 복제한 것은 아닙니다.

**이 저장소에는 카카오톡 원본 설치 파일·계정·대화 DB·로컬 진단 로그를 포함하지 않습니다.**
