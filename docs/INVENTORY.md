# 저장소 구성

```text
.
├── README.md / LICENSE
├── release/windows-x64/
│   ├── KakaoTalkAdGuard-Qt6Beta.exe      # 1.3.0.0 실제 성공본
│   ├── SoNGQt6Layout.dll                # 실행에 반드시 필요
│   └── SoNG-KakaoBetaAdGuard.ico
├── src/
│   ├── KakaoTalkAdGuard.sln              # Visual Studio 솔루션
│   └── KakaoTalkAdGuard/
│       ├── main.cpp                     # 최종 C++ 가드
│       ├── main.before-clip.cpp         # 이전 Win32 구현 기록
│       ├── KakaoTalkAdGuard.vcxproj
│       └── 리소스 / 헤더 / 아이콘 등
├── research/
│   ├── Qt6/                             # Qt6 GUI 구조 분석·실행 후크
│   └── patch-history/                   # 임시 빌드·수정 스크립트
├── archive/legacy/                      # 이전 버전 소스·EXE 백업
├── scripts/                             # 상대 경로 기반 설치·중지·해제
├── tools/                               # 당시 PC에서 사용한 유틸리티 원본
├── third_party/original-upstream/       # 원본 라이선스/코드 스냅샷
└── docs/                                # 분석·빌드·테스트·유지관리
```

Git에 포함하지 않은 항목: KakaoTalk 프로그램 본체, 개인 카카오톡 계정·대화 DB, 실행 로그, 운영체제/개발자 PC 개인 설정 파일, `.git` 내부 데이터, `.obj`/`.pdb` 임시 빌드 파일, 사용자 채팅 스크린샷.
