using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MagicStorage.Common.Systems.Debugging {
	partial class DebugControls {
		private static readonly Dictionary<string, Node> _controlLookup = [];
		private static readonly List<string> _registeredControlpaths = [];

		private static void PopulateLookups() {
			#if DEBUG
			var logger = MagicStorageMod.Instance.Logger;

			HashSet<string> definedControls = [];
			#endif

			foreach (var (path, node) in _controlRoot.EnumerateControls(null)) {
				_controlLookup[path] = node;
				_registeredControlpaths.Add(path);

				#if DEBUG
				logger.Info($"Registered debug control: {path}");

				definedControls.Add(path);
				#endif
			}

			#if DEBUG
			// Verify that the constants match the entire controls tree
			var originalControlsSet = new HashSet<string>(definedControls);

			var constants = typeof(Names).GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(static f => f.IsLiteral && !f.IsInitOnly)
				.Select(static f => new ConstantDefinition(f.Name, (string)f.GetRawConstantValue()));

			bool failure = false;

			foreach (var (name, value) in constants) {
				if (!definedControls.Remove(value)) {
					failure = true;

					string variable = nameof(DebugControls) + "." + nameof(Names) + "." + name;

					if (originalControlsSet.Contains(value)) {
						// The control was duplicated
						logger.Error($"Constant {variable} defines a duplicate control path \"{value}\"");
					} else {
						// The control does not exist
						logger.Error($"Constant {variable} defines a non-existent control path \"{value}\"");
					}
				}
			}

			if (failure)
				throw new InvalidOperationException("Initialization for DebugControls failed, check client log");
			#endif
		}

		#if DEBUG
		private record struct ConstantDefinition(string Name, string Value);
		#endif

		private static void ClearLookups() {
			_controlLookup.Clear();
			_registeredControlpaths.Clear();
		}

		public static bool Get(string control) => GetControl(control).GetState();

		public static bool Any(params ReadOnlySpan<string> controls) {
			foreach (var group in controls) {
				if (Get(group))
					return true;
			}

			return false;
		}

		public static bool All(params ReadOnlySpan<string> controls) {
			foreach (var group in controls) {
				if (!Get(group))
					return false;
			}

			return true;
		}

		internal static void Set(string control, bool value) {
			GetControl(control).SetState(value);
			MagicStorageMod.Instance.debugConfig.Save();
		}

		private static Node GetControl(string control) {
			ArgumentException.ThrowIfNullOrWhiteSpace(control);

			if (_controlLookup.TryGetValue(control, out var cachedNode))
				return cachedNode;

			throw new ArgumentException($"Provided control \"{control}\" does not exist.", nameof(control));
		}

		public static IEnumerable<KeyValuePair<string, bool>> Enumerate() {
			foreach (var path in _registeredControlpaths)
				yield return new(path, Get(path));
		}

		public static bool Has(string control) => _controlLookup.ContainsKey(control);

		public static Combination Combine() => new(true);

		public static Combination Combine(bool defaultValue) => new(defaultValue);

		public ref struct Combination(bool defaultValue) {
			private bool _workingValue = defaultValue;

			public Combination Get(string control) {
				_workingValue = Get(control);
				return this;
			}

			public Combination Set(bool value) {
				_workingValue = value;
				return this;
			}

			public Combination And(string control) {
				if (_workingValue)
					_workingValue = Get(control);
				return this;
			}

			public Combination And(Combination other) {
				if (_workingValue)
					_workingValue = other.Result();
				return this;
			}

			public Combination AndAny(params ReadOnlySpan<string> controls) {
				if (!_workingValue)
					return this;

				foreach (var group in controls) {
					if (Get(group)) {
						_workingValue = true;
						break;
					}
				}

				return this;
			}

			public Combination AndAll(params ReadOnlySpan<string> controls) {
				if (!_workingValue)
					return this;

				foreach (var group in controls) {
					if (!Get(group)) {
						_workingValue = false;
						break;
					}
				}

				return this;
			}

			public Combination Or(string control) {
				if (!_workingValue)
					_workingValue = Get(control);
				return this;
			}

			public Combination Or(Combination other) {
				if (!_workingValue)
					_workingValue = other.Result();
				return this;
			}

			public Combination OrAny(params ReadOnlySpan<string> controls) {
				if (_workingValue)
					return this;

				foreach (var group in controls) {
					if (Get(group)) {
						_workingValue = true;
						break;
					}
				}

				return this;
			}

			public Combination OrAll(params ReadOnlySpan<string> controls) {
				if (_workingValue)
					return this;

				foreach (var group in controls) {
					if (!Get(group)) {
						_workingValue = false;
						break;
					}
				}

				return this;
			}

			public readonly bool Result() => _workingValue;

			public static implicit operator bool(Combination combination) => combination.Result();
		}
	}
}
