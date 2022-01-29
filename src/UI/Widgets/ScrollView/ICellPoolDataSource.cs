using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UniverseLib.UI.Widgets.ScrollView
{
    /// <summary>
    /// A data source for a ScrollPool.
    /// </summary>
    public interface ICellPoolDataSource<T> where T : ICell
    {
        int ItemCount { get; }

        void OnCellBorrowed(T cell);

        void SetCell(T cell, int index);
    }
}
