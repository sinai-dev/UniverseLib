using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.UI
{
    public class UIBase
    {
        public UIBase(string id, GameObject rootObject, Action updateMethod)
        {
            ID = id;
            RootObject = rootObject;
            UpdateMethod = updateMethod;
        }

        public string ID;
        public GameObject RootObject;
        public Action UpdateMethod;

        public bool Enabled => RootObject.activeSelf;

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
