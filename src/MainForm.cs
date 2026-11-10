// BethKEY - key binding editor for Bethesda games (UI)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("BethKEY")]
[assembly: AssemblyDescription("Key binding editor for Fallout 3, Fallout: New Vegas, Fallout 4 and Skyrim Special Edition")]
[assembly: AssemblyProduct("BethKEY")]
[assembly: AssemblyCopyright("BethKEY")]
[assembly: AssemblyVersion("1.2.2.0")]
[assembly: AssemblyFileVersion("1.2.2.0")]

namespace BethKEY
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Crash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Crash(e.ExceptionObject as Exception);
            CmTables.ResourceText = name =>
            {
                using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                {
                    if (st == null) return null;
                    using (StreamReader r = new StreamReader(st)) return r.ReadToEnd();
                }
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try { Application.Run(new MainForm()); }
            catch (Exception ex) { Crash(ex); }
        }

        public static void Crash(Exception ex)
        {
            string text = ex == null ? "Unknown error" : ex.ToString();
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BethKEY-error.log"), DateTime.Now + Environment.NewLine + text); }
            catch { }
            MessageBox.Show("BethKEY hit an error:\n\n" + (ex == null ? text : ex.Message) + "\n\nDetails were saved to BethKEY-error.log next to BethKEY.exe.",
                "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(34, 34, 34);
        public static readonly Color Surface = Color.FromArgb(47, 47, 47);
        public static readonly Color Surface2 = Color.FromArgb(58, 58, 58);
        public static readonly Color Row = Color.FromArgb(40, 40, 40);
        public static readonly Color RowAlt = Color.FromArgb(46, 46, 46);
        public static readonly Color Border = Color.FromArgb(68, 68, 68);
        public static readonly Color Primary = Color.FromArgb(55, 90, 127);
        public static readonly Color PrimaryLight = Color.FromArgb(76, 118, 163);
        public static readonly Color Success = Color.FromArgb(0, 188, 140);
        public static readonly Color Danger = Color.FromArgb(231, 76, 60);
        public static readonly Color DangerBg = Color.FromArgb(110, 40, 34);
        public static readonly Color Warning = Color.FromArgb(243, 156, 18);
        public static readonly Color Text = Color.White;
        public static readonly Color Muted = Color.FromArgb(173, 181, 189);
        public static readonly Font Font = new Font("Segoe UI", 9.75f);
        public static readonly Font Bold = new Font("Segoe UI Semibold", 9.75f);
        public static readonly Font Mono = new Font("Consolas", 9.75f);
        public static float Scale = 1f;
        public static int S(int px) { return (int)Math.Round(px * Scale); }

        public static Button Button(string text, bool primary)
        {
            Button b = new Button();
            b.Text = text; b.AutoSize = true;
            b.MinimumSize = new Size(S(96), S(34));
            b.Padding = new Padding(S(10), 0, S(10), 0);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = primary ? Primary : Surface2;
            b.FlatAppearance.MouseOverBackColor = primary ? PrimaryLight : Border;
            b.FlatAppearance.MouseDownBackColor = primary ? PrimaryLight : Border;
            b.ForeColor = Text; b.Font = primary ? Bold : Font;
            b.Cursor = Cursors.Hand; b.UseVisualStyleBackColor = false;
            b.Margin = new Padding(S(6), S(4), 0, S(4));
            return b;
        }
    }

    /// Small INI next to the exe that remembers the selected game and folders.
    static class Settings
    {
        static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BethKEY.ini");
        static readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static Settings()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0 && !line.StartsWith("[")) Values[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            catch { }
        }

        public static string Get(string key) { string v; return Values.TryGetValue(key, out v) ? v : null; }

        public static void Set(string key, string value)
        {
            Values[key] = value;
            try
            {
                StringBuilder sb = new StringBuilder("[BethKEY]\r\n");
                foreach (KeyValuePair<string, string> kv in Values) sb.Append(kv.Key).Append('=').Append(kv.Value).Append("\r\n");
                File.WriteAllText(FilePath, sb.ToString());
            }
            catch { }
        }
    }

    /// Modal "press a key / click a mouse button" dialog.
    /// Reports DirectInput scancodes, or Windows virtual-key codes for Fallout 4.
    class CaptureDialog : Form
    {
        public int KeyCode = -1, MouseCode = -1;
        readonly bool allowKeys, allowMouse, allowWheel, virtualKeys;
        readonly int wheelUp, wheelDown;
        readonly Label box;

        public CaptureDialog(string action, GameDef g, bool allowKeys, bool allowMouse)
        {
            this.allowKeys = allowKeys; this.allowMouse = allowMouse;
            virtualKeys = g.Keys == KeySet.VirtualKey;
            wheelUp = virtualKeys ? 0x800 : 0x8; wheelDown = virtualKeys ? 0x900 : 0x9;
            allowWheel = allowMouse && g.Mouse.ContainsKey(wheelUp);
            Text = "BethKEY - bind " + action;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false; KeyPreview = true;
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font;
            ClientSize = new Size(Theme.S(420), Theme.S(200));

            string what = allowKeys && allowMouse ? "Press a key, or click a mouse button in this box"
                        : allowKeys ? "Press a key\r\n(this action can't use the mouse)"
                        : "Click a mouse button in this box\r\n(this action can't use the keyboard)";
            box = new Label();
            box.Text = what + (allowWheel ? "\r\n(or scroll the wheel)" : "") + "\r\n\r\n" + action + "\r\n\r\nEsc = cancel";
            box.TextAlign = ContentAlignment.MiddleCenter;
            box.BackColor = Theme.Surface; box.ForeColor = Theme.Text; box.Font = new Font("Segoe UI Semibold", 11f);
            box.SetBounds(Theme.S(14), Theme.S(14), Theme.S(392), Theme.S(126));
            box.MouseDown += (s, e) => OnMouse(e.Button);
            Controls.Add(box);

            Button cancel = Theme.Button("Cancel", false);
            cancel.AutoSize = false; cancel.TabStop = false;
            cancel.SetBounds(Theme.S(306), Theme.S(152), Theme.S(100), Theme.S(34));
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            MouseWheel += (s, e) => Wheel(e.Delta);
            box.MouseWheel += (s, e) => Wheel(e.Delta);
            Shown += (s, e) => ActiveControl = null;
        }

        void Wheel(int delta)
        {
            if (!allowWheel || delta == 0) return;
            MouseCode = delta > 0 ? wheelUp : wheelDown;
            DialogResult = DialogResult.OK; Close();
        }

        void OnMouse(MouseButtons b)
        {
            if (!allowMouse) return;
            switch (b)
            {
                case MouseButtons.Left: MouseCode = 0; break;
                case MouseButtons.Right: MouseCode = 1; break;
                case MouseButtons.Middle: MouseCode = 2; break;
                case MouseButtons.XButton1: MouseCode = 3; break;
                case MouseButtons.XButton2: MouseCode = 4; break;
                default: return;
            }
            DialogResult = DialogResult.OK; Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int WM_KEYDOWN = 0x100, WM_SYSKEYDOWN = 0x104;
            if (msg.Msg == WM_KEYDOWN || msg.Msg == WM_SYSKEYDOWN)
            {
                long lp = msg.LParam.ToInt64();
                int sc = (int)((lp >> 16) & 0xFF);
                bool extended = ((lp >> 24) & 1) == 1;
                int vk = (int)msg.WParam.ToInt64();
                if (vk == 0x1B) { DialogResult = DialogResult.Cancel; Close(); return true; }   // Esc cancels
                if (!allowKeys) return true;
                int code;
                if (virtualKeys)
                {
                    code = vk;
                    if (vk == 0x10) code = sc == 0x36 ? 0xA1 : 0xA0;          // Shift -> left/right
                    else if (vk == 0x11) code = extended ? 0xA3 : 0xA2;       // Ctrl
                    else if (vk == 0x12) code = extended ? 0xA5 : 0xA4;       // Alt
                }
                else
                {
                    code = sc;
                    if (vk == 0x13) code = 0xC5;            // Pause
                    else if (vk == 0x90) code = 0x45;       // Num Lock
                    else if (extended) code |= 0x80;        // arrows, right ctrl/alt, numpad enter...
                    if (code == 0x80) code = 0;
                }
                if (code > 0) { KeyCode = code; DialogResult = DialogResult.OK; Close(); }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    class MainForm : Form
    {
        readonly List<GameDef> games = GameDef.All();
        GameDef game;
        List<BindRow> rows = new List<BindRow>();
        TextLines cmDoc;            // text control map being edited
        CmBinary cmBinary;          // binary ControlMap_Custom.txt being edited
        bool cmNew;                 // file doesn't exist yet; Save creates it
        List<CmEvent> table;        // action list used for the binary map (vanilla or a mod's)
        Button btnActionList;
        ComboBox cboMap;
        Label lblMap;
        string hint;                // extra line for the status bar (e.g. how to reach the console key)
        string loadedPath;
        bool loading;
        Label lblPath;

        ComboBox cboGame;
        TextBox txtFolder, txtSearch;
        Button btnFindKey;
        string findField, findName; bool findMouse;       // "Find by key": the key or mouse button being looked for
        CheckBox chkSecondary, chkReadOnly;
        bool syncingRo;
        ListBox lstCats;
        DataGridView grid;
        DataGridViewComboBoxColumn colKb, colMouse;
        Label lblStatus, lblFile, lblMissing;
        Button btnSave, btnRevert, btnDefaults, btnUnbind, btnSavePreset, btnLoadPreset;
        Panel missingPanel;

        public MainForm()
        {
            using (Graphics g = CreateGraphics()) Theme.Scale = g.DpiX / 96f;
            AutoScaleMode = AutoScaleMode.None;
            SuspendLayout();
            Text = "BethKEY";
            try { using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("BethKEY.ico")) if (st != null) Icon = new Icon(st); } catch { }
            BackColor = Theme.Bg; ForeColor = Theme.Text; Font = Theme.Font;
            Size = new Size(Theme.S(1180), Theme.S(780));
            MinimumSize = new Size(Theme.S(900), Theme.S(560));
            StartPosition = FormStartPosition.CenterScreen;

            BuildGrid();
            Panel center = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg, Padding = new Padding(0, Theme.S(8), Theme.S(12), 0) };
            missingPanel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Visible = false, Padding = new Padding(Theme.S(30)) };
            lblMissing = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleCenter };
            missingPanel.Controls.Add(lblMissing);
            btnActionList = Theme.Button("Use a mod's ControlMap.txt...", true);
            btnActionList.Dock = DockStyle.Bottom; btnActionList.Visible = false;
            btnActionList.Click += (s, e) => PickActionList();
            missingPanel.Controls.Add(btnActionList);
            center.Controls.Add(grid);
            center.Controls.Add(missingPanel);

            Controls.Add(center);
            Controls.Add(BuildSidebar());
            Controls.Add(BuildFooter());
            Controls.Add(BuildPathBar());
            Controls.Add(BuildHeader());
            ResumeLayout(true);

            Shown += (s, e) => SelectInitialGame();
            FormClosing += (s, e) => { if (!ConfirmDiscard("quit")) e.Cancel = true; };
        }

        // ------------------------------------------------------------------ layout

        Control BuildHeader()
        {
            Panel p = new Panel { Dock = DockStyle.Top, Height = Theme.S(70), BackColor = Theme.Surface, Padding = new Padding(Theme.S(16), 0, Theme.S(16), 0) };

            PictureBox pic = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, Width = Theme.S(44), Height = Theme.S(44), Location = new Point(Theme.S(16), Theme.S(13)) };
            try { using (Stream st = Assembly.GetExecutingAssembly().GetManifestResourceStream("BethKEY.ico")) if (st != null) pic.Image = new Icon(st, 64, 64).ToBitmap(); } catch { }
            p.Controls.Add(pic);

            Label title = new Label { Text = "BethKEY", AutoSize = true, Font = new Font("Segoe UI Semibold", 18f), ForeColor = Theme.Text, Location = new Point(Theme.S(68), Theme.S(8)) };
            p.Controls.Add(title);
            Label sub = new Label { Text = "Key bindings for Bethesda games", AutoSize = true, ForeColor = Theme.Muted, Location = new Point(Theme.S(71), Theme.S(44)) };
            p.Controls.Add(sub);

            FlowLayoutPanel right = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, Padding = new Padding(0, Theme.S(18), 0, 0), BackColor = Theme.Surface };
            Label lbl = new Label { Text = "Game", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, Theme.S(8), Theme.S(8), 0) };
            cboGame = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Width = Theme.S(270), BackColor = Theme.Surface2, ForeColor = Theme.Text, Font = Theme.Bold };
            cboGame.DrawMode = DrawMode.OwnerDrawFixed;
            cboGame.ItemHeight = Theme.S(24);
            cboGame.DrawItem += DrawDarkItem;
            foreach (GameDef g in games) cboGame.Items.Add(g);
            cboGame.SelectionChangeCommitted += (s, e) => SwitchGame((GameDef)cboGame.SelectedItem);
            lblMap = new Label { Text = "Map", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(Theme.S(16), Theme.S(8), Theme.S(8), 0) };
            cboMap = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Width = Theme.S(330), BackColor = Theme.Surface2, ForeColor = Theme.Text, Font = Theme.Font };
            cboMap.DrawMode = DrawMode.OwnerDrawFixed;
            cboMap.ItemHeight = Theme.S(24);
            cboMap.DrawItem += DrawDarkItem;
            cboMap.SelectionChangeCommitted += (s, e) => SwitchMap(cboMap.SelectedIndex);
            lblMap.Margin = new Padding(0, Theme.S(8), Theme.S(8), 0);
            lbl.Margin = new Padding(Theme.S(16), Theme.S(8), Theme.S(8), 0);
            right.Controls.Add(lblMap);
            right.Controls.Add(cboMap);
            right.Controls.Add(lbl);
            right.Controls.Add(cboGame);
            p.Controls.Add(right);
            return p;
        }

        Control BuildPathBar()
        {
            TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Top, Height = Theme.S(50), ColumnCount = 7, RowCount = 1, BackColor = Theme.Bg, Padding = new Padding(Theme.S(12), Theme.S(6), Theme.S(12), 0) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            Label lbl = lblPath = new Label { Text = "Folder", AutoSize = true, ForeColor = Theme.Muted, Anchor = AnchorStyles.Left, Margin = new Padding(Theme.S(4), 0, Theme.S(8), 0) };
            txtFolder = new TextBox { BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface2, ForeColor = Theme.Text, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            txtFolder.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ReloadWithConfirm(); } };
            Button browse = Theme.Button("Browse...", false);
            browse.Click += (s, e) => Browse();
            Button reload = Theme.Button("Reload", false);
            reload.Click += (s, e) => ReloadWithConfirm();
            Button open = Theme.Button("Open folder", false);
            open.Click += (s, e) => OpenFolder();
            chkSecondary = new CheckBox { Text = "Also write Fallout.ini", AutoSize = true, Checked = true, ForeColor = Theme.Text, Anchor = AnchorStyles.Left, Margin = new Padding(Theme.S(14), 0, 0, 0) };

            t.Controls.Add(lbl, 0, 0); t.Controls.Add(txtFolder, 1, 0); t.Controls.Add(browse, 2, 0);
            chkReadOnly = new CheckBox { Text = "Read-only", AutoSize = true, ForeColor = Theme.Text, Anchor = AnchorStyles.Left, Margin = new Padding(Theme.S(14), 0, 0, 0) };
            new ToolTip().SetToolTip(chkReadOnly, "Ticked: the file is set read-only (now, and after every save), so the game or its launcher can't change your keys.\nUnticked: the file is left writable.");
            chkReadOnly.CheckedChanged += (s, e) => { if (!syncingRo) ApplyReadOnly(true); };
            t.Controls.Add(reload, 3, 0); t.Controls.Add(open, 4, 0); t.Controls.Add(chkSecondary, 5, 0); t.Controls.Add(chkReadOnly, 6, 0);
            return t;
        }

        Control BuildSidebar()
        {
            Panel p = new Panel { Dock = DockStyle.Left, Width = Theme.S(220), BackColor = Theme.Bg, Padding = new Padding(Theme.S(12), Theme.S(8), Theme.S(12), 0) };
            lstCats = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Theme.Surface, ForeColor = Theme.Text, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = Theme.S(34), IntegralHeight = false };
            lstCats.DrawItem += DrawCategory;
            lstCats.SelectedIndexChanged += (s, e) => ApplyFilter();

            const string placeholder = "Search actions or keys...";
            Panel searchWrap = new Panel { Dock = DockStyle.Top, Height = Theme.S(84), BackColor = Theme.Bg };
            txtSearch = new TextBox { Dock = DockStyle.Top, BorderStyle = BorderStyle.FixedSingle, BackColor = Theme.Surface2, ForeColor = Theme.Muted, Text = placeholder };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.ForeColor == Theme.Muted) { txtSearch.Text = ""; txtSearch.ForeColor = Theme.Text; } };
            txtSearch.LostFocus += (s, e) => { if (txtSearch.Text.Length == 0) { txtSearch.ForeColor = Theme.Muted; txtSearch.Text = placeholder; } };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            btnFindKey = Theme.Button("Find by key...", false);
            btnFindKey.AutoSize = false; btnFindKey.Dock = DockStyle.Bottom; btnFindKey.Height = Theme.S(34);
            btnFindKey.Margin = new Padding(0);
            btnFindKey.Click += (s, e) => FindByKey();
            searchWrap.Controls.Add(btnFindKey);
            searchWrap.Controls.Add(txtSearch);

            p.Controls.Add(lstCats);
            p.Controls.Add(searchWrap);
            return p;
        }

        Control BuildFooter()
        {
            Panel p = new Panel { Dock = DockStyle.Bottom, Height = Theme.S(84), BackColor = Theme.Surface, Padding = new Padding(Theme.S(16), Theme.S(6), Theme.S(16), Theme.S(6)) };
            FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft, BackColor = Theme.Surface, Padding = new Padding(0, Theme.S(14), 0, 0) };
            btnSave = Theme.Button("Save", true); btnSave.MinimumSize = new Size(Theme.S(120), Theme.S(36));
            btnSave.Click += (s, e) => Save();
            btnRevert = Theme.Button("Revert changes", false);
            btnRevert.Click += (s, e) => { foreach (BindRow r in rows) r.Revert(); RefreshAll(); };
            btnDefaults = Theme.Button("Vanilla defaults", false);
            btnDefaults.Click += (s, e) => VanillaDefaults();
            btnUnbind = Theme.Button("Unbind selected", false);
            btnUnbind.Click += (s, e) => UnbindSelected();
            btnLoadPreset = Theme.Button("Load preset...", false);
            btnLoadPreset.Click += (s, e) => LoadPreset();
            btnSavePreset = Theme.Button("Save preset...", false);
            btnSavePreset.Click += (s, e) => SavePreset();
            buttons.Controls.Add(btnSave); buttons.Controls.Add(btnRevert); buttons.Controls.Add(btnDefaults); buttons.Controls.Add(btnUnbind);
            buttons.Controls.Add(btnLoadPreset); buttons.Controls.Add(btnSavePreset);

            Panel left = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
            lblFile = new Label { Dock = DockStyle.Top, Height = Theme.S(30), ForeColor = Theme.Muted, TextAlign = ContentAlignment.BottomLeft, AutoEllipsis = true };
            lblStatus = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true, Padding = new Padding(0, Theme.S(4), 0, 0) };
            left.Controls.Add(lblStatus);
            left.Controls.Add(lblFile);

            p.Controls.Add(left);
            p.Controls.Add(buttons);
            return p;
        }

        void BuildGrid()
        {
            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false; grid.AllowUserToDeleteRows = false; grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.BackgroundColor = Theme.Bg; grid.BorderStyle = BorderStyle.None; grid.GridColor = Theme.Border;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Theme.Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Muted;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Surface;
            grid.ColumnHeadersDefaultCellStyle.Font = Theme.Bold;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(Theme.S(6), 0, 0, 0);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = Theme.S(36);
            grid.DefaultCellStyle.BackColor = Theme.Row; grid.DefaultCellStyle.ForeColor = Theme.Text;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 70, 92);
            grid.DefaultCellStyle.SelectionForeColor = Theme.Text;
            grid.DefaultCellStyle.Padding = new Padding(Theme.S(6), 0, Theme.S(4), 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Theme.RowAlt;
            grid.RowTemplate.Height = Theme.S(32);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            DataGridViewTextBoxColumn colAction = new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", ReadOnly = true, FillWeight = 150, SortMode = DataGridViewColumnSortMode.NotSortable };
            colAction.DefaultCellStyle.Font = Theme.Bold;
            DataGridViewTextBoxColumn colEvent = new DataGridViewTextBoxColumn { Name = "Event", HeaderText = "Name in file", ReadOnly = true, FillWeight = 100, SortMode = DataGridViewColumnSortMode.NotSortable };
            colEvent.DefaultCellStyle.ForeColor = Theme.Muted;
            DataGridViewTextBoxColumn colCat = new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", ReadOnly = true, FillWeight = 90, SortMode = DataGridViewColumnSortMode.NotSortable, Visible = false };
            colCat.DefaultCellStyle.ForeColor = Theme.PrimaryLight;
            colKb = new DataGridViewComboBoxColumn { Name = "Keyboard", HeaderText = "Keyboard", FillWeight = 130, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing };
            colMouse = new DataGridViewComboBoxColumn { Name = "Mouse", HeaderText = "Mouse", FillWeight = 110, FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing };
            DataGridViewButtonColumn colBind = new DataGridViewButtonColumn { Name = "Bind", HeaderText = "", Text = "Press to bind", UseColumnTextForButtonValue = true, FillWeight = 80, FlatStyle = FlatStyle.Flat };
            colBind.DefaultCellStyle.BackColor = Theme.Primary;
            colBind.DefaultCellStyle.SelectionBackColor = Theme.PrimaryLight;
            DataGridViewTextBoxColumn colPad = new DataGridViewTextBoxColumn { Name = "Pad", HeaderText = "Gamepad (kept)", ReadOnly = true, FillWeight = 70, SortMode = DataGridViewColumnSortMode.NotSortable };
            colPad.DefaultCellStyle.ForeColor = Theme.Muted;
            DataGridViewTextBoxColumn colValue = new DataGridViewTextBoxColumn { Name = "Value", HeaderText = "Value written", ReadOnly = true, FillWeight = 85, SortMode = DataGridViewColumnSortMode.NotSortable };
            colValue.DefaultCellStyle.Font = Theme.Mono;
            grid.Columns.AddRange(new DataGridViewColumn[] { colAction, colCat, colEvent, colKb, colMouse, colBind, colPad, colValue });

            grid.DataError += (s, e) => { e.ThrowException = false; };
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += OnCellValueChanged;
            grid.CellContentClick += OnCellContentClick;
            grid.EditingControlShowing += (s, e) =>
            {
                ComboBox cb = e.Control as ComboBox;
                if (cb != null) { cb.BackColor = Theme.Surface2; cb.ForeColor = Theme.Text; cb.FlatStyle = FlatStyle.Flat; cb.MaxDropDownItems = 20; }
            };
        }

        void DrawDarkItem(object sender, DrawItemEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush b = new SolidBrush(sel ? Theme.Primary : Theme.Surface2)) e.Graphics.FillRectangle(b, e.Bounds);
            if (e.Index >= 0)
                TextRenderer.DrawText(e.Graphics, cb.Items[e.Index].ToString(), cb.Font, new Rectangle(e.Bounds.X + Theme.S(6), e.Bounds.Y, e.Bounds.Width, e.Bounds.Height),
                    Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        void DrawCategory(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            bool sel = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush b = new SolidBrush(sel ? Theme.Primary : Theme.Surface)) e.Graphics.FillRectangle(b, e.Bounds);
            if (sel) using (SolidBrush a = new SolidBrush(Theme.Success)) e.Graphics.FillRectangle(a, e.Bounds.X, e.Bounds.Y, Theme.S(4), e.Bounds.Height);
            string text = lstCats.Items[e.Index].ToString();
            TextRenderer.DrawText(e.Graphics, text, sel ? Theme.Bold : Theme.Font, new Rectangle(e.Bounds.X + Theme.S(14), e.Bounds.Y, e.Bounds.Width - Theme.S(16), e.Bounds.Height),
                Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        // ------------------------------------------------------------------ game / file loading

        bool IsCm { get { return game != null && game.Format == FileFormat.ControlMap; } }
        int MapIndex { get { return IsCm && cboMap.SelectedIndex > 0 ? 1 : 0; } }
        string SettingKey { get { return IsCm ? "sFile_" + game.ShortName + "_" + MapIndex : "sFolder_" + game.ShortName; } }

        void SelectInitialGame()
        {
            string saved = Settings.Get("sGame");
            GameDef pick = null;
            foreach (GameDef g in games) if (g.Name == saved) pick = g;
            if (pick == null) foreach (GameDef g in games) if (pick == null && Directory.Exists(g.DefaultFolder())) pick = g;
            if (pick == null) pick = games[0];
            cboGame.SelectedItem = pick;
            SwitchGame(pick, true);
        }

        void SwitchGame(GameDef g, bool force = false)
        {
            if (!force && g == game) return;
            if (!ConfirmDiscard("switch games")) { cboGame.SelectedItem = game; return; }
            game = g;
            Settings.Set("sGame", g.Name);
            cboMap.Items.Clear();
            lblMap.Visible = cboMap.Visible = IsCm;
            if (IsCm)
            {
                cboMap.Items.Add("In-game key map (ControlMap_Custom.txt)");
                cboMap.Items.Add(g.TextMapLabel);
                int m;
                cboMap.SelectedIndex = int.TryParse(Settings.Get("sMap_" + g.ShortName), out m) && m == 1 ? 1 : 0;
                lblPath.Text = "File";
                SetMapPath();
            }
            else
            {
                lblPath.Text = "Folder";
                string saved = Settings.Get(SettingKey);
                txtFolder.Text = !string.IsNullOrEmpty(saved) && Directory.Exists(saved) ? saved : g.DefaultFolder();
            }
            chkSecondary.Visible = !IsCm;
            LoadFiles();
        }

        void SwitchMap(int index)
        {
            int current;
            int.TryParse(Settings.Get("sMap_" + game.ShortName), out current);
            if (index == current) return;
            if (!ConfirmDiscard("switch maps")) { cboMap.SelectedIndex = current; return; }
            Settings.Set("sMap_" + game.ShortName, index.ToString());
            SetMapPath();
            LoadFiles();
        }

        /// Fills the path box for the selected map: the saved file if the user picked one, else the standard location.
        void SetMapPath()
        {
            string saved = Settings.Get(SettingKey);
            if (!string.IsNullOrEmpty(saved)) { try { if (File.Exists(saved) || Directory.Exists(Path.GetDirectoryName(saved))) { txtFolder.Text = saved; return; } } catch { } }
            txtFolder.Text = StandardMapPath(game, MapIndex) ?? "";
        }

        /// 0 = in-game ControlMap_Custom.txt in My Games; 1 = the text map inside the game folder (null if the folder isn't found).
        static string StandardMapPath(GameDef g, int map)
        {
            if (map == 0) return Path.Combine(g.DefaultFolder(), "ControlMap_Custom.txt");
            string install = GamePaths.InstallFolder(g);
            return install == null ? null : Path.Combine(install, g.TextMapRelPath);
        }

        string PrimaryPath() { return IsCm ? txtFolder.Text.Trim() : Path.Combine(txtFolder.Text.Trim(), game.FileName); }
        string SecondaryPath() { return game.SecondaryFileName == null ? null : Path.Combine(txtFolder.Text.Trim(), game.SecondaryFileName); }

        void ReloadWithConfirm()
        {
            if (!ConfirmDiscard("reload")) return;
            Settings.Set(SettingKey, txtFolder.Text.Trim());
            LoadFiles();
        }

        void OpenFolder()
        {
            try
            {
                string p = txtFolder.Text.Trim();
                if (p.Length == 0) return;
                if (!Directory.Exists(p)) p = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) Process.Start("explorer.exe", "\"" + p + "\"");
            }
            catch { }
        }

        void Browse()
        {
            if (IsCm)
            {
                using (OpenFileDialog fd = new OpenFileDialog())
                {
                    fd.Title = "Pick a control map: ControlMap_Custom.txt, controlmap.txt or F4SE CustomControlMap.txt";
                    fd.Filter = "Control map (*.txt)|*.txt|All files (*.*)|*.*";
                    fd.CheckFileExists = false;
                    string cur = txtFolder.Text.Trim();
                    try
                    {
                        string dir = Directory.Exists(cur) ? cur : Path.GetDirectoryName(cur);
                        if (Directory.Exists(dir)) fd.InitialDirectory = dir;
                        fd.FileName = Path.GetFileName(cur);
                    }
                    catch { }
                    if (fd.ShowDialog(this) != DialogResult.OK) return;
                    if (!ConfirmDiscard("load another file")) return;
                    txtFolder.Text = fd.FileName;
                }
            }
            else
            {
                using (FolderBrowserDialog fb = new FolderBrowserDialog())
                {
                    fb.Description = "Select the folder that contains " + game.FileName;
                    if (Directory.Exists(txtFolder.Text)) fb.SelectedPath = txtFolder.Text;
                    if (fb.ShowDialog(this) != DialogResult.OK) return;
                    if (!ConfirmDiscard("load another folder")) return;
                    txtFolder.Text = fb.SelectedPath;
                }
            }
            Settings.Set(SettingKey, txtFolder.Text.Trim());
            LoadFiles();
        }

        string ActionListKey { get { return "sActionList_" + game.ShortName; } }

        /// Action list for the binary map: a file the user picked, a mod's map (Fallout 4), or vanilla.
        List<CmEvent> ResolveTable(out string from)
        {
            from = null;
            string picked = Settings.Get(ActionListKey);
            if (!string.IsNullOrEmpty(picked) && File.Exists(picked))
            {
                string text = picked.EndsWith(".ba2", StringComparison.OrdinalIgnoreCase)
                    ? Fo4Maps.Read(Ba2.Extract(picked, Fo4Maps.MapPath)) : TextLines.Decode(File.ReadAllBytes(picked));
                List<CmEvent> t = CmTables.ParseMain(text);
                if (t.Count > 0) { from = picked; return t; }
            }
            if (game.Keys == KeySet.VirtualKey)
            {
                string mod = Fo4Maps.FindModded(GamePaths.InstallFolder(game), out from);
                List<CmEvent> t = CmTables.ParseMain(mod);
                if (t.Count > 0) return t;
                from = null;
            }
            return game.MainEvents;
        }

        void PickActionList()
        {
            using (OpenFileDialog fd = new OpenFileDialog())
            {
                fd.Title = "Pick the control map your key mod installs (ControlMap.txt, or the mod's .ba2)";
                fd.Filter = "Control map or archive (*.txt;*.ba2)|*.txt;*.ba2|All files (*.*)|*.*";
                if (fd.ShowDialog(this) != DialogResult.OK) return;
                Settings.Set(ActionListKey, fd.FileName);
                LoadFiles();
            }
        }

        void LoadFiles()
        {
            rows = new List<BindRow>();
            cmDoc = null; cmBinary = null; cmNew = false; table = null; hint = null;
            if (findField != null) { findField = null; btnFindKey.Text = "Find by key..."; btnFindKey.BackColor = Theme.Surface2; }
            btnActionList.Visible = false;
            loadedPath = null;
            string problem = null, mode = "";

            try
            {
                if (!IsCm)
                {
                    string primary = PrimaryPath(), secondary = SecondaryPath();
                    bool secondaryExists = secondary != null && File.Exists(secondary);
                    if (File.Exists(primary) || secondaryExists) rows = NvIni.Load(game, primary, secondary);
                    chkSecondary.Enabled = secondaryExists;
                    if (!secondaryExists) chkSecondary.Checked = false;
                    loadedPath = primary;
                    mode = game.FileName + (secondaryExists ? "  (+ " + game.SecondaryFileName + " if ticked)" : "") + "   in   " + txtFolder.Text;
                    hint = "Using Mod Organizer 2 with profile-specific INIs? Browse to MO2's profiles\\<profile> folder: the game reads the INIs there instead.";
                }
                else
                {
                    string path = PrimaryPath();
                    string name = path.Length == 0 ? "" : Path.GetFileName(path);
                    loadedPath = path;
                    bool inGame = string.Equals(name, "ControlMap_Custom.txt", StringComparison.OrdinalIgnoreCase);
                    if (path.Length == 0)
                        problem = "Couldn't find your " + game.Name + " folder.\n\nClick Browse..., go to your " + game.Name + " folder and pick (or type) " + game.TextMapRelPath + ". BethKEY creates the file if it isn't there.";
                    else if (File.Exists(path))
                    {
                        byte[] bytes = File.ReadAllBytes(path);
                        if (CmBinary.LooksBinary(bytes))
                        {
                            CmBinary m = CmBinary.Parse(bytes);
                            string why = "the file layout is not recognised", from;
                            table = ResolveTable(out from);
                            if (m != null && !m.Matches(table, out why) && table != game.MainEvents && m.Matches(game.MainEvents, out why)) { table = game.MainEvents; from = null; }
                            if (m != null && m.Matches(table, out why))
                            {
                                cmBinary = m;
                                rows = m.ToRows(game, table, name);
                                mode = "in-game key map (" + name + ")" + (from != null ? ", action list from " + Path.GetFileName(from) : "") + "   " + path;
                            }
                            else
                            {
                                problem = "This " + name + " doesn't match " + (from != null ? "the action list in " + Path.GetFileName(from) : "the vanilla " + game.Name + " action list")
                                    + " (" + why + "), so BethKEY won't edit it.\n\n"
                                    + (game.Keys == KeySet.VirtualKey
                                        ? "This usually means a mod (such as a key-mapping mod) changed the game's control map. If you use Mod Organizer, run BethKEY from inside MO2, or click the button below and pick the ControlMap.txt (or .ba2) from that mod.\n\nIt can also happen after a game update: rebind any key in the game's Controls menu to refresh the file."
                                        : "It may come from a different Skyrim version or a mod. Rebind any key in the game's Controls menu to refresh it, or switch Map to the text control map.");
                                btnActionList.Visible = game.Keys == KeySet.VirtualKey;
                            }
                        }
                        else
                        {
                            cmDoc = TextLines.FromBytes(bytes);
                            rows = ControlMap.Load(cmDoc, name, game);
                            mode = "text control map   " + path;
                            if (rows.Count == 0)
                                problem = name + " doesn't contain any key bindings BethKEY can read.\n\n"
                                    + (string.Equals(name, "ControlMap_Custom.txt", StringComparison.OrdinalIgnoreCase)
                                        ? "Your game may store this file in a format BethKEY doesn't know yet. Please send the file to the BethKEY author so support can be added."
                                          + (game.Keys == KeySet.DirectInput ? "\n\nMeanwhile: click Browse..., go to your Skyrim folder's Data\\Interface\\Controls\\PC and type controlmap.txt. BethKEY creates it from the default map and can switch this file off when you save." : "")
                                        : "Make sure it's a control map file (lines like  Forward  0x11  0xff ...).");
                        }
                    }
                    else if (inGame)
                    {
                        string from;
                        table = ResolveTable(out from);
                        cmBinary = CmBinary.FromDefaults(table, game.BinaryTag);
                        rows = cmBinary.ToRows(game, table, name);
                        cmNew = true;
                        mode = "in-game key map (new file, created when you save)" + (from != null ? ", action list from " + Path.GetFileName(from) : "") + "   " + path;
                    }
                    else if (game.Keys == KeySet.VirtualKey && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        // e.g. F4SE's Data\F4SE\CustomControlMap.txt: start from the game's own map (or a mod's)
                        string from;
                        string def = Fo4Maps.FindModded(GamePaths.InstallFolder(game), out from) ?? Fo4Maps.FromExe(Fo4Maps.FindExe(path, game));
                        if (def != null)
                        {
                            cmDoc = TextLines.FromBytes(Encoding.GetEncoding(28591).GetBytes(def.Replace("\r\n", "\n").Replace("\n", "\r\n")));
                            rows = ControlMap.Load(cmDoc, name, game);
                            cmNew = true;
                            mode = "text control map (new file from your game's default map, created when you save)   " + path;
                        }
                        else problem = "Couldn't find Fallout4.exe to copy the default control map from. Put the file inside your Fallout 4 folder (for F4SE: Data\\F4SE\\CustomControlMap.txt).";
                    }
                    else if (game.Keys == KeySet.DirectInput && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        string src;
                        string def = GamePaths.SkyrimDefaultMap(path, game, out src);
                        if (def != null)
                        {
                            cmDoc = TextLines.FromBytes(Encoding.GetEncoding(28591).GetBytes(def));
                            rows = ControlMap.Load(cmDoc, name, game);
                            cmNew = true;
                            mode = "text control map (new file from " + src + ", created when you save)   " + path;
                        }
                    }

                    // Hints: where fixed keys live, and F4SE for Fallout 4's text map
                    if (cmBinary != null)
                        hint = "Console and other fixed keys aren't in the in-game key map: switch Map to " + game.TextMapLabel.Split('(')[0].Trim() + ".";
                    else if (cmDoc != null && game.Keys == KeySet.VirtualKey)
                    {
                        string root = GamePaths.GameRootFrom(path, game);
                        if (root != null && !File.Exists(Path.Combine(root, "f4se_loader.exe")))
                            hint = "F4SE isn't installed in " + root + ". Fallout 4 only reads this file when started with F4SE.";
                    }
                }
            }
            catch (Exception ex)
            {
                problem = "Could not read the file:\n\n" + ex.Message;
                rows = new List<BindRow>();
            }

            bool hasData = rows.Count > 0;
            missingPanel.Visible = !hasData;
            grid.Visible = hasData;
            if (!hasData)
                lblMissing.Text = problem ?? ((IsCm || Directory.Exists(txtFolder.Text) ? "" : "This folder does not exist.\n\n") + game.MissingFileHelp);
            btnSave.Enabled = hasData; btnRevert.Enabled = hasData; btnUnbind.Enabled = hasData;
            btnDefaults.Enabled = hasData;
            btnDefaults.Visible = true;
            lblFile.Text = hasData ? "Editing: " + mode : "";
            btnSavePreset.Enabled = hasData;
            syncingRo = true;
            chkReadOnly.Checked = File.Exists(loadedPath ?? "") && (File.GetAttributes(loadedPath) & FileAttributes.ReadOnly) != 0;
            syncingRo = false;

            FillGrid();
        }

        List<string> KeyNames()
        {
            List<string> names = new List<string> { Codes.None };
            if (game.Keys == KeySet.VirtualKey)
            {
                List<int> codes = new List<int>(Codes.VkByCode.Keys);
                codes.Sort();
                foreach (int c in codes) names.Add(Codes.VkByCode[c]);
            }
            else foreach (KeyValuePair<int, string> k in Codes.Keys) names.Add(k.Value);
            return names;
        }

        const string NotUsed = "Not available";
        const string KeyboardOnly = "Keyboard only", MouseOnly = "Mouse only";
        static bool IsInfo(string v) { return v == NotUsed || v == KeyboardOnly || v == MouseOnly; }

        /// Text shown in a device cell the game doesn't accept for this action.
        static string InfoFor(BindRow r, bool keyboardColumn)
        {
            if (keyboardColumn) return r.MouseEditable ? MouseOnly : NotUsed;
            return r.KbEditable ? KeyboardOnly : NotUsed;
        }

        void FillGrid()
        {
            loading = true;
            try
            {
                grid.Rows.Clear();
                colKb.Items.Clear(); colMouse.Items.Clear();
                foreach (string n in KeyNames()) colKb.Items.Add(n);
                colMouse.Items.Add(Codes.None);
                List<int> mc = new List<int>(game.Mouse.Keys); mc.Sort();
                foreach (int m in mc) colMouse.Items.Add(game.Mouse[m]);
                foreach (BindRow r in rows) { EnsureItem(colKb, KbName(r)); EnsureItem(colMouse, MouseName(r)); }

                List<string> cats = new List<string> { "All" };
                foreach (string c in new[] { "Movement", "Combat", "Actions", "Menus", "Hotkeys", "System" })
                    foreach (BindRow r in rows) if (r.Context == c && !cats.Contains(c)) cats.Add(c);
                foreach (BindRow r in rows)
                {
                    int i = grid.Rows.Add();
                    DataGridViewRow gr = grid.Rows[i];
                    gr.Tag = r;
                    gr.Cells["Action"].Value = r.Label;
                    string help = ActionHelp.Get(game, r);
                    gr.Cells["Action"].ToolTipText = (help != null ? help + "\n\n" : "") + r.Event + "  -  " + r.Context + "  -  from " + r.Source;
                    gr.Cells["Event"].Value = r.Event;
                    gr.Cells["Category"].Value = r.Context;
                    gr.Cells["Pad"].Value = r.Pad;
                    // A device the game doesn't allow for this action gets a plain "-" cell: no dropdown, nothing to pick.
                    if (!r.KbEditable) gr.Cells["Keyboard"] = NotUsedCell(InfoFor(r, true), "The game only accepts a mouse button for this action, not a keyboard key.");
                    if (!r.MouseEditable) gr.Cells["Mouse"] = NotUsedCell(InfoFor(r, false), "The game only accepts a keyboard key for this action, not a mouse button.");
                    if (!r.KbEditable && !r.MouseEditable) gr.Cells["Bind"] = NotUsedCell("", null);
                    gr.Cells["Keyboard"].Value = KbName(r);
                    gr.Cells["Mouse"].Value = MouseName(r);
                    if (!r.KbEditable) gr.Cells["Keyboard"].ReadOnly = true;
                    if (!r.MouseEditable) gr.Cells["Mouse"].ReadOnly = true;
                    if (!r.KbEditable && !r.MouseEditable) gr.Cells["Bind"].ReadOnly = true;
                    if (!cats.Contains(r.Context)) cats.Add(r.Context);
                }
                lstCats.Items.Clear();
                foreach (string c in cats) lstCats.Items.Add(c);
                lstCats.SelectedIndex = 0;
            }
            finally { loading = false; }
            RefreshAll();
        }

        static void EnsureItem(DataGridViewComboBoxColumn col, string name)
        {
            if (!string.IsNullOrEmpty(name) && !IsInfo(name) && !col.Items.Contains(name)) col.Items.Add(name);
        }

        static DataGridViewTextBoxCell NotUsedCell(string text, string tip)
        {
            DataGridViewTextBoxCell c = new DataGridViewTextBoxCell { Value = text, ToolTipText = tip ?? "" };
            c.Style.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            c.Style.ForeColor = Theme.Border;
            c.Style.BackColor = Color.FromArgb(26, 26, 26);              // visibly greyed out
            c.Style.SelectionForeColor = Theme.Border;
            c.Style.SelectionBackColor = Color.FromArgb(36, 40, 46);
            c.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            return c;
        }

        readonly Dictionary<string, string> displayToField = new Dictionary<string, string>();

        string KbName(BindRow r)
        {
            if (!r.KbEditable) return InfoFor(r, true);
            string d = Codes.FieldToDisplay(r.Kb, Codes.Table(game.Keys), Codes.Prefix(game.Keys)) ?? r.Kb;
            if (Codes.ParseSingle(r.Kb) < 0) displayToField[d] = r.Kb;
            return d;
        }
        string MouseName(BindRow r) { return !r.MouseEditable ? InfoFor(r, false) : Codes.FieldToName(r.Mouse, game.Mouse, "Mouse code"); }

        // ------------------------------------------------------------------ editing

        void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (loading || e.RowIndex < 0) return;
            DataGridViewRow gr = grid.Rows[e.RowIndex];
            BindRow r = (BindRow)gr.Tag;
            string col = grid.Columns[e.ColumnIndex].Name;
            string v = gr.Cells[e.ColumnIndex].Value as string;
            if (IsInfo(v)) { RefreshAll(); return; }
            string combo;
            if (col == "Keyboard" && r.KbEditable) SetKey(r, v != null && displayToField.TryGetValue(v, out combo) ? combo : Codes.NameToField(v, Codes.Table(game.Keys), Codes.Prefix(game.Keys)));
            else if (col == "Mouse" && r.MouseEditable) r.Mouse = Codes.NameToField(v, game.Mouse, "Mouse code");
            else return;
            RefreshAll();
        }

        void OnCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Bind") return;
            BindRow r = (BindRow)grid.Rows[e.RowIndex].Tag;
            if (!r.KbEditable && !r.MouseEditable) return;
            using (CaptureDialog d = new CaptureDialog(r.Label, game, r.KbEditable, r.MouseEditable))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                if (d.KeyCode >= 0 && r.KbEditable) SetKey(r, Codes.Field(d.KeyCode));
                else if (d.MouseCode >= 0 && r.MouseEditable) r.Mouse = Codes.Field(d.MouseCode);
            }
            RefreshAll();
        }

        /// Events that appear in several contexts but are one key to the player (the console opens and closes on the same key).
        static readonly HashSet<string> Linked = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Console" };

        void SetKey(BindRow r, string field)
        {
            if (!r.KbEditable) return;
            List<string> old = Codes.Alternatives(r.OrigKb);
            r.Kb = field;
            if (!r.IsMain || !Linked.Contains(r.Event)) return;
            foreach (BindRow o in rows)
            {
                if (o == r || !o.KbEditable || !string.Equals(o.Event, r.Event, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (string k in Codes.Alternatives(o.OrigKb)) if (old.Contains(k)) { o.Kb = field; break; }
            }
        }

        void UnbindSelected()
        {
            foreach (DataGridViewRow gr in grid.SelectedRows)
            {
                BindRow r = (BindRow)gr.Tag;
                if (r.KbEditable) r.Kb = "0xff";
                if (r.MouseEditable) r.Mouse = "0xff";
            }
            RefreshAll();
        }

        void VanillaDefaults()
        {
            if (MessageBox.Show(this, "Set the keyboard and mouse bindings back to the vanilla " + game.Name + " defaults?" +
                (IsCm && cmDoc != null ? "\n\n(Main gameplay controls only. Menu controls are left as they are.)" : "") +
                "\n\nNothing is written until you click Save.",
                "Vanilla defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (BindRow r in rows)
            {
                if (!IsCm)
                {
                    int kb, m, p;
                    NvIni.Parse(r.NvDefault, out kb, out m, out p);
                    if (kb < 0) continue;
                    r.Kb = Codes.Field(kb); r.Mouse = Codes.Field(m);
                }
                else
                {
                    if (r.KbEditable && r.DefKb != null) r.Kb = r.DefKb;
                    if (r.MouseEditable && r.DefMouse != null) r.Mouse = r.DefMouse;
                }
            }
            RefreshAll();
        }

        static bool Bound(string field, bool mouse)
        {
            int c = Codes.ParseSingle(field);
            if (c < 0 || c == 0xFF) return false;
            if (mouse && (c == 0xA || c == 0xA00)) return false;     // "mouse move" is shared by design
            return true;
        }

        /// Push model -> grid cells, highlight changes and conflicts, update status.
        void RefreshAll()
        {
            loading = true;
            Dictionary<string, List<BindRow>> kbUse = new Dictionary<string, List<BindRow>>();
            Dictionary<string, List<BindRow>> mUse = new Dictionary<string, List<BindRow>>();
            foreach (BindRow r in rows)
            {
                if (r.KbEditable)
                    foreach (string alt in Codes.Alternatives(r.Kb))
                        if (alt.Contains("+") || Bound(alt, false)) Add(kbUse, r.Context + "|" + alt, r);
                if (r.MouseEditable && Bound(r.Mouse, true)) Add(mUse, r.Context + "|" + r.Mouse.ToLowerInvariant(), r);
            }
            // Movement/Combat/Actions/Menus are all "main gameplay" in the control map games: check them together.
            if (IsCm) { kbUse = MergeMain(kbUse); mUse = MergeMain(mUse); }

            int changed = 0;
            List<string> conflicts = new List<string>();
            HashSet<BindRow> kbClash = new HashSet<BindRow>(), mClash = new HashSet<BindRow>();
            foreach (KeyValuePair<string, List<BindRow>> kv in kbUse)
                if (Distinct(kv.Value).Count > 1)
                {
                    string field = kv.Key.Substring(kv.Key.IndexOf('|') + 1);
                    conflicts.Add((Codes.FieldToDisplay(field, Codes.Table(game.Keys), Codes.Prefix(game.Keys)) ?? field) + ": " + Names(Distinct(kv.Value)));
                    kbClash.UnionWith(kv.Value);
                }
            foreach (KeyValuePair<string, List<BindRow>> kv in mUse) if (kv.Value.Count > 1) { conflicts.Add(MouseName(kv.Value[0]) + ": " + Names(kv.Value)); mClash.UnionWith(kv.Value); }

            foreach (DataGridViewRow gr in grid.Rows)
            {
                BindRow r = (BindRow)gr.Tag;
                string kn = KbName(r), mn = MouseName(r);
                EnsureItem(colKb, kn); EnsureItem(colMouse, mn);
                if (!Equals(gr.Cells["Keyboard"].Value, kn)) gr.Cells["Keyboard"].Value = kn;
                if (!Equals(gr.Cells["Mouse"].Value, mn)) gr.Cells["Mouse"].Value = mn;
                gr.Cells["Value"].Value = !IsCm ? r.NvValue() : (r.Kb ?? "-") + "  " + (r.Mouse ?? "-");

                bool ch = r.Changed;
                if (ch) changed++;
                gr.Cells["Value"].Style.ForeColor = ch ? Theme.Success : Theme.Muted;
                gr.Cells["Action"].Style.ForeColor = ch ? Theme.Success : Theme.Text;
                if (r.KbEditable) gr.Cells["Keyboard"].Style.BackColor = kbClash.Contains(r) ? Theme.DangerBg : Color.Empty;
                if (r.MouseEditable) gr.Cells["Mouse"].Style.BackColor = mClash.Contains(r) ? Theme.DangerBg : Color.Empty;
            }
            loading = false;

            if (rows.Count == 0) { lblStatus.Text = ""; btnSave.Text = "Save"; return; }
            string msg = changed == 0 ? (cmNew ? "This file doesn't exist yet. Click Save to create it." : "No unsaved changes.")
                                      : changed + " unsaved change" + (changed == 1 ? "" : "s") + " (shown in green).";
            if (hint != null) msg += "   " + hint;
            if (conflicts.Count > 0)
            {
                msg += "   Same key used twice -> " + string.Join("   |   ", conflicts.ToArray());
                lblStatus.ForeColor = Theme.Warning;
            }
            else lblStatus.ForeColor = changed > 0 || cmNew ? Theme.Success : Theme.Muted;
            lblStatus.Text = msg;
            btnSave.Text = changed > 0 ? "Save (" + changed + ")" : cmNew ? "Create file" : "Save";
        }

        static readonly HashSet<string> MainCats = new HashSet<string> { "Movement", "Combat", "Actions", "Menus", "Hotkeys", "System", "Main Gameplay", "Other" };

        static Dictionary<string, List<BindRow>> MergeMain(Dictionary<string, List<BindRow>> d)
        {
            Dictionary<string, List<BindRow>> o = new Dictionary<string, List<BindRow>>();
            foreach (KeyValuePair<string, List<BindRow>> kv in d)
            {
                int bar = kv.Key.IndexOf('|');
                string ctx = kv.Key.Substring(0, bar), key = kv.Key.Substring(bar + 1);
                string k = (MainCats.Contains(ctx) ? "main" : ctx) + "|" + key;
                foreach (BindRow r in kv.Value) Add(o, k, r);
            }
            return o;
        }

        static List<BindRow> Distinct(List<BindRow> l)
        {
            List<BindRow> o = new List<BindRow>();
            foreach (BindRow r in l) if (!o.Contains(r)) o.Add(r);
            return o;
        }

        static void Add(Dictionary<string, List<BindRow>> d, string k, BindRow r)
        {
            List<BindRow> l;
            if (!d.TryGetValue(k, out l)) { l = new List<BindRow>(); d[k] = l; }
            l.Add(r);
        }

        static string Names(List<BindRow> l)
        {
            List<string> n = new List<string>();
            foreach (BindRow r in l) n.Add(r.Label);
            return string.Join(", ", n.ToArray());
        }

        /// Names of the keys / mouse buttons a row is bound to ("Left Shift", "F5", "Wheel Up"...).
        List<string> BoundNames(BindRow r)
        {
            List<string> n = new List<string>();
            if (r.KbEditable)
                foreach (string alt in Codes.Alternatives(r.Kb))
                    foreach (string k in alt.Split('+'))
                        n.Add(Codes.FieldToName(k, Codes.Table(game.Keys), Codes.Prefix(game.Keys)));
            if (r.MouseEditable && Codes.ParseSingle(r.Mouse) >= 0 && Codes.ParseSingle(r.Mouse) != 0xFF)
                n.Add(Codes.FieldToName(r.Mouse, game.Mouse, "Mouse code"));
            return n;
        }

        /// Text search: one character or an exact key name ("E", "F5", "space") finds actions bound to that key;
        /// longer text also matches action names and parts of key names ("left" -> Strafe Left, Left Shift...).
        bool SearchMatch(BindRow r, string q)
        {
            List<string> keys = BoundNames(r);
            foreach (string k in keys)
            {
                string kl = k.ToLowerInvariant();
                if (kl == q || kl.StartsWith(q + " (")) return true;      // "e", "; (semicolon)" via ";"
            }
            if (q.Length < 2) return false;
            if ((r.Label + " " + r.Event).ToLowerInvariant().Contains(q)) return true;
            foreach (string k in keys) if (k.ToLowerInvariant().Contains(q)) return true;
            return false;
        }

        /// "Find by key": press a key or click a mouse button, list the actions using it.
        void FindByKey()
        {
            if (findField != null) { ClearFind(); return; }
            if (rows.Count == 0) return;
            using (CaptureDialog d = new CaptureDialog("Find which actions use it", game, true, true))
            {
                if (d.ShowDialog(this) != DialogResult.OK) return;
                if (d.KeyCode >= 0) { findField = Codes.Field(d.KeyCode); findMouse = false; }
                else if (d.MouseCode >= 0) { findField = Codes.Field(d.MouseCode); findMouse = true; }
                else return;
            }
            findName = findMouse ? Codes.FieldToName(findField, game.Mouse, "Mouse code")
                                 : Codes.FieldToName(findField, Codes.Table(game.Keys), Codes.Prefix(game.Keys));
            btnFindKey.BackColor = Theme.Primary;
            lstCats.SelectedIndex = 0;
            ApplyFilter();
        }

        void ClearFind()
        {
            findField = null;
            btnFindKey.Text = "Find by key...";
            btnFindKey.BackColor = Theme.Surface2;
            ApplyFilter();
        }

        bool FindMatch(BindRow r)
        {
            if (findMouse) return r.MouseEditable && Codes.ParseSingle(r.Mouse) == Codes.ParseSingle(findField);
            if (!r.KbEditable) return false;
            foreach (string alt in Codes.Alternatives(r.Kb))
                foreach (string k in alt.Split('+')) if (k == findField) return true;
            return false;
        }

        void ApplyFilter()
        {
            if (grid.Rows.Count == 0) return;
            string cat = lstCats.SelectedItem as string ?? "All";
            string q = txtSearch.ForeColor == Theme.Muted ? "" : txtSearch.Text.Trim().ToLowerInvariant();
            grid.CurrentCell = null;
            int shown = 0;
            foreach (DataGridViewRow gr in grid.Rows)
            {
                BindRow r = (BindRow)gr.Tag;
                bool show = cat == "All" || r.Context == cat;
                if (show && q.Length > 0) show = SearchMatch(r, q);
                if (show && findField != null) show = FindMatch(r);
                gr.Visible = show;
                if (show) shown++;
            }
            grid.Columns["Event"].Visible = !IsCm;
            grid.Columns["Category"].Visible = q.Length > 0 || findField != null;   // show where each result lives
            if (findField != null)
                btnFindKey.Text = findName + (shown == 0 ? ": not used" : ": " + shown + " action" + (shown == 1 ? "" : "s")) + "  (clear)";
        }

        int ChangedCount() { int n = 0; foreach (BindRow r in rows) if (r.Changed) n++; return n; }

        bool ConfirmDiscard(string what)
        {
            int n = ChangedCount();
            if (n == 0) return true;
            return MessageBox.Show(this, "You have " + n + " unsaved change" + (n == 1 ? "" : "s") + ". Discard them and " + what + "?",
                "BethKEY", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        // ------------------------------------------------------------------ saving

        /// The in-game ControlMap_Custom.txt is applied on top of the text map, for the actions the game lets you remap.
        /// If the user changed any of those in the text map, offer to switch the in-game map off.
        bool HandleOverride(List<string> report, string stamp)
        {
            if (cmDoc == null || !IsCm) return true;
            string custom = Path.Combine(game.DefaultFolder(), "ControlMap_Custom.txt");
            if (!File.Exists(custom) || string.Equals(Path.GetFullPath(custom), Path.GetFullPath(loadedPath), StringComparison.OrdinalIgnoreCase)) return true;
            List<string> hit = new List<string>();
            foreach (BindRow r in rows)
            {
                if (!r.IsMain || !r.Changed) continue;
                foreach (CmEvent e in game.MainEvents)
                    if (e.Name == r.Event && ((e.RemapKb && r.Kb != r.OrigKb) || (e.RemapMouse && r.Mouse != r.OrigMouse))) { hit.Add(r.Label); break; }
            }
            if (hit.Count == 0) return true;
            DialogResult a = MessageBox.Show(this, "Your in-game key map also sets these actions, and the game applies it on top of this file:\n\n  "
                + string.Join(", ", hit.ToArray()) + "\n\n" + custom
                + "\n\nTurn the in-game key map off so your changes here are used? It's renamed to ControlMap_Custom.txt.bethkey-off, so you can rename it back any time.\n\n"
                + "Yes = turn it off   No = save anyway   Cancel = don't save",
                "BethKEY", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (a == DialogResult.Cancel) return false;
            if (a == DialogResult.Yes)
            {
                Backup.Make(custom, stamp);
                string off = custom + ".bethkey-off";
                if (File.Exists(off)) off = custom + ".bethkey-off-" + stamp.Replace(' ', '_');
                File.Move(custom, off);
                report.Add("Turned off the in-game key map (renamed to " + Path.GetFileName(off) + ")");
            }
            return true;
        }

        /// The file(s) BethKEY writes for the current game: FalloutPrefs.ini (+ Fallout.ini if ticked), or the control map.
        List<string> TargetFiles()
        {
            List<string> t = new List<string>();
            if (rows.Count == 0 || string.IsNullOrEmpty(loadedPath)) return t;
            if (!IsCm)
            {
                t.Add(PrimaryPath());
                if (chkSecondary.Checked && chkSecondary.Enabled && File.Exists(SecondaryPath())) t.Add(SecondaryPath());
            }
            else t.Add(loadedPath);
            return t;
        }

        /// Sets or clears the read-only flag on the target files to match the checkbox. Returns a line for reports.
        string ApplyReadOnly(bool announce)
        {
            List<string> done = new List<string>();
            foreach (string f in TargetFiles())
            {
                if (!File.Exists(f)) continue;
                try
                {
                    FileAttributes a = File.GetAttributes(f);
                    FileAttributes want = chkReadOnly.Checked ? a | FileAttributes.ReadOnly : a & ~FileAttributes.ReadOnly;
                    if (want != a) File.SetAttributes(f, want);
                    done.Add(Path.GetFileName(f));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Couldn't change the read-only setting of " + f + ":\n\n" + ex.Message, "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            if (done.Count == 0)
            {
                if (announce) lblStatus.Text = "The file doesn't exist yet. It will be set " + (chkReadOnly.Checked ? "read-only" : "writable") + " when you save.";
                return null;
            }
            string line = string.Join(", ", done.ToArray()) + (chkReadOnly.Checked ? " set read-only." : " left writable (not read-only).");
            if (announce) { lblStatus.ForeColor = Theme.Success; lblStatus.Text = line; }
            return line;
        }

        // ------------------------------------------------------------------ presets

        static string PresetRoot(GameDef g) { return Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets"), g.ShortName); }

        static string SafeName(string n)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return n.Trim().TrimEnd('.');
        }

        /// Saves what's in the editor (including unsaved changes) as a preset.
        void SavePreset()
        {
            grid.EndEdit();
            if (rows.Count == 0) return;
            string name = SafeName(InputDialog.Ask(this, "Save preset", "Name for this " + game.Name + " key preset:", ""));
            if (name.Length == 0) return;
            string dir = Path.Combine(PresetRoot(game), name);
            if (Directory.Exists(dir) && MessageBox.Show(this, "A preset called \"" + name + "\" already exists. Replace it?", "BethKEY",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);
                string kind, file;
                if (!IsCm)
                {
                    kind = "ini"; file = "Controls.ini";
                    StringBuilder sb = new StringBuilder("[Controls]\r\n");
                    foreach (BindRow r in rows) sb.Append(r.Event).Append('=').Append(r.NvValue()).Append("\r\n");
                    File.WriteAllText(Path.Combine(dir, file), sb.ToString());
                }
                else if (cmBinary != null)
                {
                    kind = "binary"; file = "ControlMap_Custom.txt";
                    CmBinary copy = CmBinary.Parse(cmBinary.Build());
                    copy.Apply(rows);
                    File.WriteAllBytes(Path.Combine(dir, file), copy.Build());
                }
                else
                {
                    kind = "text"; file = Path.GetFileName(loadedPath);
                    ControlMap.Save(Path.Combine(dir, file), cmDoc, rows);
                    File.SetAttributes(Path.Combine(dir, file), FileAttributes.Normal);
                }
                File.WriteAllText(Path.Combine(dir, "preset.ini"),
                    "[Preset]\r\nGame=" + game.Name + "\r\nKind=" + kind + "\r\nFile=" + file + "\r\nMap=" + MapIndex + "\r\nSaved=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "\r\n");
                lblStatus.ForeColor = Theme.Success;
                lblStatus.Text = "Saved preset \"" + name + "\" in " + dir;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Couldn't save the preset:\n\n" + ex.Message, "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static Dictionary<string, string> ReadPresetInfo(string dir)
        {
            Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string f = Path.Combine(dir, "preset.ini");
            if (!File.Exists(f)) return d;
            foreach (string line in File.ReadAllLines(f))
            {
                int i = line.IndexOf('=');
                if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            return d;
        }

        /// Copies a preset to the right place for this game (backing up what's there), then reloads.
        void LoadPreset()
        {
            string root = PresetRoot(game);
            List<string> names = new List<string>();
            if (Directory.Exists(root))
                foreach (string d in Directory.GetDirectories(root))
                    if (File.Exists(Path.Combine(d, "preset.ini"))) names.Add(Path.GetFileName(d));
            names.Sort(StringComparer.OrdinalIgnoreCase);
            string pick = PresetDialog.Pick(this, game.Name, names, root);
            if (pick == null) return;
            if (!ConfirmDiscard("load a preset")) return;

            string dir = Path.Combine(root, pick);
            Dictionary<string, string> info = ReadPresetInfo(dir);
            string kind, file, mapS;
            info.TryGetValue("Kind", out kind); info.TryGetValue("File", out file); info.TryGetValue("Map", out mapS);
            string src = file == null ? null : Path.Combine(dir, file);
            if (src == null || !File.Exists(src)) { MessageBox.Show(this, "This preset is missing its file.", "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            if (Process.GetProcessesByName(game.ProcessName).Length > 0 &&
                MessageBox.Show(this, game.Name + " is running. It may overwrite your bindings when it closes.\n\nLoad the preset anyway?", "BethKEY",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            List<string> report = new List<string>();
            try
            {
                if (kind == "ini")
                {
                    if (IsCm) throw new InvalidDataException("This preset is for a different game.");
                    // Only the key lines go into the real INI; every other setting stays as it is.
                    Dictionary<string, string> vals = NvIni.ReadSection(TextLines.Load(src), "Controls");
                    foreach (BindRow r in rows)
                    {
                        string v; int kb, m, p;
                        if (!vals.TryGetValue(r.Event, out v)) continue;
                        NvIni.Parse(v, out kb, out m, out p);
                        if (kb < 0) continue;
                        r.Kb = Codes.Field(kb); r.Mouse = Codes.Field(m); r.NvPad = p;
                    }
                    List<string> targets = new List<string> { PrimaryPath() };
                    if (chkSecondary.Checked && chkSecondary.Enabled && File.Exists(SecondaryPath())) targets.Add(SecondaryPath());
                    foreach (string t in targets)
                    {
                        Backup.Make(t, stamp);
                        NvIni.Save(t, rows);
                        report.Add("Wrote the preset's keys into " + t);
                    }
                }
                else
                {
                    if (!IsCm) throw new InvalidDataException("This preset is for a different game.");
                    int map = mapS == "1" ? 1 : 0;
                    byte[] bytes = File.ReadAllBytes(src);
                    if (kind == "binary")
                    {
                        CmBinary m = CmBinary.Parse(bytes); string why;
                        if (m == null || !m.Matches(game.MainEvents, out why)) throw new InvalidDataException("The preset's key map doesn't match this game's actions.");
                    }
                    if (map != MapIndex)
                    {
                        Settings.Set("sMap_" + game.ShortName, map.ToString());
                        cboMap.SelectedIndex = map;
                        SetMapPath();
                    }
                    string target = PrimaryPath();
                    if (string.IsNullOrEmpty(target)) target = StandardMapPath(game, map);
                    if (string.IsNullOrEmpty(target)) throw new InvalidDataException("Couldn't find where this map goes. Pick the file with Browse... first.");
                    Backup.Make(target, stamp);
                    FileUtil.WriteAllBytes(target, bytes);
                    txtFolder.Text = target;
                    report.Add("Copied the preset to " + target);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Couldn't load the preset:\n\n" + ex.Message, "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadFiles();
                return;
            }
            LoadFiles();
            string ro = ApplyReadOnly(false);
            if (ro != null) report.Add(ro);
            MessageBox.Show(this, "Loaded preset \"" + pick + "\".\n\n" + string.Join("\n", report.ToArray()) +
                "\n\nThe previous file was backed up to the \"BethKEY Backups\" folder next to it.", "Preset loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void Save()
        {
            grid.EndEdit();
            if (rows.Count == 0) return;
            if (Process.GetProcessesByName(game.ProcessName).Length > 0 &&
                MessageBox.Show(this, game.Name + " is running. It may overwrite your bindings when it closes.\n\nSave anyway?", "BethKEY",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            List<string> report = new List<string>();
            try
            {
                if (!IsCm)
                {
                    List<string> targets = new List<string> { PrimaryPath() };
                    if (chkSecondary.Checked && chkSecondary.Enabled && File.Exists(SecondaryPath())) targets.Add(SecondaryPath());
                    foreach (string t in targets)
                    {
                        string bk = Backup.Make(t, stamp);
                        NvIni.Save(t, rows);
                        report.Add((bk == null ? "Created " : "Updated ") + Path.GetFileName(t));
                    }
                }
                else
                {
                    if (!HandleOverride(report, stamp)) return;
                    bool existed = File.Exists(loadedPath);
                    Backup.Make(loadedPath, stamp);
                    if (cmBinary != null)
                    {
                        cmBinary.Apply(rows);
                        FileUtil.WriteAllBytes(loadedPath, cmBinary.Build());
                    }
                    else ControlMap.Save(loadedPath, cmDoc, rows);
                    report.Insert(0, (existed ? "Updated " : "Created ") + loadedPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not save:\n\n" + ex.Message, "BethKEY", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            foreach (BindRow r in rows) r.Commit();
            cmNew = false;
            string ro = ApplyReadOnly(false);
            if (ro != null) report.Add(ro);
            RefreshAll();
            Settings.Set(SettingKey, txtFolder.Text.Trim());
            MessageBox.Show(this, string.Join("\n", report.ToArray()) +
                "\n\nOnly the key binding values were changed. A backup of the old file is in the \"BethKEY Backups\" folder next to it.",
                "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
