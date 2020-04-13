namespace Incode
{
	partial class Form1
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources =
				new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.appToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this._speedText = new System.Windows.Forms.TextBox();
			this._accelText = new System.Windows.Forms.TextBox();
			this._scrollScaleText = new System.Windows.Forms.TextBox();
			this._scrollAccelText = new System.Windows.Forms.TextBox();
			this.statusStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {this.toolStripStatusLabel1});
			this.statusStrip1.Location = new System.Drawing.Point(0, 161);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
			this.statusStrip1.Size = new System.Drawing.Size(257, 22);
			this.statusStrip1.SizingGrip = false;
			this.statusStrip1.Stretch = false;
			this.statusStrip1.TabIndex = 0;
			this.statusStrip1.Text = "statusStrip1";
			this.statusStrip1.ItemClicked +=
				new System.Windows.Forms.ToolStripItemClickedEventHandler(this.statusStrip1_ItemClicked);
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(39, 17);
			this.toolStripStatusLabel1.Text = "Ready";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {this.appToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip1.Size = new System.Drawing.Size(257, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// appToolStripMenuItem
			// 
			this.appToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
				{this.exitToolStripMenuItem});
			this.appToolStripMenuItem.Name = "appToolStripMenuItem";
			this.appToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
			this.appToolStripMenuItem.Text = "&App";
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(16, 38);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 15);
			this.label1.TabIndex = 2;
			this.label1.Text = "Speed";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(16, 69);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(36, 15);
			this.label2.TabIndex = 2;
			this.label2.Text = "Accel";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(15, 97);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(63, 15);
			this.label3.TabIndex = 2;
			this.label3.Text = "ScrollScale";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(15, 126);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(65, 15);
			this.label4.TabIndex = 2;
			this.label4.Text = "ScrollAccel";
			// 
			// _speedText
			// 
			this._speedText.Location = new System.Drawing.Point(92, 38);
			this._speedText.Name = "_speedText";
			this._speedText.Size = new System.Drawing.Size(90, 23);
			this._speedText.TabIndex = 1;
			this._speedText.Leave += new System.EventHandler(this._speedText_Leave);
			// 
			// _accelText
			// 
			this._accelText.Location = new System.Drawing.Point(92, 66);
			this._accelText.Name = "_accelText";
			this._accelText.Size = new System.Drawing.Size(90, 23);
			this._accelText.TabIndex = 2;
			this._accelText.Leave += new System.EventHandler(this._accelText_Leave);
			// 
			// _scrollScaleText
			// 
			this._scrollScaleText.Location = new System.Drawing.Point(92, 93);
			this._scrollScaleText.Name = "_scrollScaleText";
			this._scrollScaleText.Size = new System.Drawing.Size(90, 23);
			this._scrollScaleText.TabIndex = 3;
			this._scrollScaleText.Leave += new System.EventHandler(this._scrollScaleText_Leave);
			// 
			// _scrollAccelText
			// 
			this._scrollAccelText.Location = new System.Drawing.Point(92, 123);
			this._scrollAccelText.Name = "_scrollAccelText";
			this._scrollAccelText.Size = new System.Drawing.Size(90, 23);
			this._scrollAccelText.TabIndex = 4;
			this._scrollAccelText.Leave += new System.EventHandler(this._scrollAccelText_Leave);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(257, 183);
			this.Controls.Add(this._scrollAccelText);
			this.Controls.Add(this._scrollScaleText);
			this.Controls.Add(this._accelText);
			this.Controls.Add(this._speedText);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon) (resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "InCode";
			this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem appToolStripMenuItem;
		private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox _speedText;
        private System.Windows.Forms.TextBox _accelText;
        private System.Windows.Forms.TextBox _scrollScaleText;
        private System.Windows.Forms.TextBox _scrollAccelText;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
	}
}

