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
                "age": 24
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
        ]
    }
    """
];

var data = 
    Dy.FromJson(jsons)
    .Select(item => item.friends.MaxBy("age").name);


