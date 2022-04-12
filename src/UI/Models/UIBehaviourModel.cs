using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.UI.Models
{
    /// <summary>
    /// A class which can be used as an abstract UI object, which does not exist as a Component but which can receive Update calls.
    /// </summary>
    public abstract class UIBehaviourModel : UIModel
    {
        // Static 
        static readonly List<UIBehaviourModel> Instances = new();

        internal static void UpdateInstances()
        {
            if (!Instances.Any())
                return;

            try
            {
                for (int i = Instances.Count - 1; i >= 0; i--)
                {
                    UIBehaviourModel instance = Instances[i];
                    if (instance == null || !instance.UIRoot)
                    {
                        Instances.RemoveAt(i);
                        continue;
                    }
                    if (instance.Enabled)
                        instance.Update();
                }
            }
            catch (Exception ex)
            {
                Universe.Log(ex);
            }
        }
        
        // Instance

        public UIBehaviourModel()
        {
            Instances.Add(this);
        }

        public virtual void Update()
        {
        }

        public override void Destroy()
        {
            if (Instances.Contains(this))
                Instances.Remove(this);

            base.Destroy();
        }
    }
}
