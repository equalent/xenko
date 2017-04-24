// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Presentation.Collections
{
    /// <summary>
    /// A class that wraps an instance of the <see cref="ObservableList{T}"/> class and implement the <see cref="IList"/> interface.
    /// In some scenarii, <see cref="IList"/> does not support range changes on the collection (Especially when bound to a ListCollectionView).
    /// This is why the <see cref="ObservableList{T}"/> class does not implement this interface directly. However this wrapper class can be used
    /// when the <see cref="IList"/> interface is required.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the <see cref="ObservableList{T}"/>.</typeparam>
    public class NonGenericObservableListWrapper<T> : NonGenericObservableCollectionWrapper<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonGenericObservableListWrapper{T}"/> class.
        /// </summary>
        /// <param name="list">The <see cref="IObservableList{T}"/> to wrap.</param>
        public NonGenericObservableListWrapper([NotNull] IObservableList<T> list)
            : base(list)
        {
        }

        public void AddRange([NotNull] IEnumerable values)
        {
            ((IObservableList<T>)List).AddRange(values.Cast<T>());
        }
        
        public void AddRange([NotNull] IEnumerable<T> values)
        {
            ((IObservableList<T>)List).AddRange(values);
        }      
    }
}
