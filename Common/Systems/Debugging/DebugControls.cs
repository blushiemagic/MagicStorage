using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems.Debugging {
	internal static partial class DebugControls {
		private class Loadable : ILoadable {
			void ILoadable.Load(Mod mod) {
				Initialize();
				PopulateLookups();
			}

			void ILoadable.Unload() {
				ClearLookups();
				Deinitialize();
			}
		}

		internal class Node {
			private bool _state;
			private Dictionary<string, Node> _childMap;

			private readonly List<Node> _anyNodeTrackers = [];

			public bool IsLeafNode => _childMap is null;

			public Node() {
				_state = false;
				_childMap = [];
			}

			public Node(bool state) {
				_state = state;
				_childMap = null;
			}

			public bool GetState() {
				if (!IsLeafNode)
					throw new InvalidOperationException("Cannot get state of a non-leaf node.");

				return _state || _anyNodeTrackers.Exists(node => node.GetAnyState());
			}

			private bool GetAnyState() {
				if (IsLeafNode)
					throw new InvalidOperationException("Leaf nodes do not have an \"any\" state to get.");

				if (!TryGetControl("any", out var anyNode))
					throw new InvalidOperationException("Leaf node \"any\" was not defined.");

				return anyNode._state;
			}

			public void SetState(bool value) {
				if (!IsLeafNode)
					throw new InvalidOperationException("Cannot set state of a non-leaf node.");

				_state = value;
			}

			public bool HasControl(string key) => !IsLeafNode && _childMap.ContainsKey(key);

			public bool TryGetControl(string key, out Node node) {
				if (IsLeafNode) {
					node = null;
					return false;
				}

				return _childMap.TryGetValue(key, out node);
			}

			public Node Set(string key, bool value) {
				// No longer a leaf node
				_state = false;
				_childMap ??= [];
				_childMap[key] = new(value);
				return this;
			}

			public Node Set(string key, Node node) {
				// No longer a leaf node
				_state = false;
				if (_childMap is null) {
					_childMap = [];
					_childMap["any"] = new Node(false);
				}

				_childMap[key] = node;
				return this;
			}

			public IEnumerable<HierarchyConnection> EnumerateHierarchy(Node parent) {
				yield return new(parent, this);

				if (!IsLeafNode) {
					// Enumerate through the branches
					foreach (var (key, node) in _childMap) {
						foreach (var kvp in node.EnumerateHierarchy(this))
							yield return kvp;
					}
				}
			}

			public IEnumerable<EnumerationPath> EnumerateControls(string path) {
				if (IsLeafNode) {
					// The branch ends here
					yield return new(path, this);
				} else {
					// Enumerate through the branches
					foreach (var (key, node) in _childMap) {
						string childPath = string.IsNullOrEmpty(path) ? key : $"{path}.{key}";

						foreach (var kvp in node.EnumerateControls(childPath))
							yield return kvp;
					}
				}
			}

			internal void InitializeTrackers(Node parent) {
				_anyNodeTrackers.Clear();

				if (parent is null || IsLeafNode)
					return;

				_anyNodeTrackers.AddRange(parent._anyNodeTrackers);
				_anyNodeTrackers.Add(parent);
			}

			public record struct HierarchyConnection(Node Parent, Node Child);

			public record struct EnumerationPath(string FullPath, Node Node);
		}
	}
}
