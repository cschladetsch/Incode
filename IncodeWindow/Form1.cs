// (C) 2015-20 christian.schladetsch@gmail.com

namespace Incode
{
    using System;
    using System.Media;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
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
        public float Speed = 300; //250;
        public float Accel = 15;
        public float ScrollScale = 0.7f;
        public float ScrollAccel = 1.15f; // amount of scroll events to make per second

        private bool Abbreviating
        {
            get => _abbrMode;
            set
            {
                _abbrMode = value;
                _abbreviation.Clear();
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
        private const Keys _abbrStartKey = Keys.NumLock;
        private readonly Dictionary<string, List<Keys>> _abbreviations = new Dictionary<string, List<Keys>>();
        private readonly List<Keys> _abbreviation = new List<Keys>();
        private const string ConfigFileName = "Config.json";
        private int _inserting;
        private bool _abbrMode;

        public Form1()
        {
            InitializeComponent();
            Configure();
            InstallHooks();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true; 
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            _keyboardIn.Stop();
            _mouseIn.Stop();
            
            _keyboardIn.Dispose();
            _mouseIn.Stop();
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
            _keys.Add(Keys.C, new Action(Command.RightDown));
            _keys.Add(Keys.RShiftKey, new Action(Command.InsertText));
            
            _abbreviations.Add("christian.schladetsch@gmail.com", new List<Keys>() {Keys.P});
            _abbreviations.Add("christian@schladetsch.com", new List<Keys>() {Keys.W});
            _abbreviations.Add("+61(0)476 561 112", new List<Keys>() {Keys.M});

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
            var earliest = DateTime.MaxValue;

            // only used for cursor-movement keys
            foreach (var action in _keys)
            {
                var act = action.Value;
                var button = act.Command == Command.LeftDown || act.Command == Command.RightDown;
                if (button)
                    continue;
                if (act.Started > DateTime.MinValue && act.Started < earliest)
                    earliest = act.Started;
            }

            // for mouse movement
            var millis = (float) (now - earliest).TotalMilliseconds;
            var scale = Accel * millis / 1000.0f;
            var delta = dt * Speed * scale;

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

            // TODO: clip against cursor bounds. not as trivial as it seems, due to multiple monitor configurations
            var fx = _mx.Next(_tx);
            var fy = _my.Next(_ty);

            // for accuracy, keep track of desired location in floats, and get nearest integer to set
            // allow for negative values correctly, as we all have multiple monitors!
            var nx = (int) (fx < 0 ? (fx - 0.5f) : (fx + 0.5f));
            var ny = (int) (fy < 0 ? (fy - 0.5f) : (fy + 0.5f));

            Cursor.Position = new Point(nx, ny);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (false)
            {
                foreach (var ke in Enum.GetValues(typeof(Keys)).Cast<Keys>())
                {
                    if (ke == e.KeyCode)
                    {
                        Trace("{0}", ke.ToString());
                    }
                }
            }

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

            if (CheckCompleteAbbreviation(e))
            {
                return;
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
                    _mouseOut.VerticalScroll(1);
                    break;
                case Keys.V:
                    _mouseOut.VerticalScroll(-1);
                    break;
                case Keys.C:
                    _mouseOut.RightButtonDown();
                    break;
                case Keys.Space:
                    _mouseOut.LeftButtonDown();
                    break;
            }
        }

        private static int StartsWith<T>(IEnumerable<T> list, IList<T> prefix) where T : IComparable
        {
            if (prefix.Count == 0)
                return -1;

            var n = 0;
            foreach (var a in list)
            {
                if (n >= prefix.Count)
                    return 0;

                if (!prefix[n++].Equals(a))
                    return -1;
            }

            return 1;
        }

        private bool CheckCompleteAbbreviation(KeyEventArgs e)
        {
            if (!Abbreviating)
                return false;

            _abbreviation.Add(e.KeyCode);

            // check for an abbreviation being completed
            foreach (var kv in _abbreviations)
            {
                Eat(e);

                var test = StartsWith(kv.Value, _abbreviation);
                switch (test)
                {
                    case 0:
                        Trace("Prefix matches so far");
                        SystemSounds.Asterisk.Play();
                        return true;

                    case 1:
                        Trace("Inserting: '{0}'", kv.Value);
                        SystemSounds.Hand.Play();
                        _inserting = kv.Key.Length;

                        _keyboardOut.TextEntry(kv.Key);
                        Abbreviating = false;
                        return true;
                }
            }

            SystemSounds.Beep.Play();
            Abbreviating = false;

            return false;
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

            if (key != _abbrStartKey)
                return false;

            Abbreviating = true;

            Trace("Entering abbreviation mode");

            return true;
        }

        private static void Trace(string fmt, params object[] args)
        {
            Debug.WriteLine(string.Format(fmt, args));
        }
        
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            //if (!Controlled)
            //return;

            if (e.KeyCode == _overrideKey)
            {
                Eat(e);
                Controlled = false;
                Trace("Not controlling");
                return;
            }

            if (!_keys.ContainsKey(e.KeyCode))
                return;

            // sentinel values are bad. I use one here to indicate that an action is not active.
            _keys[e.KeyCode].Started = DateTime.MinValue;

            Eat(e);

            // there is a better way
            switch (e.KeyCode)
            {
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

        private bool Controlled
        {
            get { return _controlled; }
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

        void ResetMouseFilter()
        {
            Trace("MouseCursor: {0}", Cursor.Position);
            _mx.Set(Cursor.Position.X);
            _my.Set(Cursor.Position.Y);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: reset key state
            Application.Exit();
        }

        private void WriteValue(Action<float> write, TextBox text)
        {
            write(float.Parse(text.Text));
            WriteConfig();
        }

        private void ReadConfig()
        {
            if (File.Exists(ConfigFileName))
            {
                dynamic cfg = JsonConvert.DeserializeObject(System.IO.File.ReadAllText(ConfigFileName));
                if (cfg != null)
                {
                    Speed = cfg.Speed;
                    Accel = cfg.Accel;
                    ScrollScale = cfg.ScrollScale;
                    ScrollAccel = cfg.ScrollAccel;
                }
            }

            UpdateUi();
        }

        private void WriteConfig()
        {
            var json = new
            {
                Speed,
                Accel,
                ScrollScale,
                ScrollAccel,
            };
            File.WriteAllText(ConfigFileName, JsonConvert.SerializeObject(json));

            // TODO: abbreviations
        }

        private void UpdateUi()
        {
            _speedText.Text = Speed.ToString();
            _accelText.Text = Accel.ToString();
            _scrollAccelText.Text = ScrollAccel.ToString();
            _scrollScaleText.Text = ScrollScale.ToString();

            // TODO: abbreviations
        }
        
        private void _scrollAccelText_Leave(object sender, EventArgs e)
            => WriteValue(f => ScrollAccel = f, _scrollAccelText);

        private void _scrollScaleText_Leave(object sender, EventArgs e)
            => WriteValue(f => ScrollScale = f, _scrollScaleText);

        private void _accelText_Leave(object sender, EventArgs e)
            => WriteValue(f => Accel = f, _accelText);

        private void _speedText_Leave(object sender, EventArgs e)
            => WriteValue(f => Speed = f, _speedText);

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }
    }
}