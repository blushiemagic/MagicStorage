using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MagicStorage.Common.Threading {
	/// <summary>
	/// A thread-safe list of objects.  Modifications to the list are delayed until no thread is accessing it.
	/// </summary>
	public class ThreadSafeList<TList, TItem> : IList<TItem>
		where TList : IList<TItem>, new()
	{
		private readonly TList _list;
		private int _version;

		private readonly ConcurrentQueue<Operation> _delayedModifications = [];

		// The list of objects currently reading from this list
		private readonly List<WeakReference> _readHolders = [];
		private readonly ConditionalWeakTable<object, Boxed<int>> _readHolderToIndex = [];

		private int _holderLock;
		private int _listLock;
		private int _modificationCheckLock;

		private const int UNLOCKED = 0;
		private const int LOCKED = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="ThreadSafeList{TCollection, TItem}"/> class set to an empty list.
		/// </summary>
		public ThreadSafeList() => _list = [];

		/// <summary>
		/// Marks this list as being accessed by the given object.<br/>
		/// While any object is accessing the list, modifications to it will be delayed.<br/>
		/// To stop accessing the list, call <see cref="Release"/> with the same object or let it be garbage collected.
		/// </summary>
		/// <typeparam name="T">The type of the object accessing the list.</typeparam>
		/// <param name="user">The object accessing the list.</param>
		public void Freeze<T>(T user) where T : class {
			Boxed<int> boxedIndex = new(-1);

			using var _ = new Locker(ref _holderLock);

			if (_readHolderToIndex.TryAdd(user, boxedIndex)) {
				// Holder wasn't already watching this list, so add references to it
				int index = FindAvailableIndex();
				boxedIndex.value = index;
				_readHolders[index] = new WeakReference(user);
			}
		}

		/// <summary>
		/// Marks the given object as no longer accessing this list.<br/>
		/// If no objects are accessing the list after this call, any delayed modifications will be applied.
		/// </summary>
		/// <typeparam name="T">The type of the object accessing the list.</typeparam>
		/// <param name="user">The object that was accessing the list.</param>
		public void Release<T>(T user) where T : class {
			using var _ = new Locker(ref _holderLock);

			if (_readHolderToIndex.TryGetValue(user, out var boxedIndex)) {
				_readHolderToIndex.Remove(user);
				_readHolders[boxedIndex.value] = null;

				if (AreModificationsAllowed())
					ProcessDelayedQueue();
			}
		}

		public void CopyCurrentSnapshotTo(TList target) {
			if (AreModificationsAllowed())
				ProcessDelayedQueue();

			using var _ = new Locker(ref _listLock);

			target.Clear();
			
			if (typeof(TList) == typeof(List<TItem>)) {
				// Common path, use the built-in method
				Unsafe.As<TList, List<TItem>>(ref target).AddRange(_list);
			} else {
				foreach (TItem item in _list)
					target.Add(item);
			}
		}

		private int FindAvailableIndex() {
			for (int i = 0; i < _readHolders.Count; i++) {
				var holderReference = _readHolders[i];

				if (holderReference is null)
					return i;

				if (!holderReference.IsAlive) {
					// Force the slot to null
					_readHolders[i] = null;
					return i;
				}
			}

			// No slot was empty, so add one
			int index = _readHolders.Count;
			_readHolders.Add(null);
			return index;
		}

		private bool AreModificationsAllowed() {
			using var _ = new Locker(ref _modificationCheckLock);

			foreach (var holderReference in _readHolders) {
				if (holderReference is { IsAlive: true })
					return false;
			}

			return true;
		}

		/// <summary>
		/// Processes all delayed modifications to the list.<br/>
		/// This function will do nothing if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		public void ProcessDelayedQueue() {
			if (!AreModificationsAllowed())
				return;

			using var _ = new Locker(ref _listLock);

			while (_delayedModifications.TryDequeue(out var operation)) {
				operation.Handle(_list);
				Interlocked.Increment(ref _version);
			}
		}

		#region Nested types
		private class Boxed<T> where T : struct {
			public T value;

			public Boxed(T value) => this.value = value;
		}

		private ref struct Locker {
			private ref int _lock;
			private readonly bool _hasLock;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Locker(ref int @lock) {
				_lock = ref @lock;
				_hasLock = true;

				while (Interlocked.CompareExchange(ref @lock, LOCKED, UNLOCKED) == LOCKED)
					Thread.Yield();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose() {
				if (_hasLock)
					Interlocked.Exchange(ref _lock, UNLOCKED);
			}
		}

		private abstract class Operation {
			public bool HasExecuted { get; protected set; }
			public abstract void Handle(TList target);
		}

		private class OperationAction(Action<TList> action) : Operation {
			private readonly Action<TList> _action = action;
			public override void Handle(TList target) {
				_action(target);
				base.HasExecuted = true;
			}
		}

		private class OperationAction<T1>(Action<TList, T1> action, T1 arg1) : Operation {
			private readonly Action<TList, T1> _action = action;
			private readonly T1 _arg1 = arg1;
			public override void Handle(TList target) {
				_action(target, _arg1);
				base.HasExecuted = true;
			}
		}

		private class OperationAction<T1, T2>(Action<TList, T1, T2> action, T1 arg1, T2 arg2) : Operation {
			private readonly Action<TList, T1, T2> _action = action;
			private readonly T1 _arg1 = arg1;
			private readonly T2 _arg2 = arg2;
			public override void Handle(TList target) {
				_action(target, _arg1, _arg2);
				base.HasExecuted = true;
			}
		}

		private class OperationFunc<TReturn>(Func<TList, TReturn> func) : Operation {
			private readonly Func<TList, TReturn> _func = func;
			public TReturn Result { get; private set; }
			public override void Handle(TList target) {
				Result = _func(target);
				base.HasExecuted = true;
			}
		}

		private class OperationFunc<T1, TReturn>(Func<TList, T1, TReturn> func, T1 arg1) : Operation {
			private readonly Func<TList, T1, TReturn> _func = func;
			private readonly T1 _arg1 = arg1;
			public TReturn Result { get; private set; }
			public override void Handle(TList target) {
				Result = _func(target, _arg1);
				base.HasExecuted = true;
			}
		}
		#endregion

		#region IList members
		/// <summary>
		/// Gets or sets the element at the specified index.<br/>
		/// Element access will use the current snapshot of the list.<br/>
		/// Element assignment will be delayed if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get or set.</param>
		/// <returns>The element at the specified index.</returns>
		public TItem this[int index] {
			get {
				// Indexing is always allowed
				return _list[index];
			}
			set {
				if (AreModificationsAllowed()) {
					ProcessDelayedQueue();
					using var _ = new Locker(ref _listLock);
					_list[index] = value;
					Interlocked.Increment(ref _version);
				} else
					_delayedModifications.Enqueue(new OperationAction<TItem, int>(static (t, v, i) => t[i] = v, value, index));
			}
		}

		/// <summary>
		/// Gets the number of elements contained in the list.<br/>
		/// This property uses the current snapshot of the list.<br/>
		/// If any modifications are being delayed due to active holds from <see cref="Freeze"/>, they will not be reflected in the result.
		/// </summary>
		public int Count => _list.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => _list.IsReadOnly;

		/// <summary>
		/// Adds an item to the list.<br/>
		/// Item insertion will be delayed if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		/// <param name="item">The object to add to the list.</param>
		public void Add(TItem item) {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				_list.Add(item);
				Interlocked.Increment(ref _version);
			} else
				_delayedModifications.Enqueue(new OperationAction<TItem>(static (t, v) => t.Add(v), item));
		}

		/// <summary>
		/// Removes all items from the list.<br/>
		/// Clearing the list will be delayed if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		public void Clear() {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				_list.Clear();
				Interlocked.Increment(ref _version);
			} else
				_delayedModifications.Enqueue(new OperationAction(static t => t.Clear()));
		}

		/// <summary>
		/// Determines whether the list contains a specific value.<br/>
		/// This function uses the current snapshot of the list.<br/>
		/// If any modifications are being delayed due to active holds from <see cref="Freeze"/>, they will not be reflected in the result.
		/// </summary>
		/// <param name="item">The object to locate in the list.</param>
		/// <returns><see langword="true"/> if <paramref name="item"/> is found in the list; otherwise, <see langword="false"/>.</returns>
		public bool Contains(TItem item) {
			if (AreModificationsAllowed())
				ProcessDelayedQueue();

			using var _ = new Locker(ref _listLock);
			return _list.Contains(item);
		}

		/// <summary>
		/// Copies the elements of the list to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.<br/>
		/// This function uses the current snapshot of the list.<br/>
		/// If any modifications are being delayed due to active holds from <see cref="Freeze"/>, they will not be reflected in the result.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from the list. The <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		public void CopyTo(TItem[] array, int arrayIndex) {
			ArgumentNullException.ThrowIfNull(array);
			if (array.Length < arrayIndex)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex is greater than the length of array.");

			if (AreModificationsAllowed())
				ProcessDelayedQueue();

			using var _ = new Locker(ref _listLock);

			if (array.Length < arrayIndex + Count)
				throw new ArgumentException("The number of elements in the list is greater than the length of the array");

			_list.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the list.<br/>
		/// The enumerator will throw an exception if any non-delayed modifications are made to the list while it is being enumerated.<br/>
		/// If any modifications are being delayed due to active holds from <see cref="Freeze"/>, they will not be reflected in the enumeration.
		/// </summary>
		/// <returns>An enumerator for the list.</returns>
		public Enumerator GetEnumerator() => new Enumerator(this);

		IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => Count == 0 ? ((IEnumerable<TItem>)[]).GetEnumerator() : GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TItem>)this).GetEnumerator();

		/// <summary>
		/// Determines the index of a specific item in the list.<br/>
		/// This function uses the current snapshot of the list.<br/>
		/// If any modifications are being delayed due to active holds from <see cref="Freeze"/>, they will not be reflected in the result.
		/// </summary>
		/// <param name="item">The object to locate in the list.</param>
		/// <returns>The index of <paramref name="item"/> if found in the list; otherwise, -1.</returns>
		public int IndexOf(TItem item) {
			if (AreModificationsAllowed())
				ProcessDelayedQueue();

			using var _ = new Locker(ref _listLock);
			return _list.IndexOf(item);
		}

		/// <summary>
		/// Inserts an item to the list at the specified index.<br/>
		/// Item insertion will be delayed if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
		/// <param name="item">The object to insert into the list.</param>
		public void Insert(int index, TItem item) {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				_list.Insert(index, item);
				Interlocked.Increment(ref _version);
			} else
				_delayedModifications.Enqueue(new OperationAction<TItem, int>(static (t, v, i) => t.Insert(i, v), item, index));
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the list.<br/>
		/// Item removal will be delayed if any holds from <see cref="Freeze"/> are active, and the current thread will be blocked until the result is obtained.
		/// </summary>
		/// <param name="item">The object to remove from the list.</param>
		/// <returns><see langword="true"/> if <paramref name="item"/> was successfully removed from the list; otherwise, <see langword="false"/>.</returns>
		public bool Remove(TItem item) {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				Interlocked.Increment(ref _version);
				return _list.Remove(item);
			}

			// Since this operation has a return value, this must block until the result is obtained
			var operation = new OperationFunc<TItem, bool>(static (t, v) => t.Remove(v), item);
			_delayedModifications.Enqueue(operation);

			while (!operation.HasExecuted)
				Thread.Yield();

			return operation.Result;
		}

		/// <summary>
		/// Removes the item at the specified index.<br/>
		/// Item removal will be delayed if any holds from <see cref="Freeze"/> are active.
		/// </summary>
		/// <param name="index">The zero-based index of the item to remove.</param>
		public void RemoveAt(int index) {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				_list.RemoveAt(index);
				Interlocked.Increment(ref _version);
			} else
				_delayedModifications.Enqueue(new OperationAction<int>(static (t, i) => t.RemoveAt(i), index));
		}
		#endregion

		#region Additional List-like methods
		/// <summary>
		/// Removes all the items that match the conditions defined by the specified predicate.<br/>
		/// Item removal will be delayed if any holds from <see cref="Freeze"/> are active, and the current thread will be blocked until the result is obtained.
		/// </summary>
		/// <param name="match">The <see cref="Predicate{T}"/> delegate that defines the conditions of the elements to remove.</param>
		/// <returns>The number of items removed from the list.</returns>
		public int RemoveAll(Predicate<TItem> match) {
			if (AreModificationsAllowed()) {
				ProcessDelayedQueue();
				using var _ = new Locker(ref _listLock);
				Interlocked.Increment(ref _version);
				return RemoveAll_Inner(_list, match);
			}

			// Since this operation has a return value, this must block until the result is obtained
			var operation = new OperationFunc<Predicate<TItem>, int>(RemoveAll_Inner, match);
			_delayedModifications.Enqueue(operation);

			while (!operation.HasExecuted)
				Thread.Yield();

			return operation.Result;
		}

		private static int RemoveAll_Inner(TList list, Predicate<TItem> match) {
			if (typeof(TList) == typeof(List<TItem>)) {
				// Common path, use the built-in method
				return Unsafe.As<TList, List<TItem>>(ref list).RemoveAll(match);
			} else {
				int removedCount = 0;

				for (int i = list.Count - 1; i >= 0; i--) {
					if (match(list[i])) {
						list.RemoveAt(i);
						removedCount++;
					}
				}

				return removedCount;
			}
		}
		#endregion

		#region Enumerator implementation
		// Copy of List<T>.Enumerator
		/// <summary/>
		public struct Enumerator(ThreadSafeList<TList, TItem> source) : IEnumerator<TItem>, IEnumerator {
			private readonly ThreadSafeList<TList, TItem> _source = source;
			private readonly int _version = source._version;

			private int _index;
			private TItem _current;

			/// <inheritdoc/>
			public void Dispose() { }

			/// <inheritdoc/>
			public bool MoveNext() {
				var localSource = _source;

				if (_version != _source._version)
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

				if ((uint)_index < (uint)localSource.Count) {
					_current = localSource[_index];
					_index++;
					return true;
				}

				_current = default;
				_index = -1;
				return false;
			}

			/// <inheritdoc/>
			public readonly TItem Current => _current;

			readonly object IEnumerator.Current {
				get {
					if (_index <= 0)
						throw new InvalidOperationException("Enumeration has either not started or has already finished.");

					return Current;
				}
			}

			void IEnumerator.Reset() {
				if (_version != _source._version)
					throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");

				_index = 0;
				_current = default;
			}
		}
		#endregion
	}
}
