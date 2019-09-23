namespace EtAlii.PhotoShuffle
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Windows;
    using System.Windows.Threading;

    public class DispatcherObservableCollection<T> : IReadOnlyList<T>, INotifyCollectionChanged
    {
        private readonly ObservableCollection<T> _collection;

        public DispatcherObservableCollection(ObservableCollection<T> collection)
        {
            _collection = collection;
            _collection.CollectionChanged += OnChanged;
        }

        private void OnChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Application.Current?.Dispatcher?.Invoke(DispatcherPriority.Render, new Action(() => CollectionChanged?.Invoke(this, e)));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _collection.Count;

        public T this[int index] => _collection[index];
    }
}
