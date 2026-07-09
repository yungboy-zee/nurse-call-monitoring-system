using System;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace NurseCallSystem
{
    public partial class Form1 : Form
    {
        private SerialPort _serialPort;
        private System.Windows.Forms.Timer _clockTimer;
        private System.Windows.Forms.Timer _bannerTimer;
        private DateTime  _startTime;
        private int       _alertCounter    = 0;
        private bool      _bannerFlash     = false;
        private int       _selectedRowIndex = -1;

        // ── Colours ────────────────────────────────────────────────
        private readonly Color C_BG       = Color.FromArgb(13,  17,  23);
        private readonly Color C_SURFACE  = Color.FromArgb(22,  27,  34);
        private readonly Color C_BORDER   = Color.FromArgb(33,  38,  45);
        private readonly Color C_TEXT     = Color.FromArgb(201, 209, 217);
        private readonly Color C_MUTED    = Color.FromArgb(139, 148, 158);
        private readonly Color C_GREEN    = Color.FromArgb(63,  185, 80);
        private readonly Color C_GREEN_BG = Color.FromArgb(13,  43,  26);
        private readonly Color C_RED      = Color.FromArgb(248, 81,  73);
        private readonly Color C_RED_BG   = Color.FromArgb(43,  13,  13);
        private readonly Color C_AMBER    = Color.FromArgb(210, 153, 34);
        private readonly Color C_AMBER_BG = Color.FromArgb(43,  29,  13);
        private readonly Color C_BLUE     = Color.FromArgb(88,  166, 255);

        // ── Controls ───────────────────────────────────────────────
        private TableLayoutPanel mainLayout;
        private Panel        pnlTitleBar;
        private Panel        pnlBanner;
        private Panel        pnlToolbar;
        private Panel        pnlBody;
        private Panel        pnlFooter;
        private Panel        pnlSidebar;
        private Panel        pnlStatusLed;
        private Label        lblAppName;
        private Label        lblBannerText;
        private Label        lblBannerSub;
        private Label        lblClock;
        private Label        lblDate;
        private Label        lblActiveCount;
        private Label        lblActiveLabel;
        private ComboBox     cmbPort;
        private Button       btnConnect;
        private Button       btnDisconnect;
        private Button       btnResolve;
        private Button       btnAcknowledge;
        private Button       btnClearResolved;
        private Button       btnExport;
        private TextBox      txtSearch;
        private DataGridView dgvAlerts;
        private Label        lblStatTotal, lblStatActive, lblStatPending, lblStatResolved, lblStatAvg;
        private Label        lblFooterPort, lblFooterUptime, lblFooterTotal, lblFooterStatus;

        public Form1()
        {
            _startTime = DateTime.Now;
            InitializeComponent();
            BuildUI();
            SetupTimers();
            PopulateComPorts();
        }

        // ══════════════════════════════════════════════════════════
        //  MAIN UI BUILDER
        // ══════════════════════════════════════════════════════════
        private void BuildUI()
        {
            this.Text          = "Nurse Call System — Ward Monitor";
            this.Size          = new Size(1100, 700);
            this.MinimumSize   = new Size(900, 600);
            this.BackColor     = C_BG;
            this.ForeColor     = C_TEXT;
            this.Font          = new Font("Courier New", 9f);
            this.StartPosition = FormStartPosition.CenterScreen;

            // ── TableLayoutPanel: 5 rows, fixed heights except body ──
            // Row 0 = TitleBar  (30px)
            // Row 1 = Banner    (68px)
            // Row 2 = Toolbar   (40px)
            // Row 3 = Body      (fills remaining space — 100%)
            // Row 4 = Footer    (26px)
            mainLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 5,
                BackColor   = C_BG,
                Padding     = new Padding(0),
                Margin      = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));   // TitleBar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));   // Banner
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));   // Toolbar
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent,  100f));  // Body (fills all remaining)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));   // Footer

            this.Controls.Add(mainLayout);

            BuildTitleBar();
            BuildBanner();
            BuildToolbar();
            BuildBody();
            BuildFooter();
        }

        // ── Title bar ─────────────────────────────────────────────
        private void BuildTitleBar()
        {
            pnlTitleBar = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE, Margin = new Padding(0) };
            pnlTitleBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, 29, pnlTitleBar.Width, 29);

            lblAppName = new Label
            {
                Text      = "■  ARDUINO NURSE CALL SYSTEM — WARD MONITOR v1.0",
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 8f),
                Location  = new Point(10, 8),
                AutoSize  = true,
            };
            pnlTitleBar.Controls.Add(lblAppName);
            mainLayout.Controls.Add(pnlTitleBar, 0, 0);
        }

        // ── Banner ────────────────────────────────────────────────
        private void BuildBanner()
        {
            pnlBanner = new Panel { Dock = DockStyle.Fill, BackColor = C_GREEN_BG, Margin = new Padding(0) };
            pnlBanner.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_GREEN, 2), 0, 67, pnlBanner.Width, 67);

            pnlStatusLed = new Panel { Size = new Size(12, 12), Location = new Point(14, 28), BackColor = C_GREEN };
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(0, 0, 12, 12);
            pnlStatusLed.Region = new Region(path);
            pnlBanner.Controls.Add(pnlStatusLed);

            lblBannerText = new Label
            {
                Text      = "NO EMERGENCIES YET",
                ForeColor = C_GREEN,
                Font      = new Font("Courier New", 18f, FontStyle.Bold),
                Location  = new Point(36, 8),
                AutoSize  = true,
            };
            pnlBanner.Controls.Add(lblBannerText);

            lblBannerSub = new Label
            {
                Text      = "ALL WARDS CLEAR — SYSTEM MONITORING",
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 8f),
                Location  = new Point(38, 44),
                AutoSize  = true,
            };
            pnlBanner.Controls.Add(lblBannerSub);

            lblClock = new Label
            {
                Text      = "--:--:--",
                ForeColor = C_TEXT,
                Font      = new Font("Courier New", 16f),
                TextAlign = ContentAlignment.MiddleRight,
                Size      = new Size(140, 26),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            lblClock.Location = new Point(pnlBanner.Width - 240, 8);
            lblClock.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            pnlBanner.Controls.Add(lblClock);

            lblDate = new Label
            {
                Text      = "",
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 7f),
                TextAlign = ContentAlignment.MiddleRight,
                Size      = new Size(200, 14),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            lblDate.Location = new Point(pnlBanner.Width - 210, 42);
            lblDate.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            pnlBanner.Controls.Add(lblDate);

            lblActiveCount = new Label
            {
                Text      = "0",
                ForeColor = C_RED,
                Font      = new Font("Courier New", 22f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(50, 34),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            lblActiveCount.Location = new Point(pnlBanner.Width - 62, 6);
            lblActiveCount.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            pnlBanner.Controls.Add(lblActiveCount);

            lblActiveLabel = new Label
            {
                Text      = "ACTIVE",
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 7f),
                TextAlign = ContentAlignment.MiddleCenter,
                Size      = new Size(50, 12),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            lblActiveLabel.Location = new Point(pnlBanner.Width - 62, 44);
            lblActiveLabel.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            pnlBanner.Controls.Add(lblActiveLabel);

            mainLayout.Controls.Add(pnlBanner, 0, 1);
        }

        // ── Toolbar ───────────────────────────────────────────────
        private void BuildToolbar()
        {
            pnlToolbar = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE, Margin = new Padding(0) };
            pnlToolbar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, 39, pnlToolbar.Width, 39);

            int x = 8;

            cmbPort = new ComboBox
            {
                Location      = new Point(x, 8),
                Size          = new Size(130, 24),
                BackColor     = C_BG,
                ForeColor     = C_TEXT,
                FlatStyle     = FlatStyle.Flat,
                Font          = new Font("Courier New", 8f),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            pnlToolbar.Controls.Add(cmbPort);
            x += 138;

            btnConnect       = MakeToolBtn("CONNECT",        x, C_GREEN_BG, C_GREEN);  x += 88;
            btnDisconnect    = MakeToolBtn("DISCONNECT",     x, C_SURFACE,  C_MUTED);  x += 106;
            btnResolve       = MakeToolBtn("✓ RESOLVE",      x, C_SURFACE,  C_GREEN);  x += 98;
            btnAcknowledge   = MakeToolBtn("ACK",            x, C_SURFACE,  C_AMBER);  x += 56;
            btnClearResolved = MakeToolBtn("CLEAR RESOLVED", x, C_SURFACE,  C_RED);

            pnlToolbar.Controls.AddRange(new Control[]
                { btnConnect, btnDisconnect, btnResolve, btnAcknowledge, btnClearResolved });

            btnExport = new Button
            {
                Text      = "EXPORT CSV",
                Size      = new Size(90, 26),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = C_SURFACE,
                ForeColor = C_BLUE,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 8f),
                Cursor    = Cursors.Hand,
            };
            btnExport.FlatAppearance.BorderColor = C_BORDER;
            btnExport.Location = new Point(pnlToolbar.Width - 286, 7);
            btnExport.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnExport.Click   += BtnExport_Click;
            pnlToolbar.Controls.Add(btnExport);

            txtSearch = new TextBox
            {
                Text        = "SEARCH ROOM / WARD...",
                BackColor   = C_BG,
                ForeColor   = C_MUTED,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Courier New", 8f),
                Size        = new Size(180, 24),
                Anchor      = AnchorStyles.Top | AnchorStyles.Right,
            };
            txtSearch.Location = new Point(pnlToolbar.Width - 190, 8);
            txtSearch.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            txtSearch.GotFocus += (s, e) =>
            {
                if (txtSearch.Text == "SEARCH ROOM / WARD...") { txtSearch.Text = ""; txtSearch.ForeColor = C_TEXT; }
            };
            txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text)) { txtSearch.Text = "SEARCH ROOM / WARD..."; txtSearch.ForeColor = C_MUTED; }
            };
            txtSearch.TextChanged += (s, e) =>
            {
                string q = txtSearch.Text == "SEARCH ROOM / WARD..." ? "" : txtSearch.Text;
                FilterGrid(q);
            };
            pnlToolbar.Controls.Add(txtSearch);

            btnConnect.Click       += BtnConnect_Click;
            btnDisconnect.Click    += BtnDisconnect_Click;
            btnResolve.Click       += BtnResolve_Click;
            btnAcknowledge.Click   += BtnAcknowledge_Click;
            btnClearResolved.Click += BtnClearResolved_Click;

            mainLayout.Controls.Add(pnlToolbar, 0, 2);
        }

        // ── Body ──────────────────────────────────────────────────
        private void BuildBody()
        {
            pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = C_BG, Margin = new Padding(0) };

            // Sidebar — fixed width, added FIRST so Fill grid gets remaining space
            pnlSidebar = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 200,
                BackColor = C_SURFACE,
                Padding   = new Padding(12),
            };
            pnlSidebar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, 0, 0, pnlSidebar.Height);

            int sy = 12;
            AddSidebarTitle("STATISTICS", ref sy);
            lblStatTotal    = AddSidebarStat("Total alerts",  "0", C_TEXT,  ref sy);
            lblStatActive   = AddSidebarStat("Active",        "0", C_RED,   ref sy);
            lblStatPending  = AddSidebarStat("Pending",       "0", C_AMBER, ref sy);
            lblStatResolved = AddSidebarStat("Resolved",      "0", C_GREEN, ref sy);
            lblStatAvg      = AddSidebarStat("Avg response",  "—", C_BLUE,  ref sy);
            sy += 10;
            AddSidebarTitle("WARD STATUS", ref sy);
            BuildWardStatus(ref sy);

            // DataGridView — added SECOND so it fills remaining space
            dgvAlerts = new DataGridView
            {
                Dock                     = DockStyle.Fill,
                BackgroundColor          = C_SURFACE,
                GridColor                = C_BORDER,
                BorderStyle              = BorderStyle.None,
                RowHeadersVisible        = false,
                AllowUserToAddRows       = false,
                AllowUserToDeleteRows    = false,
                AllowUserToResizeRows    = false,
                ReadOnly                 = true,
                SelectionMode            = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect              = false,
                Font                     = new Font("Courier New", 8.5f),
                CellBorderStyle          = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                AutoSizeColumnsMode      = DataGridViewAutoSizeColumnsMode.Fill,
                ScrollBars               = ScrollBars.Vertical,
            };

            dgvAlerts.DefaultCellStyle.BackColor          = C_SURFACE;
            dgvAlerts.DefaultCellStyle.ForeColor          = C_TEXT;
            dgvAlerts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(31, 41, 55);
            dgvAlerts.DefaultCellStyle.SelectionForeColor = C_TEXT;
            dgvAlerts.DefaultCellStyle.Padding            = new Padding(6, 6, 6, 6);

            dgvAlerts.ColumnHeadersDefaultCellStyle.BackColor = C_BG;
            dgvAlerts.ColumnHeadersDefaultCellStyle.ForeColor = C_MUTED;
            dgvAlerts.ColumnHeadersDefaultCellStyle.Font      = new Font("Courier New", 7.5f);
            dgvAlerts.ColumnHeadersHeight                     = 30;
            dgvAlerts.RowTemplate.Height                      = 30;

            dgvAlerts.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(16, 21, 28);

            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColID",       HeaderText = "#",              FillWeight = 30  });
            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColRoom",     HeaderText = "ROOM / WARD",    FillWeight = 160 });
            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTime",     HeaderText = "TIME TRIGGERED", FillWeight = 100 });
            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColStatus",   HeaderText = "STATUS",         FillWeight = 80  });
            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColResolved", HeaderText = "RESOLVED AT",    FillWeight = 100 });
            dgvAlerts.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColResponse", HeaderText = "RESPONSE TIME",  FillWeight = 80  });

            dgvAlerts.CellFormatting   += DgvAlerts_CellFormatting;
            dgvAlerts.SelectionChanged += DgvAlerts_SelectionChanged;

            pnlBody.Controls.Add(pnlSidebar);  // RIGHT first
            pnlBody.Controls.Add(dgvAlerts);   // FILL second

            mainLayout.Controls.Add(pnlBody, 0, 3);
        }

        // ── Footer ────────────────────────────────────────────────
        private void BuildFooter()
        {
            pnlFooter = new Panel { Dock = DockStyle.Fill, BackColor = C_SURFACE, Margin = new Padding(0) };
            pnlFooter.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(C_BORDER), 0, 0, pnlFooter.Width, 0);

            lblFooterPort   = MakeFooterLabel("COM PORT: —",      8);
            lblFooterUptime = MakeFooterLabel("UPTIME: 00:00:00", 160);
            lblFooterTotal  = MakeFooterLabel("TOTAL ALERTS: 0",  340);
            lblFooterStatus = MakeFooterLabel("STATUS: IDLE",     520);

            pnlFooter.Controls.AddRange(new Control[]
                { lblFooterPort, lblFooterUptime, lblFooterTotal, lblFooterStatus });

            mainLayout.Controls.Add(pnlFooter, 0, 4);
        }

        // ══════════════════════════════════════════════════════════
        //  SERIAL PORT
        // ══════════════════════════════════════════════════════════
        private void PopulateComPorts()
        {
            cmbPort.Items.Clear();
            foreach (string p in SerialPort.GetPortNames())
                cmbPort.Items.Add(p);
            if (cmbPort.Items.Count > 0)
                cmbPort.SelectedIndex = 0;
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            if (cmbPort.SelectedItem == null) { ShowMsg("Please select a COM port."); return; }
            try
            {
                _serialPort = new SerialPort(cmbPort.SelectedItem.ToString(), 9600, Parity.None, 8, StopBits.One)
                {
                    NewLine  = "\n",
                    Encoding = System.Text.Encoding.ASCII,
                };
                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                lblFooterPort.Text      = $"COM PORT: {cmbPort.SelectedItem}  |  9600 BAUD";
                lblFooterStatus.Text    = "STATUS: CONNECTED";
                lblFooterStatus.ForeColor = C_GREEN;
                btnConnect.Enabled      = false;
                btnDisconnect.Enabled   = true;
            }
            catch (Exception ex) { ShowMsg($"Could not open port: {ex.Message}"); }
        }

        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            CloseSerial();
            lblFooterStatus.Text      = "STATUS: DISCONNECTED";
            lblFooterStatus.ForeColor = C_AMBER;
            btnConnect.Enabled        = true;
            btnDisconnect.Enabled     = false;
        }

        private void CloseSerial()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string raw = _serialPort.ReadLine().Trim();
                this.Invoke((MethodInvoker)delegate { ProcessIncoming(raw); });
            }
            catch { }
        }

        private void ProcessIncoming(string raw)
        {
            string[] parts = raw.Split(',');
            if (parts.Length >= 3 && parts[0].Trim().ToUpper() == "ALERT")
                AddAlert(parts[1].Trim(), parts[2].Trim());
        }

        // ══════════════════════════════════════════════════════════
        //  ALERT LOGIC
        // ══════════════════════════════════════════════════════════
        private void AddAlert(string room, string ward)
        {
            _alertCounter++;
            string id        = $"#{_alertCounter:D3}";
            string triggered = DateTime.Now.ToString("HH:mm:ss");

            dgvAlerts.Rows.Add(id, $"{ward}  /  {room}", triggered, "ACTIVE", "—", "—");

            int idx = dgvAlerts.Rows.Count - 1;
            dgvAlerts.Rows[idx].Tag = DateTime.Now;

            dgvAlerts.ClearSelection();
            dgvAlerts.Rows[idx].Selected                  = true;
            dgvAlerts.FirstDisplayedScrollingRowIndex     = idx;
            dgvAlerts.Invalidate();
            dgvAlerts.Update();

            UpdateStats();
            UpdateBanner();
            UpdateWardSidebar();
            lblFooterTotal.Text = $"TOTAL ALERTS: {_alertCounter}";
        }

        private void BtnResolve_Click(object sender, EventArgs e)
        {
            if (_selectedRowIndex < 0 || _selectedRowIndex >= dgvAlerts.Rows.Count) return;
            var row = dgvAlerts.Rows[_selectedRowIndex];
            if (row.Cells["ColStatus"].Value?.ToString() == "RESOLVED") return;
            DateTime triggered = row.Tag is DateTime t ? t : DateTime.Now;
            TimeSpan response  = DateTime.Now - triggered;
            row.Cells["ColStatus"].Value   = "RESOLVED";
            row.Cells["ColResolved"].Value = DateTime.Now.ToString("HH:mm:ss");
            row.Cells["ColResponse"].Value = $"{(int)response.TotalSeconds}s";
            UpdateStats(); UpdateBanner(); UpdateWardSidebar();
        }

        private void BtnAcknowledge_Click(object sender, EventArgs e)
        {
            if (_selectedRowIndex < 0 || _selectedRowIndex >= dgvAlerts.Rows.Count) return;
            var row = dgvAlerts.Rows[_selectedRowIndex];
            if (row.Cells["ColStatus"].Value?.ToString() == "ACTIVE")
                row.Cells["ColStatus"].Value = "PENDING";
            UpdateStats(); UpdateBanner(); UpdateWardSidebar();
        }

        private void BtnClearResolved_Click(object sender, EventArgs e)
        {
            for (int i = dgvAlerts.Rows.Count - 1; i >= 0; i--)
                if (dgvAlerts.Rows[i].Cells["ColStatus"].Value?.ToString() == "RESOLVED")
                    dgvAlerts.Rows.RemoveAt(i);
            _selectedRowIndex = -1;
            UpdateStats(); UpdateBanner(); UpdateWardSidebar();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
            {
                Filter   = "CSV files (*.csv)|*.csv",
                FileName = "NurseCallLog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv",
            })
            {
                if (dlg.ShowDialog() != DialogResult.OK) return;
                using (var sw = new System.IO.StreamWriter(dlg.FileName))
                {
                    sw.WriteLine("ID,Room/Ward,Time Triggered,Status,Resolved At,Response Time");
                    foreach (DataGridViewRow row in dgvAlerts.Rows)
                        sw.WriteLine(string.Join(",",
                            row.Cells["ColID"].Value,
                            row.Cells["ColRoom"].Value,
                            row.Cells["ColTime"].Value,
                            row.Cells["ColStatus"].Value,
                            row.Cells["ColResolved"].Value,
                            row.Cells["ColResponse"].Value));
                }
                ShowMsg("Log exported to:\n" + dlg.FileName);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  UI STATE UPDATES
        // ══════════════════════════════════════════════════════════
        private void UpdateBanner()
        {
            int active  = CountByStatus("ACTIVE");
            int pending = CountByStatus("PENDING");

            if (active > 0)
            {
                pnlBanner.BackColor     = C_RED_BG;
                lblBannerText.ForeColor = C_RED;
                pnlStatusLed.BackColor  = C_RED;
                lblBannerText.Text      = active == 1
                    ? $"EMERGENCY — {GetFirstActiveRoom()}"
                    : $"{active} ACTIVE EMERGENCIES";
                lblBannerSub.Text  = "IMMEDIATE RESPONSE REQUIRED";
                _bannerTimer.Start();
            }
            else if (pending > 0)
            {
                pnlBanner.BackColor     = C_AMBER_BG;
                lblBannerText.ForeColor = C_AMBER;
                pnlStatusLed.BackColor  = C_AMBER;
                lblBannerText.Text      = "ALERTS PENDING ACKNOWLEDGEMENT";
                lblBannerSub.Text       = "PLEASE ACKNOWLEDGE AND RESOLVE OPEN ALERTS";
                _bannerTimer.Stop();
            }
            else
            {
                pnlBanner.BackColor     = C_GREEN_BG;
                lblBannerText.ForeColor = C_GREEN;
                pnlStatusLed.BackColor  = C_GREEN;
                lblBannerText.Text      = "NO EMERGENCIES YET";
                lblBannerSub.Text       = "ALL WARDS CLEAR — SYSTEM MONITORING";
                _bannerTimer.Stop();
                // All emergencies resolved — tell Arduino to turn off the LED
                SendClearToArduino();
            }
            pnlBanner.Invalidate();
            lblActiveCount.Text = active.ToString();
        }

        private void UpdateStats()
        {
            int active   = CountByStatus("ACTIVE");
            int pending  = CountByStatus("PENDING");
            int resolved = CountByStatus("RESOLVED");

            lblStatTotal.Text    = dgvAlerts.Rows.Count.ToString();
            lblStatActive.Text   = active.ToString();
            lblStatPending.Text  = pending.ToString();
            lblStatResolved.Text = resolved.ToString();

            double total = 0; int count = 0;
            foreach (DataGridViewRow r in dgvAlerts.Rows)
            {
                string v = r.Cells["ColResponse"].Value?.ToString() ?? "—";
                if (v.EndsWith("s") && int.TryParse(v.TrimEnd('s'), out int s)) { total += s; count++; }
            }
            lblStatAvg.Text = count > 0 ? $"{(int)(total / count)}s" : "—";
        }

        private void UpdateWardSidebar()
        {
            foreach (Control c in pnlSidebar.Controls)
            {
                if (c.Tag is string ward)
                {
                    bool hasActive  = RowExistsWithWardAndStatus(ward, "ACTIVE");
                    bool hasPending = RowExistsWithWardAndStatus(ward, "PENDING");
                    c.ForeColor = hasActive ? C_RED : hasPending ? C_AMBER : C_GREEN;
                    c.Text      = $"{ward}  [{(hasActive ? "ALERT" : hasPending ? "PENDING" : "CLEAR")}]";
                }
            }
        }

        private void FilterGrid(string filter)
        {
            foreach (DataGridViewRow row in dgvAlerts.Rows)
            {
                string combined = $"{row.Cells["ColRoom"].Value} {row.Cells["ColStatus"].Value}".ToLower();
                row.Visible = string.IsNullOrWhiteSpace(filter) || combined.Contains(filter.ToLower());
            }
        }

        // ══════════════════════════════════════════════════════════
        //  GRID FORMATTING
        // ══════════════════════════════════════════════════════════
        private void DgvAlerts_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var    row    = dgvAlerts.Rows[e.RowIndex];
            string status = row.Cells["ColStatus"].Value?.ToString() ?? "";

            switch (status)
            {
                case "ACTIVE":
                    row.DefaultCellStyle.BackColor = Color.FromArgb(35, 13, 13);
                    row.DefaultCellStyle.ForeColor = C_TEXT;
                    break;
                case "PENDING":
                    row.DefaultCellStyle.BackColor = Color.FromArgb(35, 25, 10);
                    row.DefaultCellStyle.ForeColor = C_TEXT;
                    break;
                case "RESOLVED":
                    row.DefaultCellStyle.BackColor = C_SURFACE;
                    row.DefaultCellStyle.ForeColor = C_MUTED;
                    break;
            }

            string col = dgvAlerts.Columns[e.ColumnIndex].Name;
            if (col == "ColStatus")
            {
                e.CellStyle.ForeColor = status == "ACTIVE" ? C_RED : status == "PENDING" ? C_AMBER : C_GREEN;
                e.CellStyle.Font      = new Font("Courier New", 8f, FontStyle.Bold);
            }
            if (col == "ColID")   e.CellStyle.ForeColor = C_MUTED;
            if (col == "ColRoom") e.CellStyle.ForeColor = C_BLUE;
        }

        private void DgvAlerts_SelectionChanged(object sender, EventArgs e)
        {
            _selectedRowIndex = dgvAlerts.SelectedRows.Count > 0
                ? dgvAlerts.SelectedRows[0].Index : -1;
        }

        // ══════════════════════════════════════════════════════════
        //  TIMERS
        // ══════════════════════════════════════════════════════════
        private void SetupTimers()
        {
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) =>
            {
                lblClock.Text   = DateTime.Now.ToString("HH:mm:ss");
                lblDate.Text    = DateTime.Now.ToString("ddd dd MMM yyyy").ToUpper();
                TimeSpan up     = DateTime.Now - _startTime;
                lblFooterUptime.Text = $"UPTIME: {(int)up.TotalHours:D2}:{up.Minutes:D2}:{up.Seconds:D2}";
            };
            _clockTimer.Start();

            _bannerTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _bannerTimer.Tick += (s, e) =>
            {
                _bannerFlash        = !_bannerFlash;
                pnlBanner.BackColor = _bannerFlash
                    ? Color.FromArgb(180, 20, 20)
                    : Color.FromArgb(10,   5,  5);
                lblBannerText.ForeColor = _bannerFlash
                    ? Color.FromArgb(255, 255, 100)
                    : Color.FromArgb(248,  81,  73);
            };
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════
        private int CountByStatus(string status)
        {
            int n = 0;
            foreach (DataGridViewRow r in dgvAlerts.Rows)
                if (r.Cells["ColStatus"].Value?.ToString() == status) n++;
            return n;
        }

        private string GetFirstActiveRoom()
        {
            foreach (DataGridViewRow r in dgvAlerts.Rows)
                if (r.Cells["ColStatus"].Value?.ToString() == "ACTIVE")
                    return r.Cells["ColRoom"].Value?.ToString() ?? "";
            return "";
        }

        private bool RowExistsWithWardAndStatus(string ward, string status)
        {
            foreach (DataGridViewRow r in dgvAlerts.Rows)
                if ((r.Cells["ColRoom"].Value?.ToString() ?? "").Contains(ward) &&
                     r.Cells["ColStatus"].Value?.ToString() == status)
                    return true;
            return false;
        }

        private Button MakeToolBtn(string text, int x, Color bg, Color fg)
        {
            var btn = new Button
            {
                Text      = text,
                Location  = new Point(x, 7),
                Size      = new Size(text.Length * 7 + 20, 26),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Courier New", 8f),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor = C_BORDER;
            return btn;
        }

        private Label MakeFooterLabel(string text, int x)
            => new Label
            {
                Text      = text,
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 7.5f),
                Location  = new Point(x, 6),
                AutoSize  = true,
            };

        private void AddSidebarTitle(string text, ref int y)
        {
            var sep = new Panel { Location = new Point(0, y), Size = new Size(176, 1), BackColor = C_BORDER };
            pnlSidebar.Controls.Add(sep);
            y += 4;
            var lbl = new Label
            {
                Text      = text,
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 7f),
                Location  = new Point(0, y),
                Size      = new Size(176, 16),
            };
            pnlSidebar.Controls.Add(lbl);
            y += 20;
        }

        private Label AddSidebarStat(string label, string value, Color valColor, ref int y)
        {
            pnlSidebar.Controls.Add(new Label
            {
                Text      = label,
                ForeColor = C_MUTED,
                Font      = new Font("Courier New", 8f),
                Location  = new Point(0, y),
                Size      = new Size(110, 18),
            });
            var val = new Label
            {
                Text      = value,
                ForeColor = valColor,
                Font      = new Font("Courier New", 8f, FontStyle.Bold),
                Location  = new Point(116, y),
                Size      = new Size(60, 18),
                TextAlign = ContentAlignment.MiddleRight,
            };
            pnlSidebar.Controls.Add(val);
            y += 22;
            return val;
        }

        private void BuildWardStatus(ref int y)
        {
            foreach (string ward in new[] { "Ward A", "Ward B", "Ward C", "ICU", "Emergency" })
            {
                var lbl = new Label
                {
                    Text      = $"{ward}  [CLEAR]",
                    ForeColor = C_GREEN,
                    Font      = new Font("Courier New", 8f),
                    Location  = new Point(0, y),
                    Size      = new Size(176, 18),
                    Tag       = ward,
                };
                pnlSidebar.Controls.Add(lbl);
                y += 20;
            }
        }

        private void PopulateComPorts(bool refresh) => PopulateComPorts();

        // Send CLEAR command to Arduino when all alerts are resolved
        // This turns off the LED at the nurses station
        private void SendClearToArduino()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.WriteLine("CLEAR");
            }
            catch { /* port may have closed */ }
        }

        private void ShowMsg(string msg) =>
            MessageBox.Show(msg, "Nurse Call System", MessageBoxButtons.OK, MessageBoxIcon.Information);

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CloseSerial();
            _clockTimer?.Stop();
            _bannerTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
