using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI.History {
	public abstract class HistoryEntry<T> : UIPanel, IHistory<T> {
		public MagicStorageItemSlot resultSlot;

		object IHistory.Value => Value;

		public T Value { get; private set; }

		public IHistoryCollection<T> History { get; }
		
		IHistoryCollection IHistory.History => History;

		public int Index { get; }

		public HistoryEntry(int index, IHistoryCollection<T> history) {
			Index = index;
			History = history;
			Width = StyleDimension.Fill;
			Height = StyleDimension.Fill;

			BackgroundColor = Color.Transparent;
			BorderColor = Color.Transparent;

			SetPadding(0);
			MarginLeft = MarginTop = MarginRight = MarginBottom = 0;
		}

		public override void OnInitialize() {
			resultSlot = new(0, scale: CraftingGUI.InventoryScale) {
				IgnoreClicks = true
			};

			Append(resultSlot);
		}

		public void SetValue(T value) {
			Value = value;

			resultSlot.SetBoundItem(GetItemForResult().Clone());

			OnValueSet(value);

			Recalculate();
		}

		protected abstract Item GetItemForResult();

		protected virtual void OnValueSet(T value) { }

		public void Refresh() {
			int context = 0;
			GetResultContext(Value, ref context);
			resultSlot.Context = context;
		}

		protected abstract void GetResultContext(T value, ref int context);

		public override void LeftClick(UIMouseEvent evt) {
			base.LeftClick(evt);

			History.Goto(Index);

			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);

			BorderColor = Color.Yellow;
		}

		public override void MouseOut(UIMouseEvent evt) {
			base.MouseOut(evt);

			BorderColor = Color.Transparent;
		}
	}
}
