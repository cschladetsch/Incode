// (C) 2015-20 christian.schladetsch@gmail.com

using System.Runtime.InteropServices;
using AudioSwitcher.AudioApi.CoreAudio;
using IncodeWindow;
using LedCSharp;

namespace Incode
{
    using System;
    using System.Media;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;
    using MouseKeyboardActivityMonitor;
    using MouseKeyboardActivityMonitor.WinApi;
    using WindowsInput;
    using Newtonsoft.Json;

    /// <summary>
    /// Press and hold the MouseEscape key (default to Right-Control) to enter MouseMode.
    /// Remap Right-Control to CapsLock using:
    /// 
    /// Yeah, this should be a service, or at least an app that minimises to the system tray.
    /// </summary>
    public partial class IncodeWindow : Form
    {
        public float Speed => _config.Speed;
        public float Accel => _config.Accel;
        public float ScrollScale => _config.ScrollScale;
        public float ScrollAccel => _config.ScrollAccel;
        public int ScrollAmount => _config.ScrollAmount;
        public float FilterRes => _config.MouseFilterResonance;
        public float FilterFreq => _config.MouseFilterFrequency;

        private bool Abbreviating
        {
            get => _abbrMode;
            set
            {
                _abbrMode = value;
                _abbreviation = "";
                if (!value)
                {
                    _abbrevWindow?.Close();
                    _abbrevWindow = null;
                }
            }
        }

        // true if this app is interpreting and controlling input
        private bool Controlled
        {
            get => _controlled;
            set
            {
                _controlled = value;
                _timer.Enabled = value;

                if (value)
                    _controlStartTime = DateTime.Now;
                else
                    _mouseOut.LeftButtonUp();

                ResetMouseFilter();

                // TODO: can't find correct format for dll (although LogiNumLock tool works)
                //SetKeyboardLights();
            }
        }

        private readonly keyboardNames[] _incodeKeys =
        {
            keyboardNames.Q,
            keyboardNames.E,
            keyboardNames.S,
            keyboardNames.D,
            keyboardNames.F,
            keyboardNames.R,
            keyboardNames.V,
            keyboardNames.SPACE,
        };

        private void SetKeyboardLights()
        {
            int r = _controlled ? 255 : 0;
            int g = _controlled ? 255 : 0;
            int b = _controlled ? 0 : 255;
            foreach (var key in _incodeKeys)
            {
                LogitechGSDK.LogiLedSetLightingForKeyWithKeyName(key,r,g,b);
            }
        }

        private KeyboardHookListener _keyboardIn;
        private MouseHookListener _mouseIn;
        private InputSimulator _inputSimulator;
        private IMouseSimulator _mouseOut;
        private IKeyboardSimulator _keyboardOut;
        private bool _controlled; // true while we control all input and output
        private const float Frequency = 100.0f; // Hertz
        private Timer _timer;
        private float _tx, _ty; // the target mouse position
        private LowPass _mx = new LowPass(Frequency, 2000, 2.5f);
        private LowPass _my = new LowPass(Frequency, 2000, 2.5f);
        private readonly Dictionary<Keys, Action> _keys = new Dictionary<Keys, Action>();
        private readonly Stopwatch _watch = new Stopwatch();
        private DateTime _controlStartTime;

        // the key to press to activate the custom mode
        // works well for Wasd 88-key blank keyboards ;)
        //private const Keys OverrideKey = Keys.OemBackslash;   // for WASD 88-key
        private const Keys _overrideKey = Keys.RControlKey;
        private const string ConfigFileName = "Config.json";
        private int _inserting;
        private bool _mouseLeftDown;
        private bool _mouseRightDown;

        // enter abbreviation mode. press escape to leave
        private const Keys _abbrStartKey = Keys.Q;
        private bool _abbrMode;
        private string _abbreviation;
        private ConfigData _config;
        private AbbreviationForm _abbrevWindow;

        private void PlaySound(string name)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var effects = Path.Combine(docs, "SoundBoard");
            var sfx = Path.Combine(effects, name);
            var player = new SoundPlayer(sfx);
            player.Play();
        }

        public IncodeWindow()
        {
            InitializeComponent();
            Configure();
            InstallHooks();

            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true;

            //if (!LogitechGSDK.LogiLedInit())
            //{
            //    Console.Error.WriteLine("Failed to start LogiTech SDK. Plug in a keyboard or something.");
            //    return;
            //}

            //LogitechGSDK.LogiLedSetTargetDevice(LogitechGSDK.LOGI_DEVICETYPE_ALL);e
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _keyboardIn.Stop();
            _mouseIn.Stop();

            _keyboardIn.Dispose();
            _mouseIn.Dispose();

            base.OnFormClosed(e);
        }

        private void Configure()
        {
            // clearly, this should be configured via a file, and the UI.
            _keys.Add(Keys.Escape, new Action(Command.Escape));
            _keys.Add(Keys.E, new Action(Command.Up));
            _keys.Add(Keys.S, new Action(Command.Left));
            _keys.Add(Keys.D, new Action(Command.Down));
            _keys.Add(Keys.F, new Action(Command.Right));
            _keys.Add(Keys.R, new Action(Command.ScrollUp));
            _keys.Add(Keys.V, new Action(Command.ScrollDown));
            _keys.Add(Keys.G, new Action(Command.RightDown));
            _keys.Add(Keys.Q, new Action(Command.Abbreviate));
            _keys.Add(Keys.Space, new Action(Command.LeftDown));
            _keys.Add(Keys.D1, new Action(Command.VolumeDown));
            _keys.Add(Keys.D2, new Action(Command.VolumeUp));
            _keys.Add(Keys.D3, new Action(Command.VolumeMute));

            ReadConfig();
        }

        private void InstallHooks()
        {
            _inputSimulator = new InputSimulator();

            _mouseOut = _inputSimulator.Mouse;
            _keyboardOut = _inputSimulator.Keyboard;

            _mouseIn = new MouseHookListener(new GlobalHooker()) {Enabled = true};
            _keyboardIn = new KeyboardHookListener(new GlobalHooker()) {Enabled = true};

            _keyboardIn.KeyDown += OnKeyDown;
            _keyboardIn.KeyUp += OnKeyUp;

            _timer = new Timer {Interval = (int) (1000 / Frequency)};
            _timer.Tick += PerformCommands;

            _watch.Start();
        }

        private void PerformCommands(object sender, EventArgs e)
        {
            var dt = _watch.ElapsedMilliseconds / 1000.0f;
            _watch.Restart();

            var now = DateTime.Now;
            var earliest = ButtonsDown(DateTime.MaxValue);

            // for mouse movement
            var millis = (float) (now - earliest).TotalMilliseconds;
            var scale = Accel * millis / 1000.0f;
            var delta = dt * Speed * scale;

            PerformActions(now, delta);

            MoveMouse();
        }

        private DateTime ButtonsDown(DateTime earliest)
        {
            foreach (var action in _keys)
            {
                var act = action.Value;
                var button = act.Command == Command.LeftDown || act.Command == Command.RightDown;
                if (button)
                    continue;

                if (act.Started > DateTime.MinValue && act.Started < earliest)
                    earliest = act.Started;
            }

            return earliest;
        }

        private void MoveMouse()
        {
            // For accuracy, keep track of desired location in floats, and get nearest integer to set.
            // Allow for negative values correctly, as we all have multiple monitors!
            var fx = _mx.Next(_tx);
            var fy = _my.Next(_ty);
            var nx = (int) (fx < 0 ? (fx - 0.5f) : (fx + 0.5f));
            var ny = (int) (fy < 0 ? (fy - 0.5f) : (fy + 0.5f));

            Cursor.Position = new Point(nx, ny);
        }

        private void PerformActions(DateTime now, float delta)
        {
            foreach (var action in _keys)
            {
                var act = action.Value;
                if (act.Started == DateTime.MinValue)
                    continue;

                // for vertical scroll
                var ts = (now - act.Started).TotalSeconds;
                var accel = 1 + ScrollAccel * ts;
                var t = (int) (ts * accel * ScrollScale);

                switch (act.Command)
                {
                    case Command.Up:
                        _ty -= delta;
                        break;
                    case Command.Down:
                        _ty += delta;
                        break;
                    case Command.Left:
                        _tx -= delta;
                        break;
                    case Command.Right:
                        _tx += delta;
                        break;
                    case Command.ScrollUp:
                        _mouseOut.VerticalScroll(t);
                        break;
                    case Command.ScrollDown:
                        _mouseOut.VerticalScroll(-t);
                        break;
                }
            }
        }

        public void DeltaVolume(int amount)
        {
            //var device = new CoreAudioController().DefaultPlaybackDevice;
            //device.Volume += amount;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            // We're inserting a text expansion. in this case, we get phony key downs.
            // From window's input system. ignore them.
            if (_inserting > 0)
            {
                _inserting--;
                return;
            }

            // If we're in the middle of an abbreviation, stop it.
            if (e.KeyCode == Keys.Escape && Abbreviating)
            {
                Abbreviating = false;
                Eat(e);
                return;
            }

            switch (CheckCompleteAbbreviation(e))
            {
                case AbbrevResult.Matching:
                    PlaySound("MacroCorrect.wav");
                    return;
                case AbbrevResult.NoMatch:
                    PlaySound("MacroFailed.wav");
                    return;
                case AbbrevResult.None:
                    //PlaySound("MacroFailed.wav");
                    break;
                case AbbrevResult.Matched:
                    PlaySound("MacroSuccess.wav");
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (TestAbbreviationStart(e.KeyCode))
            {
                ShowAbbreviations();
                Eat(e);
                return;
            }

            if (!Controlled)
            {
                if (e.KeyCode == _overrideKey)
                {
                    Eat(e);
                    StartControl();
                }

                return;
            }

            Eat(e);

            if (!_keys.ContainsKey(e.KeyCode))
                return;

            // We get repeated key-down events - only set it the first time we get a key-down.
            var action = _keys[e.KeyCode];
            if (action.Started != DateTime.MinValue)
                return;

            action.Started = DateTime.Now;

            switch (e.KeyCode)
            {
                case Keys.R:
                    _mouseOut.VerticalScroll(ScrollAmount);
                    break;
                case Keys.V:
                    _mouseOut.VerticalScroll(-ScrollAmount);
                    break;
                case Keys.G:
                    _mouseOut.RightButtonDown();
                    _mouseRightDown = true;
                    break;
                case Keys.Space:
                    _mouseOut.LeftButtonDown();
                    _mouseLeftDown = true;
                    break;
                case Keys.D1:
                    DeltaVolume(10);
                    break;
                case Keys.D2:
                    DeltaVolume(-10);
                    break;
                case Keys.D3:
                    //new CoreAudioController().DefaultPlaybackDevice.ToggleMute();
                    break;
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(int hwnd);

        private void ShowAbbreviations()
        {
            // Need to show a window of available abbreviations, but want all subsequent input to
            // be sent to current (native) control.
            var current = GetForegroundWindow();
            var cp = Cursor.Position;
            _abbrevWindow?.Close();
            _abbrevWindow = new AbbreviationForm(this) { Location = new Point(cp.X + 20, cp.Y - 20) };
            _abbrevWindow.Populate(_config.Abbreviations);
            _abbrevWindow.Show();
            SetForegroundWindow(current);
        }

        private AbbrevResult CheckCompleteAbbreviation(KeyEventArgs e)
        {
            if (!Abbreviating)
                return AbbrevResult.None;

            // Append char from keycode.
            _abbreviation += new KeysConverter().ConvertToString(e.KeyData)?.ToLower();

            // Eat the part of the abbreviation, even if it fails.
            Eat(e);

            // Check for an abbreviation being completed.
            foreach (var kv in _config.Abbreviations)
            {
                var test = CheckAbbrev(kv.Key);
                switch (test)
                {
                    case AbbrevResult.Matching:
                        Trace($"Prefix {kv.Key} matches so far");
                        return test;

                    case AbbrevResult.Matched:
                        Trace($"Inserting: {kv.Key} -> {kv.Value}");
                        _inserting = kv.Value.Length;

                        _keyboardOut.TextEntry(kv.Value);
                        Abbreviating = false;
                        return test;
                }
            }

            Trace($"No abbrev found for {_abbreviation}");
            Abbreviating = false;

            return AbbrevResult.NoMatch;
        }

        private AbbrevResult CheckAbbrev(string key)
        {
            if (_abbreviation.ToLower() == key)
                return AbbrevResult.Matched;
            return key.StartsWith(_abbreviation) ? AbbrevResult.Matching : AbbrevResult.NoMatch;
        }

        /// <summary>
        /// Return true if we have just entered abbreviation mode
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private bool TestAbbreviationStart(Keys key)
        {
            if (Abbreviating)
                return true;

            if (!Controlled)
                return false;

            if (key != _abbrStartKey)
                return false;

            Abbreviating = true;

            Trace("Entering abbreviation mode");
            PlaySound("MacroStart.wav");

            return true;
        }

        private static void Trace(string fmt, params object[] args)
            => Debug.WriteLine(fmt, args);

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _overrideKey)
            {
                if (_mouseLeftDown)
                {
                    _mouseOut.LeftButtonUp();
                    _mouseLeftDown = false;
                }

                if (_mouseRightDown)
                {
                    _mouseOut.RightButtonUp();
                    _mouseRightDown = false;
                }

                Eat(e);
                Controlled = false;
                Trace("Not controlling");
                _abbrevWindow?.Close();
                Abbreviating = false;
                return;
            }

            if (!Controlled)
                return;

            if (!_keys.ContainsKey(e.KeyCode))
                return;

            // Sentinel values are bad. I use one here to indicate that an action is not active.
            _keys[e.KeyCode].Started = DateTime.MinValue;

            Eat(e);

            // TODO There is a better way!
            switch (e.KeyCode)
            {
                case Keys.G:
                    _mouseOut.RightButtonUp();
                    break;
                case Keys.Space:
                    _mouseOut.LeftButtonUp();
                    break;
            }
        }

        private static void Eat(KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        /// <summary>
        /// Take over keyboard control from Windows
        /// </summary>
        private void StartControl()
        {
            var pos = Cursor.Position;
            _tx = pos.X;
            _ty = pos.Y;

            _mx.Set(_tx);
            _my.Set(_ty);

            var delta = DateTime.Now - _controlStartTime;
            if (delta.TotalMilliseconds < 300)
            {
                CenterCursor();
                return;
            }

            Controlled = true;
        }

        /// <summary>
        /// Move the cursor to the center of the first display.
        /// </summary>
        private void CenterCursor()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            var area = screen.WorkingArea;
            Cursor.Position = new Point(area.Width / 2, area.Height / 2);

            Controlled = false;
            _timer.Enabled = false;
            _watch.Restart();

            ResetMouseFilter();
        }

        private void ResetMouseFilter()
        {
            Trace("MouseCursor: {0}", Cursor.Position);
            _mx.Set(Cursor.Position.X);
            _my.Set(Cursor.Position.Y);
        }

        private void WriteValue(Action<float> write, TextBox text)
        {
            write(float.Parse(text.Text));
            WriteConfig();
        }

        private void ReadConfig()
        {
            var configFileName = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);

            if (File.Exists(configFileName))
            {
                var text = File.ReadAllText(configFileName);
                _config = JsonConvert.DeserializeObject<ConfigData>(text);
            }

            UpdateUi();
        }

        private void UpdateUi()
        {
            _speedText.Text = Speed.ToString();
            _accelText.Text = Accel.ToString();
            _scrollAccelText.Text = ScrollAccel.ToString();
            _scrollScaleText.Text = ScrollScale.ToString();
            _scrollAmount.Text = ScrollAmount.ToString();
            _filterFreq.Text = FilterFreq.ToString();
            _filterRes.Text = FilterRes.ToString();
        }

        private void WriteConfig()
            => File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(_config));

        private void _scrollAccelText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollAccel = f, _scrollAccelText);

        private void _scrollScaleText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollScale = f, _scrollScaleText);

        private void _scrollAmountText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollAmount = (int) f, _scrollAmount);

        private void _accelText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.Accel = f, _accelText);

        private void _speedText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.Speed = f, _speedText);

        private void _filterFreq_Leave(object sender, EventArgs e)
        {
            WriteValue(f => _config.MouseFilterFrequency = f, _filterFreq);
            UpdateMouseFilter();
        }

        private void _filterRes_Leave(object sender, EventArgs e)
        {
            WriteValue(f => _config.MouseFilterResonance = f, _filterRes);
            UpdateMouseFilter();
        }

        private void UpdateMouseFilter()
        {
            _mx = new LowPass(Frequency, _config.MouseFilterFrequency, _config.MouseFilterResonance);
            _my = new LowPass(Frequency, _config.MouseFilterFrequency, _config.MouseFilterResonance);
            ResetMouseFilter();
        }

        private void _exitToolStripMenuItem_Click(object sender, EventArgs e)
            => Application.Exit();
    }
}