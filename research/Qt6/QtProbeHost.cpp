#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include <wchar.h>
int wmain(int argc,wchar_t** argv){
    if(argc<2 || argc>3){puts("usage: QtProbeHost.exe decimal_HWND [99|100|101]");return 2;}
    HWND wnd=reinterpret_cast<HWND>(_wcstoui64(argv[1],nullptr,10));
    DWORD pid=0;
    DWORD thread=GetWindowThreadProcessId(wnd,&pid);
    wprintf(L"target HWND=%p pid=%lu tid=%lu\n",wnd,pid,thread);
    if(!thread)return 3;
    HMODULE dll=LoadLibraryW(L"C:\\Users\\SoNG-MaIN\\AppData\\Local\\SoNG-KakaoBetaAdGuard\\Research\\QmlBridge\\QtProbeHook.dll");
    if(!dll){printf("LoadLibrary failed: %lu\n",GetLastError());return 4;}
    HOOKPROC callback=reinterpret_cast<HOOKPROC>(GetProcAddress(dll,"QtProbeHook"));
    if(!callback){printf("Missing export err=%lu\n",GetLastError());FreeLibrary(dll);return 5;}
    HHOOK hook=SetWindowsHookExW(WH_GETMESSAGE,callback,dll,thread);
    printf("SetWindowsHookEx handle=%p err=%lu\n",hook,GetLastError());
    if(hook){
        Sleep(150);
        int action=argc==3?_wtoi(argv[2]):99;
    BOOL posted=PostMessageW(wnd,WM_APP+action,0,0);
        printf("PostMessage=%d err=%lu\n",posted,GetLastError());
        Sleep(700);
        BOOL unhook=UnhookWindowsHookEx(hook);
        printf("Unhook=%d err=%lu\n",unhook,GetLastError());
    }
    FreeLibrary(dll);
    return hook?0:6;
}
