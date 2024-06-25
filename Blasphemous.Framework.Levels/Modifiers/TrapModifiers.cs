using UnityEngine;

namespace Blasphemous.Framework.Levels.Modifiers;

/// <summary>
/// modifier for spike objects
/// </summary>
public class SpikeModifier : IModifier
{
    private Vector2 _colliderSize;

    /// <inheritdoc/>
    public void Apply(GameObject obj, ObjectData data)
    {
        obj.name = "Spikes";

        obj.tag = "SpikeTrap";
        obj.layer = LayerMask.NameToLayer("Trap");

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = _colliderSize;
    }

    /// <summary>
    /// construct a basic spike with hitbox size (1.8, 0.8)
    /// </summary>
    public SpikeModifier()
    {
        _colliderSize = new Vector2(1.8f, 0.8f);
    }

    /// <summary>
    /// construct spike with custom collider size
    /// </summary>
    public SpikeModifier(Vector2 colliderSize)
    {
        _colliderSize = colliderSize;
    }
}

