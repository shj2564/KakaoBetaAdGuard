#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdint.h>
#include <stddef.h>
#include <stdarg.h>
#include <string.h>
struct QtList { void* d;void** ptr;ptrdiff_t size; };
typedef void(__fastcall* GetQtWindows)(QtList*);
typedef unsigned long long(__fastcall* QtWinId)(void*);
typedef bool(__fastcall* QtInherits)(void*,const char*);
typedef const void*(__fastcall* QtMeta)(void*);
typedef const char*(__fastcall* QtClassName)(const void*);
typedef void*(__fastcall* QtContent)(void*);
typedef void(__fastcall* QtChildItems)(void*,QtList*);
typedef double(__fastcall* QtMeasure)(void*);
static GetQtWindows allWindows;
static QtWinId winId;
static QtInherits inherits;
static QtClassName className;
static QtContent content;
static QtChildItems childItems;
static QtMeasure getX,getY,getW,getH;
static void* seen[2200];static int nseen=0;
static int lines=0;
static char logpath[]="C:\\Users\\SoNG-MaIN\\AppData\\Local\\SoNG-KakaoBetaAdGuard\\Research\\QmlBridge\\qt_readonly_tree.log";
static void Log(const char* fmt,...){
 if(lines++>2200)return;FILE* f=nullptr;
 if(fopen_s(&f,logpath,"a") || !f)return;
 va_list args;va_start(args,fmt);vfprintf(f,fmt,args);va_end(args);
 fputc('\n',f);fclose(f);
}
static const char* GetClass(void* o){
 if(!o)return "<nil>";
 // Q_OBJECT declares QObject::metaObject as the first virtual entry.
 void** vtbl=*(void***)o;
 if(!vtbl)return "<no-vtbl>";
 QtMeta dynMeta=(QtMeta)vtbl[0];
 if(!dynMeta)return "<no-meta>";
 const void* meta=dynMeta(o);
 if(!meta)return "<meta-null>";
 const char* name=className(meta);
 return name?name:"<unknown>";
}
static bool Visited(void* p){
 for(int i=0;i<nseen;i++)if(seen[i]==p)return true;
 if(nseen<2200)seen[nseen++]=p;return false;
}
static void Walk(void* obj,int depth){
 if(!obj||depth>19||nseen>=1300||Visited(obj))return;
 if(!inherits(obj,"QQuickItem"))return;
 const char* name=GetClass(obj);
 double x=getX(obj),y=getY(obj),w=getW(obj),h=getH(obj);
 if(depth<=6||strstr(name,"List")||strstr(name,"Ad")||
    (w>250 && h>80 && depth<=14)){
   Log("%03d d%02d %p %s x=%.0f y=%.0f w=%.0f h=%.0f",
       nseen,depth,obj,name,x,y,w,h);
 }
 QtList children={};
 childItems(obj,&children);
 if(children.ptr&&children.size>0&&children.size<=800) {
     for(ptrdiff_t i=0;i<children.size;i++)Walk(children.ptr[i],depth+1);
 }
}
static bool Resolve() {
 HMODULE gui=GetModuleHandleW(L"Qt6Gui.dll");
 HMODULE quick=GetModuleHandleW(L"Qt6Quick.dll");
 HMODULE core=GetModuleHandleW(L"Qt6Core.dll");
 if(!gui||!quick||!core)return false;
#define FN(v,lib,sym) v=(decltype(v))GetProcAddress(lib,sym)
 FN(allWindows,gui,"?allWindows@QGuiApplication@@SA?AV?$QList@PEAVQWindow@@@@XZ");
 FN(winId,gui,"?winId@QWindow@@QEBA_KXZ");
 FN(inherits,core,"?inherits@QObject@@QEBA_NPEBD@Z");
 FN(className,core,"?className@QMetaObject@@QEBAPEBDXZ");
 FN(content,quick,"?contentItem@QQuickWindow@@QEBAPEAVQQuickItem@@XZ");
 FN(childItems,quick,"?childItems@QQuickItem@@QEBA?AV?$QList@PEAVQQuickItem@@@@XZ");
 FN(getX,quick,"?x@QQuickItem@@QEBANXZ");
 FN(getY,quick,"?y@QQuickItem@@QEBANXZ");
 FN(getW,quick,"?width@QQuickItem@@QEBANXZ");
 FN(getH,quick,"?height@QQuickItem@@QEBANXZ");
#undef FN
 return allWindows&&winId&&inherits&&className&&content&&childItems&&getX&&getY&&getW&&getH;
}
static void Probe(HWND target){
 Log("Probe start pid=%lu target=%p",GetCurrentProcessId(),target);
 if(!Resolve()){Log("Qt exports missing");return;}
 QtList windows={};allWindows(&windows);
 Log("allWindows ptr=%p size=%lld",windows.ptr,(long long)windows.size);
 if(windows.size<1||windows.size>100||!windows.ptr)return;
 for(ptrdiff_t i=0;i<windows.size;i++){
   void* qwin=windows.ptr[i];if(!qwin)continue;
   auto wid=winId(qwin);
   const char* name=GetClass(qwin);
   Log("window[%lld] ptr=%p HWND=%llx class=%s",
       (long long)i,qwin,wid,name);
   if(wid != (unsigned long long)(uintptr_t)target)continue;
   if(!inherits(qwin,"QQuickWindow")){Log("Main is not QQuickWindow");return;}
   void* item=content(qwin);
   Log("MAIN content=%p class=%s",item,GetClass(item));
   nseen=0;Walk(item,0);
   Log("END inspected=%d nodes",nseen);return;
 }
 Log("Main Qt window not found");
}
static void Guard(HWND target){
 __try {Probe(target);}
 __except(EXCEPTION_EXECUTE_HANDLER){
     Log("EXCEPTION=0x%lx",GetExceptionCode());
 }
}
extern "C" __declspec(dllexport) LRESULT CALLBACK QtProbeHook(int code,WPARAM w,LPARAM l){
 if(code>=0&&l){
  MSG* msg=(MSG*)l;
  if(msg->message==WM_APP+99)Guard(msg->hwnd);
 }
 return CallNextHookEx(nullptr,code,w,l);
}
BOOL WINAPI DllMain(HINSTANCE h,DWORD reason,LPVOID){
 if(reason==DLL_PROCESS_ATTACH)DisableThreadLibraryCalls(h);
 return TRUE;
}
