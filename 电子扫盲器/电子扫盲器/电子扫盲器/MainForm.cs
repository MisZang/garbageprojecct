/*
 * 由SharpDevelop创建。
 * 用户： Administrator
 * 日期: 2024/6/5
 * 时间: 19:07
 * 
 * 要改变这种模板请点击 工具|选项|代码编写|编辑标准头文件
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Drawing;
using System.Reflection;

namespace 电子扫盲器
{
    public partial class MainForm : Form
    {
        private string basePath;
        private WebClient steamWebClient;
        private WebClient epicWebClient;
        private WebClient urlWebClient;
        private bool isSteamDownloading = false;
        private bool isEpicDownloading = false;
        private bool isUrlDownloading = false;

        public MainForm()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            InitializeComponent();
            InitializeProgram();
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            return null;
        }

        private void InitializeProgram()
        {
            DetectLargestDiskAndCreateFolders();
            LoadInstallationStatus();
            InitializeLabels();
            comboBox1.SelectedIndexChanged += ComboBox1SelectedIndexChanged;
            button1.Click += Button1Click;
            button2.Click += Button2Click;
            button3.Click += Button3Click;
            button4.Click += Button4Click;
            button5.Click += Button5Click;

            // 设置label6支持拖放操作
            label6.AllowDrop = true;
            label6.DragEnter += new DragEventHandler(Label6DragEnter);
            label6.DragDrop += new DragEventHandler(Label6DragDrop);

            // 设置textBox1提示信息
            textBox1.Text = "暂不支持网盘和磁力链接！";
            textBox1.GotFocus += RemoveText;
            textBox1.LostFocus += AddText;
        }

        private void RemoveText(object sender, EventArgs e)
        {
            if (textBox1.Text == "暂不支持网盘和磁力链接！" || textBox1.Text == "Cloud and magnetic links are not supported!")
            {
                textBox1.Text = "";
                textBox1.ForeColor = Color.Black;
            }
        }

        private void AddText(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                if (comboBox1.SelectedItem.ToString() == "中文")
                {
                    textBox1.Text = "暂不支持网盘和磁力链接！";
                }
                else
                {
                    textBox1.Text = "Cloud and magnetic links are not supported!";
                }
                textBox1.ForeColor = Color.Gray;
            }
        }

        private void InitializeLabels()
        {
            label1.Text = "Languages";
            comboBox1.Items.Add("中文");
            comboBox1.Items.Add("English");
            comboBox1.SelectedIndex = 0; // 默认选择中文
            label2.Text = "下载 0%";
            label3.Text = "下载 0%";
            label4.Text = "下载 0%";
            label5.Text = "解压：";
            label6.Text = "将需要解压的压缩文件拖到这里";
            label6.BorderStyle = BorderStyle.FixedSingle; // 设置边框样式
            label6.TextAlign = ContentAlignment.MiddleCenter; // 文本居中
            button3.Text = "下载";
            button4.Text = "选择压缩文件";
            button5.Text = "打开 games 文件夹";
        }

        private void DetectLargestDiskAndCreateFolders()
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            DriveInfo largestDrive = null;
            foreach (var drive in allDrives)
            {
                if (drive.IsReady && drive.DriveType == DriveType.Fixed && drive.Name != "C:\\")
                {
                    if (largestDrive == null || drive.TotalFreeSpace > largestDrive.TotalFreeSpace)
                    {
                        largestDrive = drive;
                    }
                }
            }

            if (largestDrive == null)
            {
                DialogResult result = MessageBox.Show("没有找到除C盘外的其他磁盘。是否将Steam和Epic安装在C盘？", "磁盘检测", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    basePath = "C:\\games";
                }
                else
                {
                    return;
                }
            }
            else
            {
                basePath = Path.Combine(largestDrive.Name, "games");
            }

            Directory.CreateDirectory(basePath);
            Directory.CreateDirectory(Path.Combine(basePath, "steamgames"));
            Directory.CreateDirectory(Path.Combine(basePath, "epicgames"));
            Directory.CreateDirectory(Path.Combine(basePath, "downloads"));
            Directory.CreateDirectory(Path.Combine(basePath, "compress"));
        }

        private void LoadInstallationStatus()
        {
            if (Directory.Exists(Path.Combine(basePath, "steamgames", "Steam")))
            {
                button1.Text = "卸载steam";
            }
            else
            {
                button1.Text = "下载steam";
            }

            if (Directory.Exists(Path.Combine(basePath, "epicgames", "EpicGamesLauncher")))
            {
                button2.Text = "卸载epic";
            }
            else
            {
                button2.Text = "下载epic";
            }
        }

        private void ComboBox1SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedItem.ToString() == "English")
            {
                ChangeLanguage("en");
            }
            else
            {
                ChangeLanguage("zh");
            }
        }

        private void ChangeLanguage(string lang)
        {
            if (lang == "en")
            {
                label1.Text = "Languages";
                button1.Text = button1.Text == "下载steam" ? "Install steam" : button1.Text == "卸载steam" ? "Uninstall steam" : button1.Text;
                button2.Text = button2.Text == "下载epic" ? "Install epic" : button2.Text == "卸载epic" ? "Uninstall epic" : button2.Text;
                button3.Text = button3.Text == "下载" ? "Install" : button3.Text == "暂停" ? "Pause" : button3.Text;
                button4.Text = "Select";
                button5.Text = "Open Games";
                label5.Text = "Decompress:";
                label6.Text = "Drag compressed file here";
                label2.Text = "Install 0%";
                label3.Text = "Installs 0%";
                label4.Text = "Install 0%";
                if (textBox1.Text == "暂不支持网盘和磁力链接！")
                {
                    textBox1.Text = "Cloud and magnetic links are not supported!";
                    textBox1.ForeColor = Color.Gray;
                }
            }
            else
            {
                label1.Text = "语言";
                button1.Text = button1.Text == "Install steam" ? "下载steam" : button1.Text == "Uninstall steam" ? "卸载steam" : button1.Text;
                button2.Text = button2.Text == "Install epic" ? "下载epic" : button2.Text == "Uninstall epic" ? "卸载epic" : button2.Text;
                button3.Text = button3.Text == "Install" ? "下载" : button3.Text == "Pause" ? "暂停" : button3.Text;
                button4.Text = "选择压缩文件";
                button5.Text = "打开游戏文件夹";
                label5.Text = "解压：";
                label6.Text = "将需要解压的压缩文件拖到这里";
                label2.Text = "下载 0%";
                label3.Text = "下载 0%";
                label4.Text = "下载 0%";
                if (textBox1.Text == "Cloud and magnetic links are not supported!")
                {
                    textBox1.Text = "暂不支持网盘和磁力链接！";
                    textBox1.ForeColor = Color.Gray;
                }
            }
        }

        private void Button1Click(object sender, EventArgs e)
        {
            if (button1.Text == "下载steam" || button1.Text == "Install steam")
            {
                StartDownload("steam", "https://steamcdn-a.akamaihd.net/client/installer/SteamSetup.exe", progressBar1, label2);
            }
            else if (button1.Text == "暂停" || button1.Text == "Pause")
            {
                PauseDownload(steamWebClient);
            }
            else if (button1.Text == "继续下载" || button1.Text == "Resume")
            {
                ResumeDownload("steam", "https://steamcdn-a.akamaihd.net/client/installer/SteamSetup.exe", progressBar1, label2);
            }
            else if (button1.Text == "卸载" || button1.Text == "Uninstall")
            {
                Uninstall("steamgames");
            }
        }

        private void Button2Click(object sender, EventArgs e)
        {
            if (button2.Text == "下载epic" || button2.Text == "Install epic")
            {
                StartDownload("epic", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", progressBar2, label3);
            }
            else if (button2.Text == "暂停" || button2.Text == "Pause")
            {
                PauseDownload(epicWebClient);
            }
            else if (button2.Text == "继续下载" || button2.Text == "Resume")
            {
                ResumeDownload("epic", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi", progressBar2, label3);
            }
            else if (button2.Text == "卸载" || button2.Text == "Uninstall")
            {
                Uninstall("epicgames");
            }
        }

        private void Button3Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text) || textBox1.Text == "暂不支持网盘和磁力链接！" || textBox1.Text == "Cloud and magnetic links are not supported!")
            {
                MessageBox.Show("请输入有效的下载链接。");
                return;
            }

            if (button3.Text == "下载" || button3.Text == "Install")
            {
                StartDownload("url", textBox1.Text, progressBar3, label4);
            }
            else if (button3.Text == "暂停" || button3.Text == "Pause")
            {
                PauseDownload(urlWebClient);
            }
            else if (button3.Text == "继续下载" || button3.Text == "Resume")
            {
                ResumeDownload("url", textBox1.Text, progressBar3, label4);
            }
        }

        private void Button4Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "压缩文件|*.zip;*.7z;*.rar";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;
                string extractPath = Path.Combine(basePath, "compress");
                ExtractCompressedFile(filePath, extractPath);
                DialogResult result = MessageBox.Show("解压完成，是否打开解压后的文件夹？", "解压完成", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", extractPath);
                }
            }
        }

        private void Button5Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", basePath);
        }

        private void StartDownload(string type, string url, ProgressBar progressBar, Label label)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) =>
            {
                progressBar.Value = e.ProgressPercentage;
                label.Text = string.Format("下载进度 {0}%", e.ProgressPercentage);
            };
            webClient.DownloadFileCompleted += (s, e) =>
            {
                if (type == "steam")
                {
                    Install(type, "SteamSetup.exe", Path.Combine(basePath, "steamgames"));
                    button1.Text = "卸载";
                }
                else if (type == "epic")
                {
                    Install(type, "EpicGamesLauncherInstaller.msi", Path.Combine(basePath, "epicgames"));
                    button2.Text = "卸载";
                }
                else if (type == "url")
                {
                    DialogResult result = MessageBox.Show("下载完成，是否打开下载文件夹？", "下载完成", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        Process.Start("explorer.exe", Path.Combine(basePath, "downloads"));
                    }
                    button3.Text = "下载";
                    isUrlDownloading = false;
                }
            };

            string downloadPath = Path.Combine(basePath, "downloads", Path.GetFileName(url));
            webClient.DownloadFileAsync(new Uri(url), downloadPath);

            if (type == "steam")
            {
                steamWebClient = webClient;
                isSteamDownloading = true;
                button1.Text = "暂停";
            }
            else if (type == "epic")
            {
                epicWebClient = webClient;
                isEpicDownloading = true;
                button2.Text = "暂停";
            }
            else if (type == "url")
            {
                urlWebClient = webClient;
                isUrlDownloading = true;
                button3.Text = "暂停";
            }
        }

        private void PauseDownload(WebClient webClient)
        {
            if (webClient != null)
            {
                webClient.CancelAsync();
                if (webClient == steamWebClient)
                {
                    button1.Text = "继续下载";
                }
                else if (webClient == epicWebClient)
                {
                    button2.Text = "继续下载";
                }
                else if (webClient == urlWebClient)
                {
                    button3.Text = "继续下载";
                }
            }
        }

        private void ResumeDownload(string type, string url, ProgressBar progressBar, Label label)
        {
            StartDownload(type, url, progressBar, label);
        }

        private void Install(string type, string setupFile, string installPath)
        {
            string setupPath = Path.Combine(basePath, "downloads", setupFile);
            Process installer = new Process();
            installer.StartInfo.FileName = setupPath;

            if (type == "steam")
            {
                installer.StartInfo.Arguments = "/S";
            }
            else if (type == "epic")
            {
                installer.StartInfo.Arguments = "/quiet";
            }

            installer.StartInfo.UseShellExecute = true;
            installer.Start();
            installer.WaitForExit();

            // 确认安装文件夹是否存在
            if (Directory.Exists(installPath))
            {
                DialogResult result = MessageBox.Show("安装完成，是否打开安装文件夹？", "安装完成", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", installPath);
                }
            }
            else
            {
                MessageBox.Show("安装失败，请检查安装路径。");
            }
        }

        private void Uninstall(string folder)
        {
            string folderPath = Path.Combine(basePath, folder);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
                MessageBox.Show(folder + " 已成功卸载。", "卸载完成");
                if (folder == "steamgames")
                {
                    button1.Text = "下载";
                }
                else if (folder == "epicgames")
                {
                    button2.Text = "下载";
                }
            }
        }

        private void ExtractCompressedFile(string filePath, string extractPath)
        {
            using (var archive = ArchiveFactory.Open(filePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(extractPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }
            }
        }

        private void Label6DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Label6DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string filePath in files)
            {
                string extractPath = Path.Combine(basePath, "compress");
                ExtractCompressedFile(filePath, extractPath);
                DialogResult result = MessageBox.Show("解压完成，是否打开解压后的文件夹？", "解压完成", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Process.Start("explorer.exe", extractPath);
                }
            }
        }
    }
}
