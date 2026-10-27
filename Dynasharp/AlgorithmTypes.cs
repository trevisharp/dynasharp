#pragma warning disable IDE1006
#pragma warning disable CS8981

namespace Dynasharp;

using Core;
using Core.AlgorithmObjects;

/// <summary>
/// Represents a ordered json. A json object with all
/// keys every in the same order.
/// </summary>
public class ojson : IAlgorithmType
{
    public Algorithm ToAlgorithm()
        => new OrderedJsonAlgorithm();
}