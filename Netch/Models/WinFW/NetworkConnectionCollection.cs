using System.Collections;
using System.Linq;
using NETCONLib;

namespace Netch.Models.WinFW
{
    /// <summary>
    ///     A collection that stores 'NetworkConnection' objects.
    /// </summary>
    public class NetworkConnectionCollection : CollectionBase
    {
        /// <summary>
        ///     Initializes a new instance of 'NetworkConnectionCollection'.
        /// </summary>
        public NetworkConnectionCollection()
        {
            NetSharingManager icsMgr = new NetSharingManagerClass();

            foreach (var icsConn in icsMgr.EnumEveryConnection.Cast<INetConnection>())
            {
                Add(new NetworkConnection(icsConn));
            }
        }

        /// <summary>
        ///     Represents the 'NetworkConnection' item at the specified index position.
        /// </summary>
        /// <param name="intIndex">
        ///     The zero-based index of the entry to locate in the collection.
        /// </param>
        /// <value>
        ///     The entry at the specified index of the collection.
        /// </value>
        public NetworkConnection this[int intIndex]
        {
            get => (NetworkConnection) List[intIndex];
            set => List[intIndex] = value;
        }

        /// <summary>
        ///     Adds a 'NetworkConnection' item with the specified value to the 'NetworkConnectionCollection'
        /// </summary>
        /// <param name="conValue">
        ///     The 'NetworkConnection' to add.
        /// </param>
        /// <returns>
        ///     The index at which the new element was inserted.
        /// </returns>
        public int Add(NetworkConnection conValue)
        {
            return List.Add(conValue);
        }

        /// <summary>
        ///     Copies the elements of an array at the end of this instance of 'NetworkConnectionCollection'.
        /// </summary>
        /// <param name="conValue">
        ///     An array of 'NetworkConnection' objects to add to the collection.
        /// </param>
        public void AddRange(NetworkConnection[] conValue)
        {
            checked
            {
                foreach (var t in conValue)
                    Add(t);
            }
        }

        /// <summary>
        ///     Adds the contents of another 'NetworkConnectionCollection' at the end of this instance.
        /// </summary>
        /// <param name="conValue">
        ///     A 'NetworkConnectionCollection' containing the objects to add to the collection.
        /// </param>
        public void AddRange(NetworkConnectionCollection conValue)
        {
            checked
            {
                for (var intCounter = 0; intCounter < conValue.Count; intCounter++) Add(conValue[intCounter]);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether the 'NetworkConnectionCollection' contains the specified value.
        /// </summary>
        /// <param name="conValue">
        ///     The item to locate.
        /// </param>
        /// <returns>
        ///     True if the item exists in the collection; false otherwise.
        /// </returns>
        public bool Contains(NetworkConnection conValue)
        {
            return List.Contains(conValue);
        }

        /// <summary>
        ///     Copies the 'NetworkConnectionCollection' values to a one-dimensional System.Array
        ///     instance starting at the specified array index.
        /// </summary>
        /// <param name="conArray">
        ///     The one-dimensional System.Array that represents the copy destination.
        /// </param>
        /// <param name="intIndex">
        ///     The index in the array where copying begins.
        /// </param>
        public void CopyTo(NetworkConnection[] conArray, int intIndex)
        {
            List.CopyTo(conArray, intIndex);
        }

        /// <summary>
        ///     Returns the index of a 'NetworkConnection' object in the collection.
        /// </summary>
        /// <param name="conValue">
        ///     The 'NetworkConnection' object whose index will be retrieved.
        /// </param>
        /// <returns>
        ///     If found, the index of the value; otherwise, -1.
        /// </returns>
        public int IndexOf(NetworkConnection conValue)
        {
            return List.IndexOf(conValue);
        }

        /// <summary>
        ///     Inserts an existing 'NetworkConnection' into the collection at the specified index.
        /// </summary>
        /// <param name="intIndex">
        ///     The zero-based index where the new item should be inserted.
        /// </param>
        /// <param name="conValue">
        ///     The item to insert.
        /// </param>
        public void Insert(int intIndex, NetworkConnection conValue)
        {
            List.Insert(intIndex, conValue);
        }

        /// <summary>
        ///     Returns an enumerator that can be used to iterate through
        ///     the 'NetworkConnectionCollection'.
        /// </summary>
        public new ConnectionEnumerator GetEnumerator()
        {
            return new ConnectionEnumerator(this);
        }

        /// <summary>
        ///     Removes a specific item from the 'NetworkConnectionCollection'.
        /// </summary>
        /// <param name="conValue">
        ///     The item to remove from the 'NetworkConnectionCollection'.
        /// </param>
        public void Remove(NetworkConnection conValue)
        {
            List.Remove(conValue);
        }

        /// <summary>
        ///     A strongly typed enumerator for 'NetworkConnectionCollection'
        /// </summary>
        public class ConnectionEnumerator : IEnumerator
        {
            private readonly IEnumerator iEnBase;

            private readonly IEnumerable iEnLocal;

            /// <summary>
            ///     Enumerator constructor
            /// </summary>
            public ConnectionEnumerator(NetworkConnectionCollection conMappings)
            {
                iEnLocal = conMappings;
                iEnBase = iEnLocal.GetEnumerator();
            }

            /// <summary>
            ///     Gets the current element from the collection
            /// </summary>
            public object Current => iEnBase.Current;

            /// <summary>
            ///     Advances the enumerator to the next element of the collection
            /// </summary>
            public bool MoveNext()
            {
                return iEnBase.MoveNext();
            }

            /// <summary>
            ///     Sets the enumerator to the first element in the collection
            /// </summary>
            public void Reset()
            {
                iEnBase.Reset();
            }
        }
    }
}