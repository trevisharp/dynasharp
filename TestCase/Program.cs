using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json;
using Dynasharp;

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

evaluate("Solução Genérica", solutionA, 8);
evaluate("Solução Própria", solutionB, 119);
evaluate("Dynasharp", solutionC, 5);

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

        int i = s.IndexOf("\"friends\"");
        if (i < 0)
            return null;
        i += 9;

        int open = s[i..].IndexOf('[');
        if (open < 0)
            return null;
        i += open + 1;

        // Varre o array uma única vez com SIMD: cada bloco de 16 chars gera uma
        // máscara de '"' e de ']', e os bits são percorridos em ordem. Evita o
        // custo fixo de dezenas de chamadas a IndexOf em trechos curtos.
        ref ushort r = ref Unsafe.As<char, ushort>(ref MemoryMarshal.GetReference(s));
        var quote = Vector256.Create((ushort)'"');
        var close = Vector256.Create((ushort)']');

        int strStart = -1;
        bool nextIsName = false;
        int nameStart = -1, nameLength = 0;
        int bestStart = -1, bestLength = 0;
        int maxAge = int.MinValue;

        for (; i < s.Length; i += Vector256<ushort>.Count)
        {
            uint quotes = 0, closes = 0;

            if (i + Vector256<ushort>.Count <= s.Length)
            {
                var v = Vector256.LoadUnsafe(ref r, (nuint)i);
                quotes = Vector256.Equals(v, quote).ExtractMostSignificantBits();
                closes = Vector256.Equals(v, close).ExtractMostSignificantBits();
            }
            else
            {
                for (int k = 0; i + k < s.Length; k++)
                {
                    if (s[i + k] == '"')
                        quotes |= 1u << k;
                    else if (s[i + k] == ']')
                        closes |= 1u << k;
                }
            }

            bool done = closes != 0;
            if (done)
                quotes &= (closes & (0u - closes)) - 1; // só aspas antes do primeiro ']'

            while (quotes != 0)
            {
                int q = i + BitOperations.TrailingZeroCount(quotes);
                quotes &= quotes - 1;

                if (strStart < 0)
                {
                    strStart = q + 1;
                    continue;
                }

                ReadOnlySpan<char> str = s[strStart..q];

                if (nextIsName)
                {
                    nameStart = strStart;
                    nameLength = str.Length;
                    nextIsName = false;
                }
                else if (str is "name")
                {
                    nextIsName = true;
                }
                else if (str is "age")
                {
                    int p = q + 1;
                    uint digit;

                    while ((digit = (uint)(s[p] - '0')) > 9)
                        p++;

                    int age = 0;
                    do
                    {
                        age = age * 10 + (int)digit;
                        p++;
                    }
                    while ((digit = (uint)(s[p] - '0')) <= 9);

                    if (age > maxAge)
                    {
                        maxAge = age;
                        bestStart = nameStart;
                        bestLength = nameLength;
                    }
                }

                strStart = -1;
            }

            if (done)
                break;
        }

        return bestStart < 0 ? null : json.Substring(bestStart, bestLength);
    }
}

List<string?> solutionC()
{
    return null!;
}

record Person(string Name, int Age, List<Person> Friends, List<Person> Parents);