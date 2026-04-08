using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FreeKantar.Services
{
    public class LanguageService
    {
        public enum Language { TR, EN, DE, BG, RU, AR, RO, SR, SQ, ES }
        public Language CurrentLanguage { get; set; } = Language.TR;

        private Dictionary<string, Dictionary<string, string>> _allTranslations;

        public LanguageService()
        {
            _allTranslations = new Dictionary<string, Dictionary<string, string>>();
            LoadTranslations();
        }

        public string Translate(string key)
        {
            string langCode = CurrentLanguage.ToString();
            
            if (_allTranslations.ContainsKey(langCode) && _allTranslations[langCode].ContainsKey(key))
                return _allTranslations[langCode][key];
            
            // Fallback to EN if key not found in current language
            if (_allTranslations.ContainsKey("EN") && _allTranslations["EN"].ContainsKey(key))
                return _allTranslations["EN"][key];

            return key;
        }

        private void LoadTranslations()
        {
            string localesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Locales");
            
            if (Directory.Exists(localesPath))
            {
                foreach (var file in Directory.GetFiles(localesPath, "*.json"))
                {
                    try
                    {
                        string langCode = Path.GetFileNameWithoutExtension(file).ToUpper();
                        string jsonContent = File.ReadAllText(file);
                        var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                        if (data != null)
                        {
                            _allTranslations[langCode] = data;
                        }
                    }
                    catch { /* Skip corrupted files */ }
                }
                
                if (_allTranslations.Count > 0) return;
            }

            // Bootstrap defaults if Locales folder is missing or empty
            _allTranslations["TR"] = new Dictionary<string, string> {
                { "AppTitle", "Free kantar v2.11" },
                { "LiveWeighing", "ANLIK TARTIM VERİSİ" },
                { "Plate", "PLAKA" },
                { "GetWeight", "TARTIM AL" },
                { "Settings", "AYARLAR" },
                { "Language", "Dil / Language" }
            };

            _allTranslations["EN"] = new Dictionary<string, string> {
                { "AppTitle", "Free Weighing v2.11" },
                { "LiveWeighing", "LIVE WEIGHT DATA" },
                { "Plate", "LICENSE PLATE" },
                { "GetWeight", "TAKE WEIGHT" },
                { "Settings", "SETTINGS" },
                { "Language", "Language" }
            };
        }

        public void TranslateControl(System.Windows.Forms.Control parent)
        {
            if (parent.Tag != null && parent.Tag is string key)
                parent.Text = Translate(key);

            if (parent is System.Windows.Forms.MenuStrip ms)
            {
                foreach (System.Windows.Forms.ToolStripItem item in ms.Items) TranslateMenuItem(item);
            }

            foreach (System.Windows.Forms.Control child in parent.Controls)
            {
                TranslateControl(child);
            }
        }

        private void TranslateMenuItem(System.Windows.Forms.ToolStripItem item)
        {
            if (item.Tag != null && item.Tag is string key)
                item.Text = Translate(key);

            if (item is System.Windows.Forms.ToolStripMenuItem tsmi && tsmi.HasDropDownItems)
            {
                foreach (System.Windows.Forms.ToolStripItem sub in tsmi.DropDownItems) TranslateMenuItem(sub);
            }
        }
    }
}
