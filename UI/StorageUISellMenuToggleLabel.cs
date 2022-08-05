namespace MagicStorage.UI {
	public class StorageUISellMenuToggleLabel : UIToggleLabel {
		public readonly int Index;

		public StorageUISellMenuToggleLabel(string name, int index, bool defaultState = false) : base(name, defaultState) {
			Index = index;
		}
	}
}
