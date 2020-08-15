namespace IncodeWindow
{
    partial class Form2
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "One",
            "Bar"}, -1);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem(new string[] {
            "Two",
            "Foo"}, -1);
            this._abbrList = new System.Windows.Forms.ListView();
            this.Abbr = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Expand = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // _abbrList
            // 
            this._abbrList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Abbr,
            this.Expand});
            this._abbrList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._abbrList.HideSelection = false;
            this._abbrList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
            this._abbrList.Location = new System.Drawing.Point(0, 0);
            this._abbrList.Name = "_abbrList";
            this._abbrList.Size = new System.Drawing.Size(299, 136);
            this._abbrList.TabIndex = 0;
            this._abbrList.UseCompatibleStateImageBehavior = false;
            this._abbrList.View = System.Windows.Forms.View.Details;
            this._abbrList.SelectedIndexChanged += new System.EventHandler(this.listView1_SelectedIndexChanged);
            // 
            // Abbr
            // 
            this.Abbr.Text = "Abbr";
            this.Abbr.Width = 73;
            // 
            // Expand
            // 
            this.Expand.Text = "Expand";
            this.Expand.Width = 243;
            // 
            // Form2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(299, 136);
            this.ControlBox = false;
            this.Controls.Add(this._abbrList);
            this.Name = "Form2";
            this.ShowIcon = false;
            this.Text = "Macro";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView _abbrList;
        private System.Windows.Forms.ColumnHeader Abbr;
        private System.Windows.Forms.ColumnHeader Expand;
    }
}