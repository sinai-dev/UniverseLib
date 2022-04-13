using UnityEngine;
using UnityEngine.EventSystems;

namespace UniverseLib.Input
{
    /// <summary>
    /// Interface for handling Unity Input API.
    /// </summary>
    public interface IHandleInput
    {
        Vector2 MousePosition { get; }
        Vector2 MouseScrollDelta { get; }

        bool GetKeyDown(KeyCode key);
        bool GetKey(KeyCode key);
        bool GetKeyUp(KeyCode key);

        bool GetMouseButtonDown(int btn);
        bool GetMouseButton(int btn);
        bool GetMouseButtonUp(int btn);

        void ResetInputAxes();

        BaseInputModule UIInputModule { get; }

        void AddUIInputModule();
        void ActivateModule();
    }
}