using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using KeyMouse;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using WindowsInput;

namespace IncodeWindow
{
	/// <summary>
	/// Yeah, this should be a service, or at least an app that minimises to the system tray.
	/// </summary>
	public partial class Form1 : Form
	{
		private KeyboardHookListener keyboardIn;
		private MouseHookListener mouseIn;
		
		private InputSimulator inputSimulator;
		private IMouseSimulator mouseOut;
		private IKeyboardSimulator keyboardOut;

		private bool controlled; // true while we control all input and output
		private const float Frequency = 100.0f; // Hertz

		private Timer timer;
		private float tx, ty; // the target mouse position

		private LowPass mx = new LowPass(Frequency, 1000, 2); // the filtered mouse position
		private LowPass my = new LowPass(Frequency, 1000, 2);

		[Flags]
		enum Command
		{
			Up = 1, Down = 2, Left = 4, Right = 8,

			ScrollUp, ScrollDown,
			LeftClick, RightClick, LeftDown, RightDown,
		}

		// TODO: expose these to UI
		public float Speed = 250;
		public float Accel = 11;
		public float ScrollScale = 0.7f;
		public float ScrollAccel = 1.15f; // amount of scroll events to make per second

		/// <summary>
		/// A pending thing to do - also used to map keys to actions
		/// </summary>
		class Action
		{
			public readonly Command Command;
			public DateTime Started;

			public Action(Command dir)
			{
				Command = dir;
			}
		}

		readonly Dictionary<Keys, Action> keys = new Dictionary<Keys, Action>();

		private readonly Stopwatch watch = new Stopwatch();

		// the key to press to activate the custom mode
		// works well for WASD 88-key blank keyboards ;)
		private const Keys overrideKey = Keys.OemBackslash; 

		public Form1()
		{
			InitializeComponent();

			Configure();

			InstallHooks();
		}

		private void Configure()
		{
			keys.Add(Keys.E, new Action(Command.Up));
			keys.Add(Keys.S, new Action(Command.Left));
			keys.Add(Keys.D, new Action(Command.Down));
			keys.Add(Keys.F, new Action(Command.Right));

			keys.Add(Keys.W, new Action(Command.ScrollUp));
			keys.Add(Keys.R, new Action(Command.ScrollDown));

			keys.Add(Keys.Space, new Action(Command.LeftDown));
		}

		private void InstallHooks()
		{
			inputSimulator = new InputSimulator();

			mouseOut = inputSimulator.Mouse;
			keyboardOut = inputSimulator.Keyboard;

			mouseIn = new MouseHookListener(new GlobalHooker()) {Enabled = true};
			keyboardIn = new KeyboardHookListener(new GlobalHooker()) {Enabled = true};

			keyboardIn.KeyDown += OnKeyDown;
			keyboardIn.KeyUp += OnKeyUp;

			timer = new Timer {Interval = (int) (1000/Frequency)};
			timer.Tick += PerformCommands;

			watch.Start();
		}

		private void PerformCommands(object sender, EventArgs e)
		{
			var dt = watch.ElapsedMilliseconds/1000.0f;
			watch.Restart();

			var now = DateTime.Now;
			var earliest = DateTime.MaxValue;

			// only used for cursor-movement keys
			foreach (var action in keys)
			{
				var act = action.Value;
				if (act.Started > DateTime.MinValue && act.Started < earliest && act.Command != Command.LeftDown)
					earliest = act.Started;
			}
			var millis = (float)(now - earliest).TotalMilliseconds;
			var scale = Accel * millis / 1000.0f;
			var delta = dt * Speed * scale;

			foreach (var action in keys)
			{
				var act = action.Value;
				if (act.Started == DateTime.MinValue)
					continue;
				
				//Debug.WriteLine(string.Format("now={0}, started={1}, ms={2}", now, action.Value.Started, millis));

				switch (act.Command)
				{
					case Command.Up:
						ty -= delta;
						break;
					case Command.Down:
						ty += delta;
						break;
					case Command.Left:
						tx -= delta;
						break;
					case Command.Right:
						tx += delta;
						break;

					case Command.ScrollUp:
						var ts = (now - act.Started).TotalSeconds;
						var accel = ScrollAccel*ts;
						var t = (int)(ts*accel*ScrollScale);
						mouseOut.VerticalScroll(t);
						break;

					case Command.ScrollDown:
						var ts2 = (now - act.Started).TotalSeconds;
						var accel2 = ScrollAccel*ts2;
						var t2 = (int)(ts2*accel2*ScrollScale);
						mouseOut.VerticalScroll(t2);
						break;
				}
			}

			// TODO: clip tx and ty against cursor bounds. not as trivial as it seems, due to multiple monitor configurations
			var fx = mx.Next(tx);
			var fy = my.Next(ty);

			// for accuracy, keep track of desired location in floats, and get nearest integer to set
			// allow for negative values correctly, as we all have multiple monitors!
			var nx = (int) (fx < 0 ? (fx - 0.5f) : (fx + 0.5f));
			var ny = (int) (fy < 0 ? (fy - 0.5f) : (fy + 0.5f));
			Cursor.Position = new Point(nx, ny);
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (!controlled)
			{
				if (e.KeyCode == overrideKey)
				{
					Eat(e);
					StartControl();
				}
				return;
			}

			if (!keys.ContainsKey(e.KeyCode))
				return;

			Eat(e);

			// we get key-down events as repeats - only set it the first time we get a key-down
			var action = keys[e.KeyCode];
			if (action.Started == DateTime.MinValue)
				action.Started = DateTime.Now;
			
			// TODO: this is a shitty hack and I need more or less scotch
			switch (e.KeyCode)
			{
				case Keys.W:
					mouseOut.VerticalScroll(1);
					break;
				case Keys.R:
					mouseOut.VerticalScroll(-1);
					break;
				case Keys.Space:
					mouseOut.LeftButtonDown();
					break;
			}
		}

		private static void Eat(KeyEventArgs e)
		{
			e.Handled = true;
			e.SuppressKeyPress = true;
		}

		private void StartControl()
		{
			var pos = Cursor.Position;
			tx = pos.X;
			ty = pos.Y;

			mx.Set(tx);
			my.Set(ty);

			controlled = true;
			timer.Enabled = true;
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (!controlled)
				return;

			if (e.KeyCode == overrideKey)
			{
				Eat(e);
				LeaveControl();
				return;
			}

			if (!keys.ContainsKey(e.KeyCode))
				return;

			Eat(e);

			// sentinel values are bad. I use one here to indicate that an action is not active.
			keys[e.KeyCode].Started = DateTime.MinValue;

			// kill me
			switch (e.KeyCode)
			{
				case Keys.Space:
					mouseOut.LeftButtonUp();
					break;
			}
		}

		private void LeaveControl()
		{
			controlled = false;
			timer.Enabled = false;

			// not needed, maybe, but it seems best to do this.
			// one scenario is that the user presses control, then the space (to simulate
			// a mouse down), then releases control, then space, resulting in a state where
			// the system believes it has a left button down but there is not.
			mouseOut.LeftButtonUp();

			//mouseOut.RightButtonUp();
		}
	}
}
