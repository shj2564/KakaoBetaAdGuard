# Windows x64 빌드 가이드

검증 PC: Windows, Visual Studio 2026 Build Tools / MSVC v145 (v18), C++17, Windows SDK. VS 프로젝트는 [src/KakaoTalkAdGuard.sln](../src/KakaoTalkAdGuard.sln)에 있습니다.

## EXE 빌드

Visual Studio Developer PowerShell 또는 개발자 명령 프롬프트에서:

```bat
MSBuild.exe src\KakaoTalkAdGuard.sln /m:1 /t:Build /p:Configuration=Release /p:Platform=x64 /p:PlatformToolset=v145
```

원본 프로젝트의 상대 경로가 유지되도록 솔루션과 프로젝트 디렉터리는 `src/` 하위에 그대로 두었습니다.

## Qt6 DLL 빌드

MSVC x64 개발 환경에서:

```bat
cl.exe /nologo /std:c++17 /EHsc /LD /O2 /Fe:SoNGQt6Layout.dll research\Qt6\QtAdjustHook.cpp user32.lib
```

빌드된 DLL은 가드 실행 파일과 **동일한 디렉터리**에 복사해야 합니다. 해당 컴파일 명령은 프로젝트 루트에서 실행하는 예시이며, 컴파일러 출력 경로는 작업 디렉터리를 따릅니다.

## 정식 배포

검증된 결과물을 `release/windows-x64/` 아래에 배치하고 SHA-256을 갱신합니다. 새로운 베타에서 실제 목록 확장 검증 전에는 기존 성공본을 교체하거나 릴리스 번호를 올리지 않습니다.

**주의:** `research/Qt6/`의 진단용 프로그램과 `research/patch-history/`의 실험 코드는 배포 실행 항목이 아닙니다. 카카오톡 실행 파일 자체를 수정하거나 별도의 KakaoTalk DLL을 배포하지 않습니다.
