using MagicStorage.Sorting;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod {
	[Autoload(false)]
	public sealed class SortDefault : SortingOption {
		public override IComparer<Item> Sorter => CompareDefault.Instance;

		public override string Texture => "Terraria/Images/UI/Sort_0";

		public override string Name => "Default";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortID : SortingOption {
		public override IComparer<Item> Sorter => CompareID.Instance;

		public override string Texture => "MagicStorage/Assets/SortID";

		public override string Name => "ID";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortName : SortingOption {
		public override IComparer<Item> Sorter => CompareName.Instance;

		public override string Texture => "MagicStorage/Assets/SortName";

		public override string Name => "Name";

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortValue : SortingOption {
		public override IComparer<Item> Sorter => CompareValue.Instance;

		public override string Texture => "MagicStorage/Assets/SortValue";

		public override string Name => "Value";

		public override bool SortAgainAfterFuzzy => true;

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortQuantityAbsolute : SortingOption {
		public override IComparer<Item> Sorter => CompareQuantityAbsolute.Instance;

		public override string Texture => "MagicStorage/Assets/SortNumber";

		public override string Name => "QuantityAbsolute";

		public override bool CacheFuzzySorting => false;  // Causes issues during mod loading

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortQuantityRatio : SortingOption {
		public override IComparer<Item> Sorter => CompareQuantityRatio.Instance;

		public override string Texture => "MagicStorage/Assets/SortNumber";

		public override string Name => "QuantityRatio";

		public override bool CacheFuzzySorting => false;  // Causes issues during mod loading

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}

	[Autoload(false)]
	public sealed class SortDamage : SortingOption {
		public override IComparer<Item> Sorter => CompareDamage.Instance;

		public override string Texture => "MagicStorage/Assets/SortNumber";

		public override string Name => "Damage";

		public override bool SortAgainAfterFuzzy => true;

		public override Position GetDefaultPosition() => new Between();  //Order is determined by load order
	}
}
