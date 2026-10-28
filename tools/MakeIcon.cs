// Custom SoNG AdGuard icon: charcoal chat tile + warm gold speech bubble + turquoise status mark.
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;

internal static class MakeIcon {
    private static GraphicsPath RoundRect(RectangleF r, float rad) {
        var p=new GraphicsPath();
        float d=rad*2;
        p.AddArc(r.X,r.Y,d,d,180,90);
        p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);
        p.AddArc(r.X,r.Bottom-d,d,d,90,90);
        p.CloseFigure();
        return p;
    }
    private static byte[] Draw(int size) {
        using(var b=new Bitmap(size,size,PixelFormat.Format32bppArgb))
        using(var g=Graphics.FromImage(b)) {
            g.Clear(Color.Transparent);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            g.CompositingQuality=CompositingQuality.HighQuality;
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            g.ScaleTransform(size/128f,size/128f);
            using(var bg=RoundRect(new RectangleF(4,4,120,120),28f))
            using(var brush=new SolidBrush(Color.FromArgb(24,30,42)))
            using(var outline=new Pen(Color.FromArgb(73,88,104),2)) {
                g.FillPath(brush,bg);g.DrawPath(outline,bg);
            }
            // Chat balloon tail and body
            using(var gold=new SolidBrush(Color.FromArgb(255,219,67))) {
                g.FillPolygon(gold,new PointF[] {
                    new PointF(35,81),new PointF(25,108),new PointF(62,90)
                });
                using(var bubble=RoundRect(new RectangleF(22,28,89,70),29)) g.FillPath(gold,bubble);
            }
            // dark twin dots: identifiable even at 16px
            using(var dark=new SolidBrush(Color.FromArgb(31,37,49))) {
                g.FillEllipse(dark,45,60,11,11);
                g.FillEllipse(dark,77,60,11,11);
            }
            // tiny teal active / protected check at bottom right
            using(var teal=new SolidBrush(Color.FromArgb(67,221,184))) {
                g.FillEllipse(teal,88,91,27,27);
            }
            using(var pen=new Pen(Color.FromArgb(15,41,46),4f)) {
                pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
                g.DrawLines(pen,new[]{new PointF(95,103),new PointF(100,108),new PointF(109,98)});
            }
            using(var stream=new MemoryStream()) {
                b.Save(stream,ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
    public static void Main(string[] args) {
        string target=args.Length>0?args[0]:"SoNG-KakaoBetaAdGuard.ico";
        int[] sizes={16,24,32,48,64,256};
        var images=new List<byte[]>();
        foreach(int n in sizes)images.Add(Draw(n));
        using(var f=new FileStream(target,FileMode.Create,FileAccess.Write))
        using(var bw=new BinaryWriter(f)) {
            bw.Write((ushort)0);bw.Write((ushort)1);bw.Write((ushort)sizes.Length);
            int offset=6+16*sizes.Length;
            for(int i=0;i<sizes.Length;i++){
                bw.Write((byte)(sizes[i]==256?0:sizes[i]));
                bw.Write((byte)(sizes[i]==256?0:sizes[i]));
                bw.Write((byte)0);bw.Write((byte)0);
                bw.Write((ushort)1);bw.Write((ushort)32);
                bw.Write((uint)images[i].Length);bw.Write((uint)offset);
                offset+=images[i].Length;
            }
            foreach(var image in images)bw.Write(image);
        }
        Console.WriteLine("Icon saved: "+target+" ("+new FileInfo(target).Length+" bytes)");
    }
}
