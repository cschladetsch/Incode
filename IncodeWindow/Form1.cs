// (C) 2015-18 christian.schladetsch@gmail.com

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


namespace IncodeWindow
{
	/// <summary>
	/// Yeah, this should be a service, or at least an app that minimises to the system tray.
	/// </summary>
	public partial class Form1 : Form
	{
		// TODO: expose these to UI 
		public float Speed = 300;//250;
		public float Accel = 15;
		public float ScrollScale = 0.7f;
		public float ScrollAccel = 1.15f; // amount of scroll events to make per second

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

		// the key to press to activate the custom mode
		// works well for WASD 88-key blank keyboards ;)
		//private const Keys OverrideKey = Keys.OemBackslash; 
		private const Keys OverrideKey = Keys.RControlKey;
		private const Keys AbbrStartKey = Keys.RShiftKey;

        private bool _abbrMode;
        private Dictionary<string, List<Keys>> _abbreviations = new Dictionary<string, List<Keys>>();
        private List<Keys> _abbreviation = new List<Keys>();

        const string ConfigFileName = "Config.json";

		[Flags]
		enum Command
		{
			Up = 1, Down = 2, Left = 4, Right = 8,
            CursorLeft, CursorRight, CursorUp, CursorDown,

			ScrollUp, ScrollDown,
			LeftClick, RightClick, LeftDown, RightDown,

            InsertText,

            Escape,
		}

		/// <summary>
		/// A pending thing to do - also used to map keys to actions
		/// </summary>
		class Action
		{
			public readonly Command Command;	// what to do/emulate
			public DateTime Started;			// when the key was pressed

			public Action(Command dir)
			{
				Command = dir;
			}
		}

		public Form1()
		{
			InitializeComponent();

			Configure();

			InstallHooks();
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
            _abbreviations.Add("christian.schladetsch@gmail.com", new List<Keys>() { Keys.P });
            _abbreviations.Add("christian@liminalvr.com", new List<Keys>() { Keys.W });
            _abbreviations.Add("+61(0)476 561 112", new List<Keys>() { Keys.M });

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

			_timer = new Timer {Interval = (int) (1000/Frequency)};
			_timer.Tick += PerformCommands;

			_watch.Start();
		}

		private void PerformCommands(object sender, EventArgs e)
		{
			var dt = _watch.ElapsedMilliseconds/1000.0f;
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
			var millis = (float)(now - earliest).TotalMilliseconds;
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
				var t = (int)(ts * accel * ScrollScale); 

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

                    case Command.CursorLeft:
                        _keyboardOut.KeyDown(WindowsInput.Native.VirtualKeyCode.LEFT);
                        break;
                    case Command.CursorDown:
                        _keyboardOut.KeyDown(WindowsInput.Native.VirtualKeyCode.DOWN);
                        break;
                    case Command.CursorUp:
                        _keyboardOut.KeyDown(WindowsInput.Native.VirtualKeyCode.UP);
                        break;
                    case Command.CursorRight:
                        _keyboardOut.KeyDown(WindowsInput.Native.VirtualKeyCode.RIGHT);
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
                        Console.WriteLine("{0}", ke.ToString());
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
            if (e.KeyCode == Keys.Escape && _abbrMode)
            {
                _abbrMode = false;
                _abbreviation.Clear();
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

			if (!_controlled)
			{
				if (e.KeyCode == OverrideKey)
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

        /// <summary>
        /// Returns 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="prefix"></param>
        /// <returns>-1 if prefix doesn't match, 0 if prefix matches, 1 if prefix matches</returns>
        int StartsWith<T>(List<T> list, List<T> prefix) where T : IComparable
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
            if (!_abbrMode)
                return false;

            //Debug.WriteLine("Adding {0} to abbreviation", e.KeyCode);

            _abbreviation.Add(e.KeyCode);

            // check for an abbreviation being completed
            foreach (var kv in _abbreviations)
            {
                ////Debug.WriteLine(count++);
                Debug.WriteLine("Testing for " + kv.Key);

                Eat(e);

                var test = StartsWith(kv.Value, _abbreviation);
                switch (test)
                {
                    case 0:
                        Debug.WriteLine("Prefix matches so far, eating");
                        SystemSounds.Asterisk.Play();
                        return true;

                    case 1: 
                        Debug.WriteLine("Inserted: " + kv.Value);
                        SystemSounds.Hand.Play();
                        _inserting = kv.Key.Length;

                        _keyboardOut.TextEntry(kv.Key);
                        //System.Windows.Forms.SendKeys.Send(kv.Value);
                        //System.Windows.Forms.SendKeys.Flush();

                        _abbrMode = false;
                        _abbreviation.Clear();
                        return true;
                }
            }

            SystemSounds.Beep.Play();
            _abbreviation.Clear();
            _abbrMode = false;
            
            return false;
        }

        private int _inserting;

        /// <summary>
        /// Return true if we have just entered abbreviation mode
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool TestAbbreviationStart(Keys key)
        {
            if (_abbrMode)
                return true;

            if (key != AbbrStartKey)
                return false;

            _abbrMode = true;

            Debug.WriteLine("Entering abbreviation mode");

            return true;
        }

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (!_controlled)
				return;

			if (e.KeyCode == OverrideKey)
			{
				Eat(e);
				EndControl();
				return;
			}

			if (!_keys.ContainsKey(e.KeyCode))
				return;

			Eat(e);

			// sentinel values are bad. I use one here to indicate that an action is not active.
			_keys[e.KeyCode].Started = DateTime.MinValue;

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

			_controlled = true;
			_timer.Enabled = true;
		}

        /// <summary>
        /// Return normal input control to Windows
        /// </summary>
        private void EndControl()
		{
			_controlled = false;
			_timer.Enabled = false;

			// not needed, maybe, but it seems best to do this.
			// one scenario is that the user presses control, then the space (to simulate
			// a mouse down), then releases control, then space, resulting in a state where
			// the system believes it has a left button down but there is not.
			_mouseOut.LeftButtonUp();
		}
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: reset key state
            Application.Exit();
        }

        private void _scrollAccelText_Leave(object sender, EventArgs e)
        {
            WriteValue(f => ScrollAccel = f, _scrollAccelText);
        }

        private void _scrollScaleText_Leave(object sender, EventArgs e)
        {
            WriteValue(f => ScrollScale = f, _scrollScaleText);
        }

        private void _accelText_Leave(object sender, EventArgs e)
        {
            WriteValue(f => Accel = f, _accelText);
        }

        private void _speedText_Leave(object sender, EventArgs e)
        {
            WriteValue(f => Speed = f, _speedText);
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

            UpdateUI();
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
        }

        private void UpdateUI()
        {
            _speedText.Text = Speed.ToString();
            _accelText.Text = Accel.ToString();
            _scrollAccelText.Text = ScrollAccel.ToString();
            _scrollScaleText.Text = ScrollScale.ToString();

            // TODO: abbreviations
        }

	}
}
