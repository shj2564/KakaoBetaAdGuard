SoNG KakaoTalkAdGuard - QT6 FULL HEIGHT v1.3.0.0
Date: 2026-10-28 (KST)
Tested KakaoTalk: 26.9.2.447 beta, Qt 6.10.3
Reference: https://github.com/loopback-kr/KakaoTalkAdGuard (MIT)
This is a locally built unofficial C++ fork, not an official KakaoTalk product.

WHAT IS NOW FIXED
- Finds the actual KakaoTalk Qt6 MainWindow, not chat pop-up windows.
- Hooks the Qt UI thread and identifies the exact QQuickColumnLayout children.
- Sets the advertisement QQuickLoader and HorizontalSeperator to invisible.
- Qt's own ColumnLayout then reallocates the complete original height to StackView.
- ChatRoomList is extended by an actual 101px, rather than blank-area clipping.
- Keeps the FULL original native window height and Windows frame.
- No DWM clip, SetWindowRgn, overlay, blank white strip, or transparent phantom border.
- Keeps the existing monitor-boundary/taskbar magnetic window docking behavior.
- When this guard exits normally or ad hiding is disabled, Qt ad elements become visible again.
- Handles dynamic main-window creation and periodically reapplies the layout.
- Uses SoNG's custom charcoal, yellow, and mint tray icon.
- Starts automatically via HKCU Run, no command-line window.

INSTALLED FILES
KakaoTalkAdGuard-Qt6Beta.exe    - Native Windows x64 helper, VERSION 1.3.0.0-Qt6FullHeight
SoNGQt6Layout.dll              - Native Qt6 UI-thread helper (required; keep beside EXE)
StopGuard.ps1                  - Normal close and restore
NativeStop.py                  - Native guard WM_CLOSE request used by StopGuard.ps1
Uninstall.ps1                  - Stop and remove just the autostart entry
KakaoTalkAdGuard_BetaFork\    - Full editable Visual Studio C++ source project
Research\QmlBridge\QtAdjustHook.cpp - Qt6 layout helper source (and related research)
Backups\qt6_fullheight_research\ - Previous native builds and source backup
README.txt                     - This guide

AUTOSTART
HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SoNG-KakaoBetaAdGuard
points to KakaoTalkAdGuard-Qt6Beta.exe

VERIFIED BEHAVIOR
- Initial Qt main content: 677px; StackView 576px, ChatRoomList 462px.
- Hiding 100px ad QQuickLoader + 1px separator expands StackView to 677px, ChatRoomList to 563px.
- Test build with later window state: StackView 560px -> 661px; ChatRoomList 446px -> 547px.
- Real screen capture confirmed chat messages extend to the original native bottom edge.
- Native outer window was not resized or clipped (GetWindowRgnBox region type 0).
- Window manual resize 416x670 -> 458x706 updated list height 547 -> 583; restore to 416x670 returned list to 547.
- Closing native helper restored Qt loader/separator visibility and original list height.
- Repeated automatic Qt requests left both advertisement objects hidden.
- Qt main remained expanded even while a separate normal chat window was open.
- Current KakaoTalk and guard responded normally.
- Current-user autostart registry confirmed. A full Windows reboot was not tested.

PROTECTION / ROLLBACK
- No changes to KakaoTalkUI.exe, Qt DLLs, signed binaries, chat database, user profile, or hosts file.
- No replacement QML override file remains; original qmldir intact.
- StopGuard.ps1 stops normally and restores the ad layout. Uninstall.ps1 also removes autostart.
- Remove the helper EXE + DLL only AFTER stopping it, or use the saved preFullHeight EXE for rollback.
- KakaoTalk beta updates may rename classes or change QML structure. Validate after any version change.
- Qt layout control is local and uses native Win32 thread hooks with a companion DLL, so third-party security
  tools may flag the DLL's UI-thread attachment as an injection behavior. No remote server or network is used.

CURRENT RELEASE SHA256
EXE: 08D77F8E90E22A7337F04B2BE382C5F1DB8AFE2801C885D41AAD801BD90DAFD9
DLL: 4BAD72BBABA132B67605DFEC206DD3381DA1D2AD264295E255C7A98467336FCC
