#nullable enable

using System;

using System.Collections.Generic;

using System.Drawing;

using System.IO;

using System.Text;

using System.Threading.Tasks;

using System.Windows.Forms;



namespace BetKookBridge

{

    public sealed class MainForm : Form

    {

        private TextBox txtLogPath  = null!, txtToken   = null!, txtChannel = null!;

        private Button  btnBrowse   = null!, btnSave    = null!, btnDataLink = null!,

                        btnTestText = null!, btnTestCard= null!, btnLogView  = null!;

        private CheckBox           chkTray   = null!;

        private NotifyIcon         tray      = null!;

        private ContextMenuStrip   trayMenu  = null!;

        private StatusStrip        status    = null!;

        private ToolStripStatusLabel statusLog   = null!,

                                     statusUpload= null!,

                                     statusConn  = null!;



        private FileSystemWatcher? fsw;

        private long lastLength;



        private readonly System.Threading.SemaphoreSlim _fswGate = new(1,1);

        private readonly Queue<string> _recent = new();

        private readonly HashSet<string> _recentSet = new();

        private const int RECENT_MAX = 256;



        private AppSettings settings;

        private Translation tr = new Translation();



        public MainForm()

        {

            Text = "BetKookBridge v17";

            StartPosition = FormStartPosition.CenterScreen;

            FormBorderStyle = FormBorderStyle.FixedSingle;

            MaximizeBox = false;

            Width = 640; Height = 250;



            settings = AppSettings.Load();

            settings.UploadEnabled = false;



            InitUI();

            WireEvents();

            _ = InitConnectionStatusAsync();

        }



        private void InitUI()

        {

            int margin = 12, labelW = 84, rowH = 26, gap = 8;

            int left = margin + labelW, w = 480;

            int y = margin;



            var lblLog = new Label{Left=margin,Top=y+5,Width=labelW,Text="Game log"};

            txtLogPath = new TextBox{Left=left,Top=y,Width=w-90,Height=rowH,Text=settings.GameLogPath??""};

            btnBrowse = new Button{Left=left+w-88,Top=y-1,Width=80,Height=rowH,Text="选择..."};

            btnBrowse.Click += (s,e)=>BrowseLog();

            y += rowH + gap;



            var lblToken = new Label{Left=margin,Top=y+5,Width=labelW,Text="KOOK Token"};

            txtToken = new TextBox{Left=left,Top=y,Width=w,Height=rowH,Text=settings.KookToken??""};

            y += rowH + gap;



            var lblChan = new Label{Left=margin,Top=y+5,Width=labelW,Text="频道ID"};

            txtChannel = new TextBox{Left=left,Top=y,Width=w-90,Height=rowH,Text=settings.KookChannelId??""};

            btnSave = new Button{Left=left+w-88,Top=y-1,Width=80,Height=rowH,Text="保存"};

            btnSave.Click += (s,e)=>SaveSettings();

            y += rowH + gap;



            btnTestText = new Button{Left=left,Top=y,Width=120,Height=28,Text="测试文本"};

            btnTestCard = new Button{Left=left+126,Top=y,Width=120,Height=28,Text="测试卡片"};

            btnLogView  = new Button{Left=left+252,Top=y,Width=120,Height=28,Text="软件日志"};

            btnTestText.Click += async (s,e)=> await SendTestTextAsync();

            btnTestCard.Click += async (s,e)=> await SendTestCardAsync();

            btnLogView.Click  += (s,e)=> new LogViewerForm().ShowDialog(this);



            btnDataLink = new Button{Left=left,Top=y+34,Width=200,Height=28,Text="开启 数据链"};

            btnDataLink.Click += async (s,e)=> await ToggleDataLinkAsync();



            chkTray = new CheckBox{Left=margin, Top=y+36, Width=150, Height=24, Text="最小化到托盘", Checked = settings.MinimizeToTray};

            Controls.AddRange(new Control[]{lblLog,txtLogPath,btnBrowse,lblToken,txtToken,lblChan,txtChannel,btnSave,btnTestText,btnTestCard,btnLogView,btnDataLink,chkTray});



            status = new StatusStrip{SizingGrip=false};

            statusLog    = new ToolStripStatusLabel("");

            statusUpload = new ToolStripStatusLabel("");

            statusConn   = new ToolStripStatusLabel("");

            status.Items.Add(statusLog);

            status.Items.Add(new ToolStripStatusLabel(" | "));

            status.Items.Add(statusUpload);

            status.Items.Add(new ToolStripStatusLabel(" | "));

            status.Items.Add(statusConn);

            Controls.Add(status);



            UpdateReadStatus("关闭", false);

            UpdateDataLinkStatus(false);

            UpdateConnStatus(false);

        }



        private void WireEvents()

        {

            tray = new NotifyIcon{Visible=false,Icon=SystemIcons.Application,Text="BetKookBridge"};

            trayMenu = new ContextMenuStrip();

            trayMenu.Items.Add("开启/关闭 数据链", null, async (s,e)=> await ToggleDataLinkAsync());

            trayMenu.Items.Add(new ToolStripSeparator());

            trayMenu.Items.Add("软件日志", null, (s,e)=> new LogViewerForm().ShowDialog(this));

            trayMenu.Items.Add("打开", null, (s,e)=> ShowFromTray());

            trayMenu.Items.Add("退出", null, (s,e)=>{ tray.Visible=false; Application.Exit(); });

            tray.ContextMenuStrip = trayMenu;

            tray.MouseDoubleClick += (s,e)=> ShowFromTray();



            Resize += (s,e)=> { if (WindowState==FormWindowState.Minimized && chkTray.Checked) { tray.Visible=true; Hide(); } };

            FormClosing += (s,e)=> {

                settings.MinimizeToTray = chkTray.Checked;

                SaveSettings();

                if (chkTray.Checked && WindowState!=FormWindowState.Minimized)

                {

                    e.Cancel = true;

                    WindowState = FormWindowState.Minimized;

                    tray.Visible = true;

                    Hide();

                }

                else

                {

                    tray.Visible = false;

                }

            };

        }



        private void ShowFromTray()

        {

            try

            {

                Show();

                WindowState = FormWindowState.Normal;

                ShowInTaskbar = true;

                Activate();

                BringToFront();

                tray.Visible = true;

            }

            catch {}

        }



        private static string CleanAllWhitespace(string? s)

        {

            if (string.IsNullOrEmpty(s)) return string.Empty;

            return s.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "").Replace("\u3000", "").Trim();

        }



        private void SaveSettings()

        {

            settings.GameLogPath   = (txtLogPath.Text ?? string.Empty).Trim();

            settings.KookToken     = CleanAllWhitespace(txtToken.Text);

            settings.KookChannelId = CleanAllWhitespace(txtChannel.Text);

            settings.MinimizeToTray= chkTray.Checked;

            settings.Save();

            Logger.Log($"[Settings] 已保存，target_id={settings.KookChannelId}");

        }



        private void BrowseLog()

        {

            using var ofd = new OpenFileDialog { Title="选择 Game.log", Filter="日志 (*.log;*.txt)|*.log;*.txt|所有文件 (*.*)|*.*" };

            if (ofd.ShowDialog(this)==DialogResult.OK) txtLogPath.Text = ofd.FileName;

        }



        private void UpdateLabel(ToolStripStatusLabel lab, bool green, string text)

        {

            lab.ForeColor = green ? Color.Green : Color.Red;

            lab.Text = (green ? "■ " : "■ ") + text;

        }

        private void UpdateReadStatus(string text, bool green) => UpdateLabel(statusLog, green, $"GameLog读取状态：{text}");

        private void UpdateDataLinkStatus(bool on) => UpdateLabel(statusUpload, on, on ? "数据链状态：开启" : "数据链状态：关闭");

        private void UpdateConnStatus(bool online) => UpdateLabel(statusConn, online, online ? "数据链连接：在线" : "数据链连接：离线");



        private async Task InitConnectionStatusAsync()

        {

            var ok = await TestKookAsync();

            UpdateConnStatus(ok);

        }



        private async Task<bool> TestKookAsync()

        {

            try

            {

                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };

                using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.kookapp.cn/api/v3/user/me");

                req.Headers.TryAddWithoutValidation("Authorization", $"Bot {settings.KookToken}");

                using var resp = await http.SendAsync(req);

                return resp.IsSuccessStatusCode;

            }

            catch { return false; }

        }



        private async Task ToggleDataLinkAsync()

        {

            SaveSettings();



            if (!settings.UploadEnabled)

            {

                try

                {

                    if (fsw == null)

                    {

                        if (string.IsNullOrWhiteSpace(settings.GameLogPath) || !File.Exists(settings.GameLogPath))

                        {

                            Logger.Log("[DataLink] 无效的 Game.log 路径");

                            UpdateReadStatus("异常", false);

                            return;

                        }



                        fsw = new FileSystemWatcher(Path.GetDirectoryName(settings.GameLogPath)!)

                        {

                            Filter = Path.GetFileName(settings.GameLogPath),

                            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size

                        };

                        fsw.Changed += async (s, e) => await OnFileChanged(settings.GameLogPath!);

                        fsw.EnableRaisingEvents = true;



                        UpdateReadStatus("接通", true);

                    }



                    settings.UploadEnabled = true;

                    settings.Save();

                    btnDataLink.Text = "关闭 数据链";

                    UpdateDataLinkStatus(true);

                    Logger.Log("[DataLink] 已开启（读取+上传）");



                    // 开启时做一次全量发送

                    lastLength = 0;

                    await OnFileChanged(settings.GameLogPath!);



                    var ok = await TestKookAsync();

                    UpdateConnStatus(ok);

                    if (!ok) Logger.Log("[DataLink] KOOK 连通性：离线");

                }

                catch (Exception ex)

                {

                    Logger.Log("开启 数据链 异常：" + ex);

                    settings.UploadEnabled = false;

                    settings.Save();

                    btnDataLink.Text = "开启 数据链";

                    UpdateDataLinkStatus(false);

                    UpdateReadStatus("异常", false);

                }

            }

            else

            {

                try

                {

                    settings.UploadEnabled = false;

                    settings.Save();

                    btnDataLink.Text = "开启 数据链";

                    UpdateDataLinkStatus(false);



                    if (fsw != null)

                    {

                        fsw.Dispose();

                        fsw = null;

                        UpdateReadStatus("关闭", false);

                    }



                    Logger.Log("[DataLink] 已关闭（读取+上传）");

                }

                catch (Exception ex)

                {

                    Logger.Log("关闭 数据链 异常：" + ex);

                    UpdateReadStatus("异常", false);

                }

            }

        }



        private static bool IsNpc(string? name)

        {

            if (string.IsNullOrEmpty(name)) return true;

            var v = name.ToLowerInvariant();

            return v.StartsWith("vlk_") || v.StartsWith("npc_") || v.StartsWith("ai_") || v.Contains("juvenile") || v.Contains("irradiated");

        }



        private async Task OnFileChanged(string path)

        {

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))

            {

                Logger.Log("[OnFileChanged] 路径无效或文件不存在");

                UpdateReadStatus("异常", false);

                return;

            }



            await _fswGate.WaitAsync();

            try

            {

                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);



                if (fs.Length < lastLength)

                    lastLength = 0;



                fs.Seek(lastLength, SeekOrigin.Begin);

                using var sr = new StreamReader(fs, Encoding.UTF8, true);

                while (!sr.EndOfStream)

                {

                    var line = await sr.ReadLineAsync();

                    if (line is null) break;



                    var info = LogParser.TryParse(line);

                    if (info is null) continue;



                    // NPC 过滤

                    if (info.LogType == LogType.ActorDeath)

                    {

                        if (IsNpc(info.Handle) && IsNpc(info.Key)) continue;

                    }

                    else if (info.LogType == LogType.HostilityEvent)

                    {

                        if (IsNpc(info.Key) && (string.IsNullOrEmpty(info.Handle) || IsNpc(info.Handle))) continue;

                    }



                    string sig = $"{info.LogType}|{info.Utc:O}|{info.Handle}|{info.Key}|{info.Value}";

                    if (_recentSet.Contains(sig)) continue;

                    _recent.Enqueue(sig);

                    _recentSet.Add(sig);

                    while (_recent.Count > RECENT_MAX) { var old = _recent.Dequeue(); _recentSet.Remove(old); }



                    bool shouldSend = info.LogType == LogType.ActorDeath || info.LogType == LogType.HostilityEvent;

                    if (shouldSend && settings.UploadEnabled)

                        await PushKookWebhookAsync(info, tr);

                }

                lastLength = fs.Length;

            }

            catch (Exception ex)

            {

                Logger.Log("OnFileChanged 异常：" + ex);

                UpdateReadStatus("异常", false);

            }

            finally

            {

                _fswGate.Release();

            }

        }



        private static string ToChinaTimeString(DateTime utc)

        {

            if (utc == DateTime.MinValue) return "";

            try

            {

                var tz = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");

                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);

                return local.ToString("yyyy年MM月dd日HH时mm分ss秒");

            }

            catch

            {

                var local = utc.ToLocalTime();

                return local.ToString("yyyy年MM月dd日HH时mm分ss秒");

            }

        }



        private async Task PushKookWebhookAsync(LogMonitorInfo info, Translation tr)

        {

            try

            {

                var cards = new List<object>();

                string timeCN = ToChinaTimeString(info.Utc);



                if (info.LogType == LogType.ActorDeath)

                {

                    var m = LogParser.MatchActorDeathInfo(info.Value);

                    string usingText = "", typeText = "", zoneText = "";

                    if (m.Success)

                    {

                        usingText = m.Groups["Using"].Value ?? "";

                        typeText  = m.Groups["Type"].Value ?? "";

                        zoneText  = m.Groups["Zone"].Value ?? "";

                    }



                    bool isSuicide =

                        (!string.IsNullOrEmpty(typeText) && typeText.Equals("Suicide", StringComparison.OrdinalIgnoreCase))

                        || string.Equals(info.Handle, info.Key, StringComparison.Ordinal);



                    if (isSuicide)

                    {

                        cards.Add(new {

                            type = "card",

                            theme = "secondary",

                            size = "lg",

                            modules = new object[] {

                                new { type="header", text = new { type="plain-text", content = "Suicide 角色自杀" } },

                                new { type="section", text = new { type="kmarkdown", content = $"**[{info.Handle}](https://robertsspaceindustries.com/en/citizens/{info.Handle})**" } },

                                new { type="section", text = new { type="kmarkdown", content = $"**{tr.Log_Monitor.Webhook_Zone}**：{zoneText}" } },

                                new { type="section", text = new { type="kmarkdown", content = $"时间：**{timeCN}**" } }

                            }

                        });

                    }

                    else

                    {

                        cards.Add(new {

                            type = "card",

                            theme = "secondary",

                            size = "lg",

                            modules = new object[] {

                                new { type="header", text = new { type="plain-text", content = tr.Log_Monitor.Webhook_Actor_Death } },

                                new { type="section", text = new { type="kmarkdown", content = $"**[{info.Handle}](https://robertsspaceindustries.com/en/citizens/{info.Handle})**" } }

                            }

                        });



                        var modules2 = new List<object>{

                            new { type="header", text = new { type="plain-text", content = tr.Log_Monitor.Webhook_Killer } },

                            new { type="section", text = new { type="kmarkdown", content = $"**[{info.Key}](https://robertsspaceindustries.com/en/citizens/{info.Key})**" } }

                        };



                        if (!string.IsNullOrEmpty(usingText))

                            modules2.Add(new { type="section", text = new { type="kmarkdown", content = $"**{tr.Log_Monitor.Webhook_Using}**：{usingText}" } });

                        if (!string.IsNullOrEmpty(typeText))

                            modules2.Add(new { type="section", text = new { type="kmarkdown", content = $"**{tr.Log_Monitor.Webhook_Damage_Type}**：{typeText}" } });

                        if (!string.IsNullOrEmpty(zoneText))

                            modules2.Add(new { type="section", text = new { type="kmarkdown", content = $"**{tr.Log_Monitor.Webhook_Zone}**：{zoneText}" } });



                        modules2.Add(new { type="section", text = new { type="kmarkdown", content = $"时间：**{timeCN}**" } });



                        cards.Add(new { type="card", theme="secondary", size="lg", modules = modules2.ToArray() });

                    }

                }

                else if (info.LogType == LogType.HostilityEvent)

                {

                    cards.Add(new {

                        type="card",

                        theme="secondary",

                        size="lg",

                        modules = new object[] {

                            new { type="header", text = new { type="plain-text", content = tr.Log_Monitor.Webhook_Hostility_Event } },

                            new { type="section", text = new { type="kmarkdown", content = $"**[{info.Handle}](https://robertsspaceindustries.com/en/citizens/{info.Handle})**" } },

                            new { type="section", text = new { type="kmarkdown", content = $"**{tr.Log_Monitor.Webhook_Hostility_Event_Ship}**：{info.Value}" } }

                        }

                    });



                    cards.Add(new {

                        type="card",

                        theme="secondary",

                        size="lg",

                        modules = new object[] {

                            new { type="header", text = new { type="plain-text", content = tr.Log_Monitor.Webhook_Hostility_Event_Attacker } },

                            new { type="section", text = new { type="kmarkdown", content = $"**[{info.Key}](https://robertsspaceindustries.com/en/citizens/{info.Key})**" } },

                            new { type="section", text = new { type="kmarkdown", content = $"时间：**{timeCN}**" } }

                        }

                    });

                }

                else

                {

                    return;

                }



                Logger.Log($"[KOOK] 即将发送，target_id={settings.KookChannelId}");

                var (ok, body) = await KookClient.SendCardsAsync(settings.KookToken!, settings.KookChannelId!, cards);

                if (!ok) { UpdateConnStatus(false); Logger.Log("KOOK 发送失败：" + body); }

                else UpdateConnStatus(true);

            }

            catch (Exception ex)

            {

                Logger.Log("PushKookWebhook 异常：" + ex);

                UpdateConnStatus(false);

            }

        }



        private async Task SendTestTextAsync()

        {

            SaveSettings();

            var okConn = await TestKookAsync();

            UpdateConnStatus(okConn);

            if (!okConn) return;



            var cards = new List<object>{

                new {

                    type="card",

                    theme="secondary",

                    size="lg",

                    modules = new object[]{

                        new { type="header", text = new { type="plain-text", content = "连接测试 / Test" } },

                        new { type="section", text = new { type="kmarkdown", content = "来自 BetKookBridge v17" } }

                    }

                }

            };

            Logger.Log($"[KOOK] 即将发送，target_id={settings.KookChannelId}");

            var (ok, body) = await KookClient.SendCardsAsync(settings.KookToken!, settings.KookChannelId!, cards);

            if (!ok) Logger.Log("测试发送失败：" + body);

            UpdateConnStatus(ok);

        }



        private async Task SendTestCardAsync()

        {

            SaveSettings();

            var okConn = await TestKookAsync();

            UpdateConnStatus(okConn);

            if (!okConn) return;



            var info = new LogMonitorInfo{

                LogType = LogType.ActorDeath,

                Handle = "Test_Victim_123",

                Key    = "Test_Killer_999",

                Utc    = DateTime.UtcNow,

                Value  = "Using: behr_rifle_ballistic_01\r\nZone: pyro1\r\nDamage Type: Bullet"

            };

            await PushKookWebhookAsync(info, tr);

        }

    }

}

