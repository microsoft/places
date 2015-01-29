/*	
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Places.Common
{
    /// <summary>
    /// Implementation of IObservableMap that supports reentrancy for use as a default view model.
    /// </summary>
    public class ObservableDictionary : IObservableMap<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the Dictionary
        /// </summary>
        private readonly Dictionary<string, object> dictionary = new Dictionary<string, object>();

        /// <summary>
        /// Occurs when the map changes
        /// </summary>
        public event MapChangedEventHandler<string, object> MapChanged;

        /// <summary>
        /// Returns the keys from the collection
        /// </summary>
        public ICollection<string> Keys
        {
            get { return this.dictionary.Keys; }
        }

        /// <summary>
        /// Returns the values from the collection
        /// </summary>
        public ICollection<object> Values
        {
            get { return this.dictionary.Values; }
        }

        /// <summary>
        /// Counts elements in the collection
        /// </summary>
        public int Count
        {
            get { return this.dictionary.Count; }
        }

        /// <summary>
        /// Determines if collection is readonly
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Returns item at given index
        /// </summary>
        /// <param name="key">Index</param>
        /// <returns>Returns a dictionary instance</returns>
        public object this[string key]
        {
            get
            {
                return this.dictionary[key];
            }
            set
            {
                this.dictionary[key] = value;
                this.InvokeMapChanged(CollectionChange.ItemChanged, key);
            }
        }

        /// <summary>
        /// Add an item to the collection based on the key and value
        /// </summary>
        /// <param name="key">Index</param>
        /// <param name="value">The value to add</param>
        public void Add(string key, object value)
        {
            this.dictionary.Add(key, value);
            this.InvokeMapChanged(CollectionChange.ItemInserted, key);
        }

        /// <summary>
        /// Adds an item to the collection
        /// </summary>
        /// <param name="item">The item added to the collection</param>
        public void Add(KeyValuePair<string, object> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <summary>
        /// Removes item with the given key from the collection
        /// </summary>
        /// <param name="key">Index</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public bool Remove(string key)
        {
            if (this.dictionary.Remove(key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the given item from the collection
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public bool Remove(KeyValuePair<string, object> item)
        {
            object currentValue;
            if (this.dictionary.TryGetValue(item.Key, out currentValue) && object.Equals(item.Value, currentValue) && this.dictionary.Remove(item.Key))
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, item.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all elements in the collection
        /// </summary>
        public void Clear()
        {
            var priorKeys = this.dictionary.Keys.ToArray();
            this.dictionary.Clear();
            foreach (var key in priorKeys)
            {
                this.InvokeMapChanged(CollectionChange.ItemRemoved, key);
            }
        }

        /// <summary>
        /// Determines if the collection contains the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public bool ContainsKey(string key)
        {
            return this.dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns the value of an item based on the given key
        /// </summary>
        /// <param name="key">The key of the item to retrieve.</param>
        /// <param name="value">The value corresponding to the key.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public bool TryGetValue(string key, out object value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Determines if the collection contains the given item
        /// </summary>
        /// <param name="item">The KeyValuePair to test.</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        public bool Contains(KeyValuePair<string, object> item)
        {
            return this.dictionary.Contains(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        /// <summary>
        /// Copies each element from the current instance of the collection to an array
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection. The array must have zero-based indexing</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            int arraySize = array.Length;
            foreach (var pair in this.dictionary)
            {
                if (arrayIndex >= arraySize)
                {
                    break;
                }
                array[arrayIndex++] = pair;
            }
        }

        /// <summary>
        /// Invoked when a change has been made on the map
        /// </summary>
        /// <param name="change">The element to change the key of.</param>
        /// <param name="key">The new key for item.</param>
        private void InvokeMapChanged(CollectionChange change, string key)
        {
            var eventHandler = this.MapChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new ObservableDictionaryChangedEventArgs(change, key));
            }
        }

        /// <summary>
        /// Implementation of IMapChangedEventArgs when a change has been made of a map
        /// model.
        /// </summary>
        private class ObservableDictionaryChangedEventArgs : IMapChangedEventArgs<string>
        {
            /// <summary>
            /// Executes when a change has been made to the collection
            /// </summary>
            /// <param name="change">The collection that can be changed.</param>
            /// <param name="key">The new key for the item.</param>
            public ObservableDictionaryChangedEventArgs(CollectionChange change, string key)
            {
                this.CollectionChange = change;
                this.Key = key;
            }

            /// <summary>
            /// CollectionChange instance 
            /// Describes the action that causes a change
            /// </summary>
            public CollectionChange CollectionChange { get; private set; }

            /// <summary>
            /// Identifier from the collection
            /// </summary>
            public string Key { get; private set; }
        }
    }
}