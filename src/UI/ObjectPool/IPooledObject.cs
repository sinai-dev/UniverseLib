using UnityEngine;

namespace UniverseLib.UI.ObjectPool
{
    /// <summary>
    /// An object which can be pooled by a <see cref="Pool"/>.
    /// </summary>
    public interface IPooledObject
    {
        GameObject UIRoot { get; set; }
        float DefaultHeight { get; }

        GameObject CreateContent(GameObject parent);
    }
}
