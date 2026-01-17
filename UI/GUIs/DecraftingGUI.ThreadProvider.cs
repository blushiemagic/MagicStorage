using MagicStorage.Common.Threading;

namespace MagicStorage {
	partial class DecraftingGUI {
		private class SelectionProvider : IReadOnlyValueProvider<int> {
			public int StaticSource => selectedItem;
			public int Value { get; private set; }
			public SelectionProvider() => CopyFromStatic();
			public SelectionProvider(int defaultValue) => Value = defaultValue;
			public void ClearStatic() { }
			public void CopyFromStatic() => Value = selectedItem;
			public void CopyToStatic() => selectedItem = Value;
		}
	}
}
