﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SimpleNavigation;

/// <summary>
/// Extends the functionality of the <see cref="ObservableCollection{T}"/> type.
/// https://github.com/AndrewKeepCoding/ObservableCollectionExSampleApp/blob/main/ObservableCollectionExSampleApp/ObservableCollectionEx.cs
/// </summary>
/// <typeparam name="T">data type for the collection</typeparam>
public class ObservableCollectionEx<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> collection)
    {
        CheckReentrancy(); // from the System.Collections.ObjectModel.ObservableCollection class

        // Is there anything to add?
        if (collection.Any() is false)
            return;

        List<T> itemsList = (List<T>)Items;
        itemsList.AddRange(collection);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                action: NotifyCollectionChangedAction.Add,
                changedItems: itemsList,
                startingIndex: itemsList.Count - 1));
    }

    public void AddRange(IList<T> collection)
    {
        CheckReentrancy(); // from the System.Collections.ObjectModel.ObservableCollection class

        // Is there anything to add?
        if (collection.Any() is false)
            return;

        List<T> itemsList = (List<T>)Items;
        itemsList.AddRange(collection);

        OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                action: NotifyCollectionChangedAction.Add,
                changedItems: itemsList,
                startingIndex: itemsList.Count - 1));
    }
}