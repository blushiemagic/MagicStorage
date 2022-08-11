namespace MagicStorage.UI.States {
	public enum ButtonConfigurationMode {
		/// <summary>
		/// The layout of the buttons from v0.5.6.5 and previous versions, modified slightly to account for new buttons.
		/// </summary>
		Legacy,
		/// <summary>
		/// All sorting and filtering buttons are moved to the Sorting and Filtering tabs.
		/// </summary>
		ModernPaged,
		/// <summary>
		/// Which buttons appear in the Storage and Crafting pages can be configured via the Sorting and Filtering pages.
		/// </summary>
		ModernConfigurable,
		/// <summary>
		/// The layout of the buttons is set to the layout from v0.5.6.5 and previous versions, with an extra gear button added.<br/>
		/// Clicking the gear button opens a separate panel for choosing which sorting/filtering option is desired.
		/// </summary>
		LegacyWithGear,
		/// <summary>
		/// The layout of the buttons is set to the layout from v0.5.6.5 and previous versions.<br/>
		/// Any additional buttons are moved to the Sorting and Filtering pages.
		/// </summary>
		LegacyBasicWithPaged,
		/// <summary>
		/// The buttons are replaced with dropdown menus which contains all buttons in a list.
		/// </summary>
		ModernDropdown
	}
}
