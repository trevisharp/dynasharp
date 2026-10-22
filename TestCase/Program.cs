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


void solutionA()
{
    var data =
        from json in jsons
        let person = JsonSerializer.Deserialize<Person>(json)
        select person.Friends.MaxBy(f => f.Age)?.Name;
}

void solutionB()
{
    List<string?> names = [];
    for (int i = 0; i < jsons.Count; i++)
    {
        var json = jsons[i];

        int k;
        int level = 0;
        bool infriends = false;

        for (k = 0; k < json.Length; k++)
        {
            var token = json[k];
            if (token is '[' or '{')
            {
                level++;
                continue;
            }
            
            if (token is ']' or '}')
            {
                level--;
                continue;
            }

            if (level != 1)
                continue;

            if (token != 'f')
                continue;
            
            if (json[k + 1] == 'r' && 
                json[k + 2] == 'i' &&
                json[k + 3] == 'e' &&
                json[k + 4] == 'n' &&
                json[k + 5] == 'd' &&
                json[k + 6] == 's'
            )
            {
                k += 6;
                infriends = true;
                break;
            }
        }

        if (!infriends)
        {
            names.Add(null);
            continue;
        }

        // TODO  
    }
}

void solutionC()
{
    var data = 
        Dy.FromJson(jsons)
        .Select(item => item.friends.MaxBy("age").name);
}

record Person(string Name, int Age, List<Person> Friends, List<Person> Parents);