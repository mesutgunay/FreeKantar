using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FreeKantar.Data;
using FreeKantar.Models;
using FreeKantar.Services;
using System.Linq;

namespace FreeKantar.UI
{
    public class AdminManagementModal : Form
    {
        private readonly DbService _db;
        private readonly PrintService _printer;
        private WeighingRecord _selectedRecord;

        private DataGridView gridAdmin;
        private ComboBox cbTransaction, cbProduct;
        private TextBox txtPlate, txtDriverName, txtDriverSurname, txtDriverPhone, txtDest, txtDesc;
        private NumericUpDown numW1, numW2, numW3;
        private CheckBox chkCompleted;
        private Button btnSave, btnDelete, btnPrint, btnRestore;
        
        private int _currentPage = 1;
        private int _pageSize = 15;
        private int _totalPages = 1;
        private Label lblPage;

        private readonly LanguageService _lang;
        
        public AdminManagementModal(DbService db, LanguageService lang)
        {
            _db = db;
            _lang = lang;
            _printer = new PrintService(db, lang);
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Tag = "AdminMod";
            this.Size = new Size(1450, 850);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10);

            try {
                if (System.IO.File.Exists("app_icon.png")) {
                    using (var bitmap = new Bitmap("app_icon.png")) {
                        this.Icon = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            } catch { }

            var rootContainer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            rootContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340)); 
            rootContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); 
            this.Controls.Add(rootContainer);

            var formPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), BackColor = Color.White };
            rootContainer.Controls.Add(formPanel, 0, 0);

            var lblFormTitle = new Label { 
                Text = "KAYIT DÜZENLEME", 
                Dock = DockStyle.Top, Height = 45, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold), 
                ForeColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 5)
            };
            var titleUnderline = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(226, 232, 240), Margin = new Padding(0, 0, 0, 15) };
            formPanel.Controls.Add(titleUnderline);
            formPanel.Controls.Add(lblFormTitle);

            var scrollContainer = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                AutoScroll = true, 
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(5, 15, 5, 10)
            };
            formPanel.Controls.Add(scrollContainer);
            scrollContainer.BringToFront();

            void AddStyledField(string label, Control ctrl, string tag, int height = 28) {
                var container = new Panel { Width = 285, Height = height + 20, Margin = new Padding(0, 0, 0, 10) };
                var l = new Label { 
                    Text = label, Tag = tag, 
                    Location = new Point(0, 0), Width = 280, 
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), 
                    ForeColor = Color.FromArgb(100, 116, 139) 
                };
                ctrl.Location = new Point(0, 18);
                ctrl.Width = 280;
                ctrl.Height = height;
                if (ctrl is ComboBox cb) {
                    cb.FlatStyle = FlatStyle.Flat;
                }
                container.Controls.Add(l);
                container.Controls.Add(ctrl);
                scrollContainer.Controls.Add(container);
            }

            cbTransaction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            var keys = new[] { "SATIŞ", "ALIŞ", "İADE TRANSFER", "İADE + İLAVE SEVK" };
            var list = new List<KeyValuePair<string, string>>();
            foreach (var k in keys) list.Add(new KeyValuePair<string, string>(k, _lang.Translate(k)));
            cbTransaction.DisplayMember = "Value";
            cbTransaction.ValueMember = "Key";
            cbTransaction.DataSource = list;
            AddStyledField("İŞLEM TİPİ", cbTransaction, "TransactionType");

            cbProduct = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            AddStyledField(_lang.Translate("Product"), cbProduct, "Product");

            txtPlate = new TextBox(); AddStyledField("PLAKA", txtPlate, "Plate");
            txtDriverName = new TextBox(); AddStyledField("ŞOFÖR AD", txtDriverName, "DriverName");
            txtDriverSurname = new TextBox(); AddStyledField("ŞOFÖR SOYAD", txtDriverSurname, "DriverSurname");
            txtDriverPhone = new TextBox(); AddStyledField("ŞOFÖR TELEFON", txtDriverPhone, "DriverPhone");
            txtDest = new TextBox(); AddStyledField("SEVK YERİ", txtDest, "Destination");
            txtDesc = new TextBox { Multiline = true }; AddStyledField("AÇIKLAMA", txtDesc, "Description", 55);
            
            numW1 = new NumericUpDown { Maximum = 1000000 }; AddStyledField("1. TARTIM", numW1, "FirstWeight");
            numW2 = new NumericUpDown { Maximum = 1000000 }; AddStyledField("2. TARTIM", numW2, "SecondWeight");
            numW3 = new NumericUpDown { Maximum = 1000000 }; AddStyledField("3. TARTIM (IADE İÇİN)", numW3, "ThirdWeight");

            chkCompleted = new CheckBox { Tag = "CompletedCheck", Margin = new Padding(0, 5, 0, 15), Width = 280, Height = 30 };
            scrollContainer.Controls.Add(chkCompleted);

            btnSave = new Button { Tag = "Save", Width = 280, Height = 40, BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) };
            btnSave.Click += (s, e) => SaveRecord();
            scrollContainer.Controls.Add(btnSave);

            btnRestore = new Button { Tag = "Restore", Width = 280, Height = 40, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = false, Margin = new Padding(0, 0, 0, 8) };
            btnRestore.Click += (s, e) => RestoreRecord();
            scrollContainer.Controls.Add(btnRestore);

            btnDelete = new Button { Tag = "Delete", Width = 280, Height = 40, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 0, 8) };
            btnDelete.Click += (s, e) => DeleteRecord();
            scrollContainer.Controls.Add(btnDelete);

            btnPrint = new Button { Tag = "PrintReceipt", Width = 280, Height = 40, BackColor = Color.FromArgb(59, 130, 246), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(0, 10, 0, 0) };
            btnPrint.Click += (s, e) => PrintRecord();
            scrollContainer.Controls.Add(btnPrint);

            var gridContainer = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            gridContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            gridContainer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            gridContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            rootContainer.Controls.Add(gridContainer, 1, 0);

            var topActionPanel = new FlowLayoutPanel { 
                Dock = DockStyle.Fill, 
                BackColor = Color.White, 
                Padding = new Padding(10, 10, 0, 0),
                FlowDirection = FlowDirection.LeftToRight
            };
            gridContainer.Controls.Add(topActionPanel, 0, 0);

            var btnBackup = new Button { Tag = "BackupData", Width = 150, Height = 35, BackColor = Color.FromArgb(249, 115, 22), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
            btnBackup.Click += (s, e) => BackupDatabase();
            topActionPanel.Controls.Add(btnBackup);

            var btnImport = new Button { Tag = "RestoreData", Width = 150, Height = 35, BackColor = Color.FromArgb(139, 92, 246), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
            btnImport.Click += (s, e) => RestoreDatabase();
            topActionPanel.Controls.Add(btnImport);

            var btnReset = new Button { Tag = "ResetAllDataMsg", Width = 150, Height = 35, BackColor = Color.FromArgb(153, 27, 27), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 0, 10, 0) };
            btnReset.Click += (s, e) => ResetDatabase();
            topActionPanel.Controls.Add(btnReset);

            gridAdmin = new DataGridView { 
                Dock = DockStyle.Fill, AllowUserToAddRows = false, ReadOnly = true, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None 
            };
            gridAdmin.SelectionChanged += (s, e) => OnSelectionChanged();
            gridAdmin.RowPrePaint += GridAdmin_RowPrePaint;
            gridAdmin.CellFormatting += GridAdmin_CellFormatting;
            gridContainer.Controls.Add(gridAdmin, 0, 1);

            var pagPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btnPr = new Button { Text = "<", Width = 40 }; btnPr.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
            lblPage = new Label { Text = "1/1", AutoSize = true, Margin = new Padding(10, 5, 10, 0) };
            var btnNx = new Button { Text = ">", Width = 40 }; btnNx.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; RefreshGrid(); } };
            pagPanel.Controls.AddRange(new Control[] { btnPr, lblPage, btnNx });
            gridContainer.Controls.Add(pagPanel, 0, 2);

            _lang.TranslateControl(this);
        }

        private void GridAdmin_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var record = (WeighingRecord)gridAdmin.Rows[e.RowIndex].DataBoundItem;
            if (record.IsDeleted)
            {
                gridAdmin.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226); // Light Red
                gridAdmin.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.FromArgb(153, 27, 27); // Dark Red
            }
            else
            {
                gridAdmin.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.White;
                gridAdmin.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Black;
            }
        }

        private void LoadData()
        {
            var products = _db.GetProducts();
            cbProduct.DataSource = products;
            cbProduct.DisplayMember = "Name";
            cbProduct.ValueMember = "Id";
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            int total = _db.GetTotalWeighingCount(true);
            _totalPages = (int)Math.Ceiling((double)total / _pageSize);
            if (_totalPages == 0) _totalPages = 1;
            lblPage.Text = $"{_lang.Translate("Page")}: {_currentPage} / {_totalPages}";

            var records = _db.GetWeighingRecordsPaged(_currentPage, _pageSize, true);
            gridAdmin.DataSource = null;
            gridAdmin.DataSource = records;

            if (gridAdmin.Columns.Count > 0)
            {
                foreach (DataGridViewColumn col in gridAdmin.Columns) col.Visible = false;
                
                void ConfigCol(string name, string key) {
                    if (gridAdmin.Columns.Contains(name)) {
                        gridAdmin.Columns[name].Visible = true;
                        gridAdmin.Columns[name].HeaderText = _lang.Translate(key);
                    }
                }

                ConfigCol("Plate", "Plate");
                ConfigCol("ProductName", "Product");
                ConfigCol("FirstWeight", "FirstWeight");
                ConfigCol("SecondWeight", "SecondWeight");
                ConfigCol("ThirdWeight", "ThirdWeight");
                ConfigCol("NetWeight", "Net");
                ConfigCol("DisplayStatus", "Status");
                ConfigCol("DisplayDate", "Date");
                ConfigCol("WeightType", "Audit");
            }
        }

        private void GridAdmin_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            string colName = gridAdmin.Columns[e.ColumnIndex].Name;

            if (colName == "TransactionType" || colName == "DisplayStatus" || colName == "WeightType")
            {
                e.Value = _lang.Translate(e.Value.ToString());
                e.FormattingApplied = true;
            }
        }

        private void OnSelectionChanged()
        {
            if (gridAdmin.SelectedRows.Count == 0) {
                _selectedRecord = null;
                return;
            }
            _selectedRecord = (WeighingRecord)gridAdmin.SelectedRows[0].DataBoundItem;

            cbTransaction.SelectedValue = _selectedRecord.TransactionType;
            cbProduct.SelectedValue = _selectedRecord.ProductId;
            txtPlate.Text = _selectedRecord.Plate;
            txtDriverName.Text = _selectedRecord.DriverName;
            txtDriverSurname.Text = _selectedRecord.DriverSurname;
            txtDriverPhone.Text = _selectedRecord.DriverPhone;
            txtDest.Text = _selectedRecord.Destination;
            txtDesc.Text = _selectedRecord.Description;
            numW1.Value = (decimal)_selectedRecord.FirstWeight;
            numW2.Value = (decimal)(_selectedRecord.SecondWeight ?? 0);
            numW3.Value = (decimal)(_selectedRecord.ThirdWeight ?? 0);
            chkCompleted.Checked = _selectedRecord.IsCompleted;

            // Soft Delete UI
            if (_selectedRecord.IsDeleted)
            {
                btnRestore.Visible = true;
                btnDelete.Visible = false;
                btnSave.Enabled = false; 
            }
            else
            {
                btnRestore.Visible = false;
                btnDelete.Visible = true;
                btnSave.Enabled = true;
            }
        }

        private void SaveRecord()
        {
            if (_selectedRecord == null) return;
            
            _selectedRecord.TransactionType = (string)cbTransaction.SelectedValue;
            _selectedRecord.ProductId = (int)cbProduct.SelectedValue;
            _selectedRecord.Plate = txtPlate.Text;
            _selectedRecord.DriverName = txtDriverName.Text;
            _selectedRecord.DriverSurname = txtDriverSurname.Text;
            _selectedRecord.DriverPhone = txtDriverPhone.Text;
            _selectedRecord.Destination = txtDest.Text;
            _selectedRecord.Description = txtDesc.Text;
            _selectedRecord.FirstWeight = (double)numW1.Value;
            _selectedRecord.SecondWeight = (double)numW2.Value;
            _selectedRecord.ThirdWeight = (double)numW3.Value;
            _selectedRecord.IsCompleted = chkCompleted.Checked;
            _selectedRecord.WeightType = "Manuel Düzeltildi";

            _db.AdminUpdateWeighing(_selectedRecord);
            MessageBox.Show("Kayıt güncellendi.");
            RefreshGrid();
        }

        private void DeleteRecord()
        {
            if (_selectedRecord == null) return;
            var res = MessageBox.Show("Bu kaydı SİLMEK istediğinize emin misiniz?\n(Kayıt arşivlenecek ve operatörlerden gizlenecektir)", "Silme Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                _db.DeleteWeighing(_selectedRecord.Id);
                RefreshGrid();
                MessageBox.Show("Kayıt arşivlendi.");
            }
        }

        private void RestoreRecord()
        {
            if (_selectedRecord == null) return;
            var res = MessageBox.Show("Bu kaydı GERİ GETİRMEK istediğinize emin misiniz?", "Geri Getir", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                _db.RestoreWeighing(_selectedRecord.Id);
                RefreshGrid();
                MessageBox.Show("Kayıt başarıyla geri getirildi.");
            }
        }

        private void ResetFields()
        {
            txtPlate.Clear();
            txtDriverName.Clear();
            txtDriverSurname.Clear();
            txtDest.Clear();
            txtDesc.Clear();
            numW1.Value = 0;
            numW2.Value = 0;
            numW3.Value = 0;
        }

        private void PrintRecord()
        {
            if (_selectedRecord == null) return;
            _printer.PrintReceipt(_selectedRecord);
        }

        private void BackupDatabase()
        {
            if (MessageBox.Show(_lang.Translate("ConfirmBackup"), _lang.Translate("BackupData"), MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            using (var sfd = new SaveFileDialog { Filter = "Database File|*.db", FileName = $"FreeKantar_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.Copy(_db.GetDatabasePath(), sfd.FileName, true);
                        MessageBox.Show(_lang.Translate("BackupSuccess"));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(_lang.Translate("Error") + ": " + ex.Message);
                    }
                }
            }
        }

        private void RestoreDatabase()
        {
            if (MessageBox.Show(_lang.Translate("ConfirmRestore"), _lang.Translate("RestoreData"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            using (var ofd = new OpenFileDialog { Filter = "Database File|*.db" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.Copy(ofd.FileName, _db.GetDatabasePath(), true);
                        MessageBox.Show(_lang.Translate("RestoreSuccess"));
                        this.DialogResult = DialogResult.OK; // Force refresh in main window
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(_lang.Translate("Error") + ": " + ex.Message);
                    }
                }
            }
        }

        private void ResetDatabase()
        {
            if (MessageBox.Show(_lang.Translate("ConfirmReset"), _lang.Translate("ConfirmResetTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;
            if (MessageBox.Show(_lang.Translate("ConfirmReset"), "FINAL WARNING", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

            try
            {
                _db.ResetAllData();
                MessageBox.Show(_lang.Translate("ResetSuccess"));
                LoadData(); // Re-fetches products and refreshes grid
            }
            catch (Exception ex)
            {
                MessageBox.Show(_lang.Translate("Error") + ": " + ex.Message);
            }
        }
    }
}
