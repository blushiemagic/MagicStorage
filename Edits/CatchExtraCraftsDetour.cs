#nullable enable
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using OnPlayer = On.Terraria.Player;

namespace MagicStorage.Edits;

public class CatchExtraCraftsDetour : ILoadable
{
	public Mod Mod { get; private set; } = null!;

	public void Load(Mod mod)
	{
		Mod = mod;

		OnPlayer.QuickSpawnItem_IEntitySource_int_int += OnPlayerQuickSpawnItem_IEntitySource_int_int;
	}

	public void Unload()
	{
		OnPlayer.QuickSpawnItem_IEntitySource_int_int -= OnPlayerQuickSpawnItem_IEntitySource_int_int;

		Mod = null!;
	}

	private int OnPlayerQuickSpawnItem_IEntitySource_int_int(OnPlayer.orig_QuickSpawnItem_IEntitySource_int_int orig,
		Player self, IEntitySource source, int type, int stack)
	{
		if (!CraftingGUI.CatchDroppedItems)
			return orig(self, source, type, stack);

		Item item = new(type, stack);
		CraftingGUI.DroppedItems.Add(item);
		return -1; // return invalid value since this should never be used
	}
}
