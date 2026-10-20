// 蓝色大肥鱼 TodoList
// 目标框架 .NET Framework 4.x（Windows 自带）；用系统自带 csc.exe 编译，无任何第三方依赖。
//
//   csc /target:winexe /out:BlueFishTodo.exe /win32icon:fish.ico src\Todo.cs

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace BlueFishTodo
{
    // ------------------------------------------------------------------ 主题
    internal static class Theme
    {
        public static readonly Color BgTop = Color.FromArgb(8, 24, 40);
        public static readonly Color BgBottom = Color.FromArgb(17, 47, 72);
        public static readonly Color Card = Color.FromArgb(21, 47, 71);
        public static readonly Color CardHover = Color.FromArgb(29, 63, 92);
        public static readonly Color WindowBg = Color.FromArgb(12, 33, 53);
        public static readonly Color Accent = Color.FromArgb(79, 195, 247);
        public static readonly Color AccentDeep = Color.FromArgb(41, 150, 214);
        public static readonly Color Ink = Color.FromArgb(230, 243, 255);
        public static readonly Color Muted = Color.FromArgb(124, 164, 196);
        public static readonly Color DoneInk = Color.FromArgb(88, 117, 141);
        public static readonly Color Danger = Color.FromArgb(255, 107, 107);
        public static readonly Color ChipBg = Color.FromArgb(34, 79, 116);
        public static readonly Color Field = Color.FromArgb(238, 247, 255);

        public static readonly string FontName = PickFont();

        public static readonly Font F11 = new Font(FontName, 11f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font F12 = new Font(FontName, 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font F13 = new Font(FontName, 13f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font F14 = new Font(FontName, 14f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font F15 = new Font(FontName, 15f, FontStyle.Regular, GraphicsUnit.Pixel);
        public static readonly Font F14B = new Font(FontName, 14f, FontStyle.Bold, GraphicsUnit.Pixel);
        public static readonly Font F19B = new Font(FontName, 19f, FontStyle.Bold, GraphicsUnit.Pixel);

        private static string PickFont()
        {
            string[] want = new string[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei" };
            for (int i = 0; i < want.Length; i++)
            {
                try
                {
                    Font f = new Font(want[i], 12f);
                    string nm = f.Name;
                    f.Dispose();
                    if (nm == want[i]) return want[i];
                }
                catch { }
            }
            return FontFamily.GenericSansSerif.Name;
        }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            GraphicsPath p = new GraphicsPath();
            float d = rad * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0f) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180f, 90f);
            p.AddArc(r.Right - d, r.Y, d, d, 270f, 90f);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            p.AddArc(r.X, r.Bottom - d, d, d, 90f, 90f);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, RectangleF r, float rad, Color c)
        {
            using (GraphicsPath p = Round(r, rad))
            using (SolidBrush b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void VGradient(Graphics g, Rectangle r, Color a, Color b)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (LinearGradientBrush br = new LinearGradientBrush(r, a, b, 90f))
                g.FillRectangle(br, r);
        }

        // 一条胖胖的蓝鱼，朝右
        public static void DrawFish(Graphics g, RectangleF r, Color body, Color fin)
        {
            using (SolidBrush bf = new SolidBrush(fin))
            {
                PointF[] tail = new PointF[]
                {
                    new PointF(r.Left + r.Width * 0.18f, r.Top + r.Height * 0.50f),
                    new PointF(r.Left - r.Width * 0.14f, r.Top + r.Height * 0.04f),
                    new PointF(r.Left - r.Width * 0.14f, r.Top + r.Height * 0.96f)
                };
                g.FillPolygon(bf, tail);
                PointF[] dorsal = new PointF[]
                {
                    new PointF(r.Left + r.Width * 0.32f, r.Top + r.Height * 0.12f),
                    new PointF(r.Left + r.Width * 0.54f, r.Top - r.Height * 0.18f),
                    new PointF(r.Left + r.Width * 0.68f, r.Top + r.Height * 0.16f)
                };
                g.FillPolygon(bf, dorsal);
            }
            using (SolidBrush bb = new SolidBrush(body))
                g.FillEllipse(bb, r);

            float eyeX = r.Left + r.Width * 0.76f;
            float eyeY = r.Top + r.Height * 0.36f;
            float eyeR = Math.Max(2f, r.Height * 0.12f);
            using (SolidBrush w = new SolidBrush(Color.White))
                g.FillEllipse(w, eyeX - eyeR, eyeY - eyeR, eyeR * 2, eyeR * 2);
            using (SolidBrush d = new SolidBrush(Color.FromArgb(12, 30, 46)))
                g.FillEllipse(d, eyeX - eyeR * 0.2f, eyeY - eyeR * 0.5f, eyeR, eyeR);

            using (SolidBrush ro = new SolidBrush(Color.FromArgb(150, 255, 150, 170)))
                g.FillEllipse(ro, r.Left + r.Width * 0.56f, r.Top + r.Height * 0.54f,
                              r.Width * 0.15f, r.Height * 0.17f);
        }

        // ---- 角色头像（内嵌资源，缺失时退回上面的矢量小鱼） ----
        private static Bitmap _head;
        private static bool _headTried;

        public static Bitmap Head
        {
            get
            {
                if (!_headTried)
                {
                    _headTried = true;
                    try
                    {
                        Assembly asm = Assembly.GetExecutingAssembly();
                        foreach (string n in asm.GetManifestResourceNames())
                        {
                            if (!n.EndsWith("head.png", StringComparison.OrdinalIgnoreCase)) continue;
                            using (Stream s = asm.GetManifestResourceStream(n))
                            using (Bitmap raw = new Bitmap(s))
                            {
                                Bitmap copy = new Bitmap(raw.Width, raw.Height,
                                                         PixelFormat.Format32bppArgb);
                                using (Graphics g = Graphics.FromImage(copy))
                                {
                                    g.CompositingMode = CompositingMode.SourceCopy;
                                    g.DrawImage(raw, new Rectangle(0, 0, raw.Width, raw.Height));
                                }
                                _head = copy;
                            }
                            break;
                        }
                    }
                    catch { _head = null; }
                }
                return _head;
            }
        }

        public static void DrawHead(Graphics g, RectangleF r)
        {
            Bitmap b = Head;
            if (b == null) { DrawFish(g, r, AccentDeep, Accent); return; }
            float sc = Math.Min(r.Width / b.Width, r.Height / b.Height);
            float w = b.Width * sc, h = b.Height * sc;
            InterpolationMode old = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(b, r.X + (r.Width - w) / 2f, r.Y + (r.Height - h) / 2f, w, h);
            g.InterpolationMode = old;
        }
    }

    // ------------------------------------------------------------------ 数据
    public class Task
    {
        public int Id;
        public string Text = "";
        public bool Done;
        public string Category = "默认";
    }

    internal static class Store
    {
        private static string Enc(string s)
        {
            if (s == null) s = "";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        }

        private static string Dec(string s)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); }
            catch { return ""; }
        }

        public static string DataDir
        {
            get
            {
                string dir = "";
                try
                {
                    dir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "BlueFatFishTodo");
                }
                catch { }
                if (string.IsNullOrEmpty(dir))
                {
                    try { dir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "data"); }
                    catch { dir = "."; }
                }
                return dir;
            }
        }

        public static string FilePath { get { return Path.Combine(DataDir, "tasks.dat"); } }

        public static List<Task> Load()
        {
            List<Task> list = new List<Task>();
            try
            {
                if (!File.Exists(FilePath)) return list;
                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string ln = lines[i];
                    if (ln == null || ln.Trim().Length == 0) continue;
                    string[] f = ln.Split('\t');
                    if (f.Length < 4) continue;
                    Task t = new Task();
                    int id;
                    t.Id = int.TryParse(f[0], out id) ? id : (i + 1);
                    t.Done = f[1] == "1";
                    t.Category = Dec(f[2]);
                    if (string.IsNullOrEmpty(t.Category)) t.Category = "默认";
                    t.Text = Dec(f[3]);
                    if (t.Text.Length == 0) continue;
                    list.Add(t);
                }
            }
            catch { }
            return list;
        }

        public static void Save(List<Task> tasks)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < tasks.Count; i++)
                {
                    Task t = tasks[i];
                    sb.Append(t.Id).Append('\t')
                      .Append(t.Done ? "1" : "0").Append('\t')
                      .Append(Enc(t.Category)).Append('\t')
                      .Append(Enc(t.Text)).Append("\r\n");
                }
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }
    }

    // ------------------------------------------------------------------ 一行任务
    internal sealed class TaskRow : Control
    {
        private readonly MainForm _owner;
        public readonly Task Item;
        private bool _hover;
        private bool _hoverDel;
        private TextBox _editor;
        private readonly Timer _clickTimer;
        private bool _pendingToggle;
        private int _chipW;

        public const int RowHeight = 50;

        public TaskRow(MainForm owner, Task item)
        {
            _owner = owner;
            Item = item;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = RowHeight;
            Margin = new Padding(12, 4, 12, 4);
            Cursor = Cursors.Hand;

            _clickTimer = new Timer();
            _clickTimer.Interval = 200;
            _clickTimer.Tick += OnClickTick;
        }

        private void OnClickTick(object sender, EventArgs e)
        {
            _clickTimer.Stop();
            if (_pendingToggle)
            {
                _pendingToggle = false;
                _owner.ToggleTask(Item);
            }
        }

        private Rectangle DeleteRect
        {
            get { return new Rectangle(Width - 42, (Height - 28) / 2, 28, 28); }
        }

        private Rectangle CheckRect
        {
            get { return new Rectangle(14, (Height - 22) / 2, 22, 22); }
        }

        private Rectangle TextRect
        {
            get
            {
                int right = DeleteRect.Left - 10;
                if (_chipW > 0) right -= _chipW + 8;
                return new Rectangle(48, 0, Math.Max(20, right - 48), Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Theme.FillRound(g, new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), 12f,
                            _hover ? Theme.CardHover : Theme.Card);

            // 分类标签宽度（缓存，供命中测试使用）
            string cat = Item.Category == null ? "" : Item.Category;
            _chipW = cat.Length == 0 ? 0 : (int)g.MeasureString(cat, Theme.F11).Width + 16;

            Rectangle chk = CheckRect;
            using (Pen p = new Pen(Item.Done ? Theme.Accent : Theme.Muted, 1.8f))
                g.DrawEllipse(p, chk);
            if (Item.Done)
            {
                using (SolidBrush b = new SolidBrush(Theme.Accent))
                    g.FillEllipse(b, chk.X + 4, chk.Y + 4, chk.Width - 8, chk.Height - 8);
            }

            Rectangle tr = TextRect;
            if (_chipW > 0)
            {
                RectangleF chip = new RectangleF(tr.Right + 8, (Height - 22) / 2f, _chipW, 22f);
                Theme.FillRound(g, chip, 11f, Theme.ChipBg);
                using (StringFormat sf = new StringFormat())
                {
                    sf.Alignment = StringAlignment.Center;
                    sf.LineAlignment = StringAlignment.Center;
                    using (SolidBrush cb = new SolidBrush(Theme.Accent))
                        g.DrawString(cat, Theme.F11, cb, chip, sf);
                }
            }

            using (SolidBrush b = new SolidBrush(Item.Done ? Theme.DoneInk : Theme.Ink))
            using (StringFormat sf = new StringFormat())
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(Item.Text, Theme.F14, b, tr, sf);
            }
            if (Item.Done)
            {
                SizeF sz = g.MeasureString(Item.Text, Theme.F14);
                float lineW = Math.Min(sz.Width, tr.Width - 4);
                float y = tr.Top + tr.Height / 2f;
                using (Pen sp = new Pen(Theme.DoneInk, 1.6f))
                    g.DrawLine(sp, tr.Left + 1, y, tr.Left + 1 + lineW, y);
            }

            Rectangle dr = DeleteRect;
            if (_hoverDel)
                Theme.FillRound(g, dr, 8f, Color.FromArgb(60, Theme.Danger));
            using (Pen xp = new Pen(_hoverDel ? Theme.Danger : Theme.Muted, 2f))
            {
                xp.StartCap = LineCap.Round;
                xp.EndCap = LineCap.Round;
                g.DrawLine(xp, dr.Left + 9, dr.Top + 9, dr.Right - 9, dr.Bottom - 9);
                g.DrawLine(xp, dr.Right - 9, dr.Top + 9, dr.Left + 9, dr.Bottom - 9);
            }
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            if (!_hoverDel) Invalidate();
            else { _hoverDel = false; Invalidate(); }
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (DeleteRect.Contains(e.Location)) return;
            _owner.BeginRowPress(this, e.Y);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool overDel = DeleteRect.Contains(e.Location);
            if (overDel != _hoverDel) { _hoverDel = overDel; Invalidate(); }
            if (e.Button == MouseButtons.Left)
                _owner.RowPressMove(this, PointToScreen(e.Location));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            bool wasDragging = _owner.RowPressUp(this);
            if (wasDragging) return;
            if (e.Button != MouseButtons.Left) return;

            if (DeleteRect.Contains(e.Location)) { _owner.DeleteTask(Item); return; }
            if (CheckRect.Contains(e.Location) || TextRect.Contains(e.Location))
            {
                _pendingToggle = true;
                _clickTimer.Stop();
                _clickTimer.Start();
            }
        }

        protected override void OnDoubleClick(EventArgs e)
        {
            base.OnDoubleClick(e);
            _clickTimer.Stop();
            _pendingToggle = false;
            BeginEdit();
        }

        public void BeginEdit()
        {
            if (_editor != null) return;
            Rectangle tr = TextRect;
            _editor = new TextBox();
            _editor.Text = Item.Text;
            _editor.Font = Theme.F14;
            _editor.BorderStyle = BorderStyle.FixedSingle;
            _editor.BackColor = Color.White;
            _editor.ForeColor = Color.Black;
            _editor.Bounds = new Rectangle(tr.Left, (Height - 26) / 2, Math.Max(60, tr.Width), 26);
            _editor.KeyDown += EditorKey;
            _editor.LostFocus += EditorLost;
            Controls.Add(_editor);
            _editor.Focus();
            _editor.SelectAll();
        }

        private void EditorKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; CommitEdit(); }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                if (_editor != null) { _editor.Dispose(); _editor = null; Invalidate(); }
            }
        }

        private void EditorLost(object sender, EventArgs e) { CommitEdit(); }

        private void CommitEdit()
        {
            if (_editor == null) return;
            string t = _editor.Text.Trim();
            _editor.Dispose();
            _editor = null;
            if (t.Length == 0) { Invalidate(); return; }
            _owner.RenameTask(Item, t);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _clickTimer != null) _clickTimer.Dispose();
            base.Dispose(disposing);
        }
    }

    // ------------------------------------------------------------------ 最小化浮窗
    internal sealed class MiniForm : Form
    {
        private readonly List<Task> _items = new List<Task>();
        private int _total;
        private int _open;
        private readonly MainForm _owner;

        public MiniForm(MainForm owner)
        {
            _owner = owner;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Theme.WindowBg;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Width = 300;
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW：不进 Alt+Tab
                return cp;
            }
        }

        public void UpdateData(List<Task> tasks)
        {
            _items.Clear();
            _total = tasks.Count;
            _open = 0;
            for (int i = 0; i < tasks.Count; i++) if (!tasks[i].Done) _open++;
            for (int i = 0; i < tasks.Count && _items.Count < 10; i++)
                if (!tasks[i].Done) _items.Add(tasks[i]);

            Height = 64 + Math.Max(1, _items.Count) * 26 + 18;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Left = wa.Right - Width - 16;
            Top = wa.Bottom - Height - 16;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Theme.VGradient(g, new Rectangle(0, 0, Width, Height), Theme.BgTop, Theme.BgBottom);
            Theme.DrawHead(g, new RectangleF(10, 8, 44, 44));

            using (SolidBrush b = new SolidBrush(Theme.Ink))
                g.DrawString("未完成 " + _open + " 项", Theme.F14B, b, 60, 13);
            using (SolidBrush b = new SolidBrush(Theme.Muted))
                g.DrawString("前 " + _items.Count + " 条 · 共 " + _total + " 项 · 单击展开", Theme.F11, b, 60, 35);

            int y = 60;
            using (Pen p = new Pen(Color.FromArgb(46, Theme.Accent), 1f))
                g.DrawLine(p, 14, y, Width - 14, y);
            y += 8;

            if (_items.Count == 0)
            {
                using (SolidBrush b = new SolidBrush(Theme.Muted))
                    g.DrawString("全部完成啦，休息一下 ~", Theme.F13, b, 18, y + 4);
            }
            for (int i = 0; i < _items.Count; i++)
            {
                using (SolidBrush dot = new SolidBrush(Theme.Accent))
                    g.FillEllipse(dot, 18, y + 10, 6, 6);
                using (SolidBrush b = new SolidBrush(Theme.Ink))
                using (StringFormat sf = new StringFormat())
                {
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString((i + 1) + ". " + _items[i].Text, Theme.F13, b,
                                 new RectangleF(32, y + 2, Width - 48, 22), sf);
                }
                y += 26;
            }

            using (Pen p = new Pen(Color.FromArgb(80, Theme.Accent), 1.4f))
                g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            _owner.RestoreFromMini();
        }
    }

    // ------------------------------------------------------------------ 主窗口
    public class MainForm : Form
    {
        private readonly List<Task> _tasks = new List<Task>();
        private readonly List<TaskRow> _rows = new List<TaskRow>();
        private int _nextId = 1;

        private FlowLayoutPanel _listHost;
        private TextBox _input;
        private ComboBox _catInput;
        private ComboBox _catFilter;
        private Panel _header;
        private Panel _footer;
        private string _filter = "*";
        private bool _suspending;
        private MiniForm _mini;

        // 行拖拽
        private TaskRow _pressRow;
        private int _pressY;
        private bool _dragging;

        public MainForm()
        {
            Text = "蓝色大肥鱼 TodoList";
            // csc /win32icon 只把图标塞进 EXE 资源；WinForms 的 Form 默认用自带图标，
            // 必须从自己的 exe 里把图标取出来挂上，标题栏/任务栏才会显示角色头像
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 720);
            MinimumSize = new Size(470, 540);
            BackColor = Theme.WindowBg;
            DoubleBuffered = true;

            BuildUi();

            _tasks.AddRange(Store.Load());
            for (int i = 0; i < _tasks.Count; i++)
                if (_tasks[i].Id >= _nextId) _nextId = _tasks[i].Id + 1;

            RefreshCategories();
            RebuildRows();
        }

        // ---------------------------------------------------------- UI 搭建
        private void BuildUi()
        {
            // 用 TableLayoutPanel 固定纵向分区，避免 Dock 的 z-order 玄学
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.Padding = new Padding(0);
            root.Margin = new Padding(0);
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));   // 头部
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));   // 输入
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // 列表
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));   // 底部
            Controls.Add(root);

            _header = new Panel();
            _header.Dock = DockStyle.Fill;
            _header.Margin = new Padding(0);
            _header.Paint += HeaderPaint;
            root.Controls.Add(_header, 0, 0);

            Panel inputBar = new Panel();
            inputBar.Dock = DockStyle.Fill;
            inputBar.Margin = new Padding(0);
            inputBar.Paint += delegate(object s, PaintEventArgs e)
            {
                Theme.VGradient(e.Graphics, inputBar.ClientRectangle, Theme.BgTop, Theme.WindowBg);
            };
            root.Controls.Add(inputBar, 0, 1);

            _input = new TextBox();
            _input.Font = Theme.F15;
            _input.BorderStyle = BorderStyle.FixedSingle;
            _input.BackColor = Theme.Field;
            _input.ForeColor = Color.FromArgb(10, 30, 48);
            _input.SetBounds(20, 16, 330, 28);
            _input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _input.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddTask(); }
            };
            inputBar.Controls.Add(_input);

            Button add = MakeButton("添加", Theme.AccentDeep, Theme.Ink);
            add.SetBounds(360, 15, 88, 30);
            add.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            add.Click += delegate { AddTask(); };
            inputBar.Controls.Add(add);

            _catInput = new ComboBox();
            _catInput.Font = Theme.F12;
            _catInput.DropDownStyle = ComboBoxStyle.DropDown;
            _catInput.SetBounds(66, 58, 150, 24);
            _catInput.BackColor = Theme.Field;
            inputBar.Controls.Add(_catInput);

            AddLabel(inputBar, "分类", 20, 62);
            AddLabel(inputBar, "筛选", 240, 62);

            _catFilter = new ComboBox();
            _catFilter.Font = Theme.F12;
            _catFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            _catFilter.SetBounds(286, 58, 162, 24);
            _catFilter.BackColor = Theme.Field;
            _catFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _catFilter.SelectedIndexChanged += delegate { ApplyFilter(); };
            inputBar.Controls.Add(_catFilter);

            _listHost = new FlowLayoutPanel();
            _listHost.Dock = DockStyle.Fill;
            _listHost.Margin = new Padding(0);
            _listHost.FlowDirection = FlowDirection.TopDown;
            _listHost.WrapContents = false;
            _listHost.AutoScroll = true;
            _listHost.BackColor = Theme.WindowBg;
            _listHost.Padding = new Padding(0, 6, 0, 6);
            _listHost.Resize += delegate { ResizeRows(); };
            _listHost.Paint += EmptyHint;
            root.Controls.Add(_listHost, 0, 2);

            _footer = new Panel();
            _footer.Dock = DockStyle.Fill;
            _footer.Margin = new Padding(0);
            _footer.Paint += FooterPaint;
            root.Controls.Add(_footer, 0, 3);

            Button clear = MakeButton("清除已完成", Color.FromArgb(92, 40, 52), Theme.Danger);
            clear.Size = new Size(112, 32);
            // 按设计时宽度算右对齐，再交给 Anchor 跟随缩放；
            // 不要在 Resize 里手算 Left —— 首次布局时 footer 宽度还没定下来。
            clear.Location = new Point(540 - 112 - 16, 12);
            clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clear.Click += delegate { ClearCompleted(); };
            _footer.Controls.Add(clear);
        }

        private void AddLabel(Control parent, string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = Theme.F12;
            l.ForeColor = Theme.Muted;
            l.AutoSize = true;
            l.BackColor = Color.Transparent;
            l.Location = new Point(x, y);
            parent.Controls.Add(l);
        }

        private Button MakeButton(string text, Color back, Color fore)
        {
            Button b = new Button();
            b.Text = text;
            b.Font = Theme.F13;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, fore);
            b.BackColor = back;
            b.ForeColor = fore;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            return b;
        }

        private void EmptyHint(object sender, PaintEventArgs e)
        {
            if (_rows.Count != 0) return;
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                using (SolidBrush b = new SolidBrush(Theme.Muted))
                    e.Graphics.DrawString("还没有任务，在上面输入一条吧 ~", Theme.F13, b,
                        new RectangleF(0, 60, _listHost.ClientSize.Width, 30), sf);
            }
        }

        private int OpenCount()
        {
            int n = 0;
            for (int i = 0; i < _tasks.Count; i++) if (!_tasks[i].Done) n++;
            return n;
        }

        private void HeaderPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Theme.VGradient(g, _header.ClientRectangle, Theme.BgTop, Theme.BgBottom);
            Theme.DrawHead(g, new RectangleF(20, 18, 60, 60));

            using (SolidBrush b = new SolidBrush(Theme.Ink))
                g.DrawString("蓝色大肥鱼 TodoList", Theme.F19B, b, 92, 18);

            int total = _tasks.Count;
            int open = OpenCount();
            using (SolidBrush b = new SolidBrush(Theme.Muted))
                g.DrawString("共 " + total + " 项    未完成 " + open + " 项    已完成 " + (total - open) + " 项",
                             Theme.F12, b, 93, 52);

            using (Pen p = new Pen(Color.FromArgb(46, 130, 185), 2f))
                g.DrawLine(p, 20, _header.Height - 1, _header.Width - 20, _header.Height - 1);
        }

        private void FooterPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Theme.VGradient(g, _footer.ClientRectangle, Theme.WindowBg, Theme.BgTop);
            using (Pen p = new Pen(Color.FromArgb(40, 110, 160), 1f))
                g.DrawLine(p, 20, 0, _footer.Width - 20, 0);
            using (SolidBrush b = new SolidBrush(Theme.Muted))
                g.DrawString("共 " + _tasks.Count + " 项 · 未完成 " + OpenCount() + " 项",
                             Theme.F12, b, 22, 19);
        }

        // ---------------------------------------------------------- 业务
        private void Persist()
        {
            Store.Save(_tasks);
            _header.Invalidate();
            _footer.Invalidate();
            if (_mini != null && !_mini.IsDisposed && _mini.Visible) _mini.UpdateData(_tasks);
        }

        private void AddTask()
        {
            string t = _input.Text == null ? "" : _input.Text.Trim();
            if (t.Length == 0) { _input.Focus(); return; }

            Task item = new Task();
            item.Id = _nextId++;
            item.Text = t;
            item.Category = CurrentInputCategory();

            _tasks.Add(item);
            _input.Text = "";
            _input.Focus();

            RefreshCategories();
            RebuildRows();
            Persist();
        }

        private string CurrentInputCategory()
        {
            string c = _catInput.Text == null ? "" : _catInput.Text.Trim();
            return c.Length == 0 ? "默认" : c;
        }

        internal void ToggleTask(Task t)
        {
            t.Done = !t.Done;
            RepaintRows();
            Persist();
        }

        internal void DeleteTask(Task t)
        {
            _tasks.RemoveAll(delegate(Task x) { return x.Id == t.Id; });
            // 从控件自己的鼠标事件里返回后再重建，避免释放正在处理事件的行
            BeginInvoke(new MethodInvoker(delegate
            {
                RefreshCategories();
                RebuildRows();
                Persist();
            }));
        }

        internal void RenameTask(Task t, string text)
        {
            t.Text = text;
            BeginInvoke(new MethodInvoker(delegate
            {
                RepaintRows();
                Persist();
            }));
        }

        private void ClearCompleted()
        {
            int done = 0;
            for (int i = 0; i < _tasks.Count; i++) if (_tasks[i].Done) done++;
            if (done == 0) return;
            if (MessageBox.Show(this, "确定清除 " + done + " 条已完成的任务吗？", "蓝色大肥鱼",
                                MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK) return;

            _tasks.RemoveAll(delegate(Task x) { return x.Done; });
            RebuildRows();
            Persist();
        }

        private void RepaintRows()
        {
            for (int i = 0; i < _rows.Count; i++) _rows[i].Invalidate();
            _header.Invalidate();
            _footer.Invalidate();
        }

        private void RefreshCategories()
        {
            _suspending = true;
            try
            {
                List<string> cats = new List<string>();
                for (int i = 0; i < _tasks.Count; i++)
                {
                    string c = _tasks[i].Category;
                    if (string.IsNullOrEmpty(c)) c = "默认";
                    if (!cats.Contains(c)) cats.Add(c);
                }
                if (cats.Count == 0) cats.Add("默认");

                string keepInput = _catInput.Text;
                _catInput.Items.Clear();
                for (int i = 0; i < cats.Count; i++) _catInput.Items.Add(cats[i]);
                if (keepInput != null && keepInput.Trim().Length > 0) _catInput.Text = keepInput;
                else _catInput.Text = cats[0];

                _catFilter.Items.Clear();
                _catFilter.Items.Add("全部");
                for (int i = 0; i < cats.Count; i++) _catFilter.Items.Add(cats[i]);

                int idx = 0;
                if (_filter != "*")
                {
                    int found = _catFilter.Items.IndexOf(_filter);
                    if (found > 0) idx = found;
                    else _filter = "*";   // 分类已不存在（任务被删/清空）
                }
                _catFilter.SelectedIndex = idx;
            }
            finally { _suspending = false; }
        }

        private void ApplyFilter()
        {
            if (_suspending) return;
            if (_catFilter.SelectedIndex <= 0) _filter = "*";
            else _filter = Convert.ToString(_catFilter.SelectedItem);
            RebuildRows();
        }

        private List<Task> VisibleTasks()
        {
            List<Task> list = new List<Task>();
            for (int i = 0; i < _tasks.Count; i++)
                if (_filter == "*" || _tasks[i].Category == _filter) list.Add(_tasks[i]);
            return list;
        }

        // ---------------------------------------------------------- 行管理
        private void RebuildRows()
        {
            List<Task> vis = VisibleTasks();
            _listHost.SuspendLayout();

            for (int i = 0; i < _rows.Count; i++) _rows[i].Dispose();
            _rows.Clear();
            _listHost.Controls.Clear();

            for (int i = 0; i < vis.Count; i++)
            {
                TaskRow r = new TaskRow(this, vis[i]);
                _rows.Add(r);
                _listHost.Controls.Add(r);
            }

            ResizeRows();
            _listHost.ResumeLayout(true);
            _listHost.Invalidate();
            _header.Invalidate();
            _footer.Invalidate();
        }

        private void ResizeRows()
        {
            int w = _listHost.ClientSize.Width - 24;
            if (_listHost.VerticalScroll.Visible) w -= 17;
            if (w < 100) w = 100;
            for (int i = 0; i < _rows.Count; i++) _rows[i].Width = w;
        }

        // ---------------------------------------------------------- 拖拽排序
        internal void BeginRowPress(TaskRow row, int y)
        {
            _pressRow = row;
            _pressY = y;
            _dragging = false;
        }

        internal void RowPressMove(TaskRow row, Point screenPt)
        {
            if (_pressRow == null || row != _pressRow) return;
            if (!_dragging)
            {
                if (Math.Abs(row.PointToClient(screenPt).Y - _pressY) < 6) return;
                _dragging = true;
                _pressRow.Cursor = Cursors.SizeAll;
            }

            Point pt = _listHost.PointToClient(screenPt);
            int target = -1;
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Bounds.Contains(pt)) { target = i; break; }
            }
            if (target < 0)
            {
                if (_rows.Count > 0 && pt.Y > _rows[_rows.Count - 1].Bounds.Bottom) target = _rows.Count - 1;
                else if (pt.Y < 0) target = 0;
            }
            if (target < 0) return;

            int cur = _rows.IndexOf(_pressRow);
            if (cur < 0 || cur == target) return;

            // 控件顺序即优先级顺序：调整子控件序号即可实时换位
            _listHost.Controls.SetChildIndex(_pressRow, target);
            _rows.Remove(_pressRow);
            _rows.Insert(target, _pressRow);
            _listHost.PerformLayout();
        }

        internal bool RowPressUp(TaskRow row)
        {
            if (_pressRow == null) return false;
            bool wasDragging = _dragging;

            if (wasDragging)
            {
                // 把 _tasks 重排成「可见行当前顺序」，隐藏（被筛选掉）的任务保持原位
                List<Task> ordered = new List<Task>();
                for (int i = 0; i < _rows.Count; i++) ordered.Add(_rows[i].Item);

                int k = 0;
                for (int i = 0; i < _tasks.Count; i++)
                {
                    if (_filter == "*" || _tasks[i].Category == _filter)
                    {
                        if (k < ordered.Count) _tasks[i] = ordered[k++];
                    }
                }
                Persist();
            }

            if (_pressRow != null) _pressRow.Cursor = Cursors.Hand;
            _pressRow = null;
            _dragging = false;
            return wasDragging;
        }

        // ---------------------------------------------------------- 最小化
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized)
                BeginInvoke(new MethodInvoker(ShowMini));
            else if (_mini != null && !_mini.IsDisposed && _mini.Visible)
                _mini.Hide();
        }

        private void ShowMini()
        {
            if (_mini == null || _mini.IsDisposed) _mini = new MiniForm(this);
            _mini.UpdateData(_tasks);
            _mini.Show();
            Hide();
        }

        internal void RestoreFromMini()
        {
            if (_mini != null) _mini.Hide();
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            RebuildRows();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Persist();
            base.OnFormClosing(e);
        }
    }

    internal static class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new MainForm()); }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "蓝色大肥鱼 TodoList",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
