using System.Diagnostics;
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

var successA = solutionA() is [ "Paula", "Lucas" ];
sw.Start();
for (int i = 0; i < N; i++)
    solutionA();
sw.Stop();
Console.WriteLine($"{"solution A", -20}{sw.ElapsedMilliseconds, -4} ms  {successA, -5}  8 lines");

var successB = solutionB() is [ "Paula", "Lucas" ];
sw.Restart();
for (int i = 0; i < N; i++)
    solutionB();
sw.Stop();
Console.WriteLine($"{"solution B", -20}{sw.ElapsedMilliseconds, -4} ms  {successB, -5}  63 lines");

var successC = solutionC() is [ "Paula", "Lucas" ];
sw.Restart();
for (int i = 0; i < N; i++)
    solutionC();
sw.Stop();
Console.WriteLine($"{"solution C", -20}{sw.ElapsedMilliseconds, -4} ms  {successC, -5}  5 lines");

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
    {
        int friends = json.IndexOf("\"friends\"", StringComparison.Ordinal);

        if (friends < 0)
        {
            names.Add(null);
            continue;
        }

        int i = json.IndexOf('[', friends);
        int end = json.IndexOf(']', i);

        if (i < 0 || end < 0)
        {
            names.Add(null);
            continue;
        }

        string? oldestName = null;
        int maxAge = int.MinValue;

        while (i < end)
        {
            int nameKey = json.IndexOf("\"name\"", i, end - i, StringComparison.Ordinal);

            if (nameKey < 0)
                break;

            int nameStart = json.IndexOf('"', nameKey + 6) + 1;
            int nameEnd = json.IndexOf('"', nameStart);

            int ageKey = json.IndexOf("\"age\"", nameEnd, end - nameEnd, StringComparison.Ordinal);
            if (ageKey < 0)
                break;

            int ageStart = json.IndexOf(':', ageKey) + 1;

            while (json[ageStart] == ' ')
                ageStart++;

            int ageEnd = ageStart;

            while (ageEnd < end && json[ageEnd] >= '0' && json[ageEnd] <= '9')
                ageEnd++;

            int age = int.Parse(json.AsSpan(ageStart, ageEnd - ageStart));

            if (age > maxAge)
            {
                maxAge = age;
                oldestName = json.Substring(nameStart, nameEnd - nameStart);
            }

            i = ageEnd;
        }

        names.Add(oldestName);
    }

    return names;
}

List<string?> solutionC()
{
    var data = 
        Dy.FromJson(jsons)
        ?.Select(item => item.friends.MaxBy("age").name);
    
    return null!;
}

record Person(string Name, int Age, List<Person> Friends, List<Person> Parents);