using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace BetKookBridge
{
    public sealed class LogViewerForm : Form
    {
        private TextBox box = null!;
        private Button btnCopy = null!, btnOpenDir = null!, btnExportZip = null!;

        public LogViewerForm()
        {
            Text = "软件日志（只读）";
            Width = 800; Height = 520;
            StartPosition = FormStartPosition.CenterParent;

            box = new TextBox{
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10)
            };

            var panel = new FlowLayoutPanel{ Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(8) };
            btnCopy = new Button{ Text = "复制到剪贴板", Width = 120, Height = 26 };
            btnOpenDir = new Button{ Text = "打开日志目录", Width = 120, Height = 26 };
            btnExportZip = new Button{ Text = "导出 ZIP", Width = 100, Height = 26 };

            btnCopy.Click += (s,e)=> { try { Clipboard.SetText(box.Text); } catch {} };
            btnOpenDir.Click += (s,e)=> { try { System.Diagnostics.Process.Start("explorer.exe", Logger.Dir); } catch {} };
            btnExportZip.Click += (s,e)=> ExportZip();

            panel.Controls.AddRange(new Control[]{ btnCopy, btnOpenDir, btnExportZip });
            Controls.Add(box);
            Controls.Add(panel);

            Load += (s,e)=> RefreshContent();
        }

        private void RefreshContent()
        {
            try
            {
                if (File.Exists(Logger.FilePath))
                {
                    box.Text = File.ReadAllText(Logger.FilePath, Encoding.UTF8);
                    box.SelectionStart = box.TextLength;
                    box.ScrollToCaret();
                }
                else
                {
                    box.Text = "(暂无日志)";
                }
            }
            catch (Exception ex)
            {
                box.Text = "读取日志失败： " + ex.Message;
            }
        }

        private void ExportZip()
        {
            try
            {
                Directory.CreateDirectory(Logger.Dir);
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var zip = System.IO.Path.Combine(desktop, $"BetKookBridge_logs_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(Logger.Dir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
                MessageBox.Show(this, "已导出：" + zip, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
