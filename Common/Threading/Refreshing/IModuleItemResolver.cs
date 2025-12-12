using Terraria;

namespace MagicStorage.Common.Threading.Refreshing {
	public interface IModuleItemResolver {
		bool IsModuleItem(Item item);

		bool IsInventoryModuleItem(Item item);
	}
}
