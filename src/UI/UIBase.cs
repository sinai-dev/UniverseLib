using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.UI
{
    public class UIBase
    {
        public string ID { get; }
        public GameObject RootObject { get; }
        public Canvas Canvas { get; }
        public Action UpdateMethod { get; }

        public bool Enabled => RootObject.activeSelf;

        public UIBase(string id, GameObject rootObject, Action updateMethod)
        {
            ID = id;
            RootObject = rootObject;
            UpdateMethod = updateMethod;
            Canvas = RootObject.GetComponent<Canvas>();
        }

        public void Update()
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
