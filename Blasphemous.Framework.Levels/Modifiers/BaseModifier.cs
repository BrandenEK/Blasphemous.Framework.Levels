using UnityEngine;

namespace Blasphemous.Framework.Levels.Modifiers;

/// <summary>
/// base modifier containing geometric data of the object
/// </summary>
public class BaseModifier : IModifier
{
    /// <inheritdoc/>
    public void Apply(GameObject obj, ObjectData data)
    {
        obj.transform.position = data.position;
        obj.transform.eulerAngles = data.rotation;
        obj.transform.localScale = data.scale;
        obj.SetActive(true);
    }
}
