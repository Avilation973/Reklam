using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PusulaPcBakim
{
    internal sealed class Feature
    {
        public string Category;
        public string Name;
        public string Target;
        public Feature(string category, string name, string target)
        { Category = category; Name = name; Target = target; }
    }

    internal sealed class MemoryStatus
    {
        public uint Length = (uint)Marshal.SizeOf(typeof(MemoryStatus));
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    internal sealed class MainForm : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);

        private readonly Color bg = Color.FromArgb(15, 20, 29);
        private readonly Color panel = Color.FromArgb(25, 34, 47);
        private readonly Color muted = Color.FromArgb(152, 166, 185);
        private readonly Color accent = Color.FromArgb(85, 214, 190);
        private readonly List<Feature> features = new List<Feature>();
        private readonly ListView list = new ListView();
        private readonly TextBox search = new TextBox();
        private readonly ComboBox category = new ComboBox();
        private readonly Label status = new Label();
        private readonly Label systemInfo = new Label();
        private readonly Label freeSpace = new Label();
        private readonly Label ramInfo = new Label();

        public MainForm()
        {
            Text = "Pusula PC Bakım";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 660);
            Size = new Size(1180, 780);
            BackColor = bg;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            BuildFeatures();
            BuildUi();
            RefreshSummary();
            FilterFeatures();
        }

        private void BuildUi()
        {
            Panel header = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = bg, Padding = new Padding(28, 20, 28, 10) };
            Controls.Add(header);
            Label eyebrow = new Label { Text = "PUSULA  /  PC BAKIM", AutoSize = true, ForeColor = accent, Font = new Font("Segoe UI", 9F, FontStyle.Bold), Location = new Point(28, 14) };
            header.Controls.Add(eyebrow);
            Label title = new Label { Text = "Bilgisayarını güvenle toparla.", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 22F, FontStyle.Bold), Location = new Point(28, 34) };
            header.Controls.Add(title);
            Label subtitle = new Label { Text = "120+ Windows aracı ve bakım kısayolu. Her işlem senin kontrolünde.", AutoSize = true, ForeColor = muted, Location = new Point(31, 78) };
            header.Controls.Add(subtitle);

            Panel cards = new Panel { Dock = DockStyle.Top, Height = 104, BackColor = bg, Padding = new Padding(28, 0, 28, 12) };
            Controls.Add(cards);
            cards.Controls.Add(MakeCard("WINDOWS", systemInfo, 0));
            cards.Controls.Add(MakeCard("BOŞ ALAN", freeSpace, 1));
            cards.Controls.Add(MakeCard("BELLEK", ramInfo, 2));

            Panel tools = new Panel { Dock = DockStyle.Fill, BackColor = bg, Padding = new Padding(28, 6, 28, 10) };
            Controls.Add(tools);
            Label toolsTitle = new Label { Text = "Araç kutusu", AutoSize = true, Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(28, 5) };
            tools.Controls.Add(toolsTitle);
            Label count = new Label { Text = "120+ araç", AutoSize = true, ForeColor = accent, Location = new Point(135, 11) };
            tools.Controls.Add(count);

            search.Location = new Point(28, 42);
            search.Width = 310;
            search.Height = 28;
            search.Font = new Font("Segoe UI", 10F);
            search.ForeColor = Color.White;
            search.BackColor = panel;
            search.BorderStyle = BorderStyle.FixedSingle;
            search.TextChanged += delegate { FilterFeatures(); };
            tools.Controls.Add(search);

            category.Location = new Point(350, 42);
            category.Width = 240;
            category.DropDownStyle = ComboBoxStyle.DropDownList;
            category.BackColor = panel;
            category.ForeColor = Color.White;
            category.Font = new Font("Segoe UI", 10F);
            category.Items.Add("Tüm kategoriler");
            foreach (string group in CategoryNames()) category.Items.Add(group);
            category.SelectedIndex = 0;
            category.SelectedIndexChanged += delegate { FilterFeatures(); };
            tools.Controls.Add(category);

            Button clean = MakeButton("Eski geçici dosyaları temizle", 610, 40, 240);
            clean.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clean.Click += delegate { CleanTemporaryFiles(); };
            tools.Controls.Add(clean);
            Button dns = MakeButton("DNS önbelleğini yenile", 860, 40, 220);
            dns.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dns.Click += delegate { FlushDns(); };
            tools.Controls.Add(dns);

            list.Dock = DockStyle.Bottom;
            list.Height = 440;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = false;
            list.HideSelection = false;
            list.MultiSelect = false;
            list.BackColor = panel;
            list.ForeColor = Color.White;
            list.BorderStyle = BorderStyle.None;
            list.Font = new Font("Segoe UI", 9.5F);
            list.Columns.Add("ARAÇ", 330);
            list.Columns.Add("KATEGORİ", 220);
            list.Columns.Add("AÇILACAK ÖZELLİK", 490);
            list.DoubleClick += delegate { OpenSelectedFeature(); };
            tools.Controls.Add(list);

            status.Dock = DockStyle.Bottom;
            status.Height = 30;
            status.Padding = new Padding(28, 6, 0, 0);
            status.ForeColor = muted;
            status.BackColor = bg;
            status.Text = "Hazır · Windows ayarları ve yönetim araçları açılır; sistemde değişiklik yapmadan önce onay alınır.";
            Controls.Add(status);
        }

        private Panel MakeCard(string heading, Label value, int index)
        {
            Panel p = new Panel { BackColor = panel, Size = new Size(340, 76), Location = new Point(28 + index * 352, 8), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            Label h = new Label { Text = heading, AutoSize = true, ForeColor = muted, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Location = new Point(14, 11) };
            value.Text = "Bilgi alınıyor…";
            value.AutoSize = false;
            value.Size = new Size(310, 26);
            value.Location = new Point(14, 37);
            value.ForeColor = Color.White;
            value.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            p.Controls.Add(h); p.Controls.Add(value);
            return p;
        }

        private Button MakeButton(string text, int x, int y, int width)
        {
            Button b = new Button { Text = text, Location = new Point(x, y), Size = new Size(width, 32), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.FromArgb(10, 27, 29), Font = new Font("Segoe UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private string[] CategoryNames()
        {
            return new string[] { "Ekran ve kişiselleştirme", "Ağ ve bağlantı", "Gizlilik ve izinler", "Uygulamalar", "Güncelleme ve güvenlik", "Sistem ve güç", "Sorun giderme", "Yönetim araçları", "Depolama ve dosyalar", "Hesaplar ve erişilebilirlik" };
        }

        private void AddGroup(string group, string[] rows)
        {
            foreach (string row in rows)
            {
                string[] parts = row.Split(new char[] { '|' }, 2);
                features.Add(new Feature(group, parts[0], parts[1]));
            }
        }

        private void BuildFeatures()
        {
            AddGroup("Ekran ve kişiselleştirme", new string[] {
                "Ekran ayarları|ms-settings:display", "Gece ışığı|ms-settings:nightlight", "Gelişmiş ekran|ms-settings:display-advanced",
                "Renkler|ms-settings:colors", "Temalar|ms-settings:themes", "Kilit ekranı|ms-settings:lockscreen",
                "Başlat menüsü|ms-settings:personalization-start", "Görev çubuğu|ms-settings:taskbar", "Yazı tipleri|ms-settings:fonts",
                "Arka plan|ms-settings:personalization-background", "Dokunmatik yüzey|ms-settings:devices-touchpad", "Grafik ayarları|ms-settings:display-advancedgraphics"
            });
            AddGroup("Ağ ve bağlantı", new string[] {
                "Ağ durumu|ms-settings:network-status", "Wi-Fi|ms-settings:network-wifi", "Ethernet|ms-settings:network-ethernet",
                "VPN|ms-settings:network-vpn", "Proxy|ms-settings:network-proxy", "Mobil erişim noktası|ms-settings:network-mobilehotspot",
                "Çevirmeli bağlantı|ms-settings:network-dialup", "Gelişmiş ağ ayarları|ms-settings:network-advancedsettings",
                "Bilinen ağları yönet|ms-settings:network-wifisettings", "Bluetooth|ms-settings:bluetooth", "Cihazlar|ms-settings:connecteddevices",
                "İnternet seçenekleri|inetcpl.cpl"
            });
            AddGroup("Gizlilik ve izinler", new string[] {
                "Gizlilik ana sayfası|ms-settings:privacy", "Konum izni|ms-settings:privacy-location", "Kamera izni|ms-settings:privacy-webcam",
                "Mikrofon izni|ms-settings:privacy-microphone", "Bildirim izni|ms-settings:privacy-notifications", "Hesap bilgileri izni|ms-settings:privacy-accountinfo",
                "Kişiler izni|ms-settings:privacy-contacts", "Takvim izni|ms-settings:privacy-calendar", "Arama geçmişi izni|ms-settings:privacy-callhistory",
                "E-posta izni|ms-settings:privacy-email", "Belgeler izni|ms-settings:privacy-documents", "Dosya sistemi izni|ms-settings:privacy-broadfilesystemaccess"
            });
            AddGroup("Uygulamalar", new string[] {
                "Yüklü uygulamalar|ms-settings:appsfeatures", "Varsayılan uygulamalar|ms-settings:defaultapps", "Başlangıç uygulamaları|ms-settings:startupapps",
                "İsteğe bağlı özellikler|ms-settings:optionalfeatures", "Uygulama yürütme takma adları|ms-settings:appsfeatures-app", "Video oynatma|ms-settings:videoplayback",
                "Çevrimdışı haritalar|ms-settings:maps", "Web siteleri için uygulamalar|ms-settings:appsforwebsites", "Uygulama arşivleme|ms-settings:apparchive",
                "Program kaldırma|appwiz.cpl", "Yazıcılar|ms-settings:printers", "Fare özellikleri|main.cpl"
            });
            AddGroup("Güncelleme ve güvenlik", new string[] {
                "Windows Update|ms-settings:windowsupdate", "Güncelleme geçmişi|ms-settings:windowsupdate-history", "Gelişmiş güncelleme seçenekleri|ms-settings:windowsupdate-options",
                "Kurtarma|ms-settings:recovery", "Etkinleştirme|ms-settings:activation", "Windows Güvenliği|windowsdefender:",
                "Güvenlik duvarı|firewall.cpl", "Yedekleme|ms-settings:backup", "Sorun giderme|ms-settings:troubleshoot",
                "Cihaz şifreleme|ms-settings:deviceencryption", "Geliştiriciler için|ms-settings:developers", "Hakkında ve cihaz kimliği|ms-settings:about"
            });
            AddGroup("Sistem ve güç", new string[] {
                "Sistem bilgisi|msinfo32.exe", "Hakkında|ms-settings:about", "Güç ve pil|ms-settings:powersleep",
                "Pil kullanımı|ms-settings:batterysaver-usagedetails", "Depolama|ms-settings:storage", "Pano|ms-settings:clipboard",
                "Çoklu görev|ms-settings:multitasking", "Uzaktan masaüstü|ms-settings:remotedesktop", "Sistem özellikleri|sysdm.cpl",
                "Güç seçenekleri|powercfg.cpl", "Ekran koruyucu|control.exe desk.cpl,,@screensaver", "Ortam değişkenleri|SystemPropertiesAdvanced.exe"
            });
            AddGroup("Sorun giderme", new string[] {
                "Görev Yöneticisi|taskmgr.exe", "Kaynak İzleyicisi|resmon.exe", "Performans İzleyicisi|perfmon.exe",
                "Olay Görüntüleyicisi|eventvwr.msc", "Aygıt Yöneticisi|devmgmt.msc", "Disk Yönetimi|diskmgmt.msc",
                "Hizmetler|services.msc", "Güvenilirlik geçmişi|perfmon.exe /rel", "DirectX tanılama|dxdiag.exe",
                "Bellek tanılama|mdsched.exe", "Ağ bağlantıları|ncpa.cpl", "Ses sorunları|ms-settings:sound"
            });
            AddGroup("Yönetim araçları", new string[] {
                "Bilgisayar yönetimi|compmgmt.msc", "Görev Zamanlayıcı|taskschd.msc", "Yerel güvenlik ilkesi|secpol.msc",
                "Yerel kullanıcılar ve gruplar|lusrmgr.msc", "Bileşen hizmetleri|comexp.msc", "WMI denetimi|wmimgmt.msc",
                "Sertifikalar|certmgr.msc", "Windows Defender Güvenlik Duvarı|wf.msc", "Sistem yapılandırması|msconfig.exe",
                "Kayıt defteri düzenleyicisi|regedit.exe", "Denetim Masası|control.exe", "Komut İstemi|cmd.exe"
            });
            AddGroup("Depolama ve dosyalar", new string[] {
                "Depolama algısı|ms-settings:storagepolicies", "Geçici dosyalar|ms-settings:storagesense", "Disk temizleme|cleanmgr.exe",
                "Sürücüleri en iyi duruma getir|dfrgui.exe", "Dosya Geçmişi|control.exe /name Microsoft.FileHistory", "Yedekleme ve geri yükleme|control.exe /name Microsoft.BackupAndRestore",
                "Geri Dönüşüm Kutusu|shell:RecycleBinFolder", "İndirmeler klasörü|shell:Downloads", "Belgeler klasörü|shell:Personal",
                "Dosya Gezgini seçenekleri|control.exe folders", "Paylaşım seçenekleri|control.exe /name Microsoft.NetworkAndSharingCenter", "Bölümler|diskmgmt.msc"
            });
            AddGroup("Hesaplar ve erişilebilirlik", new string[] {
                "Hesap bilgileri|ms-settings:yourinfo", "Oturum açma seçenekleri|ms-settings:signinoptions", "Diğer kullanıcılar|ms-settings:otherusers",
                "İş veya okul erişimi|ms-settings:workplace", "Erişilebilirlik ana sayfası|ms-settings:easeofaccess",
                "Büyüteç|ms-settings:easeofaccess-magnifier", "Ekran okuyucusu|ms-settings:easeofaccess-narrator",
                "Altyazılar|ms-settings:easeofaccess-closedcaptioning", "Yüksek karşıtlık|ms-settings:easeofaccess-highcontrast",
                "Klavye erişilebilirliği|ms-settings:easeofaccess-keyboard", "Fare işaretçisi|ms-settings:easeofaccess-mouse",
                "Dil ve bölge|ms-settings:regionlanguage"
            });
        }

        private void FilterFeatures()
        {
            if (list.Columns.Count == 0) return;
            string q = search.Text.Trim().ToLowerInvariant();
            string selected = category.SelectedItem == null ? "Tüm kategoriler" : category.SelectedItem.ToString();
            list.BeginUpdate();
            list.Items.Clear();
            foreach (Feature f in features)
            {
                if (selected != "Tüm kategoriler" && f.Category != selected) continue;
                if (q.Length > 0 && !f.Name.ToLowerInvariant().Contains(q) && !f.Category.ToLowerInvariant().Contains(q)) continue;
                ListViewItem item = new ListViewItem(f.Name);
                item.SubItems.Add(f.Category);
                item.SubItems.Add(f.Target);
                item.Tag = f;
                list.Items.Add(item);
            }
            list.EndUpdate();
            status.Text = list.Items.Count.ToString() + " araç listeleniyor · Açmak için çift tıkla.";
        }

        private void OpenSelectedFeature()
        {
            if (list.SelectedItems.Count == 0) return;
            Feature f = list.SelectedItems[0].Tag as Feature;
            if (f == null) return;
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = f.Target;
                start.UseShellExecute = true;
                Process.Start(start);
                status.Text = f.Name + " açılıyor…";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bu araç Windows sürümünde bulunamadı veya açılamadı.\r\n\r\n" + ex.Message,
                    "Pusula PC Bakım", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RefreshSummary()
        {
            systemInfo.Text = Environment.OSVersion.VersionString;
            try
            {
                DriveInfo d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                freeSpace.Text = (d.AvailableFreeSpace / 1073741824.0).ToString("0.0") + " GB boş · " + d.Name;
            }
            catch { freeSpace.Text = "Bilgi alınamadı"; }
            try
            {
                MemoryStatus m = new MemoryStatus();
                ramInfo.Text = GlobalMemoryStatusEx(ref m) ? (m.AvailablePhysical / 1073741824.0).ToString("0.0") + " GB kullanılabilir" : "Bilgi alınamadı";
            }
            catch { ramInfo.Text = "Bilgi alınamadı"; }
        }

        private void CleanTemporaryFiles()
        {
            string temp = Path.GetFullPath(Path.GetTempPath());
            DateTime cutoff = DateTime.Now.AddDays(-7);
            int count = 0;
            try
            {
                Stack<string> pending = new Stack<string>();
                pending.Push(temp);
                while (pending.Count > 0)
                {
                    string current = pending.Pop();
                    try
                    {
                        foreach (string dir in Directory.GetDirectories(current))
                        {
                            try
                            {
                                if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0) pending.Push(dir);
                            }
                            catch { }
                        }
                        foreach (string file in Directory.GetFiles(current))
                        {
                            try
                            {
                                FileInfo info = new FileInfo(file);
                                if ((info.Attributes & FileAttributes.ReparsePoint) == 0 && info.LastWriteTime < cutoff) count++;
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            if (count == 0)
            {
                MessageBox.Show("7 günden eski temizlenebilir dosya bulunamadı.", "Pusula PC Bakım", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("Kullanıcı geçici klasöründe " + count + " eski dosya bulundu.\r\nSadece 7 günden eski dosyalar silinir. Devam edilsin mi?",
                "Temizlik onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            int removed = 0, skipped = 0;
            Stack<string> work = new Stack<string>();
            work.Push(temp);
            while (work.Count > 0)
            {
                string current = work.Pop();
                try
                {
                    foreach (string dir in Directory.GetDirectories(current))
                    {
                        try { if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) == 0) work.Push(dir); }
                        catch { }
                    }
                    foreach (string file in Directory.GetFiles(current))
                    {
                        try
                        {
                            FileInfo info = new FileInfo(file);
                            if ((info.Attributes & FileAttributes.ReparsePoint) == 0 && info.LastWriteTime < cutoff)
                            { info.Delete(); removed++; }
                        }
                        catch { skipped++; }
                    }
                }
                catch { skipped++; }
            }
            status.Text = "Temizlik tamamlandı · " + removed + " dosya silindi · " + skipped + " atlandı.";
            RefreshSummary();
            MessageBox.Show("Silinen: " + removed + "\r\nAtlanan: " + skipped, "Temizlik tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FlushDns()
        {
            if (MessageBox.Show("DNS önbelleği temizlensin mi? Bu işlem ağ ayarlarını değiştirmez.",
                "DNS yenileme onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                Process p = Process.Start(new ProcessStartInfo("ipconfig.exe", "/flushdns") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true });
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);
                if (String.IsNullOrEmpty(output)) output = p.StandardError.ReadToEnd();
                status.Text = p.ExitCode == 0 ? "DNS önbelleği yenilendi." : "DNS işlemi tamamlanamadı; yönetici izni gerekebilir.";
                MessageBox.Show(String.IsNullOrEmpty(output) ? status.Text : output, "DNS önbelleği", MessageBoxButtons.OK,
                    p.ExitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("DNS işlemi başlatılamadı: " + ex.Message, "Pusula PC Bakım", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
