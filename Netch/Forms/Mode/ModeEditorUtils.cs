using System.IO;
using System.Text;

namespace Netch.Forms.Mode
{
    public static class ModeEditorUtils
    {
        public static string ToSafeFileName(string text)
        {
            var fileName = new StringBuilder(text);
            foreach (var c in Path.GetInvalidFileNameChars())
                fileName.Replace(c, '_');

            return fileName.ToString();
        }
    }
}