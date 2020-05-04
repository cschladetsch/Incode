// (C) 2015-20 christian.schladetsch@gmail.com

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
    public partial class Form1 : Form
    {
        public float Speed => _config.Speed;
        public float Accel => _config.Accel;
        public float ScrollScale => _config.ScrollScale;
        public float ScrollAccel => _config.ScrollAccel;
        public int ScrollAmount => _config.ScrollAmount;

        private bool Abbreviating
        {
            get => _abbrMode;
            set
            {
                _abbrMode = value;
                _abbreviation = "";
            }
        }
        
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
        private readonly LowPass _mx = new LowPass(Frequency, 1000, 2); // the filtered mouse position
        private readonly LowPass _my = new LowPass(Frequency, 1000, 2);
        private readonly Dictionary<Keys, Action> _keys = new Dictionary<Keys, Action>();
        private readonly Stopwatch _watch = new Stopwatch();
        private DateTime _controlStartTime;

        // the key to press to activate the custom mode
        // works well for WASD 88-key blank keyboards ;)
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

        public Form1()
        {
            InitializeComponent();
            Configure();
            InstallHooks();
            
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true; 
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
            _keys.Add(Keys.Space, new Action(Command.LeftDown));
            _keys.Add(Keys.G, new Action(Command.RightDown));
            _keys.Add(Keys.Q, new Action(Command.Abbreviate));
            // _keys.Add(Keys.RShiftKey, new Action(Command.InsertText));
            
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
            var dt = _watch.ElapsedMilliseconds/1000.0f;
            _watch.Restart();

            var now = DateTime.Now;
            var earliest = ButtonsDown(DateTime.MaxValue);

            // for mouse movement
            var millis = (float) (now - earliest).TotalMilliseconds;
            var scale = Accel*millis/1000.0f;
            var delta = dt*Speed*scale;

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
            // for accuracy, keep track of desired location in floats, and get nearest integer to set
            // allow for negative values correctly, as we all have multiple monitors!
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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // we're inserting a text expansion. in this case, we get phony key downs
            // from window's input system. ignore them.
            if (_inserting > 0)
            {
                _inserting--;
                return;
            }

            // if we're in the middle of an abbreviation, stop it
            if (e.KeyCode == Keys.Escape && Abbreviating)
            {
                Abbreviating = false;
                Eat(e);
                return;
            }

            switch (CheckCompleteAbbreviation(e))
            {
                case AbbrevResult.Matched:
                    return;
                case AbbrevResult.Matching:
                    return;
                case AbbrevResult.None:
                    break;
                case AbbrevResult.NoMatch:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (TestAbbreviationStart(e.KeyCode))
            {
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

            // we get repeated key-down events - only set it the first time we get a key-down
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
            }
        }

        private AbbrevResult CheckCompleteAbbreviation(KeyEventArgs e)
        {
            if (!Abbreviating)
                return AbbrevResult.None;
            
            // append char from keycode
            _abbreviation += new KeysConverter().ConvertToString(e.KeyData)?.ToLower();
            
            // eat the part of the abbreviation, even if it fails
            Eat(e);

            // check for an abbreviation being completed
            foreach (var kv in _config.Abbreviations)
            {
                var test = CheckAbbrev(kv.Key);
                switch (test)
                {
                    case AbbrevResult.Matching:
                        Trace($"Prefix {kv.Key} matches so far");
                        SystemSounds.Asterisk.Play();
                        return test;

                    case AbbrevResult.Matched:
                        Trace($"Inserting: {kv.Key} -> {kv.Value}");
                        SystemSounds.Exclamation.Play();
                        _inserting = kv.Value.Length;

                        _keyboardOut.TextEntry(kv.Value);
                        Abbreviating = false;
                        return test;
                }
            }

            Trace($"No abbrev found for {_abbreviation}");
            SystemSounds.Hand.Play();
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

            return true;
        }

        private static void Trace(string fmt, params object[] args)
            => Debug.WriteLine(string.Format(fmt, args));

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
                return;
            }

            if (!Controlled)
                return;

            if (!_keys.ContainsKey(e.KeyCode))
                return;

            // sentinel values are bad. I use one here to indicate that an action is not active.
            _keys[e.KeyCode].Started = DateTime.MinValue;

            Eat(e);

            // there is a better way
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
                Trace("Double click control");

                // TODO: move cursor to center of current display
                var screen = Screen.FromPoint(Cursor.Position);
                var area = screen.WorkingArea;
                Cursor.Position = new Point(area.Width / 2, area.Height / 2);

                Controlled = false;
                _timer.Enabled = false;
                _watch.Restart();
                return;
            }

            Controlled = true;
        }

        private void ResetMouseFilter()
        {
            //Trace("MouseCursor: {0}", Cursor.Position);
            _mx.Set(Cursor.Position.X);
            _my.Set(Cursor.Position.Y);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
            => Application.Exit();

        private void WriteValue(Action<float> write, TextBox text)
        {
            write(float.Parse(text.Text));
            WriteConfig();
        }

        private void ReadConfig()
        {
            var cfg = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
            Trace($"Reading from {cfg}");
            
            if (File.Exists(cfg))
            {
                var text = File.ReadAllText(cfg);
                _config = JsonConvert.DeserializeObject<ConfigData>(text);
                foreach (var ab in _config.Abbreviations)
                {
                    Trace($"{ab.Key} => {ab.Value}");
                }
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
        }

        private void WriteConfig()
            => File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(_config));

        private void _scrollAccelText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollAccel = f, _scrollAccelText);

        private void _scrollScaleText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollScale = f, _scrollScaleText);

        private void _scrollAmountText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.ScrollAmount = (int)f, _scrollAmount);

        private void _accelText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.Accel = f, _accelText);

        private void _speedText_Leave(object sender, EventArgs e)
            => WriteValue(f => _config.Speed = f, _speedText);
    }
}