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
		public float ScrollScale = 2; // amount of scroll events to make per second

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

			keys.Add(Keys.W, new Action(Command.Up));
			keys.Add(Keys.A, new Action(Command.Left));
			keys.Add(Keys.S, new Action(Command.Down));
			keys.Add(Keys.D, new Action(Command.Right));
			keys.Add(Keys.R, new Action(Command.ScrollUp));
			keys.Add(Keys.F, new Action(Command.ScrollDown));

			inputSimulator = new InputSimulator();

			mouse = new MouseHookListener(new GlobalHooker()) { Enabled = true };
			keyboard = new KeyboardHookListener(new GlobalHooker()) { Enabled = true };

			keyboard.KeyDown += OnKeyDown;
			keyboard.KeyUp += OnKeyUp;

			screenWidth = Screen.PrimaryScreen.Bounds.Width;
			screenHeight = Screen.PrimaryScreen.Bounds.Height;

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
						var t = (int)((now - act.Started).TotalSeconds * ScrollScale);
						Debug.WriteLine(t);
						inputSimulator.Mouse.VerticalScroll(t);
						break;

					case Command.ScrollDown:
						var s = (int)((now - act.Started).TotalSeconds * ScrollScale);
						inputSimulator.Mouse.VerticalScroll(-s);
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
			if (e.KeyCode == Keys.OemBackslash)
			{
				var pos = System.Windows.Forms.Cursor.Position;
				tx = pos.X;
				ty = pos.Y;
				control = true;
				timer.Enabled = true;
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

				//// dirty 2am hack. need to normalise actions
				//if (action.Command == Command.ScrollDown || action.Command == Command.ScrollUp)
				//	goto fini;
			}
			
			switch (e.KeyCode)
			{
				//case Keys.R:
				//	inputSimulator.Mouse.VerticalScroll(1);
				//	break;
				//case Keys.F:
				//	inputSimulator.Mouse.VerticalScroll(-1);
				//	break;
				case Keys.F:
					inputSimulator.Mouse.LeftButtonClick();
					break;
				case Keys.G:
					inputSimulator.Mouse.RightButtonClick();
					break;
				case Keys.Space:
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
				zoomLevel = 0;
				timer.Enabled = false;
				rect = new Rectangle(0, 0, screenWidth, screenHeight);
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
					inputSimulator.Mouse.LeftButtonUp();
					break;
			}

			if (eat)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		enum Row
		{
			Top, Mid, Bottom
		}

		enum Column
		{
			Left, Mid, Right
		}

		private void OnKeyDown2(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.RControlKey)
			{
				control = true;
				return;
			}

			if (!control)
				return;

			var eat = false;
			switch (e.KeyCode)
			{
				case Keys.Q:
					eat = true;
					MoveMouseCursor(Row.Top, Column.Left);
					break;
				case Keys.W:
					eat = true;
					MoveMouseCursor(Row.Top, Column.Mid);
					break;
				case Keys.E:
					eat = true;
					MoveMouseCursor(Row.Top, Column.Right);
					break;
				case Keys.A:
					eat = true;
					MoveMouseCursor(Row.Mid, Column.Left);
					break;
				case Keys.S:
					eat = true;
					MoveMouseCursor(Row.Mid, Column.Mid);
					break;
				case Keys.D:
					eat = true;
					MoveMouseCursor(Row.Mid, Column.Right);
					break;
				case Keys.Z:
					eat = true;
					MoveMouseCursor(Row.Bottom, Column.Left);
					break;
				case Keys.X:
					eat = true;
					MoveMouseCursor(Row.Bottom, Column.Mid);
					break;
				case Keys.C:
					eat = true;
					MoveMouseCursor(Row.Bottom, Column.Right);
					break;

				case Keys.LControlKey:
					inputSimulator.Mouse.LeftButtonClick();
					break;
				case Keys.LWin:
					inputSimulator.Mouse.RightButtonClick();
					break;
			}

			if (eat)
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
			}
		}

		private int zoomLevel = 0;
		private Rectangle rect = new Rectangle();
		private int screenWidth, screenHeight;

		private void MoveMouseCursor(Row row, Column column)
		{
			zoomLevel++;

			var div = zoomLevel*3;
			rect.Width = screenWidth/div;
			rect.Height = screenHeight/div;

			switch (row)
			{
				case Row.Top:
					break;
				case Row.Mid:
					rect.Y += rect.Height;
					break;
				case Row.Bottom:
					rect.Y += rect.Height*2;
					break;
			}
			switch (column)
			{
				case Column.Left:
					break;
				case Column.Mid:
					rect.X += rect.Width;
					break;
				case Column.Right:
					rect.X += rect.Width*2;
					break;
			}

			SetCursorPos(rect.X + rect.Width/2, rect.Y + rect.Height/2);
		}

		[DllImport("user32")]
		internal static extern int SetCursorPos(int x, int y);
	}
}
