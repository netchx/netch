using System;
using System.Collections.Generic;
using System.ComponentModel;

public class SortableBindingList<T> : BindingList<T>
{
    public SortableBindingList(IList<T> list) : base(list)
    {
    }

    public SortableBindingList()
    {
    }

    public void Sort()
    {
        Sort(null, null);
    }

    public void Sort(IComparer<T> comparer)
    {
        Sort(comparer, null);
    }

    public void Sort(Comparison<T> comparison)
    {
        Sort(null, comparison);
    }

    private void Sort(IComparer<T> comparer, Comparison<T> comparison)
    {
        // if (typeof(T).GetInterface(nameof(IComparable)) == null) return;
        var raiseListChangedEvents = this.RaiseListChangedEvents;
        this.RaiseListChangedEvents = false;
        try
        {
            var items = (List<T>) this.Items;
            if (comparison != null) items.Sort(comparison);
            else items.Sort(comparer);
        }
        finally
        {
            this.RaiseListChangedEvents = raiseListChangedEvents;
            ResetBindings();
        }
    }
}