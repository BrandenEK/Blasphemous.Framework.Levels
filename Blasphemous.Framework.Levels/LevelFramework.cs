using Blasphemous.Framework.Levels.Loaders;
using Blasphemous.Framework.Levels.Modifiers;
using Blasphemous.ModdingAPI;
using Framework.Managers;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools.Level.Layout;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Blasphemous.Framework.Levels;

/// <summary>
/// Handles loading a modifying objects in levels
/// </summary>
public class LevelFramework : BlasMod
{
    internal LevelFramework() : base(ModInfo.MOD_ID, ModInfo.MOD_NAME, ModInfo.MOD_AUTHOR, ModInfo.MOD_VERSION) { }

    private readonly Dictionary<string, GameObject> _objects = new();
    private readonly IModifier _baseModifier = new BaseModifier();

    private readonly Dictionary<string, IEnumerable<ObjectData>> _additions = new();
    private readonly Dictionary<string, IEnumerable<ObjectData>> _modifications = new();
    private readonly Dictionary<string, IEnumerable<ObjectData>> _deletions = new();

    private bool _loadedObjects = false;

    private Transform x_currentObjectHolder;
    /// <summary>
    /// Only accessed when adding objects, so always set when loading scene with objects to add
    /// </summary>
    public Transform CurrentObjectHolder
    {
        get
        {
            if (x_currentObjectHolder == null)
                x_currentObjectHolder = Object.FindObjectOfType<LevelInitializer>().transform;

            return x_currentObjectHolder;
        }
    }

    /// <summary>
    /// Load level edits for all enabled mods
    /// </summary>
    protected override void OnInitialize()
    {
        foreach (var editDict in LoadAllEdits())
            ProcessLevelEdits(editDict);

        int amount = _additions.Keys.Concat(_modifications.Keys).Concat(_deletions.Keys).Distinct().Count();
        Log($"Loaded level modifications for {amount} scenes");
    }

    private IEnumerable<Dictionary<string, LevelEdit>> LoadAllEdits()
    {
        string levelsPath = Path.GetFullPath("Modding/levels");

        return Directory.GetDirectories(levelsPath)
            .Where(folder => IsModLoadedName(Path.GetFileName(folder), out _))
            .Select(LoadModEdits);
    }

    private Dictionary<string, LevelEdit> LoadModEdits(string folder)
    {
        return Directory.GetFiles(folder).ToDictionary(
            Path.GetFileNameWithoutExtension,
            path => JsonConvert.DeserializeObject<LevelEdit>(File.ReadAllText(path)));
    }

    /// <summary>
    /// Apply all additions, modifications, and deletions for this scene
    /// </summary>
    protected override void OnLevelPreloaded(string oldLevel, string newLevel)
    {
        bool hasAdditions = _additions.TryGetValue(newLevel, out var additions);
        bool hasModifications = _modifications.TryGetValue(newLevel, out var modifications);
        bool hasDeletions = _deletions.TryGetValue(newLevel, out var deletions);

        if (!hasAdditions && !hasModifications && !hasDeletions)
            return;

        Log("Applying level modifications for " + newLevel);
        if (hasAdditions) AddObjects(additions);
        if (hasModifications) ModifyObjects(modifications, newLevel);
        if (hasDeletions) DeleteObjects(deletions, newLevel);
    }

    /// <summary>
    /// When loading menu, load all necessary objects from other scenes
    /// </summary>
    protected override void OnLevelLoaded(string oldLevel, string newLevel)
    {
        if (!_loadedObjects && newLevel == "MainMenu")
        {
            _loadedObjects = true;
            Main.Instance.StartCoroutine(LoadNecessaryObjects());
        }
    }

    private void ProcessLevelEdits(Dictionary<string, LevelEdit> edits)
    {
        foreach (var edit in edits)
        {
            if (edit.Value.additions.Length > 0)
            {
                _additions[edit.Key] = _additions.TryGetValue(edit.Key, out var addition)
                    ? addition.Concat(edit.Value.additions)
                    : edit.Value.additions;
            }

            if (edit.Value.modifications.Length > 0)
            {
                _modifications[edit.Key] = _modifications.TryGetValue(edit.Key, out var modification)
                    ? modification.Concat(edit.Value.modifications)
                    : edit.Value.modifications;
            }

            if (edit.Value.deletions.Length > 0)
            {
                _deletions[edit.Key] = _deletions.TryGetValue(edit.Key, out var deletion)
                    ? deletion.Concat(edit.Value.deletions)
                    : edit.Value.deletions;
            }
        }
    }

    private void AddObjects(IEnumerable<ObjectData> objects)
    {
        foreach (var addition in objects.Where(x => CheckCondition(x.condition)))
        {
            if (!_objects.TryGetValue(addition.type, out GameObject storedObject))
                continue;

            if (!LevelRegister.TryGetModifier(addition.type, out IModifier modifier))
                continue;

            GameObject newObject = Object.Instantiate(storedObject, CurrentObjectHolder);
            _baseModifier.Apply(newObject, addition);
            modifier.Apply(newObject, addition);
        }
    }

    private void ModifyObjects(IEnumerable<ObjectData> objects, string level)
    {
        foreach (var modification in objects.Where(x => CheckCondition(x.condition)))
        {
            if (!LevelRegister.TryGetModifier(modification.type, out IModifier modifier))
                continue;

            Scene scene = GetSceneFromObjectData(level, modification.scene);
            GameObject existingObject = scene.FindObject(modification.path, false);

            if (existingObject == null)
                return;

            _baseModifier.Apply(existingObject, modification);
            modifier.Apply(existingObject, modification);
        }
    }

    private void DeleteObjects(IEnumerable<ObjectData> objects, string level)
    {
        foreach (var deletion in objects.Where(d => CheckCondition(d.condition)).GroupBy(d => d.scene))
        {
            Scene scene = GetSceneFromObjectData(level, deletion.Key);
            DisableObjectGroup(scene, deletion.Select(x => x.path));
        }
    }

    /// <summary>
    /// Deactivates all objects in a scene that exist at a path
    /// </summary>
    private void DisableObjectGroup(Scene scene, IEnumerable<string> disabledObjects)
    {
        // Store dictionary of root objects
        Dictionary<string, Transform> rootObjects = scene.FindRoots(false);

        // Loop through disabled objects and locate & disable them
        foreach (GameObject obj in disabledObjects.Select(rootObjects.FindObject))
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    /// <summary>
    /// Checks whether a modification's condition is met
    /// </summary>
    private bool CheckCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition))
            return true;

        int colon = condition.IndexOf(':');
        string conditionType = condition.Substring(0, colon);
        string conditionValue = condition.Substring(colon + 1);

        return conditionType switch
        {
            "flag" => Core.Events.GetFlag(conditionValue),
            "penitence" => Core.PenitenceManager.GetCurrentPenitence()?.Id == conditionValue,
            "gamemode" => Core.GameModeManager.CheckGameModeActive(conditionValue),
            _ => throw new System.ArgumentException($"Invalid condition type: {conditionType}")
        };
    }

    /// <summary>
    /// Gets the unity scene referenced by a modification or deletion
    /// </summary>
    private Scene GetSceneFromObjectData(string level, string scene)
    {
        return SceneManager.GetSceneByName(level + scene switch
        {
            "decoration" => "_DECO",
            "layout" => "_LAYOUT",
            "logic" => "_LOGIC",
            _ => throw new System.Exception("Invalid scene type for object deletion: " + scene)
        });
    }

    /// <summary>
    /// When loading the main menu for the first time, async load all necessary objects from their scenes
    /// </summary>
    private IEnumerator LoadNecessaryObjects()
    {
        foreach (var type in GetNecessaryObjects())
        {
            // Ensure a loader exists for this type of object
            if (!LevelRegister.TryGetLoader(type, out ILoader loader))
            {
                LogError($"There is no creator to handle {type} objects");
                continue;
            }

            // Tell it to load the object somehow
            yield return loader.Apply();
            GameObject loadedObject = loader.Result;

            if (loadedObject == null)
            {
                LogError("Failed to load object of type " + type);
                continue;
            }

            // Store it in the dictionary
            Log("Successfully loaded object of type " + type);
            loadedObject.name = type;
            loadedObject.transform.parent = Main.Instance.transform;
            loadedObject.transform.position = Vector3.zero;
            loadedObject.SetActive(false);
            _objects.Add(type, loadedObject);
        }

        // Fix camera after scene loads
        Camera.main.transform.position = new Vector3(0, 0, -10);
        Camera.main.backgroundColor = new Color(0, 0, 0, 1);
        yield return null;
    }

    /// <summary>
    /// Gets a list of all object types required by mods
    /// </summary>
    private IEnumerable<string> GetNecessaryObjects()
    {
        return _additions.Values.SelectMany(x => x).Select(x => x.type).Distinct();
    }

    /// <inheritdoc/>
    protected override void OnRegisterServices(ModServiceProvider provider)
    {
        // Items
        provider.RegisterObjectCreator("item-ground", new ObjectCreator(
            new SceneLoader("D02Z02S14_LOGIC", "LOGIC/INTERACTABLES/ACT_Collectible"),
            new GroundItemModifier()));
        provider.RegisterObjectCreator("item-shrine", new ObjectCreator(
            new SceneLoader("D06Z01S22_LOGIC", "LOGIC/INTERACTABLES"),
            new ShrineItemModifier()));

        // Chests
        IModifier chestModifier = new ChestModifier();
        provider.RegisterObjectCreator("chest-iron", new ObjectCreator(
            new SceneLoader("D01Z05S11_LOGIC", "LOGIC/INTERACTABLES/ACT_Iron Chest"), chestModifier));
        provider.RegisterObjectCreator("chest-gold", new ObjectCreator(
            new SceneLoader("D20Z02S02_LOGIC", "ACT_Wooden Chest"), chestModifier));
        provider.RegisterObjectCreator("chest-relic", new ObjectCreator(
            new SceneLoader("D17BZ01S01_LOGIC", "LOGIC/INTERACTABLES/ACT_Relicarium"), chestModifier));

        // Platforms
        provider.RegisterObjectCreator("platform-wood", new ObjectCreator(
            new SceneLoader("D05Z02S12_LOGIC", "LOGIC/INTERACTABLES/{0}"),
            new NoModifier("Platform")));
        provider.RegisterObjectCreator("platform-blood", new ObjectCreator(
            new SceneLoader("D17Z01S10_LOGIC", "LOGIC/INTERACTABLES/{0}"),
            new BloodModifier()));

        // spikes
        provider.RegisterObjectCreator("spikes-wasteland", new ObjectCreator(
            new SceneLoader("D01Z03S01_DECO", "MIDDLEGROUND/AfterPlayer/Spikes/{0}"),
            new SpikeModifier()));

        // Misc.
        provider.RegisterObjectCreator("lantern", new ObjectCreator(
            new SceneLoader("D20Z01S02_LOGIC", "LOGIC/INTERACTABLES/Chain Hook"),
            new NoModifier("Lantern")));
        provider.RegisterObjectCreator("bell-face", new ObjectCreator(
            new SceneLoader("D03Z02S06_LOGIC", "TRAPS/TRAP_SHOCK_ENEMY"),
            new NoModifier("Face bell")));
        provider.RegisterObjectCreator("bell-iron", new ObjectCreator(
            new SceneLoader("D03Z02S06_LOGIC", "TRAPS/TRAP_SHOCK_CHAIN_REACTION"),
            new NoModifier("Iron bell")));
        provider.RegisterObjectCreator("ladder-jondo", new ObjectCreator(
            new SceneLoader("D03Z02S02_DECO", "MIDDLEGROUND/AfterPlayer/Gameplay/Ladders/{0}"),
            new LadderModifier()));
    }
}
