using System.Globalization;

namespace Dynasharp.Internal;

public static class StringFunctions
{
    /// <summary>
    /// Converts the raw content of a json string (without the quotes) into
    /// its value, resolving escapes such as \", \\, \n and \uXXXX.
    /// </summary>
    public static string Unescape(ReadOnlySpan<char> raw)
    {
        int slash = raw.IndexOf('\\');
        if (slash < 0)
            return raw.ToString();

        // O texto desescapado nunca é maior que o original
        Span<char> buffer = raw.Length <= 256 ? stackalloc char[raw.Length] : new char[raw.Length];
        int length = 0;

        while (slash >= 0)
        {
            raw[..slash].CopyTo(buffer[length..]);
            length += slash;

            char c = raw[slash + 1];
            buffer[length++] = c switch
            {
                'b' => '\b',
                'f' => '\f',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                'u' => (char)ushort.Parse(raw.Slice(slash + 2, 4), NumberStyles.AllowHexSpecifier),
                _ => c, // aspas, barra invertida e barra
            };

            raw = raw[(slash + (c == 'u' ? 6 : 2))..];
            slash = raw.IndexOf('\\');
        }

        raw.CopyTo(buffer[length..]);
        length += raw.Length;
        return new string(buffer[..length]);
    }
}
