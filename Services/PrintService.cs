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
            
            PrintDocument pd = new PrintDocument();
            // 80mm width = ~315 units. Shortened default height to 500 for more compact preview.
            pd.DefaultPageSettings.PaperSize = new PaperSize("Thermal80mm", 315, 500);
            pd.PrintPage += new PrintPageEventHandler(PrintPageHandler);

            PrintPreviewDialog ppd = new PrintPreviewDialog();
            ppd.Document = pd;
            ppd.Width = 500;
            ppd.Height = 600;
            ppd.ShowDialog();
        }

        private void PrintPageHandler(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            Font titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
            Font headFont = new Font("Segoe UI", 8, FontStyle.Bold);
            Font bodyFont = new Font("Segoe UI", 8);
            Font netFont = new Font("Segoe UI", 13, FontStyle.Bold);
            Pen dashPen = new Pen(Color.Black, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            Pen solidPen = new Pen(Color.Black, 1);

            float y = 10;
            float leftMargin = 10;
            float rightMargin = 10;
            float pageWidth = 315;
            float contentWidth = pageWidth - (leftMargin + rightMargin);

            // 1. Header (Ultra-Compact)
            string company = _db.GetSetting("CompanyName", "FREE KANTAR").ToUpper();
            string scaleNo = _db.GetSetting("ScaleNo", "").ToUpper();
            
            g.DrawString(company, titleFont, Brushes.Black, new RectangleF(0, y, pageWidth, 50), new StringFormat { Alignment = StringAlignment.Center });
            y += 18;
            if (!string.IsNullOrEmpty(scaleNo)) {
                g.DrawString(scaleNo, new Font("Segoe UI", 9, FontStyle.Bold), Brushes.Black, new RectangleF(0, y, pageWidth, 40), new StringFormat { Alignment = StringAlignment.Center });
                y += 18;
            }

            g.DrawLine(solidPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 8;

            // 2. Main Info
            void DrawRow(string label, string value) {
                if (string.IsNullOrEmpty(value)) return;
                g.DrawString(label, headFont, Brushes.Black, leftMargin, y);
                g.DrawString(": " + value, bodyFont, Brushes.Black, leftMargin + 100, y);
                y += 16;
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
            g.DrawString($": {_record.NetWeight:N0} KG", netFont, Brushes.Black, leftMargin + 80, y);
            y += 25;

            g.DrawLine(solidPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 8;

            // 4. Signatures (Highly Compact)
            void DrawSig(string label) {
                g.DrawString(label, headFont, Brushes.Black, leftMargin, y);
                y += 20;
                g.DrawString("__________________________", bodyFont, Brushes.Black, leftMargin, y);
                y += 18;
            }

            DrawSig(_lang.Translate("DriverSignature"));
            DrawSig(_lang.Translate("ReceiverSignature"));

            // 5. Footer
            y += 2;
            g.DrawLine(dashPen, leftMargin, y, leftMargin + contentWidth, y);
            y += 5;
            g.DrawString($"{_lang.Translate("Date")}: {DateTime.Now:dd.MM.yyyy HH:mm}", new Font("Segoe UI", 7), Brushes.Gray, leftMargin, y);
            y += 10;
            g.DrawString("Software by FreeKantar", new Font("Segoe UI", 6), Brushes.Silver, leftMargin, y);
        }
    }
}
