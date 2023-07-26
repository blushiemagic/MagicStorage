using MagicStorage.Common.Systems;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace MagicStorage.UI {
	public class RecipeHistory {
		public int Current { get; private set; } = -1;

		public int Count => history.Count;

		internal readonly NewUIScrollbar scroll;
		internal readonly NewUIList list;
		private readonly List<RecipeHistoryEntry> history;

		public RecipeHistory() {
			list = new() {
				DisplayChildrenInReverseOrder = true
			};
			list.SetPadding(0);
			list.Width = StyleDimension.Fill;
			list.Height = StyleDimension.Fill;

			scroll = new();
			scroll.Width.Set(20, 0);
			scroll.Height.Set(0, 0.825f);
			scroll.Left.Set(-30, 1f);
			scroll.Top.Set(0, 0.1f);

			list.SetScrollbar(scroll);
			list.Append(scroll);
			list.ListPadding = 4;

			history = new();
		}

		public void Goto(int entry) {
			if (entry < 0 || entry >= history.Count)
				return;

			CraftingGUI.SetSelectedRecipe(history[entry].OriginalRecipe);
			StorageGUI.SetRefresh();
			CraftingGUI.SetNextDefaultRecipeCollectionToRefresh(Array.Empty<Recipe>());

			Current = entry;
			RefreshEntries();
		}

		public void AddHistory(Recipe recipe) {
			if (RecursiveCraftIntegration.Enabled && RecursiveCraftIntegration.IsCompoundRecipe(recipe))
				recipe = RecursiveCraftIntegration.GetOverriddenRecipe(recipe);

			if (history.Take(Current + 1).Select(h => h.OriginalRecipe).Any(r => Utility.RecipesMatchForHistory(recipe, r)))
				return;

			RecipeHistoryEntry entry = new(Current + 1, this);
			entry.Left.Set(2, 0f);

			if (Current < history.Count - 1) {
				//History was moved back.  Remove all entries after it
				int start = Current + 1;
				int count = history.Count - start;

				//Remove entries from the list
				for (int i = history.Count - 1; i >= start; i--)
					list.Remove(history[i]);

				history.RemoveRange(start, count);
			}

			history.Add(entry);
			list.Add(entry);

			entry.Activate();

			entry.SetRecipe(recipe);

			list.Recalculate();

			Current++;
		}

		public void Clear() {
			Current = -1;
			list.Clear();
			history.Clear();
		}

		public void RefreshEntries() {
			foreach (var entry in history)
				entry.Refresh();
		}
	}

	internal class RecipeHistoryEntry : UIPanel {
		public MagicStorageItemSlot resultSlot;
		public NewUISlotZone ingredientZone;

		public readonly RecipeHistory history;
		public readonly int index;

		public Recipe OriginalRecipe { get; private set; }

		public Recipe CompoundRecipe { get; private set; }

		public Recipe UsedRecipe => CompoundRecipe ?? OriginalRecipe;

		public RecipeHistoryEntry(int index, RecipeHistory history) {
			this.index = index;
			this.history = history;
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

			ingredientZone = new(CraftingGUI.InventoryScale * 0.55f);

			ingredientZone.InitializeSlot += (slot, scale) => {
				return new(slot, scale: scale) {
					IgnoreClicks = true
				};
			};

			Append(ingredientZone);
		}

		internal void SetRecipe(Recipe recipe) {
			if (RecursiveCraftIntegration.Enabled && RecursiveCraftIntegration.IsCompoundRecipe(recipe))
				recipe = RecursiveCraftIntegration.GetOverriddenRecipe(recipe);

			OriginalRecipe = recipe;

			if (RecursiveCraftIntegration.Enabled && RecursiveCraftIntegration.HasCompoundVariant(recipe))
				CompoundRecipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			Recipe used = UsedRecipe;

			resultSlot.SetItem(used.createItem, clone: true);

			ingredientZone.SetDimensions(7, Math.Max((used.requiredItem.Count - 1) / 7 + 1, 1));

			ingredientZone.Left.Set(resultSlot.Width.Pixels + 4, 0f);
			ingredientZone.Width.Set(ingredientZone.ZoneWidth, 0f);
			ingredientZone.Height.Set(ingredientZone.ZoneHeight, 0f);

			Width.Set(resultSlot.Width.Pixels + 4 + ingredientZone.ZoneWidth + 4, 0f);
			Height.Set(Math.Max(resultSlot.Height.Pixels, ingredientZone.ZoneHeight) + 4, 0f);

			ingredientZone.SetItemsAndContexts(used.requiredItem.Count, GetIngredient);

			Recalculate();
		}

		private Item GetIngredient(int slot, ref int context) => slot < UsedRecipe.requiredItem.Count ? UsedRecipe.requiredItem[slot] : new Item();

		public void Refresh() {
			int context = 0;

			Recipe used = UsedRecipe;

			// TODO can this be nicer?
			if (used == CraftingGUI.selectedRecipe)
				context = ItemSlot.Context.TrashItem;
			else if (!CraftingGUI.IsAvailable(used))
				context = used == CraftingGUI.selectedRecipe ? ItemSlot.Context.BankItem : ItemSlot.Context.ChestItem;

			resultSlot.Context = context;
		}

		public override void Click(UIMouseEvent evt) {
			base.Click(evt);

			history.Goto(index);

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
