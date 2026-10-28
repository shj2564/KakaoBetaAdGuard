// KakaoTalk 26.9.x Qt6 beta display-only banner cover.
// No DLL injection, protocol blocking, credential access, or modification of KakaoTalk binaries.
using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

internal static class Native {
    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr param);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr param);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr param);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int cmd);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr hwnd);
}

internal sealed class AdCover : Form {
    private readonly Timer timer = new Timer();
    private readonly NotifyIcon tray = new NotifyIcon();
    private readonly ToolStripMenuItem enabledMenu;
    private bool enabled = true;
    private bool initialized = false;
    private IntPtr currentHwnd = IntPtr.Zero;
    private int leftNavWidth = 68;

    public AdCover() {
        Text = "SoNG Kakao Beta AdGuard";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(34,34,34);
        Size = new Size(400,100);
        Location = new Point(-32000,-32000);
        enabledMenu = new ToolStripMenuItem("광고 영역 가리기 사용",null,(s,e) => {
            enabled = !enabled;
            enabledMenu.Checked = enabled;
            if(!enabled) { RestoreBanner(); Hide(); }
        });
        enabledMenu.Checked = true;
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("카카오톡 Qt6 베타 광고 가리기"){Enabled=false});
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(enabledMenu);
        menu.Items.Add(new ToolStripMenuItem("종료",null,(s,e)=>Close()));
        tray.Text = "SoNG Kakao Beta AdGuard - 26.9.x";
        tray.Icon = SystemIcons.Information;
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        timer.Interval = 100;
        timer.Tick += (s,e) => Poll();
        timer.Start();
    }
    protected override bool ShowWithoutActivation { get {return true;} }
    protected override CreateParams CreateParams {
        get {
            CreateParams p=base.CreateParams;
            p.ExStyle |= 0x00000080 /* WS_EX_TOOLWINDOW */ | 0x08000000 /* WS_EX_NOACTIVATE */;
            return p;
        }
    }
    protected override void OnShown(EventArgs e) {
        base.OnShown(e);
        if(!initialized) { initialized=true; Hide(); }
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        using(var bg = new SolidBrush(Color.FromArgb(34,34,34)))
            e.Graphics.FillRectangle(bg, ClientRectangle);
        using(var nav = new SolidBrush(Color.FromArgb(49,49,49)))
            e.Graphics.FillRectangle(nav,0,0,Math.Min(leftNavWidth,Width),Height);
    }
    private static IntPtr FindMainWindow() {
        IntPtr best=IntPtr.Zero;
        long bestScore=-1;
        foreach(Process p in Process.GetProcessesByName("KakaoTalkUI")) {
            int processId=p.Id;
            Native.EnumWindows((hwnd,_) => {
                uint owner;
                Native.GetWindowThreadProcessId(hwnd,out owner);
                if(owner != (uint)processId || !Native.IsWindowVisible(hwnd) || Native.IsIconic(hwnd)) return true;
                var c = new StringBuilder(100); Native.GetClassName(hwnd,c,c.Capacity);
                if(!c.ToString().Contains("QWindowIcon")) return true;
                Native.RECT r; if(!Native.GetWindowRect(hwnd,out r)) return true;
                int w=r.Right-r.Left,h=r.Bottom-r.Top;
                if(w<350 || w>600 || h<480 || h>1400) return true;
                long score=(long)w*h;
                if(score>bestScore){best=hwnd;bestScore=score;}
                return true;
            },IntPtr.Zero);
            p.Dispose();
        }
        return best;
    }
    // KakaoTalk 26.9 Qt6 exposes the 100-pixel ad as a separate top-level
    // QWindow at the bottom of the main window with a Chrome child window.
    // Identify by PID + class + child + precise geometry before hiding.
    private static bool HideMatchingBanner(IntPtr main, Native.RECT mr, int border, int adHeight, bool hide=true) {
        uint mainPid; Native.GetWindowThreadProcessId(main,out mainPid);
        bool matched=false;
        Native.EnumWindows((hwnd,_) => {
            if(hwnd==main) return true;
            uint pid;Native.GetWindowThreadProcessId(hwnd,out pid);
            if(pid!=mainPid) return true;
            var className=new StringBuilder(120);Native.GetClassName(hwnd,className,className.Capacity);
            if(!className.ToString().Contains("QWindowIcon")) return true;
            Native.RECT r;
            if(!Native.GetWindowRect(hwnd,out r)) return true;
            int innerWidth=mr.Right-mr.Left-2*border;
            if(Math.Abs(r.Left-(mr.Left+border))>5 ||
               Math.Abs(r.Bottom-(mr.Bottom-border))>5 ||
               Math.Abs((r.Right-r.Left)-innerWidth)>6 ||
               Math.Abs((r.Bottom-r.Top)-adHeight)>6) return true;
            bool webChild=false;
            Native.EnumChildWindows(hwnd,(child,z) => {
                var cname=new StringBuilder(120);
                Native.GetClassName(child,cname,cname.Capacity);
                if(cname.ToString().StartsWith("Chrome_WidgetWin_")) {
                    webChild=true; return false;
                }
                return true;
            },IntPtr.Zero);
            if(!webChild) return true;
            matched=true;
            if(hide && Native.IsWindowVisible(hwnd)) Native.ShowWindow(hwnd,0 /* SW_HIDE */);
            if(!hide && !Native.IsWindowVisible(hwnd)) Native.ShowWindow(hwnd,8 /* SW_SHOWNA */);
            return true;
        },IntPtr.Zero);
        return matched;
    }

    private void RestoreBanner() {
        try {
            IntPtr main=FindMainWindow();
            if(main==IntPtr.Zero)return;
            Native.RECT r;if(!Native.GetWindowRect(main,out r))return;
            int dpi=96;
            try {uint d=Native.GetDpiForWindow(main);if(d>=96 && d<=384)dpi=(int)d;}catch(EntryPointNotFoundException){}
            HideMatchingBanner(main,r,(int)Math.Round(8.0*dpi/96.0),(int)Math.Round(100.0*dpi/96.0),false);
        } catch {}
    }
    private void Poll(){
        try {
            if(!enabled){ if(Visible) Hide(); return; }
            IntPtr main=FindMainWindow();
            if(main==IntPtr.Zero){ if(Visible) Hide(); currentHwnd=IntPtr.Zero; return; }
            Native.RECT r;
            if(!Native.GetWindowRect(main,out r)){if(Visible) Hide();return;}
            int dpi=96;
            try { uint d=Native.GetDpiForWindow(main);if(d>=96 && d<=384) dpi=(int)d; } catch(EntryPointNotFoundException){}
            double scale=(double)dpi/96.0;
            int border=(int)Math.Round(8*scale);
            int adHeight=(int)Math.Round(100*scale);
            int x=r.Left+border, y=r.Bottom-border-adHeight;
            int w=r.Right-r.Left-2*border;
            if(w<250 || adHeight<50){if(Visible)Hide();return;}
            if(!HideMatchingBanner(main,r,border,adHeight)){if(Visible)Hide();return;}
            leftNavWidth=(int)Math.Round(68*scale);
            if(!Visible) Show();
            if(main!=currentHwnd){ currentHwnd=main; Invalidate(); }
            // Insert directly above the KakaoTalk window, but below unrelated foreground windows.
            IntPtr windowAbove=Native.GetWindow(main,3 /*GW_HWNDPREV*/);
            uint flags=0x0010 /* NOACTIVATE */ | 0x0200 /* NOOWNERZORDER */;
            if(windowAbove==Handle) flags|=0x0004 /* NOZORDER */;
            else if(windowAbove==IntPtr.Zero) windowAbove=IntPtr.Zero; /* HWND_TOP */
            Native.SetWindowPos(Handle,windowAbove,x,y,w,adHeight,flags);
        } catch { if(Visible)Hide(); }
    }
    protected override void OnFormClosed(FormClosedEventArgs e){
        RestoreBanner();
        timer.Stop();timer.Dispose();tray.Visible=false;tray.Dispose();
        base.OnFormClosed(e);
    }
}
internal static class Program {
    [STAThread] static void Main(){
        bool created;
        using(var mutex=new System.Threading.Mutex(true,@"Local\SoNG_KakaoBetaAdGuard_Qt6_v1",out created)){
            if(!created)return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new AdCover());
        }
    }
}
