using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.UI
{
    /// <summary>
    /// A simple wrapper to handle a UI created with <see cref="UniversalUI.RegisterUI"/>.
    /// </summary>
    public class UIBase
    {
        public string ID { get; }
        public GameObject RootObject { get; }
        public Canvas Canvas { get; }
        public Action UpdateMethod { get; }

        /// <summary>
        /// Whether this UI is currently being displayed or not. Disabled UIs will not receive Update calls.
        /// </summary>
        public bool Enabled
        {
            get => RootObject.activeSelf;
            set => UniversalUI.SetUIActive(this.ID, value);
        }

        internal UIBase(string id, GameObject rootObject, Action updateMethod)
        {
            ID = id;
            RootObject = rootObject;
            UpdateMethod = updateMethod;
            Canvas = RootObject.GetComponent<Canvas>();
        }

        internal void Update()
        {
            try
            {
                UpdateMethod?.Invoke();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception invoking update method for {ID}: {ex}");
            }
        }
    }
}
