namespace Dynasharp.Core;

/// <summary>
/// Represents a valid algorithm type.
/// </summary>
public interface IAlgorithmType
{
    Algorithm ToAlgorithm();
}