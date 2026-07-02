using System.Text.Json;
namespace VertiMask;
internal sealed class ControlForm : Form
{
    private readonly ComboBox _monitorBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _ratioBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _toggle = new();
    private readonly Label _zoneLabel = new();
    private readonly Button _copyBtn = new();
    private readonly Button _photoBtn = new();
    private readonly Button _photoTimerBtn = new();
    private readonly Button _gifBtn = new();
    private readonly Button _videoBtn = new();
    private readonly Button _camBtn = new();
    private readonly Button _camDefaultBtn = new();
    private readonly Button _filterBtn = new();
    private readonly Button _openFolderBtn = new();
    private readonly Label _captureStatus = new();
    private readonly ComboBox _cameraBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _shapeBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _mirrorCheck = new();
    private readonly CheckBox _camOnlyCheck = new();
    private readonly CheckBox _simpleKeysCheck = new();
    private readonly CheckBox _audioSystemCheck = new() { Checked = true };
    private readonly CheckBox _audioMicCheck = new() { Checked = true };
    private readonly Button _audioSettingsBtn = new();
    private readonly CheckBox _videoCountdownCheck = new() { Checked = true };
    private readonly Panel _scrollPanel = new() { Dock = DockStyle.Fill, AutoScroll = true };
    private readonly ToolTip _tip = new() { InitialDelay = 600, AutoPopDelay = 8000 };
    private bool _videoCountingDown;
    private float _sysGain = 1f;
    private float _micGain = 1f;
    private bool _micGate;
    private float _gateThreshold = 0.02f;
    private readonly GifRecorder _gif = new();
    private readonly VideoRecorder _video = new();
    private WebcamReader? _cam;
    private CamForm? _camForm;
    private BackdropForm? _backdrop;
    private CamCloseButton? _camClose;
    private RecBadge? _recBadge;
    private readonly ComboBox _scriptBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _teleBtn = new();
    private readonly Button _scriptsFolderBtn = new();
    private readonly Button _voiceBtn = new();
    private readonly CheckBox _azureVoiceCheck = new();
    private TeleprompterForm? _teleForm;
    private float _teleSpeed = 45f;
    private float _teleFont = 30f;
    private float _teleAnchor = 0.16f;
    private CameraFilter.Params _filterParams = CameraFilter.GetPreset(CameraFilter.Preset.Aucun);
    private double _camWidthFrac;
    private double _camCenterXFrac;
    private double _camCenterYFrac;
    private bool _loading = true;
    private readonly WindowArranger _arranger = new();
    private readonly Taskbar _taskbar = new();
    private Blackout? _blackout;
    private string _lastMargins = "";
    private readonly System.Windows.Forms.Timer _enforceTimer = new() { Interval = 200 };
    private const int HOTKEY_ID = 0xA17;
    private const int HOTKEY_PHOTO = 0xA18;
    private const int HOTKEY_VIDEO = 0xA19;
    private const int HOTKEY_GIF = 0xA1A;
    private const int HOTKEY_CAM = 0xA1B;
    private const int HOTKEY_TELE = 0xA1C;
    private const int HOTKEY_TELE_UP = 0xA1D;
    private const int HOTKEY_TELE_DOWN = 0xA1E;
    private const int HOTKEY_TELE_LEFT = 0xA1F;
    private const int HOTKEY_TELE_RIGHT = 0xA20;
    private const int HOTKEY_S = 0xA21;
    private const int HOTKEY_R = 0xA22;
    private const int HOTKEY_G = 0xA23;
    private const int HOTKEY_ARROW_UP = 0xA24;
    private const int HOTKEY_ARROW_DOWN = 0xA25;
    private const int HOTKEY_ARROW_LEFT = 0xA26;
    private const int HOTKEY_ARROW_RIGHT = 0xA27;
    private readonly string _prefsPath = Path.Combine(AppContext.BaseDirectory, "vertimask.json");
    private readonly (string Name, int W, int H)[] _ratios =
    {
        ("9:16  (Reels / TikTok / Shorts)", 9, 16),
        ("4:5  (Instagram portrait)",       4, 5),
        ("1:1  (carre)",                    1, 1),
    };
    public ControlForm()
    {
        Text = "VertiMask";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(494, 680);
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        _scrollPanel.BackColor = Color.FromArgb(32, 32, 36);
        Controls.Add(_scrollPanel);
        BuildUi();
        PopulateMonitors();
        RefreshCameraList();
        RefreshScriptList();
        LoadPrefs();
        _loading = false;
        UpdateZoneLabel();
        SetToggleVisual(false);
        ZoneCapture.PurgeOldTrash();
        _enforceTimer.Tick += (_, _) => { if (IsActive) _arranger.EnforceZone(ZoneScreen()); };
    }
    private bool IsActive => _blackout != null;
    private void BuildUi()
    {
        var sc = _scrollPanel.Controls;
        const int x = 18, w = 434;
        int y = 16;
        sc.Add(new Label
        {
            Text = "VertiMask : mode vertical pour capture",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = Color.White,
            Location = new Point(x, y),
        });
        y += 42;
        sc.Add(MakeSectionHeader("Écran et format", x, y));
        sc.Add(MakeSep(x, y + 20, w));
        y += 30;
        sc.Add(MakeLabel("Écran cible :", x, y));
        y += 22;
        _monitorBox.SetBounds(x, y, w, 26);
        _monitorBox.SelectedIndexChanged += OnSelectionChanged;
        sc.Add(_monitorBox);
        y += 38;
        sc.Add(MakeLabel("Format :", x, y));
        y += 22;
        _ratioBox.SetBounds(x, y, w, 26);
        foreach (var r in _ratios) _ratioBox.Items.Add(r.Name);
        _ratioBox.SelectedIndex = 0;
        _ratioBox.SelectedIndexChanged += OnSelectionChanged;
        sc.Add(_ratioBox);
        y += 42;
        _toggle.SetBounds(x, y, w, 46);
        _toggle.FlatStyle = FlatStyle.Flat;
        _toggle.FlatAppearance.BorderSize = 0;
        _toggle.Font = new Font("Segoe UI Semibold", 11f);
        _toggle.Click += (_, _) => Toggle();
        sc.Add(_toggle);
        y += 54;
        sc.Add(new Label
        {
            Text = "En mode vertical, cliquer dans le noir remet tout en place.",
            AutoSize = false,
            ForeColor = Color.FromArgb(115, 115, 130),
            Location = new Point(x, y),
            Size = new Size(w, 16),
            Font = new Font("Segoe UI", 8.5f),
        });
        y += 22;
        sc.Add(MakeLabel("Zone de capture (à régler une seule fois) :", x, y));
        y += 24;
        _zoneLabel.SetBounds(x, y, w, 74);
        _zoneLabel.ForeColor = Color.FromArgb(120, 220, 160);
        sc.Add(_zoneLabel);
        y += 80;
        _copyBtn.SetBounds(x, y, 204, 28);
        _copyBtn.Text = "Copier les marges OBS";
        _copyBtn.FlatStyle = FlatStyle.Flat;
        _copyBtn.FlatAppearance.BorderColor = Color.Gray;
        _copyBtn.ForeColor = Color.Gainsboro;
        _copyBtn.Click += (_, _) => CopyMargins();
        sc.Add(_copyBtn);
        _tip.SetToolTip(_copyBtn, "Copie les valeurs dans le presse-papier pour OBS : Filtre > Rogner/Compléter.");
        sc.Add(new Label
        {
            Text = "Bascule : Ctrl + Alt + V",
            AutoSize = true,
            ForeColor = Color.FromArgb(115, 115, 130),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(x + 218, y + 8),
        });
        y += 46;
        sc.Add(MakeSectionHeader("Capture", x, y));
        sc.Add(MakeSep(x, y + 20, w));
        y += 30;
        sc.Add(new Label
        {
            Text = "Raccourcis globaux : Ctrl+Alt+S photo, R vidéo, G gif, C caméra",
            AutoSize = false,
            ForeColor = Color.FromArgb(115, 115, 130),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(x, y),
            Size = new Size(w, 16),
        });
        y += 22;
        const int cw = 213, gap = 8;
        int col2 = x + cw + gap;
        _cameraBox.SetBounds(x, y, cw, 26);
        _cameraBox.DropDown += (_, _) => RefreshCameraList();
        sc.Add(_cameraBox);
        _shapeBox.SetBounds(col2, y, cw, 26);
        _shapeBox.Items.AddRange(new object[] { "Forme : rectangle", "Forme : arrondi", "Forme : cercle" });
        _shapeBox.SelectedIndex = 0;
        _shapeBox.SelectedIndexChanged += (_, _) =>
        {
            _camForm?.SetShape((CamForm.Shape)_shapeBox.SelectedIndex);
            SavePrefs();
        };
        sc.Add(_shapeBox);
        y += 32;
        _cameraBox.SelectedIndexChanged += (_, _) => SavePrefs();
        _mirrorCheck.Text = "Miroir (selfie)";
        _mirrorCheck.ForeColor = Color.Gainsboro;
        _mirrorCheck.AutoSize = true;
        _mirrorCheck.SetBounds(x, y, cw, 22);
        _mirrorCheck.CheckedChanged += (_, _) =>
        {
            if (_camForm != null) _camForm.Mirror = _mirrorCheck.Checked;
            SavePrefs();
        };
        sc.Add(_mirrorCheck);
        _camOnlyCheck.Text = "Cam seule (fond noir)";
        _camOnlyCheck.ForeColor = Color.Gainsboro;
        _camOnlyCheck.AutoSize = true;
        _camOnlyCheck.SetBounds(col2, y, cw, 22);
        _camOnlyCheck.CheckedChanged += (_, _) => SetCamOnly(_camOnlyCheck.Checked);
        sc.Add(_camOnlyCheck);
        y += 30;
        StyleCaptureButton(_photoBtn, "Photo");
        _photoBtn.SetBounds(x, y, cw, 36);
        _photoBtn.Click += (_, _) => DoPhoto();
        sc.Add(_photoBtn);
        StyleCaptureButton(_photoTimerBtn, "Photo (3 s)");
        _photoTimerBtn.SetBounds(col2, y, cw, 36);
        _photoTimerBtn.Click += (_, _) => DoPhotoTimer(3);
        sc.Add(_photoTimerBtn);
        y += 44;
        StyleCaptureButton(_gifBtn, "GIF : démarrer");
        _gifBtn.SetBounds(x, y, cw, 36);
        _gifBtn.Click += (_, _) => ToggleGif();
        sc.Add(_gifBtn);
        StyleCaptureButton(_videoBtn, "Vidéo : démarrer");
        _videoBtn.SetBounds(col2, y, cw, 36);
        _videoBtn.Click += (_, _) => ToggleVideo();
        sc.Add(_videoBtn);
        y += 44;
        StyleCaptureButton(_camBtn, "Caméra : ouvrir");
        _camBtn.SetBounds(x, y, cw, 36);
        _camBtn.Click += (_, _) => ToggleCamera();
        sc.Add(_camBtn);
        StyleCaptureButton(_openFolderBtn, "Ouvrir le dossier");
        _openFolderBtn.SetBounds(col2, y, cw, 36);
        _openFolderBtn.Click += (_, _) => ZoneCapture.OpenFolder();
        sc.Add(_openFolderBtn);
        y += 44;
        StyleCaptureButton(_camDefaultBtn, "Cam : taille/position par défaut");
        _camDefaultBtn.SetBounds(x, y, cw, 30);
        _camDefaultBtn.Click += (_, _) => ResetCamSize();
        sc.Add(_camDefaultBtn);
        StyleCaptureButton(_filterBtn, "Filtres caméra...");
        _filterBtn.SetBounds(col2, y, cw, 30);
        _filterBtn.Click += (_, _) => OpenCamFilters();
        sc.Add(_filterBtn);
        _tip.SetToolTip(_filterBtn, "Luminosité, contraste, saturation, lissage peau.");
        y += 38;
        _simpleKeysCheck.Text = "Raccourcis simples : S photo / R vidéo / G gif, sans Ctrl+Alt (actifs uniquement en mode vertical)";
        _simpleKeysCheck.ForeColor = Color.Gainsboro;
        _simpleKeysCheck.AutoSize = false;
        _simpleKeysCheck.SetBounds(x, y, w, 34);
        _simpleKeysCheck.CheckedChanged += (_, _) =>
        {
            ApplySimpleKeys();
            SetStatus(_simpleKeysCheck.Checked
                ? "Raccourcis simples ON : S=photo, R=vidéo, G=gif (actifs seulement en mode vertical)."
                : "Raccourcis simples OFF : utilise Ctrl+Alt+S / R / G.");
            SavePrefs();
        };
        sc.Add(_simpleKeysCheck);
        _tip.SetToolTip(_simpleKeysCheck, "En dehors du mode vertical, ces touches fonctionnent normalement.");
        y += 38;
        _videoCountdownCheck.Text = "Décompte 3, 2, 1 avant la vidéo (début net)";
        _videoCountdownCheck.ForeColor = Color.Gainsboro;
        _videoCountdownCheck.AutoSize = true;
        _videoCountdownCheck.SetBounds(x, y, w, 22);
        _videoCountdownCheck.CheckedChanged += (_, _) => SavePrefs();
        sc.Add(_videoCountdownCheck);
        y += 28;
        sc.Add(MakeLabel("Audio de la vidéo (décocher pour couper la source) :", x, y));
        y += 22;
        _audioSystemCheck.Text = "Son du système (musique, sons PC)";
        _audioSystemCheck.ForeColor = Color.Gainsboro;
        _audioSystemCheck.AutoSize = true;
        _audioSystemCheck.Location = new Point(x, y);
        _audioSystemCheck.CheckedChanged += (_, _) => SavePrefs();
        sc.Add(_audioSystemCheck);
        _audioMicCheck.Text = "Microphone (voix)";
        _audioMicCheck.ForeColor = Color.Gainsboro;
        _audioMicCheck.AutoSize = true;
        _audioMicCheck.Location = new Point(x + 250, y);
        _audioMicCheck.CheckedChanged += (_, _) => SavePrefs();
        sc.Add(_audioMicCheck);
        y += 26;
        StyleCaptureButton(_audioSettingsBtn, "Réglages audio : volumes + réduction de bruit micro...");
        _audioSettingsBtn.SetBounds(x, y, w, 26);
        _audioSettingsBtn.Click += (_, _) => OpenAudioSettings();
        sc.Add(_audioSettingsBtn);
        y += 32;
        _captureStatus.SetBounds(x, y, w, 20);
        _captureStatus.ForeColor = Color.FromArgb(120, 220, 160);
        _captureStatus.Text = "Captures dans .\\Captures (Photos / GIF / Vidéos), à côté de l'exe";
        sc.Add(_captureStatus);
        y += 34;
        sc.Add(MakeSectionHeader("Téléprompteur", x, y));
        sc.Add(MakeSep(x, y + 20, w));
        y += 30;
        sc.Add(new Label
        {
            Text = "Visible à l'écran, absent des captures. Ctrl+Alt+T = clic traversant.",
            AutoSize = false,
            ForeColor = Color.FromArgb(115, 115, 130),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(x, y),
            Size = new Size(w, 16),
        });
        y += 22;
        _scriptBox.SetBounds(x, y, cw, 26);
        _scriptBox.DropDown += (_, _) => RefreshScriptList();
        _scriptBox.SelectedIndexChanged += (_, _) => SavePrefs();
        sc.Add(_scriptBox);
        StyleCaptureButton(_teleBtn, "Téléprompteur : ouvrir");
        _teleBtn.SetBounds(col2, y, cw, 26);
        _teleBtn.Click += (_, _) => ToggleTeleprompter();
        sc.Add(_teleBtn);
        y += 32;
        StyleCaptureButton(_scriptsFolderBtn, "Dossier scripts (écrire dedans)");
        _scriptsFolderBtn.SetBounds(x, y, cw, 26);
        _scriptsFolderBtn.Click += (_, _) => Teleprompter.OpenFolder();
        sc.Add(_scriptsFolderBtn);
        StyleCaptureButton(_voiceBtn, "Générer la voix off");
        _voiceBtn.SetBounds(col2, y, cw, 26);
        _voiceBtn.Click += (_, _) => GenerateVoiceOff();
        sc.Add(_voiceBtn);
        y += 32;
        _azureVoiceCheck.Text = "Voix off en Azure (voix neuronale, sinon SAPI). Clé à régler dans RoleplayOverlay.";
        _azureVoiceCheck.ForeColor = Color.Gainsboro;
        _azureVoiceCheck.AutoSize = false;
        _azureVoiceCheck.SetBounds(x, y, w, 22);
        _azureVoiceCheck.CheckedChanged += (_, _) => SavePrefs();
        sc.Add(_azureVoiceCheck);
    }
    private static Label MakeSectionHeader(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10f),
        ForeColor = Color.FromArgb(110, 190, 255),
        Location = new Point(x, y),
    };
    private static Panel MakeSep(int x, int y, int w) => new()
    {
        Location = new Point(x, y),
        Size = new Size(w, 1),
        BackColor = Color.FromArgb(55, 88, 130),
    };
    private static void StyleCaptureButton(Button b, string text)
    {
        b.Text = text;
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = Color.Gray;
        b.ForeColor = Color.Gainsboro;
        b.Font = new Font("Segoe UI", 9.5f);
    }
    private static Label MakeLabel(string text, int x, int y) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Color.Gainsboro,
        Location = new Point(x, y),
    };
    private void PopulateMonitors()
    {
        _monitorBox.Items.Clear();
        Screen[] screens = Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            Rectangle b = screens[i].Bounds;
            string primary = screens[i].Primary ? "  (principal)" : "";
            _monitorBox.Items.Add($"Ecran {i + 1} : {b.Width}x{b.Height}{primary}");
        }
        if (_monitorBox.Items.Count > 0)
            _monitorBox.SelectedIndex = 0;
    }
    private Rectangle CurrentMonitorBounds()
    {
        Screen[] screens = Screen.AllScreens;
        int idx = Math.Clamp(_monitorBox.SelectedIndex, 0, screens.Length - 1);
        return screens[idx].Bounds;
    }
    private (int W, int H) CurrentRatio()
    {
        var r = _ratios[Math.Clamp(_ratioBox.SelectedIndex, 0, _ratios.Length - 1)];
        return (r.W, r.H);
    }
    private Rectangle ZoneScreen()
    {
        Rectangle mon = CurrentMonitorBounds();
        (int rw, int rh) = CurrentRatio();
        Rectangle hole = Frame.Hole(mon, rw, rh);
        return new Rectangle(mon.X + hole.X, mon.Y + hole.Y, hole.Width, hole.Height);
    }
    private void UpdateZoneLabel()
    {
        Rectangle mon = CurrentMonitorBounds();
        (int rw, int rh) = CurrentRatio();
        Rectangle hole = Frame.Hole(mon, rw, rh);
        int left = hole.X;
        int top = hole.Y;
        int right = mon.Width - hole.Right;
        int bottom = mon.Height - hole.Bottom;
        _zoneLabel.Text =
            $"Zone capturee : {hole.Width} x {hole.Height} px (centree)\r\n\r\n" +
            "OBS, filtre Rogner / Completer :\r\n" +
            $"   Gauche {left}    Droite {right}    Haut {top}    Bas {bottom}";
        _lastMargins = $"Gauche={left} Droite={right} Haut={top} Bas={bottom}";
    }
    private void CopyMargins()
    {
        try
        {
            Clipboard.SetText(_lastMargins);
            _copyBtn.Text = "Copie !";
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) => { _copyBtn.Text = "Copier les marges OBS"; t.Stop(); t.Dispose(); };
            t.Start();
        }
        catch {  }
    }
    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        UpdateZoneLabel();
        if (IsActive)
        {
            ExitMode();
            EnterMode();
        }
        if (_backdrop != null)
        {
            Rectangle z = ZoneScreen();
            _backdrop.ShowOver(z);
            _camForm?.FillZone(z);
        }
        if (_camClose != null) _camClose.ShowAt(CurrentMonitorBounds(), ZoneScreen());
        SavePrefs();
    }
    private void Toggle()
    {
        if (IsActive) ExitMode();
        else EnterMode();
    }
    private void SetStatus(string text) => _captureStatus.Text = text;
    private void DoPhoto()
    {
        Rectangle zone = ZoneScreen();
        try
        {
            string path = ZoneCapture.Screenshot(zone);
            Feedback.Flash(zone);
            Feedback.Toast(CurrentMonitorBounds(), zone, "Photo enregistree  (clic = ouvrir)",
                () => ZoneCapture.OpenFolder(ZoneCapture.Photos));
            SetStatus("Photo : " + Path.GetFileName(path));
            _blackout?.RefreshHistory();
        }
        catch (Exception ex) { SetStatus("Echec photo : " + ex.Message); }
    }
    private void DoPhotoTimer(int seconds)
    {
        Rectangle zone = ZoneScreen();
        var cd = new CountdownForm(zone, seconds, () =>
        {
            try
            {
                string path = ZoneCapture.Screenshot(zone);
                Feedback.Flash(zone);
                Feedback.Toast(CurrentMonitorBounds(), zone, "Photo enregistree  (clic = ouvrir)",
                    () => ZoneCapture.OpenFolder(ZoneCapture.Photos));
                SetStatus("Photo : " + Path.GetFileName(path));
                _blackout?.RefreshHistory();
            }
            catch (Exception ex) { SetStatus("Echec photo : " + ex.Message); }
        });
        cd.Show();
    }
    private void ToggleGif()
    {
        if (!_gif.Recording)
        {
            Rectangle zone = ZoneScreen();
            _gif.Start(zone);
            _gifBtn.Text = "GIF : arrêter";
            _gifBtn.ForeColor = Color.FromArgb(255, 120, 120);
            ShowRecBadge("GIF", zone);
            RaiseTeleprompter();
            SetStatus("Enregistrement GIF en cours...");
        }
        else
        {
            _gifBtn.Text = "GIF : démarrer";
            _gifBtn.ForeColor = Color.Gainsboro;
            HideRecBadge();
            try
            {
                string? path = _gif.Stop();
                if (path != null)
                {
                    Feedback.Toast(CurrentMonitorBounds(), ZoneScreen(), "GIF enregistre  (clic = ouvrir)",
                        () => ZoneCapture.OpenFolder(ZoneCapture.Gifs));
                    SetStatus("GIF : " + Path.GetFileName(path));
                }
                else SetStatus("GIF annule (aucune image).");
            }
            catch (Exception ex) { SetStatus("Echec GIF : " + ex.Message); }
            _blackout?.RefreshHistory();
        }
        RefreshBandCapture();
    }
    private void ToggleVideo()
    {
        if (_video.Recording) { StopVideoRecording(); return; }
        if (_videoCountingDown) return;
        if (_videoCountdownCheck.Checked)
        {
            _videoCountingDown = true;
            _videoBtn.Text = "Vidéo : 3..2..1";
            _videoBtn.ForeColor = Color.FromArgb(255, 180, 90);
            var cd = new CountdownForm(ZoneScreen(), 3, () =>
            {
                _videoCountingDown = false;
                StartVideoRecording();
            });
            cd.Show();
        }
        else StartVideoRecording();
    }
    private void StartVideoRecording()
    {
        Rectangle zone = ZoneScreen();
        bool aSys = _audioSystemCheck.Checked, aMic = _audioMicCheck.Checked;
        var aopt = new AudioOptions
        {
            System = aSys, Mic = aMic,
            SystemGain = _sysGain, MicGain = _micGain,
            MicGate = _micGate, GateThreshold = _gateThreshold,
        };
        _video.Start(zone, ZoneCapture.OutputPath("mp4", ZoneCapture.Videos), 30, aopt);
        _videoBtn.Text = "Vidéo : arrêter";
        _videoBtn.ForeColor = Color.FromArgb(255, 120, 120);
        ShowRecBadge("Video", zone);
        RaiseTeleprompter();
        string src = (aSys, aMic) switch
        {
            (true, true) => "son systeme + micro",
            (true, false) => "son systeme seul",
            (false, true) => "micro seul",
            _ => "sans son",
        };
        SetStatus($"Enregistrement video en cours ({src})...");
        RefreshBandCapture();
    }
    private void StopVideoRecording()
    {
        HideRecBadge();
        string? path = _video.Stop();
        _videoBtn.Text = "Vidéo : démarrer";
        _videoBtn.ForeColor = Color.Gainsboro;
        if (path != null)
        {
            Feedback.Toast(CurrentMonitorBounds(), ZoneScreen(), "Vidéo enregistrée  (clic = ouvrir)",
                () => ZoneCapture.OpenFolder(ZoneCapture.Videos));
            SetStatus("Vidéo : " + Path.GetFileName(path));
        }
        else SetStatus("Échec vidéo : " + (_video.LastError ?? "inconnu"));
        _blackout?.RefreshHistory();
        RefreshBandCapture();
    }
    private void OpenAudioSettings()
    {
        using var dlg = new AudioSettingsForm(_sysGain, _micGain, _micGate, _gateThreshold);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _sysGain = dlg.SystemGain;
        _micGain = dlg.MicGain;
        _micGate = dlg.MicGate;
        _gateThreshold = dlg.GateThreshold;
        SavePrefs();
        SetStatus("Réglages audio enregistrés (s'appliquent au prochain enregistrement).");
    }
    private void ShowRecBadge(string label, Rectangle zone)
    {
        HideRecBadge();
        _recBadge = new RecBadge(label);
        _recBadge.ShowAt(CurrentMonitorBounds(), zone);
    }
    private void HideRecBadge()
    {
        if (_recBadge != null) { try { _recBadge.Stop(); } catch { } _recBadge = null; }
    }
    private void RefreshBandCapture() => _blackout?.UpdateCaptureButtons(_gif.Recording, _video.Recording);
    private void RaiseTeleprompter() => _teleForm?.BringToTop();
    private void RefreshCameraList()
    {
        int sel = _cameraBox.SelectedIndex;
        _cameraBox.Items.Clear();
        string[] cams = WebcamReader.ListDevices();
        if (cams.Length == 0) _cameraBox.Items.Add("(aucune camera)");
        else _cameraBox.Items.AddRange(cams);
        _cameraBox.SelectedIndex = (sel >= 0 && sel < _cameraBox.Items.Count) ? sel : 0;
    }
    private void RefreshScriptList()
    {
        string? prev = _scriptBox.SelectedItem as string;
        _scriptBox.Items.Clear();
        string[] scripts = Teleprompter.List();
        if (scripts.Length == 0) _scriptBox.Items.Add("(aucun script)");
        else _scriptBox.Items.AddRange(scripts);
        int idx = prev != null ? _scriptBox.Items.IndexOf(prev) : -1;
        _scriptBox.SelectedIndex = idx >= 0 ? idx : 0;
    }
    private void ToggleTeleprompter()
    {
        if (_teleForm == null)
        {
            if (_scriptBox.SelectedItem is not string name || name.StartsWith("("))
            {
                SetStatus("Aucun script : clique sur 'Dossier scripts' et écris un fichier .txt.");
                return;
            }
            _teleForm = new TeleprompterForm();
            _teleForm.CloseRequested += (_, _) => CloseTeleprompter();
            _teleForm.ClickThroughChanged += on => ApplyTeleArrowHotkeys(on);
            _teleForm.FormClosed += (_, _) => { _teleForm = null; _teleBtn.Text = "Teleprompteur : ouvrir"; };
            _teleForm.SettingsChanged += () =>
            {
                if (_teleForm == null) return;
                _teleSpeed = _teleForm.SpeedValue;
                _teleFont = _teleForm.FontValue;
                _teleAnchor = _teleForm.AnchorValue;
                SavePrefs();
            };
            _teleForm.Show();
            _teleForm.PlaceNearTop(ZoneScreen());
            _teleForm.ApplySettings(_teleSpeed, _teleFont, _teleAnchor);
            _teleForm.SetScript(Teleprompter.Read(name));
            _teleBtn.Text = "Téléprompteur : fermer";
            SetStatus("Téléprompteur ouvert. Ctrl+Alt+T = clic traversant. Ctrl+Alt+flèches = défiler depuis n'importe où.");
        }
        else CloseTeleprompter();
    }
    private void ToggleTeleClickThrough()
    {
        if (_teleForm == null) { SetStatus("Ouvre d'abord le téléprompteur (Ctrl+Alt+T = clic traversant)."); return; }
        bool on = _teleForm.ToggleClickThrough();
        SetStatus(on
            ? "Téléprompteur : clic traversant ON (les clics passent aux fenêtres derrière). Ctrl+Alt+T = reprendre la main."
            : "Téléprompteur : clic traversant OFF (contrôles à nouveau cliquables).");
    }
    private void CloseTeleprompter()
    {
        ApplyTeleArrowHotkeys(false);
        if (_teleForm != null)
        {
            TeleprompterForm f = _teleForm;
            _teleForm = null;
            f.Close();
            f.Dispose();
        }
        _teleBtn.Text = "Téléprompteur : ouvrir";
    }
    private void GenerateVoiceOff()
    {
        if (_scriptBox.SelectedItem is not string name || name.StartsWith("("))
        {
            SetStatus("Choisis d'abord un script dans la liste.");
            return;
        }
        string scriptPath = Path.Combine(Teleprompter.ScriptsDir, name);
        string? exe = FindRoleplayExe();
        if (exe == null)
        {
            SetStatus("RoleplayOverlay introuvable (build-le une fois) -> voix off indisponible.");
            return;
        }
        bool azure = _azureVoiceCheck.Checked;
        string args = $"--tts \"{scriptPath}\"" + (azure ? " --azure" : "");
        SetStatus(azure
            ? "Génération de la voix off (Azure, repli SAPI si pas de clé)..."
            : "Génération de la voix off (SAPI) en cours...");
        _voiceBtn.Enabled = false;
        string wav = Path.ChangeExtension(scriptPath, ".wav");
        System.Threading.Tasks.Task.Run(() =>
        {
            string err = "";
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();
            }
            catch (Exception ex) { err = ex.Message; }
            BeginInvoke(new Action(() =>
            {
                _voiceBtn.Enabled = true;
                if (File.Exists(wav) && err.Length == 0)
                {
                    SetStatus($"Voix off générée ({(azure ? "Azure demandé" : "SAPI")}) : " + Path.GetFileName(wav));
                    try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{wav}\""); } catch { }
                }
                else SetStatus("Échec voix off : " + (err.Length > 0 ? err : "aucun audio (voir RoleplayOverlay)."));
            }));
        });
    }
    private static string? FindRoleplayExe()
    {
        string baseDir = AppContext.BaseDirectory;
        string[] candidates =
        {
            Path.Combine(baseDir, "..", "RoleplayOverlay", "RoleplayOverlay", "bin", "Release", "net8.0-windows", "RoleplayOverlay.exe"),
            Path.Combine(baseDir, "..", "RoleplayOverlay", "RoleplayOverlay", "bin", "Debug", "net8.0-windows", "RoleplayOverlay.exe"),
            Path.Combine(baseDir, "..", "RoleplayOverlay", "RoleplayOverlay", "RoleplayOverlay.exe"),
        };
        foreach (string c in candidates)
        {
            string full = Path.GetFullPath(c);
            if (File.Exists(full)) return full;
        }
        return null;
    }
    private void ToggleCamera()
    {
        if (_camForm == null)
        {
            var reader = new WebcamReader();
            int deviceIndex = Math.Max(0, _cameraBox.SelectedIndex);
            if (!reader.Start(deviceIndex))
            {
                SetStatus("Caméra indisponible : " + (reader.LastError ?? "aucune caméra détectée"));
                reader.Stop();
                return;
            }
            _cam = reader;
            _camForm = new CamForm(reader, (CamForm.Shape)Math.Max(0, _shapeBox.SelectedIndex), _mirrorCheck.Checked, _filterParams);
            _camForm.CloseRequested += (_, _) => BeginInvoke(new Action(StopCamera));
            _camForm.GeometryChanged += (_, _) => OnCamGeometryChanged();
            _camForm.Show();
            _camForm.SetFilter(_filterParams);
            if (_camWidthFrac >= 0.05)
                _camForm.ApplyGeometry(ZoneScreen(), _camWidthFrac, _camCenterXFrac, _camCenterYFrac);
            else
                _camForm.PlaceInZone(ZoneScreen());
            ShowCamClose();
            _camBtn.Text = "Caméra : fermer";
            _camBtn.ForeColor = Color.FromArgb(255, 120, 120);
            SetStatus("Caméra affichée. Glisser/redimensionner ; la croix rouge ou clic droit ferme.");
        }
        else StopCamera();
    }
    private void StopCamera()
    {
        if (_camForm != null)
        {
            CamForm f = _camForm;
            _camForm = null;
            f.Close();
            f.Dispose();
        }
        if (_cam != null) { _cam.Stop(); _cam = null; }
        HideCamClose();
        _camBtn.Text = "Caméra : ouvrir";
        _camBtn.ForeColor = Color.Gainsboro;
        if (_camOnlyCheck.Checked) _camOnlyCheck.Checked = false;
        else CloseBackdrop();
    }
    private void OnCamGeometryChanged()
    {
        if (_camForm == null) return;
        Rectangle zone = ZoneScreen();
        if (zone.Width <= 0 || zone.Height <= 0) return;
        Rectangle b = _camForm.Bounds;
        _camWidthFrac = (double)b.Width / zone.Width;
        _camCenterXFrac = (b.X + b.Width / 2.0 - zone.X) / zone.Width;
        _camCenterYFrac = (b.Y + b.Height / 2.0 - zone.Y) / zone.Height;
        SavePrefs();
    }
    private void OpenCamFilters()
    {
        var dlg = new CamFilterForm(_filterParams, p =>
        {
            _filterParams = p;
            _camForm?.SetFilter(p);
        });
        dlg.Show(this);
        dlg.FormClosed += (_, _) => SavePrefs();
    }
    private void ResetCamSize()
    {
        _camWidthFrac = 0;
        _camCenterXFrac = 0;
        _camCenterYFrac = 0;
        SavePrefs();
        if (_camForm != null && _backdrop == null) _camForm.PlaceInZone(ZoneScreen());
        SetStatus("Taille et position de la caméra réinitialisées.");
    }
    private void SetCamOnly(bool on)
    {
        if (on)
        {
            if (_camForm == null) ToggleCamera();
            if (_camForm == null) { _camOnlyCheck.Checked = false; return; }
            Rectangle zone = ZoneScreen();
            _backdrop ??= new BackdropForm();
            UpdateTaskbar();
            _backdrop.ShowOver(zone);
            _camForm.FillZone(zone);
            ShowCamClose();
            RaiseTeleprompter();
            SetStatus("Cam seule : plein cadre, seule la caméra est filmée.");
        }
        else
        {
            CloseBackdrop();
            _camForm?.ExitFill(ZoneScreen());
            ShowCamClose();
            UpdateTaskbar();
        }
    }
    private void CloseBackdrop()
    {
        if (_backdrop != null) { _backdrop.Close(); _backdrop.Dispose(); _backdrop = null; }
    }
    private void UpdateTaskbar()
    {
        if (_backdrop != null)
        {
            _taskbar.Hide();
        }
        else if (IsActive)
        {
            _taskbar.Show();
            _taskbar.SetAutoHide();
        }
        else
        {
            _taskbar.Show();
            _taskbar.RestoreAutoHide();
        }
    }
    private void ShowCamClose()
    {
        if (_camForm == null) return;
        _camClose ??= CreateCamClose();
        _camClose.ShowAt(CurrentMonitorBounds(), ZoneScreen());
    }
    private CamCloseButton CreateCamClose()
    {
        var b = new CamCloseButton();
        b.Clicked += (_, _) => BeginInvoke(new Action(StopCamera));
        return b;
    }
    private void HideCamClose()
    {
        if (_camClose != null) { _camClose.Close(); _camClose.Dispose(); _camClose = null; }
    }
    private void EnterMode()
    {
        if (IsActive) return;
        Rectangle zone = ZoneScreen();
        _arranger.Arrange(zone);
        UpdateTaskbar();
        _blackout = new Blackout();
        _blackout.CloseRequested += (_, _) => ExitMode();
        _blackout.CaptureRequested += OnBandCapture;
        _blackout.EditingStarted += () => SetSimpleKeysSuspended(true);
        _blackout.EditingEnded += () => SetSimpleKeysSuspended(false);
        _blackout.Show(CurrentMonitorBounds(), zone);
        _blackout.UpdateCaptureButtons(_gif.Recording, _video.Recording);
        _enforceTimer.Start();
        SetToggleVisual(true);
        RaiseTeleprompter();
        ApplySimpleKeys();
    }
    private void OnBandCapture(Blackout.CaptureKind kind)
    {
        switch (kind)
        {
            case Blackout.CaptureKind.Photo: DoPhoto(); break;
            case Blackout.CaptureKind.Gif: ToggleGif(); break;
            case Blackout.CaptureKind.Video: ToggleVideo(); break;
        }
    }
    private void ExitMode()
    {
        _enforceTimer.Stop();
        if (_blackout != null)
        {
            _blackout.Hide();
            _blackout = null;
        }
        _arranger.Restore();
        UpdateTaskbar();
        SetToggleVisual(false);
        ApplySimpleKeys();
    }
    private void SetToggleVisual(bool active)
    {
        _toggle.ForeColor = Color.White;
        if (active)
        {
            _toggle.Text = "Quitter le mode vertical   (Ctrl + Alt + V)";
            _toggle.BackColor = Color.FromArgb(185, 60, 60);
        }
        else
        {
            _toggle.Text = "Activer le mode vertical   (Ctrl + Alt + V)";
            _toggle.BackColor = Color.FromArgb(60, 150, 95);
        }
    }
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        uint mod = Native.MOD_CONTROL | Native.MOD_ALT | Native.MOD_NOREPEAT;
        Native.RegisterHotKey(Handle, HOTKEY_ID, mod, 0x56);
        Native.RegisterHotKey(Handle, HOTKEY_PHOTO, mod, 0x53);
        Native.RegisterHotKey(Handle, HOTKEY_VIDEO, mod, 0x52);
        Native.RegisterHotKey(Handle, HOTKEY_GIF, mod, 0x47);
        Native.RegisterHotKey(Handle, HOTKEY_CAM, mod, 0x43);
        Native.RegisterHotKey(Handle, HOTKEY_TELE, mod, 0x54);
        uint modScroll = Native.MOD_CONTROL | Native.MOD_ALT;
        Native.RegisterHotKey(Handle, HOTKEY_TELE_UP, modScroll, 0x26);
        Native.RegisterHotKey(Handle, HOTKEY_TELE_DOWN, modScroll, 0x28);
        Native.RegisterHotKey(Handle, HOTKEY_TELE_LEFT, modScroll, 0x25);
        Native.RegisterHotKey(Handle, HOTKEY_TELE_RIGHT, modScroll, 0x27);
        ApplySimpleKeys();
    }
    private bool _simpleKeysSuspended;
    private void SetSimpleKeysSuspended(bool on)
    {
        _simpleKeysSuspended = on;
        ApplySimpleKeys();
    }
    private void ApplySimpleKeys()
    {
        if (!IsHandleCreated) return;
        Native.UnregisterHotKey(Handle, HOTKEY_S);
        Native.UnregisterHotKey(Handle, HOTKEY_R);
        Native.UnregisterHotKey(Handle, HOTKEY_G);
        if (_simpleKeysCheck.Checked && IsActive && !_simpleKeysSuspended)
        {
            uint m = Native.MOD_NOREPEAT;
            Native.RegisterHotKey(Handle, HOTKEY_S, m, 0x53);
            Native.RegisterHotKey(Handle, HOTKEY_R, m, 0x52);
            Native.RegisterHotKey(Handle, HOTKEY_G, m, 0x47);
        }
    }
    private void ApplyTeleArrowHotkeys(bool on)
    {
        if (!IsHandleCreated) return;
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_UP);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_DOWN);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_LEFT);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_RIGHT);
        if (on)
        {
            Native.RegisterHotKey(Handle, HOTKEY_ARROW_UP, 0, 0x26);
            Native.RegisterHotKey(Handle, HOTKEY_ARROW_DOWN, 0, 0x28);
            Native.RegisterHotKey(Handle, HOTKEY_ARROW_LEFT, 0, 0x25);
            Native.RegisterHotKey(Handle, HOTKEY_ARROW_RIGHT, 0, 0x27);
        }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            switch (m.WParam.ToInt32())
            {
                case HOTKEY_ID: Toggle(); return;
                case HOTKEY_PHOTO: DoPhoto(); return;
                case HOTKEY_VIDEO: ToggleVideo(); return;
                case HOTKEY_GIF: ToggleGif(); return;
                case HOTKEY_CAM: ToggleCamera(); return;
                case HOTKEY_TELE: ToggleTeleClickThrough(); return;
                case HOTKEY_TELE_UP: _teleForm?.ScrollStep(-40); return;
                case HOTKEY_TELE_DOWN: _teleForm?.ScrollStep(+40); return;
                case HOTKEY_TELE_LEFT: _teleForm?.ScrollStep(-220); return;
                case HOTKEY_TELE_RIGHT: _teleForm?.ScrollStep(+220); return;
                case HOTKEY_S: DoPhoto(); return;
                case HOTKEY_R: ToggleVideo(); return;
                case HOTKEY_G: ToggleGif(); return;
                case HOTKEY_ARROW_UP: _teleForm?.ScrollStep(-40); return;
                case HOTKEY_ARROW_DOWN: _teleForm?.ScrollStep(+40); return;
                case HOTKEY_ARROW_LEFT: _teleForm?.ScrollStep(-220); return;
                case HOTKEY_ARROW_RIGHT: _teleForm?.ScrollStep(+220); return;
            }
        }
        base.WndProc(ref m);
    }
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Native.UnregisterHotKey(Handle, HOTKEY_ID);
        Native.UnregisterHotKey(Handle, HOTKEY_PHOTO);
        Native.UnregisterHotKey(Handle, HOTKEY_VIDEO);
        Native.UnregisterHotKey(Handle, HOTKEY_GIF);
        Native.UnregisterHotKey(Handle, HOTKEY_CAM);
        Native.UnregisterHotKey(Handle, HOTKEY_TELE);
        Native.UnregisterHotKey(Handle, HOTKEY_TELE_UP);
        Native.UnregisterHotKey(Handle, HOTKEY_TELE_DOWN);
        Native.UnregisterHotKey(Handle, HOTKEY_TELE_LEFT);
        Native.UnregisterHotKey(Handle, HOTKEY_TELE_RIGHT);
        Native.UnregisterHotKey(Handle, HOTKEY_S);
        Native.UnregisterHotKey(Handle, HOTKEY_R);
        Native.UnregisterHotKey(Handle, HOTKEY_G);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_UP);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_DOWN);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_LEFT);
        Native.UnregisterHotKey(Handle, HOTKEY_ARROW_RIGHT);
        HideRecBadge();
        if (_gif.Recording) { try { _gif.Stop(); } catch {  } }
        if (_video.Recording) { try { _video.Stop(); } catch {  } }
        StopCamera();
        CloseBackdrop();
        CloseTeleprompter();
        ExitMode();
        SavePrefs();
        base.OnFormClosed(e);
    }
    private sealed class Prefs
    {
        public int Monitor { get; set; }
        public int Ratio { get; set; }
        public int Camera { get; set; }
        public int Shape { get; set; }
        public bool Mirror { get; set; }
        public string? Script { get; set; }
        public int TeleSpeed { get; set; }
        public int TeleFont { get; set; }
        public double TeleAnchor { get; set; }
        public double CamWidthFrac { get; set; }
        public double CamCenterXFrac { get; set; }
        public double CamCenterYFrac { get; set; }
        public bool SimpleKeys { get; set; }
        public bool? AudioSystem { get; set; }
        public bool? AudioMic { get; set; }
        public double? SysGain { get; set; }
        public double? MicGain { get; set; }
        public bool MicGate { get; set; }
        public double? GateThreshold { get; set; }
        public bool? VideoCountdown { get; set; }
        public bool VoiceAzure { get; set; }
        public double? CamFilterBrightness { get; set; }
        public double? CamFilterContrast { get; set; }
        public double? CamFilterSaturation { get; set; }
        public double? CamFilterWarmth { get; set; }
        public int CamFilterSmoothing { get; set; }
        public double? CamFilterSharpness { get; set; }
        public double? CamFilterVignette { get; set; }
    }
    private void LoadPrefs()
    {
        try
        {
            if (!File.Exists(_prefsPath)) return;
            Prefs? p = JsonSerializer.Deserialize<Prefs>(File.ReadAllText(_prefsPath));
            if (p == null) return;
            if (p.Monitor >= 0 && p.Monitor < _monitorBox.Items.Count) _monitorBox.SelectedIndex = p.Monitor;
            if (p.Ratio >= 0 && p.Ratio < _ratioBox.Items.Count) _ratioBox.SelectedIndex = p.Ratio;
            if (p.Camera >= 0 && p.Camera < _cameraBox.Items.Count) _cameraBox.SelectedIndex = p.Camera;
            if (p.Shape >= 0 && p.Shape < _shapeBox.Items.Count) _shapeBox.SelectedIndex = p.Shape;
            _mirrorCheck.Checked = p.Mirror;
            if (p.TeleSpeed is >= 10 and <= 220) _teleSpeed = p.TeleSpeed;
            if (p.TeleFont is >= 14 and <= 64) _teleFont = p.TeleFont;
            if (p.TeleAnchor is >= 0.05 and <= 0.60) _teleAnchor = (float)p.TeleAnchor;
            if (p.CamWidthFrac is >= 0.05 and <= 1.0)
            {
                _camWidthFrac = p.CamWidthFrac;
                _camCenterXFrac = Math.Clamp(p.CamCenterXFrac, 0.0, 1.0);
                _camCenterYFrac = Math.Clamp(p.CamCenterYFrac, 0.0, 1.0);
            }
            _simpleKeysCheck.Checked = p.SimpleKeys;
            if (p.AudioSystem.HasValue) _audioSystemCheck.Checked = p.AudioSystem.Value;
            if (p.AudioMic.HasValue) _audioMicCheck.Checked = p.AudioMic.Value;
            if (p.SysGain is >= 0 and <= 2) _sysGain = (float)p.SysGain.Value;
            if (p.MicGain is >= 0 and <= 3) _micGain = (float)p.MicGain.Value;
            _micGate = p.MicGate;
            if (p.GateThreshold is >= 0.001 and <= 0.2) _gateThreshold = (float)p.GateThreshold.Value;
            if (p.VideoCountdown.HasValue) _videoCountdownCheck.Checked = p.VideoCountdown.Value;
            _azureVoiceCheck.Checked = p.VoiceAzure;
            if (p.CamFilterBrightness.HasValue)
                _filterParams = new CameraFilter.Params(
                    (float)Math.Clamp(p.CamFilterBrightness.Value, -0.5, 0.5),
                    (float)Math.Clamp(p.CamFilterContrast ?? 1.0, 0.5, 3.0),
                    (float)Math.Clamp(p.CamFilterSaturation ?? 1.0, 0.0, 2.0),
                    (float)Math.Clamp(p.CamFilterWarmth ?? 0.0, -0.3, 0.3),
                    Math.Clamp(p.CamFilterSmoothing, 0, 5),
                    (float)Math.Clamp(p.CamFilterSharpness ?? 0.0, 0.0, 2.0),
                    (float)Math.Clamp(p.CamFilterVignette ?? 0.0, 0.0, 1.0));
            if (!string.IsNullOrEmpty(p.Script))
            {
                int si = _scriptBox.Items.IndexOf(p.Script);
                if (si >= 0) _scriptBox.SelectedIndex = si;
            }
        }
        catch {  }
    }
    private void SavePrefs()
    {
        if (_loading) return;
        try
        {
            var p = new Prefs
            {
                Monitor = _monitorBox.SelectedIndex,
                Ratio = _ratioBox.SelectedIndex,
                Camera = _cameraBox.SelectedIndex,
                Shape = _shapeBox.SelectedIndex,
                Mirror = _mirrorCheck.Checked,
                Script = _scriptBox.SelectedItem as string,
                TeleSpeed = (int)_teleSpeed,
                TeleFont = (int)_teleFont,
                TeleAnchor = _teleAnchor,
                CamWidthFrac = _camWidthFrac,
                CamCenterXFrac = _camCenterXFrac,
                CamCenterYFrac = _camCenterYFrac,
                SimpleKeys = _simpleKeysCheck.Checked,
                AudioSystem = _audioSystemCheck.Checked,
                AudioMic = _audioMicCheck.Checked,
                SysGain = _sysGain,
                MicGain = _micGain,
                MicGate = _micGate,
                GateThreshold = _gateThreshold,
                VideoCountdown = _videoCountdownCheck.Checked,
                VoiceAzure = _azureVoiceCheck.Checked,
                CamFilterBrightness = _filterParams.Brightness,
                CamFilterContrast = _filterParams.Contrast,
                CamFilterSaturation = _filterParams.Saturation,
                CamFilterWarmth = _filterParams.Warmth,
                CamFilterSmoothing = _filterParams.Smoothing,
                CamFilterSharpness = _filterParams.Sharpness,
                CamFilterVignette = _filterParams.Vignette,
            };
            File.WriteAllText(_prefsPath, JsonSerializer.Serialize(p));
        }
        catch {  }
    }
}
internal sealed class AudioSettingsForm : Form
{
    private readonly TrackBar _sys = new() { Minimum = 0, Maximum = 200, TickFrequency = 25, Width = 300 };
    private readonly TrackBar _mic = new() { Minimum = 0, Maximum = 300, TickFrequency = 25, Width = 300 };
    private readonly TrackBar _thr = new() { Minimum = 2, Maximum = 60, TickFrequency = 5, Width = 300 };
    private readonly CheckBox _gate = new();
    private readonly Label _sysVal = new(), _micVal = new(), _thrVal = new();
    public float SystemGain => _sys.Value / 100f;
    public float MicGain => _mic.Value / 100f;
    public bool MicGate => _gate.Checked;
    public float GateThreshold => _thr.Value / 1000f;
    public AudioSettingsForm(float sysGain, float micGain, bool gate, float threshold)
    {
        Text = "Réglages audio";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(360, 360);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9f);
        int x = 16, y = 14, w = 328;
        Controls.Add(Title("Volume du son système (musique, sons PC)", x, y)); y += 22;
        _sys.SetBounds(x, y, w, 40); _sys.Value = Clamp(sysGain, 200);
        _sys.ValueChanged += (_, _) => _sysVal.Text = _sys.Value + " %";
        Controls.Add(_sys);
        _sysVal.SetBounds(x + w - 60, y + 44, 60, 18); _sysVal.Text = _sys.Value + " %";
        Controls.Add(_sysVal); y += 66;
        Controls.Add(Title("Volume du micro (voix)", x, y)); y += 22;
        _mic.SetBounds(x, y, w, 40); _mic.Value = Clamp(micGain, 300);
        _mic.ValueChanged += (_, _) => _micVal.Text = _mic.Value + " %";
        Controls.Add(_mic);
        _micVal.SetBounds(x + w - 60, y + 44, 60, 18); _micVal.Text = _mic.Value + " %";
        Controls.Add(_micVal); y += 66;
        _gate.Text = "Réduction de bruit micro (coupe le micro tant qu'on ne parle pas)";
        _gate.ForeColor = Color.Gainsboro;
        _gate.AutoSize = false;
        _gate.SetBounds(x, y, w, 36);
        _gate.Checked = gate;
        Controls.Add(_gate); y += 38;
        Controls.Add(Title("Sensibilité (plus à droite = coupe des bruits plus forts)", x, y)); y += 22;
        _thr.SetBounds(x, y, w, 40); _thr.Value = Math.Clamp((int)Math.Round(threshold * 1000f), _thr.Minimum, _thr.Maximum);
        _thr.ValueChanged += (_, _) => _thrVal.Text = _thr.Value.ToString();
        Controls.Add(_thr);
        _thrVal.SetBounds(x + w - 60, y + 44, 60, 18); _thrVal.Text = _thr.Value.ToString();
        Controls.Add(_thrVal); y += 64;
        var ok = MakeBtn("OK", DialogResult.OK, 150, y);
        var cancel = MakeBtn("Annuler", DialogResult.Cancel, 256, y);
        Controls.Add(ok); Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
    }
    private static int Clamp(float gain, int max) => Math.Clamp((int)Math.Round(gain * 100f), 0, max);
    private static Label Title(string text, int x, int y) => new()
    {
        Text = text, AutoSize = false, ForeColor = Color.Gainsboro,
        Location = new Point(x, y), Size = new Size(328, 18),
        Font = new Font("Segoe UI", 8.5f),
    };
    private static Button MakeBtn(string text, DialogResult result, int x, int y)
    {
        var b = new Button
        {
            Text = text, DialogResult = result, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 62), ForeColor = Color.White, Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
        b.SetBounds(x, y, 92, 28);
        return b;
    }
}
internal sealed class CamFilterForm : Form
{
    private readonly ComboBox _presetBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TrackBar _brightness = new() { Minimum = -50, Maximum = 50,  TickFrequency = 10, Width = 320 };
    private readonly TrackBar _contrast   = new() { Minimum = 50,  Maximum = 300, TickFrequency = 25, Width = 320 };
    private readonly TrackBar _saturation = new() { Minimum = 0,   Maximum = 200, TickFrequency = 25, Width = 320 };
    private readonly TrackBar _smoothing  = new() { Minimum = 0,   Maximum = 5,   TickFrequency = 1,  Width = 320 };
    private readonly TrackBar _sharpness  = new() { Minimum = 0,   Maximum = 200, TickFrequency = 25, Width = 320 };
    private readonly TrackBar _vignette   = new() { Minimum = 0,   Maximum = 100, TickFrequency = 10, Width = 320 };
    private readonly Label _bVal = new(), _cVal = new(), _sVal = new(), _smVal = new(),
                           _shVal = new(), _vVal = new();
    private readonly Action<CameraFilter.Params> _onChange;
    private float _warmth;
    private bool _updating;
    public CamFilterForm(CameraFilter.Params current, Action<CameraFilter.Params> onChange)
    {
        _onChange = onChange;
        _warmth = current.Warmth;
        Text = "Filtres caméra";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.Manual;
        MaximizeBox = false; MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(376, 530);
        BackColor = Color.FromArgb(32, 32, 36);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9f);
        int x = 16, y = 14, w = 344;
        Controls.Add(FLabel("Preset :", x, y)); y += 20;
        _presetBox.SetBounds(x, y, w, 26);
        foreach (string n in CameraFilter.PresetNames) _presetBox.Items.Add(n);
        _presetBox.SelectedIndex = DetectPreset(current);
        _presetBox.SelectedIndexChanged += OnPresetChanged;
        Controls.Add(_presetBox); y += 36;
        y = AddSlider("Luminosité (éclat du visage)", _brightness, _bVal, x, y, w, current.Brightness, 50f);
        y = AddSlider("Contraste", _contrast, _cVal, x, y, w, current.Contrast, 100f, "%");
        y = AddSlider("Saturation (couleurs)", _saturation, _sVal, x, y, w, current.Saturation, 100f, "%");
        y = AddSlider("Lissage peau (0 = aucun, 2 = doux, 5 = fort)", _smoothing, _smVal, x, y, w, current.Smoothing, 1f);
        y = AddSlider("Netteté (contours, yeux, cheveux)", _sharpness, _shVal, x, y, w, current.Sharpness, 100f);
        y = AddSlider("Vignette (bords sombres)", _vignette, _vVal, x, y, w, current.Vignette, 100f, "%");
        _brightness.Value = Math.Clamp((int)Math.Round(current.Brightness * 50f),  _brightness.Minimum, _brightness.Maximum);
        _contrast.Value   = Math.Clamp((int)Math.Round(current.Contrast * 100f),   _contrast.Minimum,   _contrast.Maximum);
        _saturation.Value = Math.Clamp((int)Math.Round(current.Saturation * 100f), _saturation.Minimum, _saturation.Maximum);
        _smoothing.Value  = Math.Clamp(current.Smoothing, 0, 5);
        _sharpness.Value  = Math.Clamp((int)Math.Round(current.Sharpness * 100f),  _sharpness.Minimum,  _sharpness.Maximum);
        _vignette.Value   = Math.Clamp((int)Math.Round(current.Vignette * 100f),   _vignette.Minimum,   _vignette.Maximum);
        UpdateLabels();
        _brightness.ValueChanged += (_, _) => OnSliderChanged();
        _contrast.ValueChanged   += (_, _) => OnSliderChanged();
        _saturation.ValueChanged += (_, _) => OnSliderChanged();
        _smoothing.ValueChanged  += (_, _) => OnSliderChanged();
        _sharpness.ValueChanged  += (_, _) => OnSliderChanged();
        _vignette.ValueChanged   += (_, _) => OnSliderChanged();
        var reset = FBtn("Réinitialiser", x, y);
        reset.Click += (_, _) => LoadPreset(CameraFilter.Preset.Aucun);
        Controls.Add(reset);
        var close = FBtn("Fermer", x + 236, y);
        close.Click += (_, _) => Close();
        Controls.Add(close);
    }
    private CameraFilter.Params CurrentParams => new(
        _brightness.Value / 50f,
        _contrast.Value / 100f,
        _saturation.Value / 100f,
        _warmth,
        _smoothing.Value,
        _sharpness.Value / 100f,
        _vignette.Value / 100f);
    private void OnSliderChanged()
    {
        if (_updating) return;
        UpdateLabels();
        _onChange(CurrentParams);
    }
    private void OnPresetChanged(object? sender, EventArgs e)
    {
        if (_updating) return;
        LoadPreset((CameraFilter.Preset)_presetBox.SelectedIndex);
    }
    private void LoadPreset(CameraFilter.Preset preset)
    {
        var p = CameraFilter.GetPreset(preset);
        _updating = true;
        _warmth = p.Warmth;
        _brightness.Value = Math.Clamp((int)Math.Round(p.Brightness * 50f),  _brightness.Minimum, _brightness.Maximum);
        _contrast.Value   = Math.Clamp((int)Math.Round(p.Contrast * 100f),   _contrast.Minimum,   _contrast.Maximum);
        _saturation.Value = Math.Clamp((int)Math.Round(p.Saturation * 100f), _saturation.Minimum, _saturation.Maximum);
        _smoothing.Value  = p.Smoothing;
        _sharpness.Value  = Math.Clamp((int)Math.Round(p.Sharpness * 100f),  _sharpness.Minimum,  _sharpness.Maximum);
        _vignette.Value   = Math.Clamp((int)Math.Round(p.Vignette * 100f),   _vignette.Minimum,   _vignette.Maximum);
        _presetBox.SelectedIndex = (int)preset;
        _updating = false;
        UpdateLabels();
        _onChange(CurrentParams);
    }
    private void UpdateLabels()
    {
        float b = _brightness.Value / 50f;
        _bVal.Text  = b >= 0 ? $"+{b:F2}" : $"{b:F2}";
        _cVal.Text  = $"{_contrast.Value} %";
        _sVal.Text  = $"{_saturation.Value} %";
        _smVal.Text = _smoothing.Value == 0 ? "aucun" : _smoothing.Value.ToString();
        _shVal.Text = _sharpness.Value == 0 ? "aucun" : $"{_sharpness.Value / 100f:F2}";
        _vVal.Text  = _vignette.Value  == 0 ? "aucun" : $"{_vignette.Value} %";
    }
    private static int DetectPreset(CameraFilter.Params p)
    {
        for (int i = 0; i < CameraFilter.PresetNames.Length; i++)
        {
            var pp = CameraFilter.GetPreset(i);
            if (Math.Abs(pp.Brightness - p.Brightness) < 0.005f &&
                Math.Abs(pp.Contrast - p.Contrast) < 0.005f &&
                Math.Abs(pp.Saturation - p.Saturation) < 0.005f &&
                pp.Smoothing == p.Smoothing &&
                Math.Abs(pp.Sharpness - p.Sharpness) < 0.005f &&
                Math.Abs(pp.Vignette - p.Vignette) < 0.005f)
                return i;
        }
        return 0;
    }
    private int AddSlider(string label, TrackBar tb, Label val, int x, int y, int w, float _, float __, string unit = "")
    {
        Controls.Add(FLabel(label, x, y)); y += 20;
        tb.SetBounds(x, y, w - 64, 36);
        val.SetBounds(x + w - 60, y + 8, 56, 20);
        val.ForeColor = Color.FromArgb(120, 220, 160);
        Controls.Add(tb); Controls.Add(val);
        return y + 44;
    }
    private static Label FLabel(string text, int x, int y) => new()
    {
        Text = text, AutoSize = true, ForeColor = Color.Gainsboro,
        Location = new Point(x, y), Font = new Font("Segoe UI", 8.5f),
    };
    private static Button FBtn(string text, int x, int y)
    {
        var b = new Button
        {
            Text = text, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 62), ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
        b.SetBounds(x, y, 100, 28);
        return b;
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (Owner != null)
        {
            Left = Owner.Right + 8;
            Top = Owner.Top + 300;
            if (Left + Width > Screen.PrimaryScreen!.WorkingArea.Right)
                Left = Owner.Left - Width - 8;
        }
    }
}