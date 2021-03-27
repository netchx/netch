using Netch.Utils;

namespace Netch.Models
{
    internal class TagItem<T>
    {
        private readonly string _text;

        public TagItem(T value, string text)
        {
            _text = text;
            Value = value;
        }

        public string Text => i18N.Translate(_text);

        public T Value { get; }
    }
}