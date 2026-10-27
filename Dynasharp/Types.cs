#pragma warning disable IDE1006
#pragma warning disable CS8981

namespace Dynasharp;

/// <summary>
/// Represents a text.
/// </summary>
public class text : Algorithm;

/// <summary>
/// Represents a list of some type T.
/// </summary>
public class list<T> : Algorithm
    where T : Algorithm;

/// <summary>
/// Represents a ordered json. A json object with all
/// keys every in the same order.
/// </summary>
public class ojson : Algorithm;