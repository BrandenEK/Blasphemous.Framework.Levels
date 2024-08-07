﻿using Blasphemous.Framework.Levels.Loaders;
using Blasphemous.Framework.Levels.Modifiers;
using Blasphemous.ModdingAPI;
using System.Collections.Generic;

namespace Blasphemous.Framework.Levels;

/// <summary> Registers a new level modifier </summary>
public static class LevelRegister
{
    private static readonly Dictionary<string, ObjectCreator> _creators = new();
    internal static IEnumerable<KeyValuePair<string, ObjectCreator>> Creators => _creators;
    
    internal static bool TryGetLoader(string type, out ILoader loader)
    {
        bool success = _creators.TryGetValue(type, out var creator);
        loader = creator?.Loader;
        return success;
    }

    internal static bool TryGetModifier(string type, out IModifier modifier)
    {
        bool success = _creators.TryGetValue(type, out var creator);
        modifier = creator?.Modifier;
        return success;
    }

    /// <summary> Registers a new level modifier </summary>
    public static void RegisterObjectCreator(this ModServiceProvider provider, string type, ObjectCreator creator)
    {
        if (provider == null)
            return;

        if (_creators.ContainsKey(type))
            return;

        _creators.Add(type, creator);
        ModLog.Info($"Registered custom object creator: {type}");
    }
}
