from pathlib import Path
b=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard\Research\QmlBridge')
src=(b/'QtReadProbe.cpp').read_text(encoding='utf-8')
src=src.replace('static QtMeasure getX,getY,getW,getH;','''static QtMeasure getX,getY,getW,getH;
typedef void(__fastcall* QtSetVisible)(void*,bool);
typedef bool(__fastcall* QtIsVisible)(void*);
static QtSetVisible setVisible;
static QtIsVisible isVisible;
static void* adLoader=nullptr;
static void* separator=nullptr;
static void* stackView=nullptr;''')
src=src.replace('static void Walk(void* obj,int depth){','''static void Walk(void* obj,int depth){''')
src=src.replace(' double x=getX(obj),y=getY(obj),w=getW(obj),h=getH(obj);',''' double x=getX(obj),y=getY(obj),w=getW(obj),h=getH(obj);
 if(depth==4 && w>390 && w<440) {
   if(strstr(name,"HorizontalSeperator") && y>530 && y<640 && h<=3)separator=obj;
   if(strstr(name,"QQuickLoader") && y>530 && y<650 && h>85 && h<125)adLoader=obj;
   if(strstr(name,"StackView") && y<5 && h>450 && h<650)stackView=obj;
 }''')
src=src.replace(' FN(getH,quick,"?height@QQuickItem@@QEBANXZ");',''' FN(getH,quick,"?height@QQuickItem@@QEBANXZ");
 FN(setVisible,quick,"?setVisible@QQuickItem@@QEAAX_N@Z");
 FN(isVisible,quick,"?isVisible@QQuickItem@@QEBA_NXZ");''')
src=src.replace('getW&&getH;', 'getW&&getH&&setVisible&&isVisible;')
src=src.replace('static void Probe(HWND target){','static void Probe(HWND target,int action){')
src=src.replace('   nseen=0;Walk(item,0);\\n   Log("END inspected=%d nodes",nseen);return;', '''   nseen=0;adLoader=nullptr;separator=nullptr;stackView=nullptr;
   Walk(item,0);
   Log("OBJECTS stack=%p adLoader=%p separator=%p",stackView,adLoader,separator);
   if(adLoader && separator && stackView) {
      Log("BEFORE action=%d stackH=%.1f loaderVis=%d separatorVis=%d",action,
            getH(stackView),isVisible(adLoader),isVisible(separator));
      if(action==100 || action==101) {
          bool show=(action==101);
          setVisible(separator,show);
          setVisible(adLoader,show);
          Log("AFTER action=%d stackH=%.1f loaderVis=%d separatorVis=%d",action,
              getH(stackView),isVisible(adLoader),isVisible(separator));
      }
   }
   Log("END inspected=%d nodes",nseen);return;''')
src=src.replace('static void Guard(HWND target){','static void Guard(HWND target,int action){')
src=src.replace(' __try {Probe(target);}', ' __try {Probe(target,action);}')
src=src.replace('  if(msg->message==WM_APP+99)Guard(msg->hwnd);','''  if(msg->message==WM_APP+99 || msg->message==WM_APP+100 || msg->message==WM_APP+101)
      Guard(msg->hwnd,(int)(msg->message-WM_APP));''')
src=src.replace('qt_readonly_tree.log','qt_adjust_test.log')
(b/'QtAdjustHook.cpp').write_text(src,encoding='utf-8')
print('QtAdjustHook.cpp written',len(src))
