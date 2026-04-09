using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using FreeKantar.Models;
using FreeKantar.Data;

namespace FreeKantar.Services
{
    public class PrintService
    {
        private WeighingRecord _record;
        private readonly LanguageService _lang;
        private readonly DbService _db;

        public PrintService(DbService db, LanguageService lang)
        {
            _db = db;
            _lang = lang;
        }

        public void PrintReceipt(WeighingRecord record)
        {
            _record = record;
            string sizeKey = _db.GetSetting("ReceiptSize", "Thermal80");
            
            PrintDocument pd = new PrintDocument();
            
            // Define paper sizes (100 DPI units)
            // 80mm = ~315, 50mm = ~197, 11x24cm = ~433x945, A5 = ~827x583
            switch (sizeKey)
            {
                case "Thermal50":
                    pd.DefaultPageSettings.PaperSize = new PaperSize("Thermal50mm", 197, 500);
                    break;
                case "StandardKantar":
                    pd.DefaultPageSettings.PaperSize = new PaperSize("Standard11x24", 433, 945);
                    break;
                case "A5Horizontal":
                    pd.DefaultPageSettings.PaperSize = new PaperSize("A5Horizontal", 827, 583);
                    pd.DefaultPageSettings.Landscape = true;
                    break;
                default: // Thermal80
                    pd.DefaultPageSettings.PaperSize = new PaperSize("Thermal80mm", 315, 500);
                    break;
            }

            pd.PrintPage += new PrintPageEventHandler(PrintPageHandler);

            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = pd;
            ppd.Width = 600;
            ppd.Height = 800;
            ppd.ShowDialog();
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            string sizeKey = _db.GetSetting("ReceiptSize", "Thermal80");
            Graphics g = e.Graphics;
            
            // Dynamic sizing
            float pageWidth = e.PageSettings.PaperSize.Width;
            float leftMargin = 10;
            float rightMargin = 10;
            
            // Adjust fonts based on width
            float baseSize = (sizeKey == "Thermal50") ? 7f : 8f;
            if (sizeKey == "A5Horizontal") baseSize = 10f;
            
            Font titleFont = new Font("Segoe UI", baseSize + 3, FontStyle.Bold);
            Font headFont = new Font("Segoe UI", baseSize, FontStyle.Bold);
            Font bodyFont = new Font("Segoe UI", baseSize);
            Font netFont = new Font("Segoe UI", baseSize + 5, FontStyle.Bold);
            
            Pen dashPen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen solidPen = new Pen(Color.Black, 1);

            float y = 10;
            float contentWidth = pageWidth - (leftMargin + rightMargin);

            // 1. Header
            string company = _db.GetSetting("CompanyName", "FREE KANTAR").ToUpper();
            string scaleNo = _db.GetSetting("ScaleNo", "").ToUpper();
            
            g.DrawString(company, titleFont, Brushes.Black, new RectangleF(0, y, pageWidth, 50), new StringFormat { Alignment = StringAlignment.Center });
            y += (baseSize + 10);
            
            if (!string.IsNullOrEmpty(scaleNo)) {
                g.DrawString(scaleNo, new Font("Segoe UI", baseSize + 1, FontStyle.Bold), Brushes.Black, new RectangleF(0, y, pageWidth, 40), new StringFormat { Alignment = StringAlignment.Center });
                y += (baseSize + 10);
            }

            g.DrawLine(solidPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 8;

            // 2. Main Info
            float labelWidth = (sizeKey == "Thermal50") ? 70 : 100;
            if (sizeKey == "A5Horizontal") labelWidth = 150;

            void DrawRow(string label, string value) {
                if (string.IsNullOrEmpty(value)) return;
                g.DrawString(label, headFont, Brushes.Black, leftMargin, y);
                g.DrawString(": " + value, bodyFont, Brushes.Black, leftMargin + labelWidth, y);
                y += (baseSize + 8);
            }

            DrawRow(_lang.Translate("Plate"), _record.Plate);
            DrawRow(_lang.Translate("Product"), _record.ProductName);
            DrawRow(_lang.Translate("DriverName"), $"{_record.DriverName} {_record.DriverSurname}");
            if (!string.IsNullOrEmpty(_record.DriverPhone))
                DrawRow(_lang.Translate("DriverPhone"), _record.DriverPhone);
            DrawRow(_lang.Translate("TransactionType"), _record.TransactionType);
            
            if (!string.IsNullOrEmpty(_record.Destination))
                DrawRow(_lang.Translate("Destination"), _record.Destination);

            y += 4;
            g.DrawLine(dashPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 8;

            // 3. Weights
            if (_record.TransactionType == "İADE + İLAVE SEVK") {
                DrawRow(_lang.Translate("FirstWeight"), $"{_record.FirstWeight:N0}");
                if (_record.SecondWeight.HasValue) DrawRow(_lang.Translate("SecondWeight"), $"{_record.SecondWeight:N0}");
                if (_record.ThirdWeight.HasValue) DrawRow(_lang.Translate("ThirdWeight"), $"{_record.ThirdWeight:N0}");
            } else {
                DrawRow(_lang.Translate("FirstWeight"), $"{_record.FirstWeight:N0}");
                if (_record.IsCompleted) DrawRow(_lang.Translate("SecondWeight"), $"{_record.SecondWeight:N0}");
            }

            y += 4;
            g.DrawString(_lang.Translate("Net").ToUpper(), netFont, Brushes.Black, leftMargin, y);
            g.DrawString($": {_record.NetWeight:N0} KG", netFont, Brushes.Black, leftMargin + labelWidth, y);
            y += (baseSize + 15);

            g.DrawLine(solidPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 8;

            // 4. Signatures
            void DrawSig(string label) {
                g.DrawString(label, headFont, Brushes.Black, leftMargin, y);
                y += (baseSize + 12);
                g.DrawString("________________________________", bodyFont, Brushes.Black, leftMargin, y);
                y += (baseSize + 10);
            }

            if (sizeKey == "A5Horizontal" || sizeKey == "StandardKantar") {
                // Side-by-side for wide formats could be added here, but keep single column for simplicity first
                DrawSig(_lang.Translate("DriverSignature"));
                DrawSig(_lang.Translate("ReceiverSignature"));
            } else {
                DrawSig(_lang.Translate("DriverSignature"));
                DrawSig(_lang.Translate("ReceiverSignature"));
            }

            // 5. Footer
            y += 2;
            g.DrawLine(dashPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 5;
            g.DrawString($"{_lang.Translate("Date")}: {DateTime.Now:dd.MM.yyyy HH:mm}", new Font("Segoe UI", baseSize - 1), Brushes.Gray, leftMargin, y);
            y += (baseSize + 2);
            g.DrawString("Software by FreeKantar", new Font("Segoe UI", baseSize - 2), Brushes.Silver, leftMargin, y);
        }
    }
}
