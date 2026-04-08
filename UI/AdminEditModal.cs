using System;
using System.Drawing;
using System.Windows.Forms;
using FreeKantar.Data;
using FreeKantar.Models;

namespace FreeKantar.UI
{
    public class AdminEditModal : Form
    {
        private readonly DbService _db;
        private WeighingRecord _record;
        
        private ComboBox cbTransaction;
        private ComboBox cbProduct;
        private TextBox txtPlate, txtDriverName, txtDriverSurname, txtDest, txtDesc;
        private NumericUpDown numWeight1, numWeight2;
        private CheckBox chkCompleted;

        public AdminEditModal(DbService db, WeighingRecord record)
        {
            _db = db;
            _record = record;
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Yönetici Kayıt Düzenleme";
            this.Size = new Size(500, 650);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 11, Padding = new Padding(20) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));

            void AddRow(string label, Control ctrl) {
                var lbl = new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                ctrl.Dock = DockStyle.Fill;
                layout.Controls.Add(lbl);
                layout.Controls.Add(ctrl);
            }

            cbTransaction = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            cbTransaction.Items.AddRange(new[] { "SEVK", "IADE" });
            AddRow("İŞLEM TİPİ", cbTransaction);

            cbProduct = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            AddRow("ÜRÜN", cbProduct);

            txtPlate = new TextBox();
            AddRow("PLAKA", txtPlate);

            txtDriverName = new TextBox();
            AddRow("ŞOFÖR AD", txtDriverName);

            txtDriverSurname = new TextBox();
            AddRow("ŞOFÖR SOYAD", txtDriverSurname);

            txtDest = new TextBox();
            AddRow("SEVK YERİ", txtDest);

            txtDesc = new TextBox { Multiline = true, Height = 60 };
            AddRow("AÇIKLAMA", txtDesc);

            numWeight1 = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 0 };
            AddRow("1. TARTIM (KG)", numWeight1);

            numWeight2 = new NumericUpDown { Maximum = 1000000, DecimalPlaces = 0 };
            AddRow("2. TARTIM (KG)", numWeight2);

            chkCompleted = new CheckBox { Text = "Tamamlandı / Bitiş İşareti" };
            layout.Controls.Add(new Label(), 0, 9);
            layout.Controls.Add(chkCompleted, 1, 9);

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var btnSave = new Button { Text = "GÜNCELLE", BackColor = Color.FromArgb(34, 197, 94), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Width = 120, Height = 40, Margin = new Padding(5) };
            var btnDelete = new Button { Text = "KAYDI SİL", BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Width = 100, Height = 40, Margin = new Padding(5) };
            
            btnSave.Click += (s, e) => SaveChange();
            btnDelete.Click += (s, e) => DeleteRecord();
            
            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnDelete);
            layout.Controls.Add(btnPanel, 0, 10);
            layout.SetColumnSpan(btnPanel, 2);

            this.Controls.Add(layout);
        }

        private void LoadData()
        {
            var products = _db.GetProducts();
            cbProduct.DataSource = products;
            cbProduct.DisplayMember = "Name";
            cbProduct.ValueMember = "Id";

            cbTransaction.Text = _record.TransactionType;
            cbProduct.SelectedValue = _record.ProductId;
            txtPlate.Text = _record.Plate;
            txtDriverName.Text = _record.DriverName;
            txtDriverSurname.Text = _record.DriverSurname;
            txtDest.Text = _record.Destination;
            txtDesc.Text = _record.Description;
            numWeight1.Value = (decimal)_record.FirstWeight;
            numWeight2.Value = (decimal)(_record.SecondWeight ?? 0);
            chkCompleted.Checked = _record.IsCompleted;
        }

        private void SaveChange()
        {
            _record.TransactionType = cbTransaction.Text;
            _record.ProductId = (int)cbProduct.SelectedValue;
            _record.Plate = txtPlate.Text;
            _record.DriverName = txtDriverName.Text;
            _record.DriverSurname = txtDriverSurname.Text;
            _record.Destination = txtDest.Text;
            _record.Description = txtDesc.Text;
            _record.FirstWeight = (double)numWeight1.Value;
            _record.SecondWeight = (double)numWeight2.Value;
            _record.IsCompleted = chkCompleted.Checked;
            _record.WeightType = "Manuel Düzeltildi";

            _db.AdminUpdateWeighing(_record);
            MessageBox.Show("Kayıt yönetici tarafından güncellendi.", "Başarılı");
            this.DialogResult = DialogResult.OK;
        }

        private void DeleteRecord()
        {
            var res = MessageBox.Show("Bu kaydı silmek istediğinizden emin misiniz?", "Onay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res == DialogResult.Yes)
            {
                _db.DeleteWeighing(_record.Id);
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}
