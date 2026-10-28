# Qt6 내부 레이아웃 처리 원리

## 이전 방식의 문제

이전 KakaoTalkAdGuard는 `EVA_ChildWindow` / `OnlineMainView_` 형태의 Win32 자식 창을 확대했습니다. KakaoTalk Beta 26.9.2.447은 Qt 6.10.3과 QML `StackView`, `ChatRoomList` 및 `ContactList`로 구성됩니다. 따라서 과거 `SetWindowPos` 기반 자식 창 조절을 그대로 사용할 수 없습니다.

실험용 C# v1~v4는 Windows 최상위 창의 실제 크기와 보이는 영역을 달리했기 때문에 아래쪽에 그림자/투명 경계선이 보이거나 크기가 누적되는 문제가 있었습니다. 시험용 Qt6Clip은 흰 칸을 화면에서 잘라냈지만 채팅 목록 자체의 크기는 늘어나지 않았습니다.

## v1.3 정상 동작 방식

1. 가드 EXE가 `KakaoTalkUI.exe`의 진짜 메인 창을 식별합니다.
2. 가드는 Windows `WH_GETMESSAGE` GUI 스레드 후크와 함께 배포 DLL을 로드합니다.
3. DLL은 Qt6 프로세스 내부의 `QQuickWindow` 콘텐츠 트리를 읽고, 해당 베타 구조의 광고 로더 `QQuickLoader`와 `HorizontalSeperator`를 찾습니다.
4. 두 객체의 `visible` 속성을 `false`로 설정합니다.
5. `QQuickColumnLayout`이 남은 높이를 `StackView`에 다시 배분합니다.
6. 반복 실행 요청은 동일 객체가 이미 비표시인 경우 다시 변경하지 않습니다.
7. 정상 종료 또는 광고 차단 해제 시 해당 객체를 다시 표시하도록 요청한 후 후크를 해제합니다.

**검증된 높이:**

| 항목 | 차단 전 | 차단 후 |
| --- | ---: | ---: |
| 전체 콘텐츠 영역 | 677px | 677px |
| `StackView` | 576px | 677px |
| `ChatRoomList` | 462px | 563px |
| 광고 로더 | 100px | 보이지 않음 |
| 광고 구분선 | 1px | 보이지 않음 |

별도 확인된 창 상태에서도 `ChatRoomList`는 **446px → 547px**로 확장됐습니다. 숫자는 테스트 창의 크기에 따라 달라지지만 차이는 **101px**입니다.

## 구현 파일

- `src/KakaoTalkAdGuard/main.cpp`: 원본 기반 C++ 가드, Qt6 메인 창 탐색, 스레드 후크 관리, 자석 정렬
- `research/Qt6/QtAdjustHook.cpp`: GUI 스레드에서 광고/구분선 객체를 비활성화하는 **최종 DLL 소스**
- `release/windows-x64/SoNGQt6Layout.dll`: 위 소스의 빌드 결과
- `research/Qt6/QtReadProbe.cpp`: Qt6 객체 트리의 읽기 전용 진단 코드

윈도우 최상위 `HWND`를 크게 만들거나 `SetWindowRgn`으로 잘라내는 접근은 **최종 v1.3의 동작 경로에서 사용하지 않습니다.** 과거 클립 함수 일부가 소스의 이력으로 남아 있을 수 있으나 최종 경로에서는 호출하지 않습니다.

## 한계

- 상기 특정 Qt6 베타에서만 실측 검증
- GUI 스레드 후크·DLL 동작 특성상 보안 프로그램이 차단할 수 있음
- 강제 종료에서는 복원 콜백이 실행되지 않을 수 있어 재시작/가드 중지 후 복구 필요
- 베타 업데이트로 클래스·구조가 바뀌면 해당 버전을 분석해 수정해야 함
