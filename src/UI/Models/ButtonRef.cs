using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using UniverseLib.UI.Models;

namespace UniverseLib.UI
{
    // A simple helper class to handle a button's OnClick more effectively.

    public class ButtonRef
    {
        public Action OnClick;

        public Button Component { get; }
        public Text ButtonText { get; }

        public ButtonRef(Button button)
        {
            this.Component = button;
            this.ButtonText = button.GetComponentInChildren<Text>();

            button.onClick.AddListener(() => { OnClick?.Invoke(); });
        }
    }
}
