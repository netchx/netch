using System.Linq;

namespace System.Windows.Forms
{
    [System.ComponentModel.DesignerCategory(@"Code")]
    public class SearchComboBox : ComboBox
    {
        public SearchComboBox()
        {
            AutoCompleteMode = AutoCompleteMode.Suggest;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            try
            {
                if (DesignMode || !string.IsNullOrEmpty(Text) || !Visible) return;

                ResetCompletionList();
            }
            finally
            {
                base.OnTextChanged(e);
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            try
            {
                if (DesignMode || e.KeyData == Keys.Up || e.KeyData == Keys.Down) return;

                if (e.KeyData == Keys.Return)
                {
                    e.Handled = true;
                    if (newList.Length > 0 && !newList.Select(o => o.ToString()).Contains(Text))
                    {
                        Text = newList[0].ToString();
                    }

                    DroppedDown = false;
                    return;
                }

                ReevaluateCompletionList();
            }
            finally
            {
                base.OnKeyUp(e);
            }
        }

        private void ResetCompletionList()
        {
            _previousSearchTerm = null;
            try
            {
                SuspendLayout();

                var originalList = (object[])Tag;
                if (originalList == null)
                {
                    Tag = originalList = Items.Cast<object>().ToArray();
                }

                if (Items.Count == originalList.Length) return;

                while (Items.Count > 0)
                {
                    Items.RemoveAt(0);
                }

                Items.AddRange(originalList);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private string _previousSearchTerm;
        private object[] newList;
        private void ReevaluateCompletionList()
        {
            var currentSearchTerm = Text.ToLowerInvariant();
            if (currentSearchTerm == _previousSearchTerm) return;

            _previousSearchTerm = currentSearchTerm;
            try
            {
                SuspendLayout();

                var originalList = (object[])Tag;
                if (originalList == null)
                {
                    Tag = originalList = Items.Cast<object>().ToArray();
                }

                if (string.IsNullOrEmpty(currentSearchTerm))
                {
                    if (Items.Count == originalList.Length) return;

                    newList = originalList;
                }
                else
                {
                    newList = originalList.Where(x => x.ToString().ToLowerInvariant().Contains(currentSearchTerm)).ToArray();
                }

                try
                {
                    while (Items.Count > 0)
                    {
                        Items.RemoveAt(0);
                    }
                }
                catch
                {
                    try
                    {
                        Items.Clear();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                Items.AddRange(newList.ToArray());
            }
            finally
            {
                if (currentSearchTerm.Length >= 2 && !DroppedDown)
                {
                    DroppedDown = true;
                    Cursor.Current = Cursors.Default;
                    Text = currentSearchTerm;
                    Select(currentSearchTerm.Length, 0);
                }

                if (Items.Count > 0)
                {
                    DroppedDown = false;
                    DroppedDown = true;
                }

                ResumeLayout(true);
            }
        }
    }
}
