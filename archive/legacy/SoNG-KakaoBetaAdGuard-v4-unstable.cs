// SoNG-KakaoBetaAdGuard v2: Qt6 main panel full-height reflow without any cover overlay.
// Installed on a local, user-authorized Windows PC. Does not modify KakaoTalk binaries or profile data.
using System;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("SoNG Kakao Beta AdGuard")]
[assembly: AssemblyDescription("Qt6 ad layout reflow and magnetic window snapping")]
[assembly: AssemblyVersion("4.0.0.0")]
[assembly: AssemblyFileVersion("4.0.0.0")]

internal static class Win {
    public delegate bool EnumProc(IntPtr h, IntPtr data);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc proc, IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr hwnd, EnumProc proc, IntPtr data);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd, StringBuilder name, int len);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, StringBuilder name, int len);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int code);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr z, int x, int y, int w, int h, uint flags);
    [DllImport("gdi32.dll")] public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr gdi);
    [DllImport("user32.dll")] public static extern int SetWindowRgn(IntPtr hwnd, IntPtr rgn, bool redraw);
    [DllImport("user32.dll")] public static extern int GetWindowRgnBox(IntPtr hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] public static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int virtualKey);
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {public int X,Y;}
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT pos);
    [DllImport("user32.dll")] public static extern IntPtr MonitorFromPoint(POINT point,uint flags);
    public delegate void WinEventCallback(IntPtr hook,uint eventType,IntPtr hwnd,int idObject,int idChild,uint thread,uint eventTime);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint eventMin,uint eventMax,IntPtr hmodule,WinEventCallback callback,uint pid,uint tid,uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hwnd,uint cmd);
}
// Temporary in-memory visual continuity during a native Qt window drag.
// Keeps the last 100px of the previously visible chat list without altering messages.
internal sealed class DragCover : Form {
    private Bitmap image;
    public DragCover(){
        Text="SoNG drag preview";
        FormBorderStyle=FormBorderStyle.None;
        StartPosition=FormStartPosition.Manual;
        ShowInTaskbar=false;
        DoubleBuffered=true;
        Location=new Point(-32000,-32000);
        Size=new Size(400,100);
        BackColor=Color.FromArgb(34,34,34);
    }
    protected override bool ShowWithoutActivation {get{return true;}}
    protected override CreateParams CreateParams {
        get {
            var p=base.CreateParams;
            p.ExStyle |= 0x00000080 /* toolwindow */ | 0x08000000 /* noactivate */ |
                0x00000020 /* transparent hit-test */;
            return p;
        }
    }
    public void CaptureFrame(IntPtr hwnd,Win.RECT r,int visibleHeight,int gap,int height){
        if(image!=null){image.Dispose();image=null;}
        int width=r.Right-r.Left;
        if(width<100 || visibleHeight<250)return;
        try {
            image=new Bitmap(width,visibleHeight);
            using(var graphics=Graphics.FromImage(image)) {
                graphics.CopyFromScreen(r.Left,r.Top,0,0,new Size(width,visibleHeight));
            }
        } catch {
            if(image!=null){image.Dispose();image=null;}
        }
        Invalidate();
    }
    public void Place(IntPtr main,Win.RECT rect,int visibleHeight,int gap,int height){
        if(height<10)return;
        int x=rect.Left;
        int y=rect.Top;
        int width=rect.Right-rect.Left;
        if(width<100)return;
        if(!Visible)Show();
        IntPtr above=Win.GetWindow(main,3 /* GW_HWNDPREV */);
        uint flags=0x0010u|0x0200u;
        if(above==Handle)flags|=0x0004u; // no Z-order change
        Win.SetWindowPos(Handle,above,x,y,width,visibleHeight,flags);
    }
    public void Clear(){
        if(Visible)Hide();
        if(image!=null){image.Dispose();image=null;}
    }
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);
        e.Graphics.Clear(Color.FromArgb(34,34,34));
        if(image!=null)e.Graphics.DrawImageUnscaled(image,0,0);
    }
    protected override void OnFormClosed(FormClosedEventArgs e){
        Clear();base.OnFormClosed(e);
    }
}
internal sealed class Guardian : Form {
    private const uint RszFlags = 0x0002u | 0x0004u | 0x0010u | 0x0200u;
    private readonly Timer poll = new Timer();
    private readonly NotifyIcon tray = new NotifyIcon();
    private readonly ToolStripMenuItem onOff;
    private readonly ToolStripMenuItem snapMenu;
    private readonly Icon appIcon;
    private bool snapEnabled=true;
    private readonly DragCover dragCover = new DragCover();
    private bool moving=false;
    private bool suspended=false;
    private int suspendedExpansion=0;
    private int dragStartVisibleHeight=0;
    private int dragStartWidth=0;
    private IntPtr moveHook=IntPtr.Zero;
    private Win.WinEventCallback moveCallback;
    private int lastMoveSeen=0;
    private int lastObservedX=int.MinValue, lastObservedY=int.MinValue;
    private int lastObservedWidth=-1, lastObservedHeight=-1;
    private bool snapPending=false;
    private IntPtr mainWindow = IntPtr.Zero;
    private int expansion = 0;
    private int lastWidth = -1, lastClip = -1;
    private bool enabled = true;
    private bool disposed = false;
    private readonly string logFile = System.IO.Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "status.log");

    private void Log(string text) {
        try { System.IO.File.AppendAllText(logFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + text + Environment.NewLine); }
        catch { }
    }
    public Guardian() {
        Text = "SoNG Kakao Beta AdGuard";
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(-32000, -32000);
        Size = new Size(1,1);
        Opacity = 0;
        onOff = new ToolStripMenuItem("광고칸 제거 + 목록 확장", null, (s,e) => {
            enabled = !enabled;
            onOff.Checked = enabled;
            if (!enabled) Restore();
        });
        onOff.Checked = true;
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("카카오톡 Qt6 베타 · 전체 목록 확장") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(onOff);
        snapMenu = new ToolStripMenuItem("화면·작업표시줄 경계 자석 정렬", null, (s,e)=>{
            snapEnabled=!snapEnabled;
            snapMenu.Checked=snapEnabled;
            snapPending=false;
        });
        snapMenu.Checked=true;
        menu.Items.Add(snapMenu);
        menu.Items.Add(new ToolStripMenuItem("종료 (원상복구)", null, (s,e)=>Close()));
        Icon loaded=null;
        try {loaded=Icon.ExtractAssociatedIcon(Application.ExecutablePath);} catch {}
        appIcon=loaded;
        tray.Icon = appIcon ?? SystemIcons.Application;
        tray.Text = "SoNG Kakao Beta AdGuard v4";
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        poll.Interval = 125;
        poll.Tick += (s,e)=>Tick();
        poll.Start();
        Log("Started v4; move-safe real geometry and taskbar-bounded magnetic snapping.");
    }
    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        Hide();
    }

    private static int AdHeight(IntPtr hwnd) {
        int dpi = 96;
        try { uint v = Win.GetDpiForWindow(hwnd); if(v>=96 && v<=384)dpi=(int)v; }
        catch(EntryPointNotFoundException) { }
        return (int)Math.Round(100.0*dpi/96.0);
    }
    private static IntPtr FindMain() {
        IntPtr main = IntPtr.Zero;
        long max = -1;
        foreach(var process in Process.GetProcessesByName("KakaoTalkUI")) {
            int id = process.Id;
            Win.EnumWindows((h,lp) => {
                uint pid; Win.GetWindowThreadProcessId(h,out pid);
                if(pid != (uint)id || !Win.IsWindowVisible(h) || Win.IsIconic(h)) return true;
                var c = new StringBuilder(128); Win.GetClassName(h,c,c.Capacity);
                if(!c.ToString().StartsWith("Qt") || !c.ToString().EndsWith("QWindowIcon"))return true;
                Win.RECT r;if(!Win.GetWindowRect(h,out r))return true;
                int w=r.Right-r.Left, ht=r.Bottom-r.Top;
                if(w<350 || w>700 || ht<470 || ht>2100)return true;
                var title=new StringBuilder(80);Win.GetWindowText(h,title,title.Capacity);
                long score=(long)w*ht + (title.ToString()=="카카오톡" || title.ToString()=="KakaoTalk" ? 1000000 : 0);
                if(score>max){max=score;main=h;}
                return true;
            },IntPtr.Zero);
            process.Dispose();
        }
        return main;
    }
    private static bool Banner(IntPtr main, Win.RECT mr, int gap, int adSize, bool hide) {
        bool found=false;
        uint owner;Win.GetWindowThreadProcessId(main,out owner);
        Win.EnumWindows((h,lp) => {
            if(h==main)return true;
            uint pid;Win.GetWindowThreadProcessId(h,out pid);
            if(pid!=owner)return true;
            var c=new StringBuilder(100);Win.GetClassName(h,c,c.Capacity);
            if(!c.ToString().StartsWith("Qt") || !c.ToString().EndsWith("QWindowIcon")) return true;
            Win.RECT r;if(!Win.GetWindowRect(h,out r))return true;
            int w=mr.Right-mr.Left-2*gap;
            if(Math.Abs(r.Left-(mr.Left+gap))>6 ||
               Math.Abs((r.Right-r.Left)-w)>8 ||
               Math.Abs((r.Bottom-r.Top)-adSize)>8) return true;
            // During the first layout update, Qt may leave the banner at old bottom temporarily.
            int bottomDelta=mr.Bottom-gap-r.Bottom;
            if(Math.Abs(bottomDelta)>9 && Math.Abs(bottomDelta-adSize)>9) return true;
            bool chrome=false;
            Win.EnumChildWindows(h,(child,z) => {
                var name=new StringBuilder(100);
                Win.GetClassName(child,name,name.Capacity);
                if(name.ToString().StartsWith("Chrome_WidgetWin_")){chrome=true;return false;}
                return true;
            },IntPtr.Zero);
            if(!chrome)return true;
            found=true;
            if(hide && Win.IsWindowVisible(h)) Win.ShowWindow(h,0);  // SW_HIDE
            if(!hide && !Win.IsWindowVisible(h)) Win.ShowWindow(h,8); // SW_SHOWNA
            return true;
        },IntPtr.Zero);
        return found;
    }
    private bool ApplyClip(IntPtr hwnd, int width, int clipHeight) {
        if(clipHeight<250 || width<300)return false;
        Win.RECT rgnBounds;
        int currentRgn = Win.GetWindowRgnBox(hwnd,out rgnBounds);
        if(width==lastWidth && clipHeight==lastClip && currentRgn!=0 &&
           rgnBounds.Right-rgnBounds.Left==width &&
           rgnBounds.Bottom-rgnBounds.Top==clipHeight) return true;
        IntPtr clip = Win.CreateRectRgn(0,0,width,clipHeight);
        if(clip==IntPtr.Zero)return false;
        // Win32 takes ownership of the HRGN on success.
        if(Win.SetWindowRgn(hwnd,clip,true)==0){ Win.DeleteObject(clip);return false; }
        lastWidth=width;lastClip=clipHeight;
        return true;
    }
    private void Restore() {
        dragCover.Clear();
        if(mainWindow != IntPtr.Zero && Win.IsWindow(mainWindow)) {
            try {
                Win.RECT r;
                if(Win.GetWindowRect(mainWindow,out r)) {
                    int w=r.Right-r.Left;
                    int original=Math.Max(470, r.Bottom-r.Top-expansion);
                    Win.SetWindowRgn(mainWindow,IntPtr.Zero,true);
                    if(expansion>0) Win.SetWindowPos(mainWindow,IntPtr.Zero,0,0,w,original,RszFlags);
                    Win.RECT restored;
                    if(Win.GetWindowRect(mainWindow,out restored))
                        Banner(mainWindow,restored,(int)Math.Round(AdHeight(mainWindow)*0.08),AdHeight(mainWindow),false);
                }
            } catch(Exception ex){Log("Restore error: "+ex.GetType().Name);}
        }
        if(moveHook!=IntPtr.Zero){Win.UnhookWinEvent(moveHook);moveHook=IntPtr.Zero;}
        mainWindow=IntPtr.Zero;
        expansion=0;lastWidth=-1;lastClip=-1;
        moving=false;suspended=false;suspendedExpansion=0;
        dragStartVisibleHeight=0;dragStartWidth=0;poll.Interval=125;
        ResetSnapHistory();
    }
    // Use the visible Qt window bounds (the QML extra 109px is region-clipped).
    // Per-monitor rcWork excludes the taskbar, including secondary displays.
    private void ResetSnapHistory() {
        lastObservedX=int.MinValue;lastObservedY=int.MinValue;
        lastObservedWidth=-1;lastObservedHeight=-1;
        snapPending=false;
    }
    // Native Windows movement notifications (Qt's startSystemMove uses this path).
    private void EnsureMoveHook(IntPtr hwnd) {
        if(moveHook!=IntPtr.Zero)return;
        uint pid; Win.GetWindowThreadProcessId(hwnd,out pid);
        if(pid==0)return;
        moveCallback=(hook,ev,win,obj,child,thread,time)=>{
            if(disposed || win!=mainWindow)return;
            if(ev==0x000A) BeginMove();   // EVENT_SYSTEM_MOVESIZESTART
            if(ev==0x000B) EndMove();     // EVENT_SYSTEM_MOVESIZEEND
        };
        // Global out-of-context event delivery, then strictly filter by Kakao main HWND.
        moveHook=Win.SetWinEventHook(0x000A,0x000B,IntPtr.Zero,moveCallback,0,0,0);
        Log("Move event hook: "+(moveHook!=IntPtr.Zero ? "active" : "fallback polling"));
    }
    private void BeginMove() {
        if(moving || suspended || mainWindow==IntPtr.Zero || !Win.IsWindow(mainWindow))return;
        if(expansion==0)return;
        Win.RECT r;
        if(!Win.GetWindowRect(mainWindow,out r))return;
        int width=r.Right-r.Left, visibleHeight=r.Bottom-r.Top-expansion;
        if(visibleHeight<450)return;
        int gap=(int)Math.Round(AdHeight(mainWindow)*0.08);
        int ad=AdHeight(mainWindow);
        dragCover.CaptureFrame(mainWindow,r,visibleHeight,gap,ad);
        int oldExpansion=expansion;
        Win.SetWindowRgn(mainWindow,IntPtr.Zero,true);
        if(!Win.SetWindowPos(mainWindow,IntPtr.Zero,0,0,width,visibleHeight,RszFlags)){
            ApplyClip(mainWindow,width,visibleHeight);
            dragCover.Clear();
            return;
        }
        suspendedExpansion=oldExpansion;
        dragStartVisibleHeight=visibleHeight;
        dragStartWidth=width;
        expansion=0;lastClip=-1;lastWidth=-1;
        moving=true;suspended=true;
        poll.Interval=40;
        lastMoveSeen=Environment.TickCount;
        ResetSnapHistory();
        if(Win.GetWindowRect(mainWindow,out r))
            dragCover.Place(mainWindow,r,visibleHeight,gap,ad);
        Log("Drag start: actual window size; captured list tail shown during movement.");
    }
    private void EndMove() {
        if(mainWindow==IntPtr.Zero || !Win.IsWindow(mainWindow)){
            dragCover.Clear();
            moving=false;suspended=false;suspendedExpansion=0;
            dragStartVisibleHeight=0;dragStartWidth=0;poll.Interval=125;return;
        }
        if(!moving && !suspended)return;
        Win.RECT r;
        if(Win.GetWindowRect(mainWindow,out r)) {
            // Qt can restore its cached expanded size while Windows is moving.
            // Avoid adding the same 109px a SECOND time on drag end.
            int rawHeight=r.Bottom-r.Top, currentWidth=r.Right-r.Left;
            if(suspended && dragStartVisibleHeight>0 &&
               Math.Abs(currentWidth-dragStartWidth)<=3 &&
               Math.Abs(rawHeight-(dragStartVisibleHeight+suspendedExpansion))<=4) {
                Win.SetWindowPos(mainWindow,IntPtr.Zero,0,0,currentWidth,
                    dragStartVisibleHeight,RszFlags);
                Log("Qt cached enlarged size corrected before reflow.");
                Win.GetWindowRect(mainWindow,out r);
            }
            ClampAndSnap(mainWindow,r,true);
            if(Win.GetWindowRect(mainWindow,out r) && suspended && !Win.IsZoomed(mainWindow)) {
                int width=r.Right-r.Left, full=r.Bottom-r.Top+suspendedExpansion;
                if(Win.SetWindowPos(mainWindow,IntPtr.Zero,0,0,width,full,RszFlags)) {
                    expansion=suspendedExpansion;
                    ApplyClip(mainWindow,width,full-expansion);
                    Log("Drag end: list re-expanded "+expansion+"px, work area respected.");
                }
            }
        }
        dragCover.Clear();
        moving=false;suspended=false;suspendedExpansion=0;
        dragStartVisibleHeight=0;dragStartWidth=0;
        poll.Interval=125;
        ResetSnapHistory();
    }
    // Physical window is normal size while moving. Restrict its VISIBLE area
    // to the active monitor work-area, never the taskbar. Snap while near edges.
    private void ClampAndSnap(IntPtr hwnd,Win.RECT r,bool magnet) {
        int gap=(int)Math.Round(AdHeight(hwnd)*0.08);
        int width=r.Right-r.Left;
        int visibleHeight=r.Bottom-r.Top-expansion;
        if(visibleHeight<280 || width<250)return;
        Win.POINT pt;
        bool mouseDown=(((ushort)Win.GetAsyncKeyState(1))&0x8000)!=0;
        IntPtr monitor=IntPtr.Zero;
        if(mouseDown && Win.GetCursorPos(out pt))
            monitor=Win.MonitorFromPoint(pt,2);
        else {
            pt=new Win.POINT {
                X=r.Left+(width/2),
                Y=r.Top+(visibleHeight/2)
            };
            monitor=Win.MonitorFromPoint(pt,2);
        }
        if(monitor==IntPtr.Zero)return;
        var info=new Win.MONITORINFO{cbSize=Marshal.SizeOf(typeof(Win.MONITORINFO))};
        if(!Win.GetMonitorInfo(monitor,ref info))return;
        Win.RECT wk=info.rcWork;
        int minX=wk.Left-gap;
        int maxX=wk.Right-(width-gap);
        int minY=wk.Top-1;
        int maxY=wk.Bottom-(visibleHeight-gap);
        if(maxX<minX)maxX=minX;
        if(maxY<minY)maxY=minY;
        int nx=Math.Max(minX,Math.Min(maxX,r.Left));
        int ny=Math.Max(minY,Math.Min(maxY,r.Top));
        int threshold=Math.Max(14,Math.Min(44,(int)Math.Round(22.0*AdHeight(hwnd)/100.0)));
        if(magnet && snapEnabled) {
            if(Math.Abs(nx-minX)<=threshold)nx=minX;
            else if(Math.Abs(nx-maxX)<=threshold)nx=maxX;
            if(Math.Abs(ny-minY)<=threshold)ny=minY;
            else if(Math.Abs(ny-maxY)<=threshold)ny=maxY;
        }
        if(nx==r.Left && ny==r.Top)return;
        uint flags=0x0001u|0x0004u|0x0010u|0x0200u;
        if(Win.SetWindowPos(hwnd,IntPtr.Zero,nx,ny,0,0,flags))
            Log((magnet?"Snap ":"Clamp ")+nx+","+ny+
                " work "+wk.Left+","+wk.Top+".."+wk.Right+","+wk.Bottom);
    }
    private void RunMoveTick(IntPtr hwnd,Win.RECT r) {
        bool button=(((ushort)Win.GetAsyncKeyState(1))&0x8000)!=0;
        if(suspended) {
            if(moving && dragStartVisibleHeight>0 &&
               Math.Abs((r.Right-r.Left)-dragStartWidth)<=3 &&
               Math.Abs((r.Bottom-r.Top)-(dragStartVisibleHeight+suspendedExpansion))<=4) {
                Win.SetWindowPos(hwnd,IntPtr.Zero,0,0,r.Right-r.Left,
                    dragStartVisibleHeight,RszFlags);
                Win.GetWindowRect(hwnd,out r);
            }
            if(moving) {
                // Magnet engages near an edge even during drag; hard clamp always applies.
                ClampAndSnap(hwnd,r,true);
                if(Win.GetWindowRect(hwnd,out r))
                    dragCover.Place(hwnd,r,
                        dragStartVisibleHeight,
                        (int)Math.Round(AdHeight(hwnd)*0.08),
                        AdHeight(hwnd));
            }
            if(!button && unchecked((uint)(Environment.TickCount-lastMoveSeen))>180u)
                EndMove(); // safety if Qt omits MOVESIZEEND
            return;
        }
        bool changed=lastObservedX!=int.MinValue &&
            (lastObservedX!=r.Left || lastObservedY!=r.Top);
        bool sizeChange=lastObservedWidth!=r.Right-r.Left ||
            lastObservedHeight!=r.Bottom-r.Top;
        if(button && changed && !sizeChange && expansion>0) {
            BeginMove();  // Qt custom titlebar fallback
            return;
        }
        if(changed) {
            lastObservedX=r.Left;lastObservedY=r.Top;
            lastMoveSeen=Environment.TickCount;snapPending=true;
        } else if(snapPending && !button &&
                  unchecked((uint)(Environment.TickCount-lastMoveSeen))>150u){
            snapPending=false;
            ClampAndSnap(hwnd,r,true);
        }
        lastObservedWidth=r.Right-r.Left;
        lastObservedHeight=r.Bottom-r.Top;
        if(lastObservedX==int.MinValue){
            lastObservedX=r.Left;lastObservedY=r.Top;
        }
    }
    private void Tick() {
        if(disposed) return;
        try {
            if(!enabled){if(mainWindow!=IntPtr.Zero)Restore();return;}
            var h=FindMain();
            if(h==IntPtr.Zero){
                // Minimized windows remain intact; do not resize an iconic Qt window.
                if(mainWindow!=IntPtr.Zero && Win.IsWindow(mainWindow) && Win.IsIconic(mainWindow))return;
                if(mainWindow!=IntPtr.Zero)Restore();return;
            }
            if(mainWindow!=IntPtr.Zero && mainWindow!=h) Restore();
            Win.RECT r;
            if(!Win.GetWindowRect(h,out r))return;
            if(Win.IsIconic(h))return;
            int width=r.Right-r.Left,ht=r.Bottom-r.Top;
            int ad=AdHeight(h);
            if(suspended){ RunMoveTick(h,r); return; }
            if(mainWindow==IntPtr.Zero) {
                if(Win.IsZoomed(h))return; // Avoid altering maximized geometry.
                int gap=(int)Math.Round(ad*0.08);
                if(!Banner(h,r,gap,ad,true))return; // Strict, non-ad UI safeguard.
                mainWindow=h;
                expansion=ad+gap+1; // Qt margin plus the 1px native edge
                if(!Win.SetWindowPos(h,IntPtr.Zero,0,0,width,ht+expansion,RszFlags)){
                    Log("Resize rejected; restoring");
                    Restore();return;
                }
                Log("Expanded QML list by "+expansion+"px");
                EnsureMoveHook(h);
                if(!Win.GetWindowRect(h,out r))return;
                width=r.Right-r.Left;ht=r.Bottom-r.Top;
            }
            if(Win.IsZoomed(h)) {
                // Let Windows own maximized geometry. Drop clipping without forcing a resize.
                Win.SetWindowRgn(h,IntPtr.Zero,true);
                mainWindow=IntPtr.Zero;expansion=0;lastWidth=-1;lastClip=-1;
                if(moveHook!=IntPtr.Zero){Win.UnhookWinEvent(moveHook);moveHook=IntPtr.Zero;}
                moving=false;suspended=false;suspendedExpansion=0;
                ResetSnapHistory();
                return;
            }
            Banner(h,r,(int)Math.Round(ad*0.08),ad,true);
            ApplyClip(h,width,ht-expansion);
            RunMoveTick(h,r);
        } catch(Exception e){Log("Tick error: "+e.GetType().Name);}
    }
    protected override void OnFormClosed(FormClosedEventArgs e) {
        disposed=true;poll.Stop();Restore();poll.Dispose();
        tray.Visible=false;tray.Dispose();
        dragCover.Dispose();
        if(appIcon!=null)appIcon.Dispose();
        Log("Stopped (original geometry restored)");
        base.OnFormClosed(e);
    }
}
internal static class Program {
    [STAThread]static void Main(){
        bool created;
        using(var m=new System.Threading.Mutex(true,@"Local\SoNG_KakaoBetaAdGuard_Qt6_v2",out created)){
            if(!created)return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Guardian());
        }
    }
}
