#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
extern "C" __declspec(dllexport) LRESULT CALLBACK QtProbeHook(int nCode,WPARAM w,LPARAM l) {
    if(nCode>=0 && l) {
        const MSG* m=(const MSG*)l;
        if(m->message==WM_APP+99) {
            FILE* f=nullptr;
            if(!fopen_s(&f,"C:\\Users\\SoNG-MaIN\\AppData\\Local\\SoNG-KakaoBetaAdGuard\\Research\\QmlBridge\\hook_status.txt","a") && f) {
                fprintf(f,"Probe received pid=%lu tid=%lu hwnd=%p QtCore=%p QtQuick=%p\n",
                    GetCurrentProcessId(),GetCurrentThreadId(),m->hwnd,
                    GetModuleHandleW(L"Qt6Core.dll"),GetModuleHandleW(L"Qt6Quick.dll"));
                fclose(f);
            }
        }
    }
    return CallNextHookEx(nullptr,nCode,w,l);
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD reason,LPVOID){
    if(reason==DLL_PROCESS_ATTACH)DisableThreadLibraryCalls(h);
    return TRUE;
}
