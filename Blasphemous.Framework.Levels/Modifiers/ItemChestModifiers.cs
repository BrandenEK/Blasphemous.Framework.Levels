using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Helpers;
using Framework.Inventory;
using Framework.Managers;
using Framework.Util;
using UnityEngine;

namespace Blasphemous.Framework.Levels.Modifiers;

/// <summary>
/// Modifier for items lying on the ground
/// </summary>
public class GroundItemModifier : IModifier
{
    /// <inheritdoc/>
    public void Apply(GameObject obj, ObjectData data)
    {
        obj.name = $"Ground item {data.id}";

        UniqueId idComp = obj.GetComponent<UniqueId>();
        idComp.uniqueId = "ITEM-GROUND-" + data.id;

        InteractableInvAdd addComp = obj.GetComponent<InteractableInvAdd>();
        addComp.item = data.id;
        addComp.itemType = ItemHelper.GetItemTypeFromId(data.id);
    }
}

/// <summary>
/// Modifier for item in a sword-heart shrine
/// </summary>
public class ShrineItemModifier : IModifier
{
    /// <inheritdoc/>
    public void Apply(GameObject obj, ObjectData data)
    {
        obj.name = $"Shrine item {data.id}";

        GameObject item = obj.transform.GetChild(0).gameObject;
        item.transform.localPosition = new Vector3(1, 1, 0);

        UniqueId idComp = item.GetComponent<UniqueId>();
        idComp.uniqueId = "ITEM-SHRINE-" + data.id;

        InteractableInvAdd addComp = item.GetComponent<InteractableInvAdd>();
        addComp.item = data.id;
        addComp.itemType = InventoryManager.ItemType.Sword;

        GameObject shrine = obj.transform.GetChild(1).gameObject;
        shrine.transform.localPosition = Vector3.zero;
    }
}

/// <summary>
/// Modifier for item stored in a chest
/// </summary>
public class ChestModifier : IModifier
{
    /// <inheritdoc/>
    public void Apply(GameObject obj, ObjectData data)
    {
        obj.name = $"Chest {data.id}";

        UniqueId idComp = obj.GetComponent<UniqueId>();
        idComp.uniqueId = "CHEST-" + data.id;

        InteractableInvAdd addComp = obj.GetComponent<InteractableInvAdd>();
        addComp.item = data.id;
        addComp.itemType = ItemHelper.GetItemTypeFromId(data.id);
    }
}
