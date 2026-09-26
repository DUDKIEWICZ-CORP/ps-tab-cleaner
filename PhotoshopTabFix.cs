// Photoshop Tabs Cleaner — pixel port of app.html / Figma node 2174:9503.
// All text via GDI TextRenderer (ClearType), layout mirrors the CSS reference.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

[assembly: AssemblyTitle("Photoshop Tabs Cleaner")]
[assembly: AssemblyProduct("Photoshop Tabs Cleaner")]
[assembly: AssemblyCompany("Dudkiewicz Corp")]
[assembly: AssemblyCopyright("© 2026 Dudkiewicz Corp — dudkiewiczcorp.com")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

static class UI
{
    public static readonly Color Bg = ColorTranslator.FromHtml("#fafafa");
    public static readonly Color Stroke = ColorTranslator.FromHtml("#e5e5e5");
    public static readonly Color StrokeMid = ColorTranslator.FromHtml("#d9d9d9");
    public static readonly Color TxBlack = ColorTranslator.FromHtml("#121212");
    public static readonly Color TxDark = ColorTranslator.FromHtml("#777777");
    public static readonly Color TxStd = ColorTranslator.FromHtml("#999999");
    public static readonly Color Blue = ColorTranslator.FromHtml("#3790f0");
    public static readonly Color BlueDark = ColorTranslator.FromHtml("#285f9c");
    public static readonly Color BlueLight = ColorTranslator.FromHtml("#64aefe");

    [DllImport("gdi32.dll")]
    static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, ref uint pcFonts);

    static PrivateFontCollection fc = new PrivateFontCollection();
    static FontFamily famReg, famSemi;

    public static void LoadFonts()
    {
        foreach (string res in new[] { "ChakraPetch-Regular.ttf", "ChakraPetch-SemiBold.ttf", "ChakraPetch-Bold.ttf" })
        {
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                {
                    if (s == null) continue;
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    IntPtr p = Marshal.AllocCoTaskMem(b.Length);
                    Marshal.Copy(b, 0, p, b.Length);
                    fc.AddMemoryFont(p, b.Length);          // GDI+ (Font objects)
                    uint n = 0;
                    AddFontMemResourceEx(p, (uint)b.Length, IntPtr.Zero, ref n); // GDI (TextRenderer)
                }
            }
            catch { }
        }
        foreach (var f in fc.Families)
        {
            if (f.Name == "Chakra Petch") famReg = f;
            if (f.Name.Contains("SemiBold")) famSemi = f;
        }
    }

    public static Font Chakra(float px) { return Mk(famReg, px, FontStyle.Regular); }
    public static Font ChakraSemi(float px) { return Mk(famSemi ?? famReg, px, famSemi != null ? FontStyle.Regular : FontStyle.Bold); }
    public static Font ChakraBold(float px) { return Mk(famReg, px, FontStyle.Bold); }
    static Font Mk(FontFamily fam, float px, FontStyle st)
    {
        if (fam == null) return new Font("Segoe UI", px, st, GraphicsUnit.Pixel);
        return new Font(fam, px, st, GraphicsUnit.Pixel);
    }
    public static Font Segoe(float px) { return new Font("Segoe UI Semibold", px, FontStyle.Regular, GraphicsUnit.Pixel); }

    public static Size Tx(string s, Font f)
    {
        return TextRenderer.MeasureText(s, f, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
    }
    public static void Draw(Graphics g, string s, Font f, Color c, int x, int y)
    {
        TextRenderer.DrawText(g, s, f, new Point(x, y), c, TextFormatFlags.NoPadding);
    }
    public static void DrawMid(Graphics g, string s, Font f, Color c, int x, int yTop, int h)
    {
        var sz = Tx(s, f);
        TextRenderer.DrawText(g, s, f, new Point(x, yTop + (h - sz.Height) / 2), c, TextFormatFlags.NoPadding);
    }

    public static void Crisp(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half;
    }

    public static GraphicsPath Round(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        float d = rad * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // CSS-exact box: stacked rounded fills, each inset 1px with radius-1
    // (renders borders as filled rings exactly like the browser box model)
    public static void Layers(Graphics g, Rectangle r, float rad, params Color[] cols)
    {
        var rf = new RectangleF(r.X, r.Y, r.Width, r.Height);
        foreach (var c in cols)
        {
            using (var p = Round(rf, Math.Max(rad, 0.5f)))
            using (var b = new SolidBrush(c)) g.FillPath(b, p);
            rf = new RectangleF(rf.X + 1, rf.Y + 1, rf.Width - 2, rf.Height - 2);
            rad -= 1;
        }
    }
}

class TriButton : Control
{
    Color outer, fill, inner, txt, fillHover;
    bool hover;
    public TriButton(string text, bool primary)
    {
        Text = text;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        Cursor = Cursors.Hand;
        Font = UI.ChakraBold(12);
        if (primary) { outer = UI.BlueDark; fill = UI.Blue; inner = UI.BlueLight; txt = Color.White; fillHover = ColorTranslator.FromHtml("#4a9ef5"); }
        else { outer = UI.Stroke; fill = ColorTranslator.FromHtml("#f6f6f6"); inner = Color.White; txt = ColorTranslator.FromHtml("#696969"); fillHover = ColorTranslator.FromHtml("#efefef"); }
        Height = 30;
        Width = UI.Tx(text, Font).Width + 16 + 4;   // padding 0 8 + two 1px borders each side
        MouseEnter += (s, e) => { hover = true; Invalidate(); };
        MouseLeave += (s, e) => { hover = false; Invalidate(); };
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        UI.Crisp(g);
        UI.Layers(g, new Rectangle(0, 0, Width, Height), 4, outer, inner, hover ? fillHover : fill);
        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), txt,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}

static class Program
{
    static readonly Regex Rx = new Regex("(\"\\$\\$\\$/ImageWindow/TitleTemplate[^=]*)=[^\"]*\"");
    static readonly Regex RxPatched = new Regex("\"\\$\\$\\$/ImageWindow/TitleTemplate=\\^0\"");
    static Dictionary<string, List<string>> installs = new Dictionary<string, List<string>>();
    static Form form;
    static Panel cardsPanel;
    static Image Logo;
    const int W = 850;

    [DllImport("user32.dll")]
    static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    [STAThread]
    static void Main()
    {
        try { SetProcessDpiAwarenessContext((IntPtr)(-4)); } catch { }
        Application.EnableVisualStyles();
        UI.LoadFonts();
        try
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo20.png"))
                if (s != null) Logo = Image.FromStream(s);
        }
        catch { }

        form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            ClientSize = new Size(W, 780),
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = UI.Bg,
            Text = "Photoshop Tabs Cleaner"
        };
        try { form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        form.Paint += (s, e) =>
        {
            using (var pen = new Pen(UI.Stroke))
                e.Graphics.DrawRectangle(pen, 0, 0, form.ClientSize.Width - 1, form.ClientSize.Height - 1);
        };

        int y = BuildTop();
        y = BuildMain(y);
        var div = new Panel { Left = 1, Top = y, Width = W - 2, Height = 1, BackColor = UI.StrokeMid };
        form.Controls.Add(div);
        y = BuildCenter(y + 1);
        BuildBottom(y);
        Scan();
        string shot = Environment.GetCommandLineArgs()
            .Where(a => a.StartsWith("/shot:")).Select(a => a.Substring(6)).FirstOrDefault();
        if (shot != null)
        {
            form.Opacity = 0;
            form.Shown += (s, e) =>
            {
                using (var bmp = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                {
                    form.DrawToBitmap(bmp, new Rectangle(Point.Empty, form.ClientSize));
                    bmp.Save(shot, System.Drawing.Imaging.ImageFormat.Png);
                }
                form.Close();
            };
        }
        Application.Run(form);
    }

    static void Buffer(Panel p)
    {
        typeof(Panel).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(p, true, null);
    }

    /* ---------- top bar: p10, icon 20, gap 10, title 14 bold ---------- */
    static int BuildTop()
    {
        var top = new Panel { Left = 1, Top = 1, Width = W - 2, Height = 40, BackColor = Color.White };
        Buffer(top);
        top.Paint += (s, e) =>
        {
            var g = e.Graphics;
            if (Logo != null)
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.DrawImage(Logo, new Rectangle(10, 10, 20, 20));
            }
            using (var f = UI.ChakraBold(14))
                UI.DrawMid(g, "Photoshop Tabs Cleaner", f, UI.TxBlack, 40, 0, 39);
            using (var pen = new Pen(UI.Stroke)) g.DrawLine(pen, 0, 39, top.Width, 39);
        };
        var close = new Label
        {
            Text = "\u2715", Font = new Font("Segoe UI", 11f), ForeColor = UI.TxDark,
            Left = W - 2 - 10 - 22, Top = 8, Width = 22, Height = 22,
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, BackColor = Color.White
        };
        close.Click += (s, e) => form.Close();
        close.MouseEnter += (s, e) => close.ForeColor = UI.TxBlack;
        close.MouseLeave += (s, e) => close.ForeColor = UI.TxDark;
        top.Controls.Add(close);
        bool drag = false; Point off = Point.Empty;
        top.MouseDown += (s, e) => { drag = true; off = e.Location; };
        top.MouseMove += (s, e) => { if (drag) form.Location = new Point(form.Location.X + e.X - off.X, form.Location.Y + e.Y - off.Y); };
        top.MouseUp += (s, e) => drag = false;
        form.Controls.Add(top);
        return 41;
    }

    /* ---------- main: p30, gap 24 ---------- */
    static string[] TabsBefore = {
        "kv_summer.psd @ 100% (keyvisual, RGB/8#)*",
        "lifestyle.jpg @50% (layer 1, CMYK/16#)",
        "logo.png (vector_flattened, RGB/32#)" };
    static string[] TabsAfter = { "kv_summer.psd*", "lifestyle.jpg", "logo.png" };
    static int TabPad = 8;
    static int selBefore = 0, selAfter = 0;
    static List<Rectangle> rectsBefore = new List<Rectangle>();
    static List<Rectangle> rectsAfter = new List<Rectangle>();

    static int BuildMain(int top)
    {
        int pad = 30, y = top + pad;
        var fH1 = UI.ChakraSemi(24); var fSub = UI.Chakra(14);
        int h1H = UI.Tx("Ag", fH1).Height, subH = UI.Tx("Ag", fSub).Height;

        var head = new Panel { Left = 1, Top = y, Width = W - 2, Height = h1H + 6 + subH, BackColor = UI.Bg };
        Buffer(head);
        head.Paint += (s, e) =>
        {
            UI.Draw(e.Graphics, "Clean Photoshop document tabs.", fH1, UI.TxBlack, pad - 1, 0);
            UI.Draw(e.Graphics, "Shows only the file name instead of zoom, layer and color mode info.", fSub, UI.TxDark, pad - 1, h1H + 6);
        };
        form.Controls.Add(head);
        y += head.Height + 24;

        // demo box: fit-content like the CSS reference
        var fLbl = UI.ChakraSemi(12); var fTab = UI.Segoe(12);
        int lblH = UI.Tx("Ag", fLbl).Height;
        // keep a 30px margin on the right: shrink tab padding until the strip fits
        int maxStrip = (W - pad * 2) - 24 * 2 - 4;
        TabPad = 8;
        while (TabPad > 4 && StripWidth(TabsBefore, fTab) > maxStrip) TabPad--;
        int stripW = StripWidth(TabsBefore, fTab);
        int demoW = 2 + 24 + stripW + 24 + 2;
        int demoH = 2 + 24 + lblH + 14 + 22 + 14 + 1 + 14 + lblH + 14 + 22 + 24 + 2;
        var demo = new Panel { Left = pad, Top = y, Width = demoW, Height = demoH, BackColor = UI.Bg };
        Buffer(demo);
        demo.Paint += (s, e) =>
        {
            var g = e.Graphics;
            UI.Crisp(g);
            UI.Layers(g, new Rectangle(0, 0, demo.Width, demo.Height), 8,
                Color.White, ColorTranslator.FromHtml("#252525"), ColorTranslator.FromHtml("#3c3c3c"));

            int x = 2 + 24, yy = 2 + 24;
            var lblC = ColorTranslator.FromHtml("#b4b4b4");
            UI.Draw(g, "Before", fLbl, lblC, x, yy); yy += lblH + 14;
            DrawStrip(g, x, yy, TabsBefore, fTab, selBefore, rectsBefore); yy += 22 + 14;
            using (var pen = new Pen(ColorTranslator.FromHtml("#4b4b4b")))
                g.DrawLine(pen, x, yy, demo.Width - 2 - 24, yy);
            yy += 1 + 14;
            UI.Draw(g, "After", fLbl, lblC, x, yy); yy += lblH + 14;
            DrawStrip(g, x, yy, TabsAfter, fTab, selAfter, rectsAfter);
        };
        demo.MouseMove += (s, e) =>
        {
            demo.Cursor = (rectsBefore.Concat(rectsAfter).Any(r => r.Contains(e.Location)))
                ? Cursors.Hand : Cursors.Default;
        };
        demo.MouseClick += (s, e) =>
        {
            for (int i = 0; i < rectsBefore.Count; i++)
                if (rectsBefore[i].Contains(e.Location)) { selBefore = i; demo.Invalidate(); return; }
            for (int i = 0; i < rectsAfter.Count; i++)
                if (rectsAfter[i].Contains(e.Location)) { selAfter = i; demo.Invalidate(); return; }
        };
        form.Controls.Add(demo);
        y += demoH + 24;

        // clutter row
        var fChip = UI.Segoe(14); var fNote = UI.Chakra(14);
        var row = new Panel { Left = pad, Top = y, Width = W - pad * 2, Height = 30, BackColor = UI.Bg };
        Buffer(row);
        row.Paint += (s, e) =>
        {
            var g = e.Graphics;
            UI.Crisp(g);
            var eee = ColorTranslator.FromHtml("#eeeeee");
            UI.Layers(g, new Rectangle(0, 0, 30, 30), 3, eee, Color.White);
            using (var pen = new Pen(Color.Red, 2f))
            {
                g.DrawLine(pen, 10, 10, 20, 20);
                g.DrawLine(pen, 20, 10, 10, 20);
            }
            string chip = "@ 33% (Layer 0, RGB/8#)";
            int cw = UI.Tx(chip, fChip).Width + 28 + 2;
            UI.Layers(g, new Rectangle(38, 0, cw, 30), 3, eee, Color.White);
            UI.DrawMid(g, chip, fChip, UI.TxBlack, 38 + 15, 0, 30);
            string note = "Backups are kept, Restore undoes everything";
            var nsz = UI.Tx(note, fNote);
            UI.DrawMid(g, note, fNote, UI.TxDark, row.Width - nsz.Width, 0, 30);
        };
        form.Controls.Add(row);
        y += 30 + pad;
        return y;
    }

    static int StripWidth(string[] tabs, Font f)
    {
        int w = 2; // strip border
        foreach (string t in tabs) w += TabPad + 3 + UI.Tx(t, f).Width + 8 + 6 + TabPad + 1;
        return w - 1;
    }

    static void DrawStrip(Graphics g, int x, int y, string[] tabs, Font f, int sel, List<Rectangle> rects)
    {
        rects.Clear();
        var old = g.SmoothingMode;
        g.SmoothingMode = SmoothingMode.None;
        using (var b = new SolidBrush(ColorTranslator.FromHtml("#383838")))
            g.FillRectangle(b, x, y, StripWidth(tabs, f), 22);
        int cx = x + 1;
        for (int i = 0; i < tabs.Length; i++)
        {
            int tw = UI.Tx(tabs[i], f).Width;
            int w = TabPad + 3 + tw + 8 + 6 + TabPad;
            var bg = i == sel ? ColorTranslator.FromHtml("#535353") : ColorTranslator.FromHtml("#424242");
            var tc = i == sel ? ColorTranslator.FromHtml("#f0f0f0") : ColorTranslator.FromHtml("#a0a0a0");
            using (var b = new SolidBrush(bg)) g.FillRectangle(b, cx, y + 1, w, 20);
            UI.DrawMid(g, tabs[i], f, tc, cx + TabPad + 3, y + 1, 20);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(ColorTranslator.FromHtml("#a8a8a8")))
            {
                int xx = cx + TabPad + 3 + tw + 8, yy = y + 1 + 7;
                g.DrawLine(pen, xx, yy, xx + 6, yy + 6);
                g.DrawLine(pen, xx + 6, yy, xx, yy + 6);
            }
            g.SmoothingMode = SmoothingMode.None;
            rects.Add(new Rectangle(cx, y + 1, w, 20));
            cx += w + 1;
        }
        g.SmoothingMode = old;
    }

    /* ---------- detected installs ---------- */
    static int BuildCenter(int top)
    {
        int pad = 30, y = top + pad;
        var f = UI.Chakra(14);
        int h = UI.Tx("Ag", f).Height;
        var lbl = new Panel { Left = 1, Top = y, Width = W - 2, Height = h, BackColor = UI.Bg };
        Buffer(lbl);
        lbl.Paint += (s, e) => UI.Draw(e.Graphics, "Photoshops Detected", f, UI.TxDark, pad - 1, 0);
        form.Controls.Add(lbl);
        y += h + 24;
        cardsPanel = new Panel { Left = pad, Top = y, Width = W - pad * 2, Height = 10, BackColor = UI.Bg };
        form.Controls.Add(cardsPanel);
        return y;
    }

    static void Scan()
    {
        installs.Clear();
        cardsPanel.Controls.Clear();
        string adobe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Adobe");
        if (Directory.Exists(adobe))
        {
            foreach (string dir in Directory.GetDirectories(adobe, "Adobe Photoshop*"))
            {
                string loc = Path.Combine(dir, "Locales");
                string[] files;
                try { files = Directory.Exists(loc) ? Directory.GetFiles(loc, "tw10428_*.dat", SearchOption.AllDirectories) : new string[0]; }
                catch { files = new string[0]; }
                if (files.Length > 0) installs[Path.GetFileName(dir)] = files.ToList();
            }
        }
        int cy = 0;
        foreach (var kv in installs.OrderBy(k => k.Key))
        {
            cardsPanel.Controls.Add(MakeCard(kv.Key, cy));
            cy += 60 + 14;
        }
        if (installs.Count == 0)
        {
            var f = UI.ChakraSemi(14);
            var none = new Panel { Left = 0, Top = 0, Width = cardsPanel.Width, Height = 24, BackColor = UI.Bg };
            Buffer(none);
            none.Paint += (s, e) => UI.Draw(e.Graphics, "No Photoshop installation found under Program Files.", f, UI.TxStd, 0, 0);
            cardsPanel.Controls.Add(none);
            cy = 38;
        }
        cardsPanel.Height = Math.Max(10, cy - 14);
        int bottomTop = cardsPanel.Top + cardsPanel.Height + 30;
        form.ClientSize = new Size(W, bottomTop + 33 + 1);
        foreach (Control c in form.Controls)
            if (c.Name == "bottombar") c.Top = bottomTop;
        form.Invalidate();
    }

    static Control MakeCard(string ver, int cy)
    {
        bool patched = IsPatched(installs[ver][0]);
        var fName = UI.ChakraSemi(14); var fSt = UI.ChakraSemi(12);
        var card = new Panel { Left = 0, Top = cy, Width = cardsPanel.Width, Height = 60, BackColor = UI.Bg };
        Buffer(card);
        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            UI.Crisp(g);
            UI.Layers(g, new Rectangle(0, 0, card.Width, 60), 8, UI.StrokeMid, Color.White);
            UI.DrawMid(g, ver, fName, UI.TxBlack, 14, 0, 60);
        };
        var undo = new TriButton("Restore original", false);
        undo.Top = 15; undo.Left = card.Width - 14 - undo.Width;
        var fix = new TriButton("Fix Tabs", true);
        fix.Top = 15; fix.Left = undo.Left - 10 - fix.Width;
        string stTxt = patched ? "patched" : "original";
        var stSz = UI.Tx(stTxt, fSt);
        var status = new Panel
        {
            Width = stSz.Width, Height = stSz.Height, BackColor = Color.White,
            Left = fix.Left - 10 - stSz.Width, Top = (60 - stSz.Height) / 2
        };
        Buffer(status);
        status.Paint += (s, e) => UI.Draw(e.Graphics, stTxt, fSt, patched ? UI.Blue : UI.TxStd, 0, 0);
        fix.Click += (s, e) => Apply(ver, false);
        undo.Click += (s, e) => Apply(ver, true);
        card.Controls.Add(status); card.Controls.Add(fix); card.Controls.Add(undo);
        return card;
    }

    /* ---------- footer ---------- */
    static void BuildBottom(int top)
    {
        var fA = UI.ChakraBold(10); var fB = UI.Chakra(10);
        var bar = new Panel { Name = "bottombar", Left = 1, Top = top, Width = W - 2, Height = 33, BackColor = Color.White };
        Buffer(bar);
        bar.Paint += (s, e) =>
        {
            using (var pen = new Pen(UI.Stroke)) e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };
        var adiSz = UI.Tx("ADI.ONLINE", fA);
        var adi = new Panel { Left = 10, Top = 1 + (32 - adiSz.Height) / 2, Width = adiSz.Width, Height = adiSz.Height, BackColor = Color.White, Cursor = Cursors.Hand };
        Buffer(adi);
        adi.Paint += (s, e) => UI.Draw(e.Graphics, "ADI.ONLINE", fA, UI.Blue, 0, 0);
        adi.Click += (s, e) => OpenUrl("https://adi.online");

        string bt = "BUY ME A CAFFE";
        var btSz = UI.Tx(bt, fB);
        var bmc = new Panel { Width = 9 + 8 + btSz.Width, Height = 16, Top = 1 + (32 - 16) / 2, BackColor = Color.White, Cursor = Cursors.Hand };
        bmc.Left = bar.Width - 10 - bmc.Width;
        Buffer(bmc);
        bmc.Paint += (s, e) =>
        {
            var g = e.Graphics;
            UI.Crisp(g);
            using (var body = new GraphicsPath())
            {
                body.AddPolygon(new[] { new PointF(1f, 4f), new PointF(8f, 4f), new PointF(7f, 12.5f), new PointF(2f, 12.5f) });
                using (var b = new SolidBrush(ColorTranslator.FromHtml("#FFDD06"))) g.FillPath(b, body);
                using (var pen = new Pen(ColorTranslator.FromHtml("#010202"), 0.8f)) g.DrawPath(pen, body);
            }
            g.SmoothingMode = SmoothingMode.None;
            using (var b = new SolidBrush(ColorTranslator.FromHtml("#010202"))) g.FillRectangle(b, 0, 0, 9, 2);
            UI.DrawMid(g, bt, fB, UI.TxBlack, 17, 0, 16);
        };
        bmc.Click += (s, e) => OpenUrl("https://buymeacoffee.com/adriandudkiewicz");
        bar.Controls.Add(adi);
        bar.Controls.Add(bmc);
        form.Controls.Add(bar);
    }

    static void OpenUrl(string url) { try { Process.Start(url); } catch { } }

    /* ---------- patch engine ---------- */
    static bool IsPatched(string file)
    {
        try { return RxPatched.IsMatch(Encoding.Unicode.GetString(File.ReadAllBytes(file))); }
        catch { return false; }
    }

    static void Apply(string ver, bool restore)
    {
        if (Process.GetProcessesByName("Photoshop").Length > 0)
        {
            MessageBox.Show("Close Photoshop first, then try again.", "Photoshop Tabs Cleaner",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var errors = new List<string>();
        foreach (string p in installs[ver])
        {
            try
            {
                if (restore)
                {
                    if (File.Exists(p + ".bak")) File.Copy(p + ".bak", p, true);
                    continue;
                }
                if (!File.Exists(p + ".bak")) File.Copy(p, p + ".bak");
                string t = Encoding.Unicode.GetString(File.ReadAllBytes(p));
                string n = Rx.Replace(t, "$1=^0\"");
                if (n != t) File.WriteAllBytes(p, Encoding.Unicode.GetBytes(n));
            }
            catch (Exception ex) { errors.Add(ex.Message); }
        }
        if (errors.Count > 0)
            MessageBox.Show(string.Join("\n", errors.Distinct()), "Photoshop Tabs Cleaner",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        Scan();
    }
}
