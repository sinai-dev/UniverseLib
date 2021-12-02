using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UniverseLib.UI.Models;

namespace UniverseLib.UI.Widgets
{
    public interface ICell : IPooledObject
    {
        bool Enabled { get; }

        RectTransform Rect { get; set; }

        void Enable();
        void Disable();
    }
}
