using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using Dynasharp;
using Dynasharp.Core.Functions;

List<string> jsons = [
    """
    {
        "name": "Pamella",
        "age": 26,
        "friends": [
            {
                "name": "Paula",
                "age": 26
            },
            {
                "name": "Bruna",
                "age": 24,
                "friends": [
                    {
                        "name": "Heloisa",
                        "age": 25
                    }
                ]
            }
        ],
        "parents" : [
            {
                "name": "Rafael",
                "age": 49
            },
            {
                "name": "Viviane",
                "age": 47,
                "friends": [
                    {
                        "name": "Fernando",
                        "age": 40
                    }
                ]
            }
        ]
    }
    """,
    """
    {
        "name": "Leonardo",
        "age": 27,
        "friends": [
            {
                "name": "Bernardo",
                "age": 26
            },
            {
                "name": "Lucas",
                "age": 27
            }
        ],
        "parents" : [
            {
                "name": "Caroline",
                "age": 56,
                "friends": [
                    {
                        "name": "Luciane",
                        "age": 48
                    }
                ]
            }
        ]
    }
    """
];

const int N = 100_000;
var sw = new Stopwatch();

evaluate("Solução Genérica", solutionA, 10);
evaluate("Solução Própria", solutionB, 141);
evaluate("Dynasharp", solutionC, 4);

void evaluate(string title, Func<List<string?>> solution, int lines)
{
    var success = solution() is ["Paula", "Lucas"];

    sw.Restart();
    for (int i = 0; i < N; i++)
        solution();
    sw.Stop();

    Console.WriteLine($"{title,-20}{sw.ElapsedMilliseconds,-4} ms  {success,-5}  {lines} lines");
}

List<string?> solutionA()
{
    var data =
        from json in jsons
        let person = JsonSerializer.Deserialize<Person>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        select person.Friends.MaxBy(f => f.Age)?.Name;
    
    return [ ..data ];
}

List<string?> solutionB()
{
    List<string?> names = [with(jsons.Count)];

    foreach (string json in jsons)
        names.Add(oldestFriend(json));

    return names;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static string? oldestFriend(string json)
    {
        ReadOnlySpan<char> s = json;

        int friends = s.IndexOf("\"friends\"");
        if (friends < 0)
            return null;

        int open = s[friends..].IndexOf('[');
        if (open < 0)
            return null;

        int strStart = -1;
        bool nextIsName = false;
        Range name = default, best = default;
        int maxAge = -1;

        for (int i = friends + open + 1; i < s.Length; i += 16)
        {
            for (uint mask = quotesAndCloses(s, i); mask != 0; mask &= mask - 1)
            {
                int p = i + BitOperations.TrailingZeroCount(mask);

                if (strStart < 0)
                {
                    if (s[p] == ']')
                        return maxAge < 0 ? null : unescape(s[best]);

                    strStart = p + 1;
                    continue;
                }

                if (s[p] != '"' || isEscaped(s, p))
                    continue;

                Range str = strStart..p;
                strStart = -1;

                if (nextIsName)
                {
                    name = str;
                    nextIsName = false;
                }
                else if (s[str] is "name")
                    nextIsName = true;
                else if (s[str] is "age")
                {
                    int age = readInt(s, p + 1);
                    if (age > maxAge)
                        (maxAge, best) = (age, name);
                }
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static uint quotesAndCloses(ReadOnlySpan<char> s, int i)
    {
        if (i + 16 > s.Length)
        {
            uint tail = 0;
            for (int k = 0; i + k < s.Length; k++)
                if (s[i + k] is '"' or ']')
                    tail |= 1u << k;
            return tail;
        }

        var v = Vector256.Create(MemoryMarshal.Cast<char, ushort>(s.Slice(i, 16)));
        var matches = Vector256.Equals(v, Vector256.Create((ushort)'"'))
                    | Vector256.Equals(v, Vector256.Create((ushort)']'));
        return matches.ExtractMostSignificantBits();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool isEscaped(ReadOnlySpan<char> s, int quote)
    {
        int backslashes = 0;
        while (s[quote - 1 - backslashes] == '\\')
            backslashes++;
        return backslashes % 2 == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int readInt(ReadOnlySpan<char> s, int i)
    {
        while (!char.IsAsciiDigit(s[i]))
            i++;

        int value = 0;
        for (; i < s.Length && char.IsAsciiDigit(s[i]); i++)
            value = value * 10 + (s[i] - '0');
        return value;
    }

    static string unescape(ReadOnlySpan<char> raw)
    {
        int slash = raw.IndexOf('\\');
        if (slash < 0)
            return raw.ToString();

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
                _ => c,
            };

            raw = raw[(slash + (c == 'u' ? 6 : 2))..];
            slash = raw.IndexOf('\\');
        }

        raw.CopyTo(buffer[length..]);
        length += raw.Length;
        return new string(buffer[..length]);
    }
}

List<string?> solutionC()
{
    List<string?> names = [with(jsons.Count)];

    foreach (string json in jsons)
        names.Add(oldestFriend(json));

    return names;

    string? oldestFriend(string json)
    {
        var index = JsonFunctions.FindSubObject(json, "friends");
        var itemIndex = JsonFunctions.FindFirstItem(json, index);

        if (itemIndex == -1)
            return null;

        string? maxName = null;
        var maxValue = -1;

        while (itemIndex != -1)
        {
            var nameIndex = JsonFunctions.FindSubObject(json, "name", itemIndex);
            var ageIndex = JsonFunctions.FindSubObject(json, "age", nameIndex);
            
            var age = JsonFunctions.FindValue<int>(json, ageIndex);
            if (age > maxValue)
            {
                maxValue = age;
                maxName = JsonFunctions.FindValue<string>(json, nameIndex);
            }

            itemIndex = JsonFunctions.FindNextItem(json, itemIndex);
        }

        return maxName;
        
    }
}

record Person(string Name, int Age, List<Person> Friends, List<Person> Parents);