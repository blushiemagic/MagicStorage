using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Sorting
{
	public class CompareDefault : CompareFunction
	{
		public CompareDefault() {
			SortClassList.Initialize();
		}

		public override int Compare(Item item1, Item item2) {
			return SortClassList.Compare(item1, item2);
		}
	}

	public static class SortClassList
	{
		private static readonly bool initialized = false;
		private static readonly List<DefaultSortClass> classes = new List<DefaultSortClass>();

		private static readonly CompareDps _dps = new CompareDps();

		public static int Compare(Item item1, Item item2) {
			int class1 = classes.Count;
			int class2 = classes.Count;
			for (int k = 0; k < classes.Count; k++)
				if (classes[k].Pass(item1)) {
					class1 = k;
					break;
				}
			for (int k = 0; k < classes.Count; k++)
				if (classes[k].Pass(item2)) {
					class2 = k;
					break;
				}
			if (class1 != class2)
				return class1 - class2;
			return classes[class1].Compare(item1, item2);
		}

		public static void Initialize() {
			if (initialized)
				return;
			classes.Add(new DefaultSortClass(MeleeWeapon, CompareDps));
			classes.Add(new DefaultSortClass(RangedWeapon, CompareDps));
			classes.Add(new DefaultSortClass(MagicWeapon, CompareDps));
			classes.Add(new DefaultSortClass(SummonWeapon, CompareValue));
			classes.Add(new DefaultSortClass(ThrownWeapon, CompareDps));
			classes.Add(new DefaultSortClass(Weapon, CompareDps));
			classes.Add(new DefaultSortClass(Ammo, CompareValue));
			classes.Add(new DefaultSortClass(Picksaw, ComparePicksaw));
			classes.Add(new DefaultSortClass(Hamaxe, CompareHamaxe));
			classes.Add(new DefaultSortClass(Pickaxe, ComparePickaxe));
			classes.Add(new DefaultSortClass(Axe, CompareAxe));
			classes.Add(new DefaultSortClass(Hammer, CompareHammer));
			classes.Add(new DefaultSortClass(TerraformingTool, CompareTerraformingPriority));
			classes.Add(new DefaultSortClass(AmmoTool, CompareRarity));
			classes.Add(new DefaultSortClass(Armor, CompareRarity));
			classes.Add(new DefaultSortClass(VanityArmor, CompareRarity));
			classes.Add(new DefaultSortClass(Accessory, CompareAccessory));
			classes.Add(new DefaultSortClass(Grapple, CompareRarity));
			classes.Add(new DefaultSortClass(Mount, CompareRarity));
			classes.Add(new DefaultSortClass(Cart, CompareRarity));
			classes.Add(new DefaultSortClass(LightPet, CompareRarity));
			classes.Add(new DefaultSortClass(VanityPet, CompareRarity));
			classes.Add(new DefaultSortClass(Dye, CompareDye));
			classes.Add(new DefaultSortClass(HairDye, CompareHairDye));
			classes.Add(new DefaultSortClass(HealthPotion, CompareHealing));
			classes.Add(new DefaultSortClass(ManaPotion, CompareMana));
			classes.Add(new DefaultSortClass(Elixir, CompareElixir));
			classes.Add(new DefaultSortClass(BuffPotion, CompareRarity));
			classes.Add(new DefaultSortClass(BossSpawn, CompareBossSpawn));
			classes.Add(new DefaultSortClass(Painting, ComparePainting));
			classes.Add(new DefaultSortClass(Wiring, CompareWiring));
			classes.Add(new DefaultSortClass(Material, CompareMaterial));
			classes.Add(new DefaultSortClass(Rope, CompareRope));
			classes.Add(new DefaultSortClass(Extractible, CompareExtractible));
			classes.Add(new DefaultSortClass(Misc, CompareMisc));
			classes.Add(new DefaultSortClass(FrameImportantTile, CompareName));
			classes.Add(new DefaultSortClass(CommonTile, CompareName));
		}

		private static bool MeleeWeapon(Item item) {
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.melee && item.pick < 1 && item.hammer < 1 && item.axe < 1;
		}

		private static bool RangedWeapon(Item item) {
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.ranged;
		}

		private static bool MagicWeapon(Item item) {
			return item.maxStack == 1 && item.damage > 0 && item.ammo == 0 && item.magic;
		}

		private static bool SummonWeapon(Item item) {
			return item.maxStack == 1 && item.damage > 0 && item.summon;
		}

		private static bool ThrownWeapon(Item item) {
			return item.damage > 0 && (item.ammo == 0 || item.notAmmo) && item.shoot > ProjectileID.None && item.thrown;
		}

		private static bool Weapon(Item item) {
			return item.damage > 0 && item.ammo == 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0;
		}

		private static bool Ammo(Item item) {
			return item.ammo > 0 && item.damage > 0;
		}

		private static bool Picksaw(Item item) {
			return item.pick > 0 && item.axe > 0;
		}

		private static bool Hamaxe(Item item) {
			return item.hammer > 0 && item.axe > 0;
		}

		private static bool Pickaxe(Item item) {
			return item.pick > 0;
		}

		private static bool Axe(Item item) {
			return item.axe > 0;
		}

		private static bool Hammer(Item item) {
			return item.hammer > 0;
		}

		private static bool TerraformingTool(Item item) {
			return ItemID.Sets.SortingPriorityTerraforming[item.type] >= 0;
		}

		private static bool AmmoTool(Item item) {
			return item.ammo > 0;
		}

		private static bool Armor(Item item) {
			return (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && !item.vanity;
		}

		private static bool VanityArmor(Item item) {
			return (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && item.vanity;
		}

		private static bool Accessory(Item item) {
			return item.accessory;
		}

		private static bool Grapple(Item item) {
			return Main.projHook[item.shoot];
		}

		public static bool Mount(Item item) {
			return item.mountType != -1 && !MountID.Sets.Cart[item.mountType];
		}

		public static bool Cart(Item item) {
			return item.mountType != -1 && MountID.Sets.Cart[item.mountType];
		}

		public static bool LightPet(Item item) {
			return item.buffType > 0 && Main.lightPet[item.buffType];
		}

		public static bool VanityPet(Item item) {
			return item.buffType > 0 && Main.vanityPet[item.buffType];
		}

		public static bool Dye(Item item) {
			return item.dye > 0;
		}

		public static bool HairDye(Item item) {
			return item.hairDye >= 0;
		}

		private static bool HealthPotion(Item item) {
			return item.consumable && item.healLife > 0 && item.healMana < 1;
		}

		private static bool ManaPotion(Item item) {
			return item.consumable && item.healLife < 1 && item.healMana > 0;
		}

		private static bool Elixir(Item item) {
			return item.consumable && item.healLife > 0 && item.healMana > 0;
		}

		private static bool BuffPotion(Item item) {
			return item.consumable && item.buffType > 0;
		}

		public static bool BossSpawn(Item item) {
			return ItemID.Sets.SortingPriorityBossSpawns[item.type] >= 0;
		}

		private static bool Painting(Item item) {
			return ItemID.Sets.SortingPriorityPainting[item.type] >= 0 || item.paint > 0;
		}

		private static bool Wiring(Item item) {
			return ItemID.Sets.SortingPriorityWiring[item.type] >= 0 || item.mech;
		}

		private static bool Material(Item item) {
			return ItemID.Sets.SortingPriorityMaterials[item.type] >= 0;
		}

		private static bool Rope(Item item) {
			return ItemID.Sets.SortingPriorityRopes[item.type] >= 0;
		}

		private static bool Extractible(Item item) {
			return ItemID.Sets.SortingPriorityExtractibles[item.type] >= 0;
		}

		private static bool Misc(Item item) {
			return item.createTile < TileID.Dirt && item.createWall < 1;
		}

		private static bool FrameImportantTile(Item item) {
			return item.createTile >= TileID.Dirt && Main.tileFrameImportant[item.createTile];
		}

		private static bool CommonTile(Item item) {
			return item.createTile >= TileID.Dirt || item.createWall > 0;
		}

		private static int CompareRarity(Item item1, Item item2) {
			return item1.rare - item2.rare;
		}

		private static int CompareValue(Item item1, Item item2) {
			return item1.value - item2.value;
		}

		private static int CompareDps(Item item1, Item item2) {
			int r = _dps.Compare(item1, item2);
			return r != 0 ? r : CompareValue(item1, item2);
		}

		private static int ComparePicksaw(Item item1, Item item2) {
			int result = item1.pick - item2.pick;
			if (result == 0)
				result = item1.axe - item2.axe;
			return result;
		}

		private static int CompareHamaxe(Item item1, Item item2) {
			int result = item1.axe - item2.axe;
			if (result == 0)
				result = item1.hammer - item2.hammer;
			return result;
		}

		private static int ComparePickaxe(Item item1, Item item2) {
			return item1.pick - item2.pick;
		}

		private static int CompareAxe(Item item1, Item item2) {
			return item1.axe - item2.axe;
		}

		private static int CompareHammer(Item item1, Item item2) {
			return item1.hammer - item2.hammer;
		}

		private static int CompareTerraformingPriority(Item item1, Item item2) {
			return ItemID.Sets.SortingPriorityTerraforming[item1.type] - ItemID.Sets.SortingPriorityTerraforming[item2.type];
		}

		private static int CompareAccessory(Item item1, Item item2) {
			int result = item1.vanity.CompareTo(item2.vanity);
			if (result == 0)
				result = CompareValue(item1, item2);
			return result;
		}

		private static int CompareDye(Item item1, Item item2) {
			int result = CompareRarity(item1, item2);
			if (result == 0)
				result = item2.dye - item1.dye;
			return result;
		}

		private static int CompareHairDye(Item item1, Item item2) {
			int result = CompareRarity(item1, item2);
			if (result == 0)
				result = item2.hairDye - item1.hairDye;
			return result;
		}

		private static int CompareHealing(Item item1, Item item2) {
			return item2.healLife - item1.healLife;
		}

		private static int CompareMana(Item item1, Item item2) {
			return item2.mana - item1.mana;
		}

		private static int CompareElixir(Item item1, Item item2) {
			int result = CompareHealing(item1, item2);
			if (result == 0)
				result = CompareMana(item1, item2);
			return result;
		}

		private static int CompareBossSpawn(Item item1, Item item2) {
			return ItemID.Sets.SortingPriorityBossSpawns[item1.type] - ItemID.Sets.SortingPriorityBossSpawns[item2.type];
		}

		private static int ComparePainting(Item item1, Item item2) {
			int result = ItemID.Sets.SortingPriorityPainting[item2.type] - ItemID.Sets.SortingPriorityPainting[item1.type];
			if (result == 0)
				result = item1.paint - item2.paint;
			return result;
		}

		private static int CompareWiring(Item item1, Item item2) {
			int result = ItemID.Sets.SortingPriorityWiring[item2.type] - ItemID.Sets.SortingPriorityWiring[item1.type];
			if (result == 0)
				result = CompareRarity(item1, item2);
			return result;
		}

		private static int CompareMaterial(Item item1, Item item2) {
			return ItemID.Sets.SortingPriorityMaterials[item2.type] - ItemID.Sets.SortingPriorityMaterials[item1.type];
		}

		private static int CompareRope(Item item1, Item item2) {
			return ItemID.Sets.SortingPriorityRopes[item2.type] - ItemID.Sets.SortingPriorityRopes[item1.type];
		}

		private static int CompareExtractible(Item item1, Item item2) {
			return ItemID.Sets.SortingPriorityExtractibles[item2.type] - ItemID.Sets.SortingPriorityExtractibles[item1.type];
		}

		public static int CompareMisc(Item item1, Item item2) {
			int result = CompareRarity(item1, item2);
			if (result == 0)
				result = item2.value - item1.value;
			return result;
		}

		private static int CompareName(Item item1, Item item2) {
			return string.Compare(item1.Name, item2.Name, StringComparison.OrdinalIgnoreCase);
		}
	}

	public class DefaultSortClass
	{
		private readonly Func<Item, Item, int> compareFunc;
		private readonly Func<Item, bool> passFunc;

		public DefaultSortClass(Func<Item, bool> passFunc, Func<Item, Item, int> compareFunc) {
			this.passFunc = passFunc;
			this.compareFunc = compareFunc;
		}

		public bool Pass(Item item) {
			return passFunc(item);
		}

		public int Compare(Item item1, Item item2) {
			return compareFunc(item1, item2);
		}
	}
}
