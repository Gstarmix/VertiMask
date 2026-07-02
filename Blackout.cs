namespace VertiMask;
internal sealed class Blackout
{
    public enum CaptureKind { Photo, Gif, Video }
    public event EventHandler? CloseRequested;
    public event Action<CaptureKind>? CaptureRequested;
    private readonly List<Form> _bands = new();
    private Button? _gifBtn, _videoBtn;
    private Label? _recLabel;
    private ListBox? _historyList;
    private List<ZoneCapture.Item> _history = new();
    private Form? _historyOwner;
    public event Action? EditingStarted;
    public event Action? EditingEnded;
    public bool Visible => _bands.Count > 0;
    public void Show(Rectangle monitor, Rectangle zone)
    {
        Hide();
        var left   = Rectangle.FromLTRB(monitor.Left, monitor.Top, zone.Left, monitor.Bottom);
        var right  = Rectangle.FromLTRB(zone.Right, monitor.Top, monitor.Right, monitor.Bottom);
        var top    = Rectangle.FromLTRB(zone.Left, monitor.Top, zone.Right, zone.Top);
        var bottom = Rectangle.FromLTRB(zone.Left, zone.Bottom, zone.Right, monitor.Bottom);
        Form? leftForm = AddBand(left);
        Form? rightForm = AddBand(right);
        AddBand(top);
        AddBand(bottom);
        if (leftForm != null && left.Width >= right.Width) AddControls(leftForm, left.Width, left.Height);
        else if (rightForm != null) AddControls(rightForm, right.Width, right.Height);
        else if (leftForm != null) AddControls(leftForm, left.Width, left.Height);
    }
    public void Hide()
    {
        foreach (Form f in _bands)
        {
            f.Close();
            f.Dispose();
        }
        _bands.Clear();
        _gifBtn = _videoBtn = null;
        _recLabel = null;
        _historyList = null;
        _historyOwner = null;
        _history = new();
    }
    public void UpdateCaptureButtons(bool gifRecording, bool videoRecording)
    {
        if (_gifBtn != null)
        {
            _gifBtn.Text = gifRecording ? "GIF : arreter" : "GIF";
            _gifBtn.BackColor = gifRecording ? Color.FromArgb(150, 60, 60) : Color.FromArgb(55, 55, 60);
        }
        if (_videoBtn != null)
        {
            _videoBtn.Text = videoRecording ? "Video : arreter" : "Video";
            _videoBtn.BackColor = videoRecording ? Color.FromArgb(150, 60, 60) : Color.FromArgb(55, 55, 60);
        }
        if (_recLabel != null)
            _recLabel.Text = (gifRecording || videoRecording) ? "REC en cours" : "";
    }
    private Form? AddBand(Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0) return null;
        var f = new BandForm();
        f.Show();
        Native.SetWindowPos(f.Handle, Native.HWND_TOPMOST,
            area.X, area.Y, area.Width, area.Height,
            Native.SWP_SHOWWINDOW | Native.SWP_NOACTIVATE);
        _bands.Add(f);
        return f;
    }
    private void AddControls(Form band, int bandWidth, int bandHeight)
    {
        int bw = Math.Clamp(bandWidth - 40, 140, 260);
        int bx = Math.Max(20, (bandWidth - bw) / 2);
        var btn = new Button
        {
            Text = "X   Revenir a la normale",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(185, 60, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.SetBounds(bx, 48, bw, 46);
        btn.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        band.Controls.Add(btn);
        if (bandWidth > 180)
        {
            var hint = new Label
            {
                Text = "Tes fenetres sont rangees dans le cadre vertical.\r\n\r\n" +
                       "Tu peux cliquer et naviguer normalement dedans.\r\n\r\n" +
                       "Clique sur la croix (ou Ctrl+Alt+V) pour tout remettre en place.",
                ForeColor = Color.FromArgb(155, 155, 155),
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
            };
            hint.SetBounds(20, 108, bandWidth - 40, 120);
            band.Controls.Add(hint);
        }
        var capLabel = new Label
        {
            Text = "Capture (Ctrl+Alt+S / R / G) :",
            ForeColor = Color.FromArgb(155, 155, 155),
            Font = new Font("Segoe UI", 9f),
            AutoSize = false,
        };
        capLabel.SetBounds(20, 244, bandWidth - 40, 20);
        band.Controls.Add(capLabel);
        band.Controls.Add(MakeCapButton("Photo", bx, 270, bw, () => CaptureRequested?.Invoke(CaptureKind.Photo)));
        _gifBtn = MakeCapButton("GIF", bx, 320, bw, () => CaptureRequested?.Invoke(CaptureKind.Gif));
        band.Controls.Add(_gifBtn);
        _videoBtn = MakeCapButton("Video", bx, 370, bw, () => CaptureRequested?.Invoke(CaptureKind.Video));
        band.Controls.Add(_videoBtn);
        _recLabel = new Label
        {
            Text = "",
            ForeColor = Color.FromArgb(255, 110, 110),
            Font = new Font("Segoe UI Semibold", 10f),
            AutoSize = false,
        };
        _recLabel.SetBounds(20, 422, bandWidth - 40, 22);
        band.Controls.Add(_recLabel);
        _historyOwner = band;
        var histLabel = new Label
        {
            Text = "Captures recentes (double-clic = ouvrir) :",
            ForeColor = Color.FromArgb(155, 155, 155),
            Font = new Font("Segoe UI", 9f),
            AutoSize = false,
        };
        histLabel.SetBounds(20, 456, bandWidth - 40, 20);
        band.Controls.Add(histLabel);
        int btnTop = Math.Max(520, bandHeight - 52);
        int listTop = 480;
        int listH = Math.Max(90, btnTop - listTop - 8);
        _historyList = new ListBox
        {
            BackColor = Color.FromArgb(22, 22, 26),
            ForeColor = Color.Gainsboro,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9f),
            IntegralHeight = false,
        };
        _historyList.SetBounds(20, listTop, bandWidth - 40, listH);
        _historyList.DoubleClick += (_, _) => OpenSelected();
        _historyList.KeyDown += (_, e) => { if (e.Control && e.KeyCode == Keys.Z) { UndoLastDelete(); e.Handled = true; } };
        band.Controls.Add(_historyList);
        int gw = (bandWidth - 40 - 4 * 6) / 5;
        int hx = 20;
        band.Controls.Add(MakeHistBtn("Ouvrir", hx, btnTop, gw, OpenSelected)); hx += gw + 6;
        band.Controls.Add(MakeHistBtn("Renommer", hx, btnTop, gw, RenameSelected)); hx += gw + 6;
        band.Controls.Add(MakeHistBtn("Supprimer", hx, btnTop, gw, DeleteSelected)); hx += gw + 6;
        band.Controls.Add(MakeHistBtn("Annuler suppr.", hx, btnTop, gw, UndoLastDelete)); hx += gw + 6;
        band.Controls.Add(MakeHistBtn("Dossier", hx, btnTop, gw, RevealSelected));
        RefreshHistory();
    }
    private static Button MakeHistBtn(string text, int x, int y, int w, Action onClick)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
        b.SetBounds(x, y, w, 30);
        b.Click += (_, _) => onClick();
        return b;
    }
    public void RefreshHistory()
    {
        if (_historyList == null) return;
        int sel = _historyList.SelectedIndex;
        _history = ZoneCapture.Recent();
        _historyList.BeginUpdate();
        _historyList.Items.Clear();
        foreach (var it in _history)
            _historyList.Items.Add($"[{it.Kind}]  {Path.GetFileName(it.Path)}");
        _historyList.EndUpdate();
        if (_history.Count > 0)
            _historyList.SelectedIndex = Math.Clamp(sel < 0 ? 0 : sel, 0, _history.Count - 1);
    }
    private ZoneCapture.Item? Selected()
    {
        int i = _historyList?.SelectedIndex ?? -1;
        return (i >= 0 && i < _history.Count) ? _history[i] : null;
    }
    private void OpenSelected() { if (Selected() is { } it) ZoneCapture.OpenFile(it.Path); }
    private void RevealSelected() { if (Selected() is { } it) ZoneCapture.SelectInExplorer(it.Path); }
    private void DeleteSelected()
    {
        if (Selected() is not { } it) return;
        bool ok = Editing(() => BandDialogs.Confirm($"Mettre ce fichier a la corbeille ?\r\n\r\n{Path.GetFileName(it.Path)}\r\n\r\n(Annulable : bouton \"Annuler suppr.\" ou Ctrl+Z)"));
        if (!ok) return;
        if (!ZoneCapture.Delete(it.Path))
            BandDialogs.Info("Suppression impossible (fichier ouvert ou en cours d'ecriture ?).");
        RefreshHistory();
    }
    private void RenameSelected()
    {
        if (Selected() is not { } it) return;
        string? name = Editing(() => BandDialogs.Prompt("Nouveau nom (sans extension) :", Path.GetFileNameWithoutExtension(it.Path)));
        if (name == null) return;
        if (ZoneCapture.Rename(it.Path, name) == null)
            BandDialogs.Info("Renommage impossible (nom invalide ou deja pris).");
        RefreshHistory();
    }
    private void UndoLastDelete()
    {
        if (!ZoneCapture.CanUndo) { BandDialogs.Info("Aucune suppression a annuler."); return; }
        string? restored = ZoneCapture.UndoDelete();
        RefreshHistory();
        if (restored != null && _historyList != null)
        {
            int i = _history.FindIndex(it => string.Equals(it.Path, restored, StringComparison.OrdinalIgnoreCase));
            if (i >= 0) _historyList.SelectedIndex = i;
        }
    }
    private T Editing<T>(Func<T> body)
    {
        EditingStarted?.Invoke();
        try { return body(); }
        finally { EditingEnded?.Invoke(); }
    }
    private static Button MakeCapButton(string text, int x, int y, int w, Action onClick)
    {
        var b = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 60),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f),
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
        b.SetBounds(x, y, w, 42);
        b.Click += (_, _) => onClick();
        return b;
    }
}
internal static class BandDialogs
{
    public static bool Confirm(string message)
    {
        using var f = MakeBase(140);
        var lbl = new Label { Text = message, AutoSize = false, ForeColor = Color.Gainsboro };
        lbl.SetBounds(16, 14, 368, 64);
        var yes = MakeBtn("Supprimer", DialogResult.Yes, 184, 92);
        var no = MakeBtn("Annuler", DialogResult.No, 294, 92);
        f.Controls.Add(lbl); f.Controls.Add(yes); f.Controls.Add(no);
        f.AcceptButton = no; f.CancelButton = no;
        return f.ShowDialog() == DialogResult.Yes;
    }
    public static string? Prompt(string message, string initial)
    {
        using var f = MakeBase(150);
        var lbl = new Label { Text = message, AutoSize = false, ForeColor = Color.Gainsboro };
        lbl.SetBounds(16, 12, 368, 20);
        var tb = new TextBox { Text = initial };
        tb.SetBounds(16, 36, 368, 24);
        var ok = MakeBtn("OK", DialogResult.OK, 184, 100);
        var cancel = MakeBtn("Annuler", DialogResult.Cancel, 294, 100);
        f.Controls.Add(lbl); f.Controls.Add(tb); f.Controls.Add(ok); f.Controls.Add(cancel);
        f.AcceptButton = ok; f.CancelButton = cancel;
        f.Shown += (_, _) => { tb.Focus(); tb.SelectAll(); };
        return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
    }
    public static void Info(string message)
    {
        using var f = MakeBase(130);
        var lbl = new Label { Text = message, AutoSize = false, ForeColor = Color.Gainsboro };
        lbl.SetBounds(16, 14, 368, 54);
        var ok = MakeBtn("OK", DialogResult.OK, 294, 86);
        f.Controls.Add(lbl); f.Controls.Add(ok);
        f.AcceptButton = ok; f.CancelButton = ok;
        f.ShowDialog();
    }
    private static Form MakeBase(int height) => new()
    {
        Text = "VertiMask",
        FormBorderStyle = FormBorderStyle.FixedDialog,
        StartPosition = FormStartPosition.CenterScreen,
        MaximizeBox = false,
        MinimizeBox = false,
        ShowInTaskbar = false,
        TopMost = true,
        ClientSize = new Size(400, height),
        BackColor = Color.FromArgb(32, 32, 36),
        Font = new Font("Segoe UI", 9f),
    };
    private static Button MakeBtn(string text, DialogResult result, int x, int y)
    {
        var b = new Button
        {
            Text = text,
            DialogResult = result,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(55, 55, 62),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 95);
        b.SetBounds(x, y, 96, 28);
        return b;
    }
}
internal sealed class BandForm : Form
{
    public BandForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        TopMost = true;
        Text = "VertiMask_Band";
    }
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST | Native.WS_EX_NOACTIVATE;
            return cp;
        }
    }
    protected override bool ShowWithoutActivation => true;
}