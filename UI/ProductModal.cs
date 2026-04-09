using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FreeKantar.Data;
using FreeKantar.Models;
using FreeKantar.Services;

namespace FreeKantar.UI
{
    public class ProductModal : Form
    {
        private readonly DbService _db;
        private readonly LanguageService _lang;
        private TextBox txtName;
        private TextBox txtCode;
        private Button btnSave;
        private Button btnCancel;
        private DataGridView gridProducts;
        private Product _selectedProduct = null;

        public ProductModal(DbService db, LanguageService lang)
        {
            _db = db;
            _lang = lang;
            InitializeComponent();
            LoadProducts();
        }

        private void InitializeComponent()
        {
            this.Text = _lang.Translate("StockManagement");
            this.Size = new Size(600, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 10);
            this.BackColor = Color.White;

            try {
                if (System.IO.File.Exists("app_icon.png")) {
                    using (var bitmap = new Bitmap("app_icon.png")) {
                        this.Icon = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            } catch { }

            var mainLayout = new TableLayoutPanel { 
                Dock = DockStyle.Fill, 
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220)); // Form
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // List
            this.Controls.Add(mainLayout);

            // 1. Form Panel
            var pnlForm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            mainLayout.Controls.Add(pnlForm, 0, 0);

            var lblName = new Label { Text = _lang.Translate("StockName") + ":", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtName = new TextBox { Location = new Point(20, 45), Width = 540 };
            pnlForm.Controls.Add(lblName);
            pnlForm.Controls.Add(txtName);

            var lblCode = new Label { Text = _lang.Translate("StockCode") + ":", Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtCode = new TextBox { Location = new Point(20, 110), Width = 540 };
            pnlForm.Controls.Add(lblCode);
            pnlForm.Controls.Add(txtCode);

            btnSave = new Button { 
                Text = _lang.Translate("Save"), 
                Location = new Point(20, 155), 
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveOrUpdate();
            pnlForm.Controls.Add(btnSave);

            var btnDeleteForm = new Button { 
                Text = _lang.Translate("Delete"), 
                Location = new Point(140, 155), 
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(239, 68, 68), // Red
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnDeleteForm.FlatAppearance.BorderSize = 0;
            btnDeleteForm.Click += (s, e) => DeleteProduct();
            pnlForm.Controls.Add(btnDeleteForm);

            btnCancel = new Button { 
                Text = _lang.Translate("Clear"), 
                Location = new Point(260, 155), 
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(203, 213, 225),
                FlatStyle = FlatStyle.Flat
            };
            btnCancel.Click += (s, e) => ClearForm();
            pnlForm.Controls.Add(btnCancel);

            // 2. Grid Panel
            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 0, 20, 20) };
            mainLayout.Controls.Add(pnlGrid, 0, 1);

            gridProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                GridColor = Color.FromArgb(226, 232, 240)
            };
            gridProducts.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(5)
            };
            gridProducts.EnableHeadersVisualStyles = false;
            
            // Context Menu for Delete
            var cms = new ContextMenuStrip();
            cms.Items.Add(_lang.Translate("DeleteStock"), null, (s, e) => DeleteProduct());
            gridProducts.ContextMenuStrip = cms;

            gridProducts.CellDoubleClick += (s, e) => SelectProduct();
            pnlGrid.Controls.Add(gridProducts);
        }

        private void LoadProducts()
        {
            gridProducts.DataSource = null;
            gridProducts.DataSource = _db.GetProducts();
            if (gridProducts.Columns.Count > 0)
            {
                gridProducts.Columns["Id"].Visible = false;
                gridProducts.Columns["Name"].HeaderText = _lang.Translate("StockName").ToUpper();
                gridProducts.Columns["Code"].HeaderText = _lang.Translate("StockCode").ToUpper();
            }
        }

        private void SelectProduct()
        {
            if (gridProducts.SelectedRows.Count == 0) return;
            _selectedProduct = (Product)gridProducts.SelectedRows[0].DataBoundItem;
            
            txtName.Text = _selectedProduct.Name;
            txtCode.Text = _selectedProduct.Code;
            btnSave.Text = _lang.Translate("UpdateStock");
            btnSave.BackColor = Color.FromArgb(59, 130, 246); // Blue for update
        }

        private void ClearForm()
        {
            _selectedProduct = null;
            txtName.Clear();
            txtCode.Clear();
            btnSave.Text = _lang.Translate("AddStock");
            btnSave.BackColor = Color.FromArgb(34, 197, 94); // Green for add
        }

        private void SaveOrUpdate()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(_lang.Translate("StockNameRequired"));
                return;
            }

            if (_selectedProduct == null)
            {
                _db.AddProduct(new Product { Name = txtName.Text, Code = txtCode.Text });
                MessageBox.Show(_lang.Translate("StockAdded"));
            }
            else
            {
                _selectedProduct.Name = txtName.Text;
                _selectedProduct.Code = txtCode.Text;
                _db.UpdateProduct(_selectedProduct);
                MessageBox.Show(_lang.Translate("StockUpdated"));
            }

            ClearForm();
            LoadProducts();
            this.DialogResult = DialogResult.OK; // Notify main form to refresh
        }

        private void DeleteProduct()
        {
            if (gridProducts.SelectedRows.Count == 0) return;
            var prod = (Product)gridProducts.SelectedRows[0].DataBoundItem;

            var msg = string.Format(_lang.Translate("StockDeleteConfirm"), prod.Name);
            var result = MessageBox.Show(msg, _lang.Translate("DeleteStock"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    _db.DeleteProduct(prod.Id);
                    LoadProducts();
                    this.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(_lang.Translate("StockDeleteError") + "\nHata: " + ex.Message);
                }
            }
        }
    }
}
