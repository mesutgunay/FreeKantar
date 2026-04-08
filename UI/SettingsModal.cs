using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using FreeKantar.Data;
using FreeKantar.Services;

namespace FreeKantar.UI
{
    public class SettingsModal : Form
    {
        private readonly DbService _db;
        private readonly LanguageService _lang;
        private ComboBox cbPorts;
        private ComboBox cbBaudRate;
        private TextBox txtCompanyName;
        private TextBox txtScaleNo;
        private Button btnSave;

        public SettingsModal(DbService db, LanguageService lang)
        {
            _db = db;
            _lang = lang;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Tag = "KantarSettings";
            this.Size = new Size(400, 480);
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

            var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown };
            this.Controls.Add(layout);

            void AddLabel(string tag) {
                layout.Controls.Add(new Label { Tag = tag, Width = 340, Font = new Font("Segoe UI", 9, FontStyle.Bold), Margin = new Padding(0, 10, 0, 0) });
            }

            // Company & Scale
            AddLabel("CompanyName");
            txtCompanyName = new TextBox { Width = 340, Text = _db.GetSetting("CompanyName", "") };
            layout.Controls.Add(txtCompanyName);

            AddLabel("ScaleNo");
            txtScaleNo = new TextBox { Width = 340, Text = _db.GetSetting("ScaleNo", "") };
            layout.Controls.Add(txtScaleNo);

            // Serial Port
            AddLabel("ComPort");
            cbPorts = new ComboBox { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            cbPorts.Items.AddRange(SerialPort.GetPortNames());
            string currentPort = _db.GetSetting("ComPort", "COM1");
            if (cbPorts.Items.Contains(currentPort)) cbPorts.SelectedItem = currentPort;
            else if (cbPorts.Items.Count > 0) cbPorts.SelectedIndex = 0;
            layout.Controls.Add(cbPorts);

            AddLabel("BaudRate");
            cbBaudRate = new ComboBox { Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
            cbBaudRate.Items.AddRange(new object[] { 2400, 4800, 9600, 19200, 38400, 57600, 115200 });
            cbBaudRate.SelectedItem = int.Parse(_db.GetSetting("BaudRate", "9600"));
            layout.Controls.Add(cbBaudRate);

            btnSave = new Button { 
                Tag = "Save", 
                Width = 340, Height = 45, 
                BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 30, 0, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.Click += (s, e) => {
                _db.SaveSetting("CompanyName", txtCompanyName.Text);
                _db.SaveSetting("ScaleNo", txtScaleNo.Text);
                if (cbPorts.SelectedItem != null) _db.SaveSetting("ComPort", cbPorts.SelectedItem.ToString());
                if (cbBaudRate.SelectedItem != null) _db.SaveSetting("BaudRate", cbBaudRate.SelectedItem.ToString());
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            layout.Controls.Add(btnSave);

            _lang.TranslateControl(this);
        }
    }
}
