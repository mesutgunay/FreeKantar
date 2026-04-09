using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using FreeKantar.Data;
using FreeKantar.Services;
using System.IO;

namespace FreeKantar.UI
{
    public class ReceiptSettingsModal : Form
    {
        private readonly DbService _db;
        private readonly LanguageService _lang;
        private ComboBox cbSizes;
        private Button btnSave;

        public ReceiptSettingsModal(DbService db, LanguageService lang)
        {
            _db = db;
            _lang = lang;
            InitializeComponent();
            this.Load += LoadCurrentSetting;
        }

        private void InitializeComponent()
        {
            this.Text = _lang.Translate("ReceiptSettings");
            this.Size = new Size(450, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10);

            try {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("FreeKantar.app_icon.png")) {
                    if (stream != null) this.Icon = Icon.FromHandle(new Bitmap(stream).GetHicon());
                }
            } catch { }

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
            this.Controls.Add(pnl);

            var lbl = new Label { 
                Text = _lang.Translate("SelectReceiptSize"), 
                Dock = DockStyle.Top, 
                Height = 30, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold) 
            };
            pnl.Controls.Add(lbl);

            cbSizes = new ComboBox { 
                Dock = DockStyle.Top, 
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 35 
            };
            
            var sizes = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("Thermal80", _lang.Translate("Thermal80")),
                new KeyValuePair<string, string>("Thermal50", _lang.Translate("Thermal50")),
                new KeyValuePair<string, string>("StandardKantar", _lang.Translate("StandardKantar")),
                new KeyValuePair<string, string>("A5Horizontal", _lang.Translate("A5Horizontal"))
            };
            
            cbSizes.DataSource = sizes;
            cbSizes.DisplayMember = "Value";
            cbSizes.ValueMember = "Key";
            pnl.Controls.Add(cbSizes);

            btnSave = new Button { 
                Text = _lang.Translate("Save"), 
                Dock = DockStyle.Bottom, 
                Height = 45,
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveSetting();
            pnl.Controls.Add(btnSave);
        }

        private void LoadCurrentSetting(object sender, EventArgs e)
        {
            try {
                string current = _db.GetSetting("ReceiptSize", "Thermal80");
                foreach (var item in cbSizes.Items) {
                    if (item is KeyValuePair<string, string> kvp && kvp.Key == current) {
                        cbSizes.SelectedItem = item;
                        break;
                    }
                }
            } catch { }
        }

        private void SaveSetting()
        {
            string selected = (string)cbSizes.SelectedValue;
            _db.SaveSetting("ReceiptSize", selected);
            MessageBox.Show(_lang.Translate("Save") + " " + _lang.Translate("CompletedCheck"));
            this.Close();
        }
    }
}
