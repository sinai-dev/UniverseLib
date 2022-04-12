using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UniverseLib.UI.Widgets.ScrollView;

namespace UniverseLib.UI.Widgets.ButtonList
{
    /// <summary>
    /// A helper to create and handle a simple <see cref="ScrollPool{T}"/> of Buttons, which can be backed by any data.
    /// </summary>
    public class ButtonListHandler<TData, TCell> : ICellPoolDataSource<TCell> where TCell : ButtonCell
    {
        public ScrollPool<TCell> ScrollPool { get; private set; }

        public int ItemCount => CurrentEntries.Count;
        public List<TData> CurrentEntries { get; } = new();

        protected Func<List<TData>> GetEntries;
        protected Action<TCell, int> SetICell;
        protected Func<TData, string, bool> ShouldDisplay;
        protected Action<int> OnCellClicked;

        public string CurrentFilter
        {
            get => currentFilter;
            set => currentFilter = value ?? "";
        }
        private string currentFilter;

        /// <summary>
        /// Create a wrapper to handle your Button ScrollPool.
        /// </summary>
        /// <param name="scrollPool">The ScrollPool&lt;ButtonCell&gt; you have already created.</param>
        /// <param name="getEntriesMethod">A method which should return your current data values.</param>
        /// <param name="setICellMethod">A method which should set the data at the int index to the cell.</param>
        /// <param name="shouldDisplayMethod">A method which should determine if the data at the index should be displayed, with an optional string filter from CurrentFilter.</param>
        /// <param name="onCellClickedMethod">A method invoked when a cell is clicked, containing the data index assigned to the cell.</param>
        public ButtonListHandler(ScrollPool<TCell> scrollPool, Func<List<TData>> getEntriesMethod,
            Action<TCell, int> setICellMethod, Func<TData, string, bool> shouldDisplayMethod,
            Action<int> onCellClickedMethod)
        {
            ScrollPool = scrollPool;

            GetEntries = getEntriesMethod;
            SetICell = setICellMethod;
            ShouldDisplay = shouldDisplayMethod;
            OnCellClicked = onCellClickedMethod;
        }

        public void RefreshData()
        {
            List<TData> allEntries = GetEntries();
            CurrentEntries.Clear();

            foreach (TData entry in allEntries)
            {
                if (!string.IsNullOrEmpty(currentFilter))
                {
                    if (!ShouldDisplay(entry, currentFilter))
                        continue;

                    CurrentEntries.Add(entry);
                }
                else
                    CurrentEntries.Add(entry);
            }
        }

        public virtual void OnCellBorrowed(TCell cell)
        {
            cell.OnClick += OnCellClicked;
        }

        public virtual void SetCell(TCell cell, int index)
        {
            if (CurrentEntries == null)
                RefreshData();

            if (index < 0 || index >= CurrentEntries.Count)
                cell.Disable();
            else
            {
                cell.Enable();
                cell.CurrentDataIndex = index;
                SetICell(cell, index);
            }
        }
    }
}
