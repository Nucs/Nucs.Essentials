using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Nucs.Collections {
    /// <summary>Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.</summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    [Serializable]
    public class ObservableConcurrentList<T> : INotifyCollectionChanged, INotifyPropertyChanged, IList<T>, IReadOnlyList<T>, IList, IDisposable {
        private readonly ConcurrentList<T> _list;

        
        private SimpleMonitor _monitor = new SimpleMonitor();

        public bool Remove(T item) {
            var index = IndexOf(item);
            if (index == -1)
                return false;
            RemoveAt(index);
            return true;
        }

        
        public int Count => _list.Count;

        /// <summary>Initializes a new instance of the <see cref="T:ObservableCollection`1" /> class.</summary>
        public ObservableConcurrentList(ConcurrentList<T> list) {
            _list = list;
        }

        /// <summary>Initializes a new instance of the <see cref="T:ObservableCollection`1" /> class.</summary>
        public ObservableConcurrentList() {
            _list = new ConcurrentList<T>();
        }

        /// <summary>Initializes a new instance of the <see cref="T:ObservableCollection`1" /> class that contains elements copied from the specified list.</summary>
        /// <param name="list">The list from which the elements are copied.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="list" /> parameter cannot be <see langword="null" />.</exception>
        public ObservableConcurrentList(List<T> list) {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            _list = new ConcurrentList<T>(list);
        }

        /// <summary>Initializes a new instance of the <see cref="T:ObservableCollection`1" /> class that contains elements copied from the specified collection.</summary>
        /// <param name="collection">The collection from which the elements are copied.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="collection" /> parameter cannot be <see langword="null" />.</exception>
        public ObservableConcurrentList(IEnumerable<T> collection) {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _list = new ConcurrentList<T>(collection);
        }

        /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        public void Move(int oldIndex, int newIndex) {
            MoveItem(oldIndex, newIndex);
        }

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged {
            add => PropertyChanged += value;
            remove => PropertyChanged -= value;
        }

        /// <summary>Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.</summary>
        [field: NonSerialized]
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        int IList.Add(object value) {
            return ((IList) _list).Add(value);
        }

        public bool Contains(object value) {
            return ((IList) _list).Contains(value);
        }

        public T this[int index] {
            get { return _list[index]; }
            set {
                CheckReentrancy();
                T obj = this[index];
                _list[index] = value;
                OnPropertyChanged("Item[]");
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, (object) obj, (object) value, index);
            }
        }

        public void Add(T item) {
            _list.Add(item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, Count - 1);
        }

        /// <summary>Removes all items from the collection.</summary>
        public void Clear() {
            CheckReentrancy();
            _list.Clear();
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionReset();
        }

        public bool Contains(T item) {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _list.CopyTo(array, arrayIndex);
        }

        int IList.IndexOf(object value) {
            return ((IList) _list).IndexOf(value);
        }

        void IList.Insert(int index, object value) {
            Insert(index, (T) value);
        }

        void IList.Remove(object value) {
            RemoveAt(((IList) this).IndexOf(value));
        }

        object IList.this[int index] {
            get => ((IList) _list)[index];
            set {
                CheckReentrancy();
                T obj = this[index];
                ((IList) _list)[index] = value;
                OnPropertyChanged("Item[]");
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, (object) obj, (object) value, index);
            }
        }

        
        public bool IsReadOnly => _list.IsReadOnly;

        
        public bool IsFixedSize => ((IList) _list).IsFixedSize;

        /// <summary>Removes the item at the specified index of the collection.</summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index) {
            CheckReentrancy();
            T obj = this[index];
            _list.RemoveAt(index);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, (object) obj, index);
        }

        public int IndexOf(T item) {
            return _list.IndexOf(item);
        }

        /// <summary>Inserts an item into the collection at the specified index.</summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        public void Insert(int index, T item) {
            CheckReentrancy();
            _list.Insert(index, item);
            OnPropertyChanged("Count");
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Add, (object) item, index);
        }


        /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
        /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
        /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
        protected virtual void MoveItem(int oldIndex, int newIndex) {
            CheckReentrancy();
            T obj = this[oldIndex];
            _list.RemoveAt(oldIndex);
            _list.Insert(newIndex, obj);
            OnPropertyChanged("Item[]");
            OnCollectionChanged(NotifyCollectionChangedAction.Move, (object) obj, newIndex, oldIndex);
        }

        /// <summary>Raises the <see cref="E:ObservableCollection`1.PropertyChanged" /> event with the provided arguments.</summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            if (PropertyChanged == null)
                return;
            PropertyChanged((object) this, e);
        }

        /// <summary>Occurs when a property value changes.</summary>
        [field: NonSerialized]
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Raises the <see cref="E:ObservableCollection`1.CollectionChanged" /> event with the provided arguments.</summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) {
            if (CollectionChanged == null)
                return;
            using (BlockReentrancy())
                CollectionChanged((object) this, e);
        }

        /// <summary>Disallows reentrant attempts to change this collection.</summary>
        /// <returns>An <see cref="T:System.IDisposable" /> object that can be used to dispose of the object.</returns>
        protected IDisposable BlockReentrancy() {
            _monitor.Enter();
            return (IDisposable) _monitor;
        }

        /// <summary>Checks for reentrant attempts to change this collection.</summary>
        /// <exception cref="T:System.InvalidOperationException">If there was a call to <see cref="M:ObservableCollection`1.BlockReentrancy" /> of which the <see cref="T:System.IDisposable" /> return value has not yet been disposed of. Typically, this means when there are additional attempts to change this collection during a <see cref="E:ObservableCollection`1.CollectionChanged" /> event. However, it depends on when derived classes choose to call <see cref="M:ObservableCollection`1.BlockReentrancy" />.</exception>
        protected void CheckReentrancy() {
            if (_monitor.Busy && CollectionChanged != null && CollectionChanged.GetInvocationList().Length > 1)
                throw new InvalidOperationException("ObservableCollectionReentrancyNotAllowed");
        }

        private void OnPropertyChanged(string propertyName) {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index) {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex) {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index) {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
        }

        private void OnCollectionReset() {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        [Serializable]
        private class SimpleMonitor : IDisposable {
            private int _busyCount;

            public void Enter() {
                ++_busyCount;
            }

            public void Dispose() {
                --_busyCount;
            }

            public bool Busy => _busyCount > 0;
        }

        #region Implementation of IEnumerable

        public IEnumerator<T> GetEnumerator() {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable) _list).GetEnumerator();
        }

        #endregion

        #region Implementation of IReadOnlyCollection<out T>

        public void CopyTo(Array array, int index) {
            ((ICollection) _list).CopyTo(array, index);
        }


        
        public object SyncRoot => ((ICollection) _list).SyncRoot;

        
        public bool IsSynchronized => ((ICollection) _list).IsSynchronized;

        #endregion


        #region Implementation of IDisposable

        public void Dispose() {
            _list.Dispose();
        }

        #endregion
    }
}