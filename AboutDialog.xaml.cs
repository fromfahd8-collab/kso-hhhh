using System.Collections.Generic;
using System.Windows;

namespace KSO
{
    public partial class AboutDialog : Window
    {
        public List<ShortcutItem> Shortcuts { get; } = new()
        {
            new ShortcutItem("Ctrl + Shift + K", "إظهار البرنامج"),
            new ShortcutItem("Ctrl + Shift + Q", "إغلاق البرنامج وحفظ الحالة"),
            new ShortcutItem("Ctrl + Shift + D", "تحميل الرابط من الحافظة"),
            new ShortcutItem("Ctrl + Shift + H", "إخفاء البرنامج من شريط المهام"),
            new ShortcutItem("Enter", "إضافة الرابط من صندوق الإضافة"),
            new ShortcutItem("Drag & Drop", "اسحب ملف أو رابط وأفلته في البرنامج")
        };

        public AboutDialog()
        {
            InitializeComponent();
            lvShortcuts.ItemsSource = Shortcuts; // مهم: ربط الداتا
        }

        public static void ShowDialogWindow(Window owner)
        {
            var dlg = new AboutDialog { Owner = owner };
            dlg.ShowDialog();
        }
    }

    public class ShortcutItem 
    { 
        public string Key { get; set; } 
        public string Description { get; set; } 
        public ShortcutItem(string key, string desc) 
        { 
            Key = key; 
            Description = desc; 
        } 
    }
}