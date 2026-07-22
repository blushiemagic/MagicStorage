using System.Collections;
using System.Collections.Generic;

namespace MagicStorage.CrossMod {
	partial class SortingOption {
		/// <summary>
		/// Describes where a sorting option should appear relative to other sorting options.
		/// </summary>
		public abstract class Position { }

		/// <summary>
		/// Places a sorting option between two optional neighboring sorting options.
		/// </summary>
		public sealed class Between : Position {
			/// <summary>
			/// Gets the option that should appear before the positioned option, or <see langword="null"/> for the beginning.
			/// </summary>
			public SortingOption Option1 { get; }

			/// <summary>
			/// Gets the option that should appear after the positioned option, or <see langword="null"/> for the end.
			/// </summary>
			public SortingOption Option2 { get; }

			/// <summary>
			/// Creates a position between two sorting options.
			/// </summary>
			public Between(SortingOption layer1, SortingOption layer2) {
				Option1 = layer1;
				Option2 = layer2;
			}

			/// <summary>
			/// Creates an unconstrained position ordered by load order.
			/// </summary>
			public Between() { }
		}

		/// <summary>
		/// Places one sorting option in multiple conditional positions.
		/// </summary>
		public class Multiple : Position, IEnumerable {
			/// <summary>
			/// Returns whether a conditional slot should be visible for the current UI.
			/// </summary>
			public delegate bool Condition(bool craftingGUI);

			/// <summary>
			/// Gets the conditional positions for this option.
			/// </summary>
			public IList<(Between, Condition)> Positions { get; } = new List<(Between, Condition)>();

			/// <summary>
			/// Adds a conditional position for the option.
			/// </summary>
			public void Add(Between position, Condition condition) => Positions.Add((position, condition));

			/// <inheritdoc/>
			public IEnumerator GetEnumerator() => Positions.GetEnumerator();
		}

		/// <summary>
		/// Places a sorting option before a parent option.
		/// </summary>
		public class BeforeParent : Position {
			/// <summary>
			/// Gets the parent option this option should precede.
			/// </summary>
			public SortingOption Parent { get; }

			/// <summary>
			/// Creates a position before <paramref name="parent"/>.
			/// </summary>
			public BeforeParent(SortingOption parent) {
				Parent = parent;
			}
		}

		/// <summary>
		/// Places a sorting option after a parent option.
		/// </summary>
		public class AfterParent : Position {
			/// <summary>
			/// Gets the parent option this option should follow.
			/// </summary>
			public SortingOption Parent { get; }

			/// <summary>
			/// Creates a position after <paramref name="parent"/>.
			/// </summary>
			public AfterParent(SortingOption parent) {
				Parent = parent;
			}
		}
	}

	partial class FilteringOption {
		/// <summary>
		/// Describes where a filtering option should appear relative to other filtering options.
		/// </summary>
		public abstract class Position { }

		/// <summary>
		/// Places a filtering option between two optional neighboring filtering options.
		/// </summary>
		public sealed class Between : Position {
			/// <summary>
			/// Gets the option that should appear before the positioned option, or <see langword="null"/> for the beginning.
			/// </summary>
			public FilteringOption Option1 { get; }

			/// <summary>
			/// Gets the option that should appear after the positioned option, or <see langword="null"/> for the end.
			/// </summary>
			public FilteringOption Option2 { get; }

			/// <summary>
			/// Creates a position between two filtering options.
			/// </summary>
			public Between(FilteringOption layer1, FilteringOption layer2) {
				Option1 = layer1;
				Option2 = layer2;
			}

			/// <summary>
			/// Creates an unconstrained position ordered by load order.
			/// </summary>
			public Between() { }
		}

		/// <summary>
		/// Places one filtering option in multiple conditional positions.
		/// </summary>
		public class Multiple : Position, IEnumerable {
			/// <summary>
			/// Returns whether a conditional slot should be visible for the current UI.
			/// </summary>
			public delegate bool Condition(bool craftingGUI);

			/// <summary>
			/// Gets the conditional positions for this option.
			/// </summary>
			public IList<(Between, Condition)> Positions { get; } = new List<(Between, Condition)>();

			/// <summary>
			/// Adds a conditional position for the option.
			/// </summary>
			public void Add(Between position, Condition condition) => Positions.Add((position, condition));

			/// <inheritdoc/>
			public IEnumerator GetEnumerator() => Positions.GetEnumerator();
		}

		/// <summary>
		/// Places a filtering option before a parent option.
		/// </summary>
		public class BeforeParent : Position {
			/// <summary>
			/// Gets the parent option this option should precede.
			/// </summary>
			public FilteringOption Parent { get; }

			/// <summary>
			/// Creates a position before <paramref name="parent"/>.
			/// </summary>
			public BeforeParent(FilteringOption parent) {
				Parent = parent;
			}
		}

		/// <summary>
		/// Places a filtering option after a parent option.
		/// </summary>
		public class AfterParent : Position {
			/// <summary>
			/// Gets the parent option this option should follow.
			/// </summary>
			public FilteringOption Parent { get; }

			/// <summary>
			/// Creates a position after <paramref name="parent"/>.
			/// </summary>
			public AfterParent(FilteringOption parent) {
				Parent = parent;
			}
		}
	}
}
