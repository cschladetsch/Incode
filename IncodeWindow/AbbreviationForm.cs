using System.Collections.Generic;
using System.Windows.Forms;

namespace IncodeWindow
{
    /// <summary>
    /// Show abbreviations list.
    /// </summary>
    public partial class AbbreviationForm : Form
    {
        public AbbreviationForm(Incode.IncodeWindow parent)
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            _abbrList.Items.Clear();
            KeyDown += parent.OnKeyDown;
        }

        public void Populate(Dictionary<string, string> fill)
        {

            foreach (var entry in fill)
            {
                var sub = new ListViewItem(new[] {entry.Key, entry.Value});
                _abbrList.Items.Add(sub);
            }
        }

        private void _abbrList_SelectedIndexChanged(object sender, System.EventArgs e)
        {

        }
    }
}
