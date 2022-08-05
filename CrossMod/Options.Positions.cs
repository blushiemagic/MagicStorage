using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.CrossMod {
	partial class SortingOption {
		public abstract class Position { }

		public sealed class Between : Position {
			public SortingOption Option1 { get; }
			public SortingOption Option2 { get; }

			public Between(SortingOption layer1, SortingOption layer2) {
				Option1 = layer1;
				Option2 = layer2;
			}

			public Between() { }
		}

		public class Multiple : Position, IEnumerable {
			public delegate bool Condition(bool craftingGUI);
			public IList<(Between, Condition)> Positions { get; } = new List<(Between, Condition)>();

			public void Add(Between position, Condition condition) => Positions.Add((position, condition));

			public IEnumerator GetEnumerator() => Positions.GetEnumerator();
		}

		public class BeforeParent : Position {
			public SortingOption Parent { get; }

			public BeforeParent(SortingOption parent) {
				Parent = parent;
			}
		}

		public class AfterParent : Position {
			public SortingOption Parent { get; }

			public AfterParent(SortingOption parent) {
				Parent = parent;
			}
		}
	}

	partial class FilteringOption {
		public abstract class Position { }

		public sealed class Between : Position {
			public FilteringOption Option1 { get; }
			public FilteringOption Option2 { get; }

			public Between(FilteringOption layer1, FilteringOption layer2) {
				Option1 = layer1;
				Option2 = layer2;
			}

			public Between() { }
		}

		public class Multiple : Position, IEnumerable {
			public delegate bool Condition(bool craftingGUI);
			public IList<(Between, Condition)> Positions { get; } = new List<(Between, Condition)>();

			public void Add(Between position, Condition condition) => Positions.Add((position, condition));

			public IEnumerator GetEnumerator() => Positions.GetEnumerator();
		}

		public class BeforeParent : Position {
			public FilteringOption Parent { get; }

			public BeforeParent(FilteringOption parent) {
				Parent = parent;
			}
		}

		public class AfterParent : Position {
			public FilteringOption Parent { get; }

			public AfterParent(FilteringOption parent) {
				Parent = parent;
			}
		}
	}
}
