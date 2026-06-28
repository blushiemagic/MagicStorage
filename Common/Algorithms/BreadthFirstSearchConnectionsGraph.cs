using System;
using System.Collections.Generic;

namespace MagicStorage.Common.Algorithms {
	/// <summary>
	/// A variation of the Breadth-First-Search searching algorithm that stores on each node what other nodes it is directly connected to.
	/// </summary>
	public abstract class BreadthFirstSearchConnectionsGraph<TValue> {
		private bool _ready;
		private readonly List<Node> _pool = [];
		private readonly Dictionary<TValue, Node> _entryToNode = [];

		protected TValue ScanOrigin { get; private set; }

		public void Scan(TValue origin) {
			ScanOrigin = origin;

			_ready = false;
			_pool.Clear();
			_entryToNode.Clear();

			Initialize();

			Queue<TValue> exploring = [];

			PopulateInitialQueue(exploring, origin);

			while (exploring.TryDequeue(out TValue explore)) {
				if (!IsValidEntry(explore))
					continue;

				TransformEntry(ref explore);

				OnEntryVisited(explore);

				var nodeExplore = GetOrReserveNode(explore, out bool created);
				ref var knownExplore = ref nodeExplore.Connections.known;

				if (created)
					knownExplore.Add(nodeExplore.Index);

				foreach (var enumerated in EnumerateNextEntries(explore)) {
					if (!IsValidEntry(enumerated))
						continue;

					var next = enumerated;
					TransformEntry(ref next);

					var nodeNext = GetOrReserveNode(next, out created);
					ref var knownNext = ref nodeNext.Connections.known;

					if (knownExplore.Add(nodeNext.Index)) {
						if (created) {
							// A new node has been found, link the graphs
							knownNext = knownExplore;

							exploring.Enqueue(next);
						} else {
							// The node already exists but wasn't connected, combine their graphs
							var combined = new HashSet<int>(knownExplore);

							combined.UnionWith(knownNext);

							knownExplore = combined;
							knownNext = combined;
						}
					}
				}
			}

			_ready = true;
		}

		protected virtual void Initialize() { }

		protected virtual void PopulateInitialQueue(Queue<TValue> queue, TValue origin) {
			queue.Enqueue(origin);

			foreach (var initialNext in EnumerateNextEntries(origin))
				queue.Enqueue(initialNext);
		}

		protected abstract bool IsValidEntry(TValue entry);

		protected virtual void TransformEntry(ref TValue entry) { }

		protected virtual void OnEntryVisited(TValue value) { }

		protected abstract IEnumerable<TValue> EnumerateNextEntries(TValue current);

		public IEnumerable<TValue> EnumerateFrom(TValue origin) {
			if (!_ready)
				yield break;

			if (!_entryToNode.TryGetValue(origin, out var nodeOrigin))
				yield break;

			foreach (var index in nodeOrigin.Connections.known)
				yield return _pool[index].Value;
		}

		public IEnumerable<TValue> EnumerateDisjointFrom(TValue origin) {
			if (!_ready)
				yield break;

			if (!_entryToNode.TryGetValue(origin, out var nodeOrigin)) {
				// All nodes are disjoint
				foreach (var node in _pool)
					yield return node.Value;

				yield break;
			}

			var connected = nodeOrigin.Connections.known;

			foreach (var node in _pool) {
				if (!connected.Contains(node.Index))
					yield return node.Value;
			}
		}

		private Node GetOrReserveNode(TValue entry, out bool created) {
			if (!_entryToNode.TryGetValue(entry, out Node node)) {
				node = new Node(entry, _pool.Count, new());
				_pool.Add(node);
				_entryToNode[entry] = node;
				created = true;
			} else
				created = false;

			return node;
		}

		private record class Node(TValue Value, int Index, SharedGraph Connections) {
			public SharedGraph Connections { get; set; } = Connections;
		}

		private class SharedGraph {
			public HashSet<int> known = [];
		}
	}
}
