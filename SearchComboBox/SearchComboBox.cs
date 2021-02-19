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


        private string prevKeyword;

        private string Keyword
        {
            get => _keyword;
            set
            {
                prevKeyword = _keyword;
                if (value == null)
                {
                    _keyword = null;
                    return;
                }

                if (_keyword == value)
                {
                    return;
                }

                _keyword = value;
                ReevaluateCompletionList();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Text))
                {
                    if (!IsOriginalItems)
                        ResetCompletionList();
                    Keyword = null;
                }
                else
                {
                    if (AutoFillTag.All(o => o.ToString() != Text))
                    {
                        Keyword = Text;
                    }
                }
            }
            finally
            {
                base.OnTextChanged(e);
            }
        }


        private object[] AutoFillTag
        {
            get
            {
                if (Tag == null)
                {
                    Tag = Items.Cast<object>().ToArray();
                }

                return (object[]) Tag;
            }
        }

        private bool IsOriginalItems => Items.Count == AutoFillTag.Length;

        public void ResetCompletionList()
        {
            Keyword = null;
            try
            {
                SuspendLayout();

                if (IsOriginalItems)
                    return;

                Items.Clear();
                Items.AddRange(AutoFillTag);
            }
            finally
            {
                ResumeLayout(true);
            }
        }

        private static int findFirstDifIndex(string s1, string s2)
        {
            for (var i = 0; i < Math.Min(s1.Length, s2.Length); i++)
                if (s1[i] != s2[i])
                    return i;

            return -1;
        }

        private object[] _newList;
        private string _keyword;

        private void ReevaluateCompletionList()
        {
            SuspendLayout();
            var keyword = Keyword.ToLowerInvariant().Trim();

            var selectionStart = SelectionStart;
            if (selectionStart == Text.Length)
            {
                selectionStart = -1;
            }
            else
            {
                selectionStart = findFirstDifIndex(prevKeyword, Keyword);
            }

            try
            {
                var originalList = AutoFillTag;
                if (originalList == null)
                {
                    Tag = originalList = Items.Cast<object>().ToArray();
                }

                if (string.IsNullOrEmpty(Keyword))
                {
                    ResetCompletionList();
                    return;
                }
                else
                {
                    _newList = originalList.Where(x => x.ToString().ToLowerInvariant().Contains(keyword)).ToArray();
                }

                if (_newList.Any())
                {
                    Items.Clear();
                    Items.AddRange(_newList.ToArray());

                    if (!DroppedDown)
                    {
                        DroppedDown = true;
                    }
                    else
                    {
                        // TODO 预期下拉框高度变长则重新打开下拉框
                    }

                    Cursor.Current = Cursors.Default;
                }
                else
                {
                    DroppedDown = false;
                    Items.Clear();
                }

                if (selectionStart == -1)
                {
                    Select(Text.Length, 0);
                }
                else
                {
                    Select(selectionStart + 1, 0);
                }
            }
            finally
            {
                ResumeLayout(true);
            }
        }
    }
}