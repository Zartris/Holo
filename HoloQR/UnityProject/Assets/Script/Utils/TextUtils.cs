using System;
using System.Text;

namespace Script.Utils
{
    public class TextUtils
    {
        /// <summary>
        /// For the text to wordwrap. For handing long text.
        /// </summary>
        /// <param name="text">the string to be wrapped</param>
        /// <param name="maxCharsPerLine">the character count that defines a line</param>
        /// <returns>string with line breaks inserted</returns>
        public static string WordWrap(string text, int maxCharsPerLine)
        {
            int pos = 0;
            int next = 0;
            StringBuilder stringBuilder = new StringBuilder();

            if (maxCharsPerLine < 1)
            {
                return text;
            }

            for (pos = 0; pos < text.Length; pos = next)
            {
                int endOfLine = text.IndexOf(Environment.NewLine, pos, StringComparison.Ordinal);

                if (endOfLine == -1)
                {
                    next = endOfLine = text.Length;
                }
                else
                {
                    next = endOfLine + Environment.NewLine.Length;
                }

                if (endOfLine > pos)
                {
                    do
                    {
                        int len = endOfLine - pos;

                        if (len > maxCharsPerLine)
                            len = BreakLine(text, pos, maxCharsPerLine);

                        stringBuilder.Append(text, pos, len);
                        stringBuilder.Append(Environment.NewLine);

                        pos += len;

                        while (pos < endOfLine && Char.IsWhiteSpace(text[pos]))
                        {
                            pos++;
                        }
                    } while (endOfLine > pos);
                }
                else
                {
                    stringBuilder.Append(System.Environment.NewLine);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Method to linebreak text
        /// </summary>
        /// <param name="text">the string to have line break inserted</param>
        /// <param name="pos">the character index where linebreak insertion is desired</param>
        /// <param name="max">maximum character count before linebreak</param>
        /// <returns></returns>
        public static int BreakLine(string text, int pos, int max)
        {
            int i = max - 1;

            while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
            {
                i--;
            }

            if (i < 0)
            {
                return max;
            }

            while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
            {
                i--;
            }

            return i + 1;
        }
    }
}