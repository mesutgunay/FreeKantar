using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FreeKantar.Data;
using FreeKantar.Models;
using FreeKantar.Services;
using System.Linq;
using System.IO;
using System.Text;

namespace FreeKantar.UI
{
    public partial class MainForm : Form
    {
        private readonly DbService _db;
        private readonly LanguageService _lang;
        private readonly SerialService _serial;
        private readonly PrintService _printer;
        private double _currentWeight = 0;
        private int? _editingRecordId = null;

        // Paging State
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;

        // UI Controls
        private Label lblLiveWeight;
        private DateTime _lastDisplayUpdate = DateTime.MinValue;
        private ComboBox cbTransactionType;
        private ComboBox cbProduct;
        private TextBox txtPlate;
        private TextBox txtDriverName;
        private TextBox txtDriverSurname;
        private TextBox txtDriverPhone;
        private TextBox txtDestination;
        private TextBox txtDescription;
        private Button btnGetWeight;
        private DataGridView gridWeighings;
        private MenuStrip menuStrip;
        private Label lblTitle;

        // Paging Controls
        private Button btnPrev;
        private Button btnNext;
        private Label lblPageInfo;
        private Button btnExcel;
        private Button btnUpgrade;

        public MainForm()
        {
            _db = new DbService();
            _lang = new LanguageService();
            _serial = new SerialService();
            _printer = new PrintService(_db, _lang);
            _serial.OnWeightReceived += (w) => this.Invoke(new Action(() => UpdateLiveWeight(w)));
            _serial.OnError += (err) => MessageBox.Show(err);

            InitializeComponent();
            LoadSettings();
            LoadData(); 
            StartSerial();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Alt | Keys.A))
            {
                OpenAdminPanel();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                ResetForm();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void OpenAdminPanel()
        {
            using (var form = new Form())
            {
                form.Tag = "AdminAuth";
                form.Size = new Size(300, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;

                var lbl = new Label { Tag = "AuthPassword", Location = new Point(20, 20), AutoSize = true };
                var txt = new TextBox { Location = new Point(20, 45), Width = 240, PasswordChar = '*' };
                var btn = new Button { Tag = "Login", Location = new Point(185, 75), DialogResult = DialogResult.OK };
                form.Controls.AddRange(new Control[] { lbl, txt, btn });
                form.AcceptButton = btn;

                _lang.TranslateControl(form);

                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (txt.Text == "AdminMod")
                    {
                        using (var adminPanel = new AdminManagementModal(_db, _lang))
                        {
                            adminPanel.ShowDialog();
                            LoadData(); 
                        }
                    }
                    else
                    {
                        MessageBox.Show(_lang.Translate("InvalidPassword"), _lang.Translate("Error") ?? "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Free kantar v2.11";
            this.WindowState = FormWindowState.Maximized; 
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 10);

            try {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream? stream = assembly.GetManifestResourceStream("FreeKantar.app_icon.png"))
                {
                    if (stream != null) {
                        using (var bitmap = new Bitmap(stream)) {
                            this.Icon = Icon.FromHandle(bitmap.GetHicon());
                        }
                    }
                }
            } catch { }

            // Menu
            menuStrip = new MenuStrip { BackColor = Color.White };
            var settingsMenu = new ToolStripMenuItem { Tag = "Settings" };
            
            var kantarSettings = new ToolStripMenuItem { Tag = "KantarSettings" };
            kantarSettings.Click += (s, e) => ShowSettings();
            settingsMenu.DropDownItems.Add(kantarSettings);
            
            var langMenu = new ToolStripMenuItem { Tag = "Language" };
            foreach (LanguageService.Language l in Enum.GetValues(typeof(LanguageService.Language)))
            {
                var item = new ToolStripMenuItem(l.ToString(), null, (s, e) => ChangeLanguage((LanguageService.Language)((ToolStripMenuItem)s).Tag));
                item.Tag = l;
                langMenu.DropDownItems.Add(item);
            }
            settingsMenu.DropDownItems.Add(langMenu);

            menuStrip.Items.Add(settingsMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);

            // Root Layout
            var rootLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // Header / Weight
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 420)); // Forms
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Grid Section
            this.Controls.Add(rootLayout);

            // 1. Header with Live Weight
            var headerTable = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                BackColor = Color.FromArgb(30, 41, 59),
                ColumnCount = 2,
                RowCount = 1
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Left
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60)); // Right
            rootLayout.Controls.Add(headerTable, 0, 0);

            // Left: Professional Features
            btnUpgrade = new Button {
                Text = "🚀 " + _lang.Translate("ProfessionalFeatures"),
                Width = 280,
                Height = 45,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 211, 102),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(20, 38, 0, 0)
            };
            btnUpgrade.FlatAppearance.BorderSize = 0;
            btnUpgrade.Click += (s, e) => new UpgradeModal(_lang).ShowDialog();
            headerTable.Controls.Add(btnUpgrade, 0, 0);

            // Right: Container for Title + Weight
            var rightContainer = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 0, 20, 0) 
            };
            headerTable.Controls.Add(rightContainer, 1, 0);

            // Add weight first (will be far right because of RTL)
            lblLiveWeight = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 46, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94),
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 0)
            };
            rightContainer.Controls.Add(lblLiveWeight);

            // Title (will be to the left of weight)
            lblTitle = new Label { 
                Text = "CANLI TARTIM", 
                Tag = "LiveWeighing",
                ForeColor = Color.FromArgb(148, 163, 184), 
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Margin = new Padding(0, 48, 10, 0)
            };
            rightContainer.Controls.Add(lblTitle);

            // 2. Form Section
            var formContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 0) };
            rootLayout.Controls.Add(formContainer, 0, 1);

            var formLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 5 };
            for(int i=0; i<5; i++) formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // Row 1
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 85)); // Row 2
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // Row 3
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65)); // Row 4
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90)); // Row 5
            formContainer.Controls.Add(formLayout);

            Panel CreateLabeledInput(string label, Control control, string tag = null, bool hasAction = false, Action action = null)
            {
                var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };
                
                // Extra space for product to make it shorter
                if (tag == "Product") p.Padding = new Padding(5, 5, 55, 5);

                var l = new Label { Text = label, Tag = tag, Dock = DockStyle.Top, Height = 20, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                if (hasAction)
                {
                    var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, 0, 0) };
                    control.Dock = DockStyle.Fill;
                    
                    var btn = new Button { 
                        Text = "+", Width = 30, Height = 35, // Use a consistent height
                        Anchor = AnchorStyles.Right | AnchorStyles.Top, 
                        FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(203, 213, 225) 
                    };
                    btn.Click += (s, e) => action?.Invoke();
                    
                    inner.Controls.Add(btn);
                    inner.Controls.Add(control);
                    
                    // Manually position button to stay on the right and respect height
                    // We also need some right-padding on the inner panel so Dock.Fill control doesn't go under it
                    inner.Padding = new Padding(0, 2, 0, 0); 
                    btn.Location = new Point(inner.Width - 30, 0);
                    
                    p.Controls.Add(inner);
                }
                else
                {
                    control.Dock = DockStyle.Fill;
                    p.Controls.Add(control);
                }
                p.Controls.Add(l);
                p.Height = 60;
                return p;
            }

            cbTransactionType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            UpdateTransactionItems();
            formLayout.Controls.Add(CreateLabeledInput("İŞLEM", cbTransactionType, "TransactionType"), 0, 0);

            txtPlate = new TextBox { AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.CustomSource };
            txtPlate.TextChanged += (s, e) => {
                // If it's an exact match from the list, treat it as a final selection
                bool isExact = txtPlate.AutoCompleteCustomSource.Contains(txtPlate.Text);
                AutoFillDriverInfo(txtPlate.Text, isExact);
            };
            txtPlate.Validated += (s, e) => AutoFillDriverInfo(txtPlate.Text, true);
            formLayout.Controls.Add(CreateLabeledInput("PLAKA", txtPlate, "Plate"), 1, 0);

            txtDriverName = new TextBox { AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.CustomSource };
            formLayout.Controls.Add(CreateLabeledInput("ŞOFÖR AD", txtDriverName, "DriverName"), 2, 0);

            txtDriverSurname = new TextBox { AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.CustomSource };
            formLayout.Controls.Add(CreateLabeledInput("ŞOFÖR SOYAD", txtDriverSurname, "DriverSurname"), 3, 0);

            txtDriverPhone = new TextBox { AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.CustomSource };
            formLayout.Controls.Add(CreateLabeledInput("ŞOFÖR TELEFON", txtDriverPhone, "DriverPhone"), 4, 0);

            cbProduct = new ComboBox { 
                DropDownStyle = ComboBoxStyle.DropDown, 
                AutoCompleteMode = AutoCompleteMode.SuggestAppend, 
                AutoCompleteSource = AutoCompleteSource.ListItems 
            };
            var pnlProduct = CreateLabeledInput("STOK ADI", cbProduct, "Product", true, () => AddProduct());
            formLayout.Controls.Add(pnlProduct, 0, 1);
            formLayout.SetColumnSpan(pnlProduct, 5);

            txtDestination = new TextBox { AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.CustomSource };
            var pnlDest = CreateLabeledInput("SEVK YERİ", txtDestination, "Destination");
            formLayout.Controls.Add(pnlDest, 0, 2);
            formLayout.SetColumnSpan(pnlDest, 5);

            txtDescription = new TextBox();
            var pnlDesc = CreateLabeledInput("AÇIKLAMA", txtDescription, "Description");
            formLayout.Controls.Add(pnlDesc, 0, 3);
            formLayout.SetColumnSpan(pnlDesc, 5);

            btnGetWeight = new Button {
                Tag = "GetWeight",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 30, 0, 20) // Remove bottom margin
            };
            btnGetWeight.FlatAppearance.BorderSize = 0;
            btnGetWeight.Click += (s, e) => ProcessWeighing();
            formLayout.Controls.Add(btnGetWeight, 0, 4);
            formLayout.SetColumnSpan(btnGetWeight, 5);

            var gridSectionLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            gridSectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); 
            gridSectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); 
            gridSectionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  
            rootLayout.Controls.Add(gridSectionLayout, 0, 2);

            var lblDoubleHint = new Label { 
                Tag = "DoubleOnClickHint", 
                Text = "İkinci tartımı almak için açık olan tartıma çift tıklayın", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.BottomLeft, 
                Font = new Font("Segoe UI", 8, FontStyle.Italic), 
                ForeColor = Color.FromArgb(100, 116, 139), 
                Margin = new Padding(20, 0, 0, 0) 
            };
            gridSectionLayout.Controls.Add(lblDoubleHint, 0, 0);

            var gridPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 0) };
            gridSectionLayout.Controls.Add(gridPanel, 0, 1);

            gridWeighings = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                GridColor = Color.FromArgb(226, 232, 240),
                EnableHeadersVisualStyles = false
            };
            gridWeighings.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Padding = new Padding(8)
            };
            gridWeighings.CellDoubleClick += (s, e) => LoadRecordForSecondWeight();
            gridWeighings.CellFormatting += GridWeighings_CellFormatting;
            gridWeighings.CellContentClick += GridWeighings_CellContentClick;
            gridWeighings.RowPrePaint += GridWeighings_RowPrePaint;
            gridPanel.Controls.Add(gridWeighings);

            var pagingPanel = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                BackColor = Color.White, 
                Padding = new Padding(20, 10, 20, 10)
            };
            gridSectionLayout.Controls.Add(pagingPanel, 0, 2);

            btnPrev = new Button { Tag = "Prev", Text = "< GERİ", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(226, 232, 240) };
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadGridData(); } };
            pagingPanel.Controls.Add(btnPrev);

            lblPageInfo = new Label { Tag = "Status", AutoSize = true, Margin = new Padding(20, 10, 20, 0), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pagingPanel.Controls.Add(lblPageInfo);

            btnNext = new Button { Tag = "Next", Text = "İLERİ >", Width = 100, Height = 35, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(226, 232, 240) };
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadGridData(); } };
            pagingPanel.Controls.Add(btnNext);

            btnExcel = new Button { 
                Text = "📊 " + _lang.Translate("ExcelReport"),
                Width = 150, Height = 35, 
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 41, 59), 
                ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(100, 0, 0, 0) 
            };
            btnExcel.Click += (s, e) => ExportToExcel();
            pagingPanel.Controls.Add(btnExcel);
        }

        private void ExportToExcel()
        {
            var records = _db.GetWeighingRecordsPaged(1, 10000); // Get all (up to 10k)
            if (!records.Any()) return;

            var sfd = new SaveFileDialog { Filter = "CSV Dosyası|*.csv", FileName = $"Kantar_Rapor_{DateTime.Now:yyyyMMdd_HHmm}.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    // CSV Header
                    sb.AppendLine("ID;İşlem;Stok;Plaka;Şoför;Sevk Yeri;1. Tartım;2. Tartım;3. Tartım;İlave Beton;Eski İade;Net;Tarih;Durum;Düzenleme Durumu");
                    
                    foreach (var r in records)
                    {
                        var line = string.Join(";", 
                            r.Id, r.TransactionType, r.ProductName, r.Plate, $"{r.DriverName} {r.DriverSurname}",
                            r.Destination, r.FirstWeight, r.SecondWeight ?? 0, r.ThirdWeight ?? 0,
                            r.AdditionalWeight, r.OriginalReturnWeight, r.NetWeight,
                            r.DisplayDate, r.DisplayStatus, r.WeightType);
                        sb.AppendLine(line);
                    }

                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Rapor başarıyla oluşturuldu.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}");
                }
            }
        }

        private void GridWeighings_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var record = (WeighingRecord)gridWeighings.Rows[e.RowIndex].DataBoundItem;
            
            if (!record.IsCompleted)
            {
                gridWeighings.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(220, 252, 231); // Soft Green
            }
            else
            {
                gridWeighings.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
            }
            
            // Re-ensure the button cell keeps its green style regardless of row color
            if (gridWeighings.Columns.Contains("PrintBtn"))
            {
                var btnCell = gridWeighings.Rows[e.RowIndex].Cells["PrintBtn"];
                btnCell.Style.BackColor = Color.FromArgb(34, 197, 94);
                btnCell.Style.ForeColor = Color.White;
                btnCell.Style.SelectionBackColor = Color.FromArgb(22, 163, 74); // Darker Green for selection
                btnCell.Style.SelectionForeColor = Color.White;
            }
        }

        private void GridWeighings_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            string colName = gridWeighings.Columns[e.ColumnIndex].Name;

            if (colName == "TransactionType" || colName == "DisplayStatus" || colName == "WeightType")
            {
                e.Value = _lang.Translate(e.Value.ToString());
                e.FormattingApplied = true;
            }
        }

        private void GridWeighings_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridWeighings.Columns[e.ColumnIndex].Name == "PrintBtn")
            {
                var record = (WeighingRecord)gridWeighings.Rows[e.RowIndex].DataBoundItem;
                _printer.PrintReceipt(record);
            }
        }

        private void UpdateLiveWeight(double weight)
        {
            _currentWeight = weight;
            if ((DateTime.Now - _lastDisplayUpdate).TotalMilliseconds > 100)
            {
                lblLiveWeight.Text = $"{weight:F0}";
                _lastDisplayUpdate = DateTime.Now;
            }
        }

        private void LoadData()
        {
            var products = _db.GetProducts();
            cbProduct.DataSource = null;
            cbProduct.DataSource = products;
            cbProduct.DisplayMember = "Name";
            cbProduct.ValueMember = "Id";

            var plates = _db.GetDistinctPlates();
            var sourcePlates = new AutoCompleteStringCollection();
            sourcePlates.AddRange(plates.ToArray());
            txtPlate.AutoCompleteCustomSource = sourcePlates;

            var names = _db.GetDistinctDriverNames();
            var sourceNames = new AutoCompleteStringCollection();
            sourceNames.AddRange(names.ToArray());
            txtDriverName.AutoCompleteCustomSource = sourceNames;

            var surnames = _db.GetDistinctDriverSurnames();
            var sourceSurnames = new AutoCompleteStringCollection();
            sourceSurnames.AddRange(surnames.ToArray());
            txtDriverSurname.AutoCompleteCustomSource = sourceSurnames;

            var dests = _db.GetDistinctDestinations();
            var sourceDests = new AutoCompleteStringCollection();
            sourceDests.AddRange(dests.ToArray());
            txtDestination.AutoCompleteCustomSource = sourceDests;

            _currentPage = 1;
            LoadGridData();
        }

        private void LoadGridData()
        {
            int totalRecords = _db.GetTotalWeighingCount();
            _totalPages = (int)Math.Ceiling((double)totalRecords / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var records = _db.GetWeighingRecordsPaged(_currentPage, _pageSize);
            gridWeighings.DataSource = null;
            gridWeighings.DataSource = records;
            
            lblPageInfo.Text = $"{_lang.Translate("Page")} {_currentPage} / {_totalPages} ({_lang.Translate("Total")}: {totalRecords})";
            btnPrev.Enabled = _currentPage > 1;
            btnNext.Enabled = _currentPage < _totalPages;

            if (gridWeighings.Columns.Count > 0)
            {
                foreach (DataGridViewColumn col in gridWeighings.Columns) col.Visible = false;

                void ConfigCol(string name, string header, int index) {
                    if (gridWeighings.Columns.Contains(name)) {
                        var c = gridWeighings.Columns[name];
                        c.Visible = true;
                        c.HeaderText = header;
                        c.DisplayIndex = index;
                    }
                }

                ConfigCol("TransactionType", _lang.Translate("TransactionType"), 0);
                ConfigCol("ProductName", _lang.Translate("Product"), 1);
                ConfigCol("Plate", _lang.Translate("Plate"), 2);
                ConfigCol("Destination", _lang.Translate("Destination"), 3);
                ConfigCol("FirstWeight", _lang.Translate("FirstWeight"), 4);
                ConfigCol("SecondWeight", _lang.Translate("SecondWeight"), 5);
                ConfigCol("ThirdWeight", _lang.Translate("ThirdWeight"), 6);
                ConfigCol("NetWeight", _lang.Translate("Net"), 7);
                ConfigCol("DisplayDate", _lang.Translate("Date"), 8);
                ConfigCol("DisplayStatus", _lang.Translate("Status"), 9);
                ConfigCol("WeightType", _lang.Translate("Audit"), 10);

                if (!gridWeighings.Columns.Contains("PrintBtn"))
                {
                    var btnCol = new DataGridViewButtonColumn
                    {
                        Name = "PrintBtn",
                        HeaderText = _lang.Translate("Print"),
                        Text = _lang.Translate("PrintReceipt"),
                        UseColumnTextForButtonValue = true,
                        FlatStyle = FlatStyle.Flat,
                        DefaultCellStyle = new DataGridViewCellStyle {
                            BackColor = Color.FromArgb(34, 197, 94),
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 8, FontStyle.Bold)
                        }
                    };
                    gridWeighings.Columns.Add(btnCol);
                }
                
                // Always update text and styles for live language switching and visibility
                var printCol = (DataGridViewButtonColumn)gridWeighings.Columns["PrintBtn"];
                printCol.HeaderText = _lang.Translate("Print");
                printCol.Text =  "📠" + _lang.Translate("PrintReceipt");
                printCol.Visible = true;
                printCol.DisplayIndex = 11;
                printCol.DefaultCellStyle.BackColor = Color.FromArgb(34, 197, 94);
                printCol.DefaultCellStyle.ForeColor = Color.White;
                printCol.DefaultCellStyle.SelectionBackColor = Color.FromArgb(22, 163, 74);
                printCol.DefaultCellStyle.SelectionForeColor = Color.White;

                gridWeighings.ColumnHeadersHeight = 45;
                foreach (DataGridViewColumn col in gridWeighings.Columns)
                {
                    if (!col.Visible) continue;
                    
                    if (col.Name == "ProductName" || col.Name == "Destination")
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    else
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                }

                gridWeighings.Columns["FirstWeight"].DefaultCellStyle.Format = "N0";
                gridWeighings.Columns["SecondWeight"].DefaultCellStyle.Format = "N0";
                gridWeighings.Columns["ThirdWeight"].DefaultCellStyle.Format = "N0";
                gridWeighings.Columns["NetWeight"].DefaultCellStyle.Format = "N0";
            }
        }

        private void StartSerial()
        {
            try {
                string port = _db.GetSetting("ComPort", "COM1");
                int baud = int.Parse(_db.GetSetting("BaudRate", "9600"));

                // If port is already open with same settings, do nothing to avoid hang
                if (_serial.IsOpen && _db.GetSetting("_lastPort", "") == port && _db.GetSetting("_lastBaud", "") == baud.ToString())
                    return;

                _serial.Start(port, baud);
                _db.SaveSetting("_lastPort", port);
                _db.SaveSetting("_lastBaud", baud.ToString());
            } catch { }
        }

        private void ProcessWeighing()
        {
            if (string.IsNullOrWhiteSpace(txtPlate.Text))
            {
                MessageBox.Show("PLAKA alanı boş bırakılamaz!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_currentWeight <= 0)
            {
                MessageBox.Show("Tartım tonajı 0 kayıt edilemez!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cbProduct.SelectedItem == null)
            {
                MessageBox.Show(_lang.Translate("PleaseSelectProduct") ?? "Lütfen stok seçin.");
                return;
            }

            if (_editingRecordId.HasValue)
            {
                var original = _db.GetWeighingRecordsPaged(1, 10000).FirstOrDefault(x => x.Id == _editingRecordId.Value);
                if (original == null) return;

                if (original.TransactionType == "İADE + İLAVE SEVK" && original.SecondWeight.HasValue && !original.ThirdWeight.HasValue)
                {
                    // Case: IADE step 3 (Empty Dara)
                    var record = new WeighingRecord { Id = _editingRecordId.Value, ThirdWeight = _currentWeight };
                    _db.UpdateWeighingThird(record);
                    MessageBox.Show("Üçüncü tartım (BOŞ DARA) alındı ve işlem tamamlandı.");
                    ResetForm();
                }
                else
                {
                    // Case: Second Weight
                    var record = new WeighingRecord
                    {
                        Id = _editingRecordId.Value,
                        TransactionType = ((KeyValuePair<string, string>)cbTransactionType.SelectedItem).Key,
                        ProductId = (int)cbProduct.SelectedValue,
                        Plate = txtPlate.Text,
                        DriverName = txtDriverName.Text,
                        DriverSurname = txtDriverSurname.Text,
                        DriverPhone = txtDriverPhone.Text,
                        Destination = txtDestination.Text,
                        Description = txtDescription.Text,
                        SecondWeight = _currentWeight,
                        SecondWeightDate = DateTime.Now
                    };

                    _db.UpdateWeighingSecond(record);
                    
                    if (record.TransactionType == "İADE + İLAVE SEVK")
                        MessageBox.Show("İkinci tartım (İLAVE) alındı. Boşaltım sonrası 3. tartımı almayı unutmayın.");
                    else
                        MessageBox.Show("İkinci tartım alındı ve işlem tamamlandı.");
                    
                    ResetForm();
                }
            }
            else
            {
                // New First Weight
                var record = new WeighingRecord
                {
                    TransactionType = ((KeyValuePair<string, string>)cbTransactionType.SelectedItem).Key,
                    ProductId = (int)cbProduct.SelectedValue,
                    Plate = txtPlate.Text,
                    DriverName = txtDriverName.Text,
                    DriverSurname = txtDriverSurname.Text,
                    DriverPhone = txtDriverPhone.Text,
                    Destination = txtDestination.Text,
                    Description = txtDescription.Text,
                    FirstWeight = _currentWeight,
                    FirstWeightDate = DateTime.Now,
                    WeightType = "Kantardan Tartıldı"
                };

                _db.SaveWeighingFirst(record);
                MessageBox.Show(_lang.Translate("FirstWeightSaved") ?? "İlk tartım kaydedildi.");
                ResetForm();
            }
            
            LoadData(); 
        }


        private void LoadRecordForSecondWeight()
        {
            if (gridWeighings.SelectedRows.Count == 0) return;
            var record = (WeighingRecord)gridWeighings.SelectedRows[0].DataBoundItem;
            
            if (record.IsCompleted)
            {
                MessageBox.Show("Bu tartım zaten tamamlanmış. Düzenlemek için Yönetici Modu (Ctrl+Alt+A) gereklidir.");
                return;
            }

            _editingRecordId = record.Id;
            SelectTransactionByKey(record.TransactionType);
            cbProduct.SelectedValue = record.ProductId;
            txtPlate.Text = record.Plate;
            txtDriverName.Text = record.DriverName;
            txtDriverSurname.Text = record.DriverSurname;
            txtDriverPhone.Text = record.DriverPhone;
            txtDestination.Text = record.Destination;
            txtDescription.Text = record.Description;

            if (record.TransactionType == "İADE + İLAVE SEVK" && record.SecondWeight.HasValue)
            {
                btnGetWeight.Text = _lang.Translate("GetWeightFinish");
                btnGetWeight.BackColor = Color.Purple;
            }
            else
            {
                btnGetWeight.Text = record.TransactionType == "İADE + İLAVE SEVK" ? _lang.Translate("GetWeightIade") : _lang.Translate("GetWeight2");
                btnGetWeight.BackColor = Color.Orange;
            }
            
            MessageBox.Show("Kayıt yüklendi. İşleme devam edebilirsiniz.");
        }

        private void AutoFillDriverInfo(string plate, bool isFinalCheck)
        {
            if (string.IsNullOrWhiteSpace(plate) || _editingRecordId != null) return;
            if (plate.Length < 4) return; // Prevent too many DB queries for very short inputs

            var last = _db.GetLastRecordByPlate(plate);
            if (last != null)
            {
                // Fill instantly if found
                txtDriverName.Text = last.DriverName;
                txtDriverSurname.Text = last.DriverSurname;
                txtDriverPhone.Text = last.DriverPhone;
            }
            else if (isFinalCheck)
            {
                // Only clear if this is the final check (Focus lost or exact list match)
                // AND it's really not in DB
                txtDriverName.Clear();
                txtDriverSurname.Clear();
                txtDriverPhone.Clear();
            }
        }

        private void AddProduct()
        {
            using (var modal = new ProductModal(_db, _lang))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    LoadData();
                }
            }
        }

        private void ResetForm()
        {
            _editingRecordId = null;
            btnGetWeight.Text = _lang.Translate("GetWeight");
            btnGetWeight.BackColor = Color.FromArgb(34, 197, 94);

            txtPlate.Clear();
            txtDriverName.Clear();
            txtDriverSurname.Clear();
            txtDriverPhone.Clear();
            txtDestination.Clear();
            txtDescription.Clear();
        }

        private void ChangeLanguage(LanguageService.Language l)
        {
            _lang.CurrentLanguage = l;
            _db.SaveSetting("Language", l.ToString());
            UpdateTransactionItems();
            UpdateUILanguage();
            LoadData(); 
        }

        private void UpdateTransactionItems()
        {
            var keys = new[] { "SATIŞ", "ALIŞ", "İADE TRANSFER", "İADE + İLAVE SEVK" };
            var list = new List<KeyValuePair<string, string>>();
            foreach (var k in keys) list.Add(new KeyValuePair<string, string>(k, _lang.Translate(k)));
            
            cbTransactionType.DisplayMember = "Value";
            cbTransactionType.ValueMember = "Key";
            cbTransactionType.DataSource = list;
        }

        private void SelectTransactionByKey(string key)
        {
            for (int i = 0; i < cbTransactionType.Items.Count; i++)
            {
                if (((KeyValuePair<string, string>)cbTransactionType.Items[i]).Key == key)
                {
                    cbTransactionType.SelectedIndex = i;
                    break;
                }
            }
        }

        private void UpdateUILanguage()
        {
            _lang.TranslateControl(this);
            
            // Re-apply icons that TranslateControl might have overwritten
            btnUpgrade.Text = "🚀 " + _lang.Translate("ProfessionalFeatures");
            btnExcel.Text = "📊 " + _lang.Translate("ExcelReport");
            
            // Update transaction list for current language
            UpdateTransactionItems();

            // Re-apply special logic for button state
            btnGetWeight.Text = GetButtonText();
            LoadGridHeaders(); // Force grid header refresh
        }

        private void LoadGridHeaders()
        {
            if (gridWeighings.Columns.Count > 0) LoadGridData();
        }

        private string GetButtonText()
        {
            if (!_editingRecordId.HasValue) return _lang.Translate("GetWeight");
            
            var original = _db.GetWeighingRecordsPaged(1, 10000).FirstOrDefault(x => x.Id == _editingRecordId.Value);
            if (original == null) return _lang.Translate("GetWeight");

            if (original.TransactionType == "İADE + İLAVE SEVK")
            {
                return original.SecondWeight.HasValue ? _lang.Translate("GetWeightFinish") : _lang.Translate("GetWeightIade");
            }
            return _lang.Translate("GetWeight2");
        }

        private void LoadSettings()
        {
            string langStr = _db.GetSetting("Language", "TR");
            if (Enum.TryParse(langStr, out LanguageService.Language l)) 
                _lang.CurrentLanguage = l;

            UpdateUILanguage();
            
            string port = _db.GetSetting("ComPort", "COM1");
            int baud = 9600;
            int.TryParse(_db.GetSetting("BaudRate", "9600"), out baud);
        }
        private void ShowSettings()
        {
            using (var modal = new SettingsModal(_db, _lang))
            {
                if (modal.ShowDialog() == DialogResult.OK)
                {
                    StartSerial();
                }
            }
        }
    }
}
