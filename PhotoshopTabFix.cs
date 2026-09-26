// Photoshop Tab Fix — show only the file name in Photoshop document tabs.
// GUI version. Patches $$$/ImageWindow/TitleTemplate* in tw10428_*.dat.
// Keeps .bak backups; Restore undoes everything.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

[assembly: AssemblyTitle("Photoshop Tab Fix")]
[assembly: AssemblyProduct("Photoshop Tab Fix")]
[assembly: AssemblyCompany("Dudkiewicz Corp")]
[assembly: AssemblyCopyright("© 2026 Dudkiewicz Corp — dudkiewiczcorp.com")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Program
{
    static TextBox log;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        var f = new Form
        {
            Text = "Photoshop Tab Fix — Dudkiewicz Corp",
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false,
            ClientSize = new Size(460, 300),
            StartPosition = FormStartPosition.CenterScreen,
            Font = new Font("Segoe UI", 9f)
        };
        var lbl = new Label
        {
            Text = "Makes Photoshop document tabs show only the file name —\nno more \"@ 33% (Layer 0, RGB/8#)\" clutter.\n\nClose Photoshop before running. A backup is kept, Restore undoes everything.\nA Photoshop update brings default tabs back — just run Fix again.",
            Location = new Point(14, 12),
            Size = new Size(432, 84)
        };
        var fix = new Button { Text = "Fix tabs", Location = new Point(14, 102), Size = new Size(210, 34) };
        var undo = new Button { Text = "Restore original", Location = new Point(236, 102), Size = new Size(210, 34) };
        log = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
            Location = new Point(14, 146), Size = new Size(432, 140),
            BackColor = Color.White, Font = new Font("Consolas", 8.5f)
        };
        fix.Click += (s, e) => Run(false);
        undo.Click += (s, e) => Run(true);
        f.Controls.AddRange(new Control[] { lbl, fix, undo, log });
        bool autoRestore = Environment.GetCommandLineArgs()
            .Any(a => a.Equals("/restore", StringComparison.OrdinalIgnoreCase));
        if (autoRestore) f.Shown += (s, e) => Run(true);
        Application.Run(f);
    }

    static void Say(string s) { log.AppendText(s + Environment.NewLine); }

    static void Run(bool restore)
    {
        log.Clear();
        if (Process.GetProcessesByName("Photoshop").Length > 0)
        {
            Say("Close Photoshop first, then try again.");
            return;
        }
        string adobe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Adobe");
        var files = Directory.Exists(adobe)
            ? Directory.GetDirectories(adobe, "Adobe Photoshop*")
                .SelectMany(v => SafeFiles(Path.Combine(v, "Locales"))).ToList()
            : new System.Collections.Generic.List<string>();
        if (files.Count == 0)
        {
            Say("No Photoshop language files (tw10428_*.dat) found under Program Files.");
            return;
        }
        var rx = new Regex("(\"\\$\\$\\$/ImageWindow/TitleTemplate[^=]*)=[^\"]*\"");
        foreach (string p in files)
        {
            string ver = p.Split(Path.DirectorySeparatorChar).First(seg => seg.StartsWith("Adobe Photoshop"));
            try
            {
                if (restore)
                {
                    if (File.Exists(p + ".bak")) { File.Copy(p + ".bak", p, true); Say("[" + ver + "] restored original tabs."); }
                    else Say("[" + ver + "] no backup — nothing to restore.");
                    continue;
                }
                if (!File.Exists(p + ".bak")) File.Copy(p, p + ".bak");
                string t = Encoding.Unicode.GetString(File.ReadAllBytes(p));
                string n = rx.Replace(t, "$1=^0\"");
                if (n == t) Say("[" + ver + "] already patched.");
                else
                {
                    File.WriteAllBytes(p, Encoding.Unicode.GetBytes(n));
                    Say("[" + ver + "] patched — tabs now show only the file name.");
                }
            }
            catch (Exception ex) { Say("[" + ver + "] ERROR: " + ex.Message); }
        }
        Say("");
        Say(restore ? "Done. Default tabs are back." : "Done. Start Photoshop to see the result.");
    }

    static string[] SafeFiles(string locales)
    {
        try
        {
            return Directory.Exists(locales)
                ? Directory.GetFiles(locales, "tw10428_*.dat", SearchOption.AllDirectories)
                : new string[0];
        }
        catch { return new string[0]; }
    }
}
