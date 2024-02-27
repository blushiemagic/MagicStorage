using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	public class RecursionTreeMaterialRecipeSlectionButton : UITextPanel<char> {
		public readonly RecursionTreeMaterialEntry parent;
		public readonly bool next;

		// TODO: incomplete implementation

		public RecursionTreeMaterialRecipeSlectionButton(RecursionTreeMaterialEntry parent, bool next, float scale = 1) : base(next ? '>' : '<', scale, false) {
			this.parent = parent;
			this.next = next;
		}

		public override void LeftClick(UIMouseEvent evt) {
			base.LeftClick(evt);

			int offset = next ? 1 : -1;

			// TODO: make parent move to previous or next recipe, then force the tree logic to refresh

			SoundEngine.PlaySound(SoundID.MenuTick);
		}
	}
}
