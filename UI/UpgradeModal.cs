using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using FreeKantar.Services;

namespace FreeKantar.UI
{
    public class UpgradeModal : Form
    {
        private readonly LanguageService _lang;

        public UpgradeModal(LanguageService lang)
        {
            _lang = lang;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Otonom Kantar - " + _lang.Translate("ProFeatures").Replace(":", "");
            this.Size = new Size(500, 520);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10);
            

            try {
                if (System.IO.File.Exists("app_icon.png")) {
                    using (var bitmap = new Bitmap("app_icon.png")) {
                        this.Icon = Icon.FromHandle(bitmap.GetHicon());
                    }
                }
            } catch { }

            var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(20) };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            this.Controls.Add(mainLayout);

            var lblTitle = new Label { 
                Text = "🚀 " + _lang.Translate("UpgradeTitle"), 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter, 
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 197, 94)
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            var txtInfo = new Label {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                Text = _lang.Translate("UpgradeInfo") + "\n\n" +
                       "🚀 " + _lang.Translate("ProFeatures") + "\n" +
                       "🔗 " + _lang.Translate("Feature1") + "\n" +
                       "🤖 " + _lang.Translate("Feature2") + "\n" +
                       "📄 " + _lang.Translate("Feature3") + "\n" +
                       "📱 " + _lang.Translate("Feature4") + "\n" +
                       "📍 " + _lang.Translate("Feature5") + "\n" +
                       "🛠️ " + _lang.Translate("Feature6") + "\n\n" +
                       "✨ " + _lang.Translate("Digitalize"),
                Padding = new Padding(0, 10, 0, 0)
            };
            mainLayout.Controls.Add(txtInfo, 0, 1);

            var contactPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(10) };
            mainLayout.Controls.Add(contactPanel, 0, 2);

            var lblOtonom = new Label { 
                Text = "Otonom Kantar Sistemleri", 
                Location = new Point(10, 10), 
                AutoSize = true, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold) 
            };
            contactPanel.Controls.Add(lblOtonom);

            var btnWhatsapp = new Button {
                Text = "💬 " + _lang.Translate("WhatsappContact") + ": +90 533 545 16 80",
                Location = new Point(10, 40),
                Size = new Size(420, 40),
                BackColor = Color.FromArgb(37, 211, 102),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnWhatsapp.FlatAppearance.BorderSize = 0;
            btnWhatsapp.Click += (s, e) => {
                try {
                    Process.Start(new ProcessStartInfo { FileName = "https://wa.me/905335451680", UseShellExecute = true });
                } catch { }
            };
            contactPanel.Controls.Add(btnWhatsapp);

            var lblEmail = new Label { 
                Text = "E-mail: mesut@otonomkantar.com", 
                Location = new Point(10, 85), 
                AutoSize = true, 
                ForeColor = Color.Gray 
            };
            contactPanel.Controls.Add(lblEmail);
        }
    }
}
