using System;
using System.Collections.Generic;

namespace MagicStorage.Common {
	/// <summary>
	/// A utility class to check for circular dependencies in a directed graph of items
	/// </summary>
	public class CircularDependencyChecker<T> {
		private record class WalkNode(T Item, WalkNode Parent);

		private readonly Queue<WalkNode> _queue = [];
		private readonly Func<T, T, bool> _areEqual;
		private readonly Func<T, IEnumerable<T>> _getDependencies;
		private readonly Func<T, string> _getName;

		private WalkNode _throwingNode;
		private bool _hasResult;

		public CircularDependencyChecker(Func<T, T, bool> areEqual, Func<T, IEnumerable<T>> getDependencies, Func<T, string> getName) {
			ArgumentNullException.ThrowIfNull(areEqual);
			ArgumentNullException.ThrowIfNull(getDependencies);
			ArgumentNullException.ThrowIfNull(getName);

			_areEqual = areEqual;
			_getDependencies = getDependencies;
			_getName = getName;
		}

		/// <summary>
		/// Checks if there are circular dependencies starting from the given initial item
		/// </summary>
		/// <param name="initial">The initial item to start the check from</param>
		/// <returns><see langword="true"/> if no circular dependency was found, <see langword="false"/> otherwise</returns>
		public bool Run(T initial) {
			ArgumentNullException.ThrowIfNull(initial);

			_queue.Clear();
			_throwingNode = null;
			_hasResult = false;

			// Start the walk with the initial item, but don't put it in the queue
			EnqueueNextItems(new WalkNode(initial, null));

			while (_queue.TryDequeue(out var current)) {
				var item = current.Item;

				if (_areEqual(item, initial)) {
					// Found a circular dependency
					_throwingNode = current;
					_hasResult = true;
					return false;
				}

				// Enqueue all dependencies of the current item
				EnqueueNextItems(current);
			}

			// No circular dependency found
			_hasResult = true;
			return true;
		}

		private void EnqueueNextItems(WalkNode current) {
			foreach (var dependent in _getDependencies(current.Item)) {
				if (dependent is null)
					continue;

				_queue.Enqueue(new WalkNode(dependent, current));
			}
		}

		/// <summary>
		/// Walks from the initial item to the item which triggered the circular dependency, returning the identifier for each item in the path.<br/>
		/// If <see cref="Run"/> has not been called, or if it returned <see langword="true"/>, this method will throw an exception.
		/// </summary>
		public IEnumerable<string> GetCircularDependencyPath() {
			if (!_hasResult)
				throw new InvalidOperationException("Cannot evaluate until Run has been called and returned false");
			if (_throwingNode is null)
				throw new InvalidOperationException("No circular dependency was found, cannot get path");

			Stack<T> path = [];
			WalkNode current = _throwingNode;

			while (current is not null) {
				path.Push(current.Item);
				current = current.Parent;
			}

			foreach (var item in path)
				yield return _getName(item);
		}

		/// <summary>
		/// Creates a copy of this <see cref="CircularDependencyChecker{T}"/> with the same configuration of functions but without any state
		/// </summary>
		public CircularDependencyChecker<T> Copy() => new(_areEqual, _getDependencies, _getName);
	}
}
