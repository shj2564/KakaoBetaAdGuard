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
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

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
}
internal sealed class Guardian : Form {
    private const uint RszFlags = 0x0002u | 0x0004u | 0x0010u | 0x0200u;
    private readonly Timer poll = new Timer();
    private readonly NotifyIcon tray = new NotifyIcon();
    private readonly ToolStripMenuItem onOff;
    private readonly ToolStripMenuItem snapMenu;
    private readonly Icon appIcon;
    private bool snapEnabled=true;
    private int lastObservedX=int.MinValue, lastObservedY=int.MinValue;
    private int lastObservedWidth=-1, lastObservedHeight=-1;
    private int lastMoveTick=0;
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
        tray.Text = "SoNG Kakao Beta AdGuard v3";
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        poll.Interval = 125;
        poll.Tick += (s,e)=>Tick();
        poll.Start();
        Log("Started v3; custom tray icon + per-monitor taskbar-safe magnetic snapping.");
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
        mainWindow=IntPtr.Zero;
        expansion=0;lastWidth=-1;lastClip=-1;
        ResetSnapHistory();
    }
    // Use the visible Qt window bounds (the QML extra 109px is region-clipped).
    // Per-monitor rcWork excludes the taskbar, including secondary displays.
    private void ResetSnapHistory() {
        lastObservedX=int.MinValue;lastObservedY=int.MinValue;
        lastObservedWidth=-1;lastObservedHeight=-1;
        snapPending=false;lastMoveTick=0;
    }
    private void TrySnap(IntPtr hwnd, Win.RECT r) {
        IntPtr monitor=Win.MonitorFromWindow(hwnd,2);
        if(monitor==IntPtr.Zero)return;
        var info=new Win.MONITORINFO { cbSize=Marshal.SizeOf(typeof(Win.MONITORINFO)) };
        if(!Win.GetMonitorInfo(monitor,ref info))return;
        Win.RECT work=info.rcWork;
        int width=r.Right-r.Left;
        int visibleHeight=r.Bottom-r.Top-expansion;
        int gap=(int)Math.Round(AdHeight(hwnd)*0.08);
        int threshold=Math.Max(14,Math.Min(44,(int)Math.Round(22.0*AdHeight(hwnd)/100.0)));
        int xl=Math.Abs(r.Left+gap-work.Left);
        int xr=Math.Abs(r.Right-gap-work.Right);
        int yt=Math.Abs(r.Top+1-work.Top);
        int yb=Math.Abs(r.Top+visibleHeight-gap-work.Bottom);
        int nx=r.Left,ny=r.Top;
        if(xl<=threshold || xr<=threshold)
            nx=(xl<=xr) ? work.Left-gap : work.Right-(width-gap);
        if(yt<=threshold || yb<=threshold)
            ny=(yt<=yb) ? work.Top-1 : work.Bottom-(visibleHeight-gap);
        if(nx!=r.Left || ny!=r.Top) {
            uint flags=0x0001u|0x0004u|0x0010u|0x0200u;
            if(Win.SetWindowPos(hwnd,IntPtr.Zero,nx,ny,0,0,flags))
                Log("Magnet snap "+nx+","+ny+" work "+
                    work.Left+","+work.Top+".."+work.Right+","+work.Bottom);
        }
    }
    private void HandleMagneticSnap(IntPtr hwnd,Win.RECT r) {
        if(!snapEnabled){ResetSnapHistory();return;}
        int w=r.Right-r.Left, ht=r.Bottom-r.Top;
        if(lastObservedX==int.MinValue) {
            lastObservedX=r.Left;lastObservedY=r.Top;
            lastObservedWidth=w;lastObservedHeight=ht;
            return;
        }
        if(lastObservedWidth!=w || lastObservedHeight!=ht) {
            lastObservedX=r.Left;lastObservedY=r.Top;
            lastObservedWidth=w;lastObservedHeight=ht;
            snapPending=false;return;
        }
        if(lastObservedX!=r.Left || lastObservedY!=r.Top) {
            lastObservedX=r.Left;lastObservedY=r.Top;
            lastMoveTick=Environment.TickCount;snapPending=true;return;
        }
        if(!snapPending || unchecked((uint)(Environment.TickCount-lastMoveTick))<180u) return;
        if((((ushort)Win.GetAsyncKeyState(1))&0x8000)!=0)return;
        snapPending=false;
        TrySnap(hwnd,r);
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
                if(!Win.GetWindowRect(h,out r))return;
                width=r.Right-r.Left;ht=r.Bottom-r.Top;
            }
            if(Win.IsZoomed(h)) {
                // Let Windows own maximized geometry. Drop clipping without forcing a resize.
                Win.SetWindowRgn(h,IntPtr.Zero,true);
                mainWindow=IntPtr.Zero;expansion=0;lastWidth=-1;lastClip=-1;
                ResetSnapHistory();
                return;
            }
            Banner(h,r,(int)Math.Round(ad*0.08),ad,true);
            ApplyClip(h,width,ht-expansion);
            HandleMagneticSnap(h,r);
        } catch(Exception e){Log("Tick error: "+e.GetType().Name);}
    }
    protected override void OnFormClosed(FormClosedEventArgs e) {
        disposed=true;poll.Stop();Restore();poll.Dispose();
        tray.Visible=false;tray.Dispose();
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
