namespace MagicStorage.Common.Threading.Refreshing {
	public interface ICraftingObjectProvider<T> {
		CraftingObject<T> CraftingObject { get; }
	}

	public class CraftingObject<T> {
		public readonly IReadOnlyValueProvider<T> selection;
		public readonly IValueProvider<int> craftAmountTarget;

		public CraftingObject(IReadOnlyValueProvider<T> selection, IValueProvider<int> craftAmountTarget) {
			this.selection = selection;
			this.craftAmountTarget = craftAmountTarget;
		}

		public void CopyToStaticFields() {
			selection.OverwriteStatic();
			craftAmountTarget.OverwriteStatic();
		}
	}
}
