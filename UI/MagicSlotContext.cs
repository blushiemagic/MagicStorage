using Terraria.UI;

namespace MagicStorage.UI {
	/// <summary>
	/// Redirections of <see cref="ItemSlot.Context"/> constants to more sensible internal names
	/// </summary>
	public static class MagicSlotContext {
		/// <summary>
		/// Standard item slot context
		/// </summary>
		public const int Normal = ItemSlot.Context.InventoryItem;                 // Blue

		/// <summary>
		/// Default context for when the Storage UI is currently in a special viewing mode
		/// </summary>
		public const int SpecialStorageMode = ItemSlot.Context.BankItem;          // Pink
		/// <summary>
		/// Currently focused item slot when the Storage UI is in a special viewing mode
		/// </summary>
		public const int SelectedActionItem = ItemSlot.Context.TrashItem;         // Light green
		/// <summary>
		/// Storage UI is in Selling mode and the item is selected for selling
		/// </summary>
		public const int SelectedForSelling = ItemSlot.Context.EquipLight;        // Green
		/// <summary>
		/// The preview item for the Selling mode quantity pop-up
		/// </summary>
		public const int SellActionPreview = ItemSlot.Context.BankItem;           // Pink

		/// <summary>
		/// Recipe ingredient item is not present in storage and cannot be crafted
		/// </summary>
		public const int IngredientNotCraftable = ItemSlot.Context.ChestItem;     // Red
		/// <summary>
		/// Recipe ingredient item is present in storage, but there isn't enough to fulfil the current recipe's requirements
		/// </summary>
		public const int IngredientPartiallyInStock = ItemSlot.Context.BankItem;  // Pink
		/// <summary>
		/// Recipe ingredient can be crafted
		/// </summary>
		public const int IngredientCraftable = ItemSlot.Context.TrashItem;        // Light green

		/// <summary>
		/// Recipe ingredient in storage was manually blocked from being used in crafting
		/// </summary>
		public const int IngredientBlocked = ItemSlot.Context.ChestItem;          // Red

		/// <summary>
		/// Recipe item is the currently selected recipe
		/// </summary>
		public const int SelectedRecipe = ItemSlot.Context.TrashItem;             // Light green
		/// <summary>
		/// Recipe item is the current selected recipe, and its requirements are not met
		/// </summary>
		public const int SelectedRecipeNotAvailable = ItemSlot.Context.BankItem;  // Pink
		/// <summary>
		/// Recipe item is not the currently selected recipe and its requirements are not met
		/// </summary>
		public const int RecipeNotAvailable = ItemSlot.Context.ChestItem;         // Red
	}
}
