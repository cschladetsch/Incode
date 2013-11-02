using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using WindowsInput;

namespace KeyMouse
{
	public partial class Form1 : Form
	{
		private readonly KeyboardHookListener keyboard;
		private readonly MouseHookListener mouse;
		private readonly InputSimulator inputSimulator;
		private bool control; // true while the control key is down

		private const float Frequency = 100.0f; // Hertz
		private readonly Timer timer;
		private float tx, ty;

		[Flags]
		enum Command
		{
			Up = 1, Down = 2, Left = 4, Right = 8,

			ScrollUp, ScrollDown,
			LeftClick, RightClick, LeftDown, RightDown,
		}

		private LowPass mx = new LowPass(Frequency, 1000, 2);
		private LowPass my = new LowPass(Frequency, 1000, 2);

		public float Speed = 250;
		public float Accel = 11;
		public float ScrollScale = 0.7f;
		public float ScrollAccel = 1.15f; // amount of scroll events to make per second

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

		public Form1()
		{
			InitializeComponent();

			keys.Add(Keys.E, new Action(Command.Up));
			keys.Add(Keys.S, new Action(Command.Left));
			keys.Add(Keys.D, new Action(Command.Down));
			keys.Add(Keys.F, new Action(Command.Right));

			keys.Add(Keys.W, new Action(Command.ScrollUp));
			keys.Add(Keys.R, new Action(Command.ScrollDown));

			inputSimulator = new InputSimulator();

			mouse = new MouseHookListener(new GlobalHooker()) { Enabled = true };
			keyboard = new KeyboardHookListener(new GlobalHooker()) { Enabled = true };

			keyboard.KeyDown += OnKeyDown;
			keyboard.KeyUp += OnKeyUp;

			// timer to move the mouse
			timer = new Timer { Enabled = true, Interval = (int)(1000/Frequency) };
			timer.Tick += MoveMouse;

			timer.Enabled = false;
			watch.Start();
		}

		private void MoveMouse(object sender, EventArgs e)
		{
			var dt = watch.ElapsedMilliseconds/1000.0f;
			watch.Restart();

			var now = DateTime.Now;
			var earliest = DateTime.MaxValue;
			foreach (var action in keys)
			{
				var act = action.Value;
				if (act.Started > DateTime.MinValue && act.Started < earliest)
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
						inputSimulator.Mouse.VerticalScroll(t);
						break;

					case Command.ScrollDown:
						var ts2 = (now - act.Started).TotalSeconds;
						var accel2 = ScrollAccel*ts2;
						var t2 = (int)(ts2*accel2*ScrollScale);
						inputSimulator.Mouse.VerticalScroll(t2);
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
			if (!control && e.KeyCode == Keys.OemBackslash)
			{
				var pos = Cursor.Position;
				tx = pos.X;
				ty = pos.Y;

				mx.Set(tx);
				my.Set(ty);

				//Debug.WriteLine("going to {0} {1}", tx, ty);
				
				control = true;
				timer.Enabled = true;
				e.Handled = true;
				e.SuppressKeyPress = true;
				return;
			}

			if (!control)
				return; 

			var eat = false;
			if (keys.ContainsKey(e.KeyCode))
			{
				eat = true;

				// we get key-down events as repeats - only set it the first time we get a keydown
				var action = keys[e.KeyCode];
				if (action.Started == DateTime.MinValue)
					action.Started = DateTime.Now;
			}
			
			switch (e.KeyCode)
			{
				case Keys.W:
					eat = true;
					inputSimulator.Mouse.VerticalScroll(1);
					break;
				case Keys.R:
					eat = true;
					inputSimulator.Mouse.VerticalScroll(-1);
					break;
				//case Keys.F:
				//	inputSimulator.Mouse.LeftButtonClick();
				//	break;
				//case Keys.G:
				//	inputSimulator.Mouse.RightButtonClick();
				//	break;
				case Keys.Space:
					eat = true;
					inputSimulator.Mouse.LeftButtonDown();
					break;
				//case Keys.LShiftKey:
				//	inputSimulator.Mouse.LeftButtonClick();
				//	break;
				//case Keys.LMenu:
				//	inputSimulator.Mouse.RightButtonClick();
				//	break;
			}
fini:
			if (eat)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.OemBackslash)
			{
				control = false;
				timer.Enabled = false;
				e.Handled = true;
				e.SuppressKeyPress = true;
				// not needed, maybe, but it seems best to do this.
				// one scenario is that the user presses control, then the space (to simulate
				// a mouse down), then releases control, then space, resulting in a state where
				// the system believes it has a left button down but there is not.
				inputSimulator.Mouse.LeftButtonUp();
				return;
			}

			if (!control)
				return;

			var eat = false;

			if (keys.ContainsKey(e.KeyCode))
			{
				eat = true;
				keys[e.KeyCode].Started = DateTime.MinValue;
			}
			switch (e.KeyCode)
			{
				case Keys.Space:
					eat = true;
					inputSimulator.Mouse.LeftButtonUp();
					break;
			}

			if (eat)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}
	}
}
