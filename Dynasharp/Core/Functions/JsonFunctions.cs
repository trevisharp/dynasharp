using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Dynasharp.Core.Functions;

/// <summary>
/// A class with many performatic json functions.
/// </summary>
public static class JsonFunctions
{
    static readonly SearchValues<char> Whitespace = SearchValues.Create(" \t\r\n");
    static readonly SearchValues<char> ScalarEnd = SearchValues.Create(",}] \t\r\n");

    /// <summary>
    /// Reads the primitive value that starts at <paramref name="start"/>.
    /// Returns default when <paramref name="start"/> is -1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static T? FindValue<T>(ReadOnlySpan<char> json, int start = 0)
        where T : ISpanParsable<T>
    {
        if (start < 0)
            return default;

        if (typeof(T) == typeof(string))
            return (T?)(object?)ReadString(json, start);

        var token = json[start..EndOfScalar(json, start)];
        return T.Parse(token, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the index of the first item of the array that starts at
    /// <paramref name="start"/>, or -1 when it is empty or not an array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int FindFirstItem(ReadOnlySpan<char> json, int start = 0)
    {
        if (start < 0 || json[start] != '[')
            return -1;

        int i = SkipWhitespace(json, start + 1);
        return i < 0 || json[i] == ']' ? -1 : i;
    }

    /// <summary>
    /// Returns the index of the item that follows the item starting at
    /// <paramref name="start"/> in the same array, or -1 when it is the last.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int FindNextItem(ReadOnlySpan<char> json, int start)
    {
        if (start < 0)
            return -1;

        return ItemAfter(json, EndOfValue(json, start));
    }

    /// <summary>
    /// Like <see cref="FindNextItem"/>, but <paramref name="position"/> points to a
    /// direct value of the current item (e.g. a <see cref="FindSubObject"/> result),
    /// so only the rest of the item is scanned instead of the whole item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int FindNextItemFrom(ReadOnlySpan<char> json, int position)
    {
        if (position < 0)
            return -1;

        int from = json[position] is '{' or '[' or '"'
            ? EndOfValue(json, position)
            : position;
        if (from < 0)
            return -1;

        int depth = 1;
        var reader = new StructuralReader(json, from);

        while (reader.Next(out int p))
        {
            switch (json[p])
            {
                case '"':
                    break;

                case '{' or '[':
                    depth++;
                    break;

                default:
                    if (--depth == 0)
                        return ItemAfter(json, p + 1);
                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Given the index right after an item, returns the index of the next
    /// item of the array, or -1 when it was the last.
    /// </summary>
    static int ItemAfter(ReadOnlySpan<char> json, int end)
    {
        int i = SkipWhitespace(json, end);
        if (i < 0 || json[i] != ',')
            return -1;

        return SkipWhitespace(json, i + 1);
    }

    /// <summary>
    /// Finds the start index of the value of a key. When <paramref name="start"/>
    /// points to a '{', searches the direct keys of that object; otherwise
    /// searches the following keys of the object that contains it. Nested
    /// objects are not searched. Returns -1 when the key is not found.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int FindSubObject(ReadOnlySpan<char> json, string key, int start = 0)
    {
        start = SkipWhitespace(json, start);
        if (start < 0)
            return -1;

        int depth = json[start] == '{' ? 0 : 1;
        var reader = new StructuralReader(json, start);

        while (reader.Next(out int p))
        {
            switch (json[p])
            {
                case '"':
                    if (depth == 1 && json[(p + 1)..reader.StringEnd].SequenceEqual(key))
                    {
                        int value = ValueOf(json, reader.StringEnd + 1);
                        if (value >= 0)
                            return value;
                    }
                    break;

                case '{' or '[':
                    depth++;
                    break;

                default: // '}' ou ']'
                    if (--depth == 0)
                        return -1;
                    break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the index right after the value that starts at <paramref name="start"/>,
    /// or -1 when the json ends first.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static int EndOfValue(ReadOnlySpan<char> json, int start)
    {
        if (json[start] is not ('{' or '[' or '"'))
            return EndOfScalar(json, start);

        int depth = 0;
        var reader = new StructuralReader(json, start);

        while (reader.Next(out int p))
        {
            switch (json[p])
            {
                case '"':
                    if (depth == 0)
                        return reader.StringEnd + 1;
                    break;

                case '{' or '[':
                    depth++;
                    break;

                default:
                    if (--depth == 0)
                        return p + 1;
                    break;
            }
        }

        return -1;
    }

    static int EndOfScalar(ReadOnlySpan<char> json, int start)
    {
        int end = json[start..].IndexOfAny(ScalarEnd);
        return end < 0 ? json.Length : start + end;
    }

    static string? ReadString(ReadOnlySpan<char> json, int start)
    {
        if (json[start] != '"')
            return null;

        int end = start;
        do
        {
            int quote = json[(end + 1)..].IndexOf('"');
            if (quote < 0)
                return null;
            end += quote + 1;
        }
        while (IsEscaped(json, end));

        return StringFunctions.Unescape(json[(start + 1)..end]);
    }

    /// <summary>
    /// Given the index right after a string, returns the index of its value
    /// when the string is a key; otherwise -1.
    /// </summary>
    static int ValueOf(ReadOnlySpan<char> json, int i)
    {
        i = SkipWhitespace(json, i);
        if (i < 0 || json[i] != ':')
            return -1;
        return SkipWhitespace(json, i + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int SkipWhitespace(ReadOnlySpan<char> json, int i)
    {
        for (int n = 0; n < 2; n++, i++)
        {
            if ((uint)i >= (uint)json.Length)
                return -1;
            if (json[i] is not (' ' or '\t' or '\r' or '\n'))
                return i;
        }

        int k = json[i..].IndexOfAnyExcept(Whitespace);
        return k < 0 ? -1 : i + k;
    }

    /// <summary>
    /// Walks the structural chars ('"', '{', '}', '[', ']') of a json from a
    /// start index using SIMD. A string is returned once, at its opening quote,
    /// with its closing quote in <see cref="StringEnd"/>; chars inside it are skipped.
    /// </summary>
    ref struct StructuralReader(ReadOnlySpan<char> json, int start)
    {
        readonly ReadOnlySpan<char> json = json;
        int next = start;
        int block;
        ulong mask;

        public int StringEnd;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Next(out int p)
        {
            if (!NextRaw(out p))
                return false;

            if (json[p] == '"')
            {
                int q;
                do
                {
                    if (!NextRaw(out q))
                        return false;
                }
                while (json[q] != '"' || IsEscaped(json, q));

                StringEnd = q;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool NextRaw(out int p)
        {
            while (mask == 0)
            {
                if (next >= json.Length)
                {
                    p = -1;
                    return false;
                }

                block = next;
                mask = FindStructurals(json, block, out int width);
                next += width;
            }

            p = block + BitOperations.TrailingZeroCount(mask);
            mask &= mask - 1;
            return true;
        }
    }

    /// <summary>
    /// Returns a mask where bit k is set when json[i + k] is one of
    /// '"', '{', '}', '[' or ']'. Uses the widest vector the hardware
    /// accelerates and falls back to a scalar loop.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static ulong FindStructurals(ReadOnlySpan<char> json, int i, out int width)
    {
        var chars = MemoryMarshal.Cast<char, ushort>(json[i..]);

        if (Vector512.IsHardwareAccelerated && chars.Length >= Vector512<ushort>.Count)
        {
            width = Vector512<ushort>.Count;
            return Structurals(Vector512.Create(chars)).ExtractMostSignificantBits();
        }

        if (Vector256.IsHardwareAccelerated && chars.Length >= Vector256<ushort>.Count)
        {
            width = Vector256<ushort>.Count;
            return Structurals(Vector256.Create(chars)).ExtractMostSignificantBits();
        }

        if (Vector128.IsHardwareAccelerated && chars.Length >= Vector128<ushort>.Count)
        {
            width = Vector128<ushort>.Count;
            return Structurals(Vector128.Create(chars)).ExtractMostSignificantBits();
        }

        width = Math.Min(chars.Length, 64);
        ulong mask = 0;
        for (int k = 0; k < width; k++)
            if (chars[k] is '"' or '{' or '}' or '[' or ']')
                mask |= 1ul << k;
        return mask;
    }

    static Vector512<ushort> Structurals(Vector512<ushort> v) =>
        Vector512.Equals(v, Vector512.Create((ushort)'"')) |
        Vector512.Equals(v, Vector512.Create((ushort)'{')) |
        Vector512.Equals(v, Vector512.Create((ushort)'}')) |
        Vector512.Equals(v, Vector512.Create((ushort)'[')) |
        Vector512.Equals(v, Vector512.Create((ushort)']'));

    static Vector256<ushort> Structurals(Vector256<ushort> v) =>
        Vector256.Equals(v, Vector256.Create((ushort)'"')) |
        Vector256.Equals(v, Vector256.Create((ushort)'{')) |
        Vector256.Equals(v, Vector256.Create((ushort)'}')) |
        Vector256.Equals(v, Vector256.Create((ushort)'[')) |
        Vector256.Equals(v, Vector256.Create((ushort)']'));

    static Vector128<ushort> Structurals(Vector128<ushort> v) =>
        Vector128.Equals(v, Vector128.Create((ushort)'"')) |
        Vector128.Equals(v, Vector128.Create((ushort)'{')) |
        Vector128.Equals(v, Vector128.Create((ushort)'}')) |
        Vector128.Equals(v, Vector128.Create((ushort)'[')) |
        Vector128.Equals(v, Vector128.Create((ushort)']'));

    /// <summary>
    /// A quote is escaped when preceded by an odd number of '\'.
    /// </summary>
    static bool IsEscaped(ReadOnlySpan<char> json, int quote)
    {
        int backslashes = 0;
        while (json[quote - 1 - backslashes] == '\\')
            backslashes++;
        return backslashes % 2 == 1;
    }
}