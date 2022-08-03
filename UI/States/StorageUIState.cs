using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public sealed class StorageUIState : BaseStorageUI {
		public override string DefaultPage => "Storage";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Storage";
			yield return "Sorting";
			yield return "Filtering";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Crafting" => new StoragePage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				_ => throw new ArgumentException("Unknown page: " + page, nameof(page))
			};

		protected override void PostInitializePages() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * CraftingGUI.SmallScale;

			panel.Left.Set(PanelLeft, 0f);
			panel.Top.Set(PanelTop, 0f);
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(PanelHeight, 0f);

			panel.OnRecalculate += UpdateFields;
		}

		private void UpdateFields() {
			PanelTop = panel.Top.Pixels;
			PanelLeft = panel.Left.Pixels;
		}

		protected override void OnOpen() {
			StorageGUI.OnRefresh += Refresh;
		}

		protected override void OnClose() {
			StorageGUI.OnRefresh -= Refresh;

			GetPage<StoragePage>("Storage").scrollBar.ViewPosition = 0f;
		}

		public void Refresh() {
			GetPage<StoragePage>("Storage").Refresh();
		}

		public class StoragePage : BaseStorageUIPage {
			public NewUIToggleButton filterFavorites;
			private UIElement topBar;
			public UITextPanel<LocalizedText> depositButton;
			public UISearchBar searchBar;
			public UIScrollbar scrollBar;
			public UIText capacityText;

			internal NewUISlotZone slotZone;  //Item slots for the items in storage

			private bool lastKnownConfigFavorites;

			public StoragePage(BaseStorageUI parent) : base(parent, "Storage") {
				OnPageSelected += StorageGUI.CheckRefresh;
			}

			public override void OnInitialize() {
				StorageUIState parent = parentUI as StorageUIState;

				topBar = new UIElement();
				topBar.Width.Set(0f, 1f);
				topBar.Height.Set(32f, 0f);
				Append(topBar);
				
				filterFavorites = new(StorageGUI.RefreshItems,
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMisc", AssetRequestMode.ImmediateLoad),
					Language.GetText("Mods.MagicStorage.ShowOnlyFavorited"),
					21);

				InitFilterButtons();

				float x = filterFavorites.GetDimensions().Width + 2 * StorageGUI.padding;

				depositButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.DepositAll"));

				depositButton.OnClick += (evt, e) => {
					bool ctrlDown = Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl);
					if (StorageGUI.TryDepositAll(ctrlDown == MagicStorageConfig.QuickStackDepositMode)) {
						StorageGUI.needRefresh = true;
						SoundEngine.PlaySound(SoundID.Grab);
					}
				};

				depositButton.OnRightClick += (evt, e) => {
					if (StorageGUI.TryRestock()) {
						StorageGUI.needRefresh = true;
						SoundEngine.PlaySound(SoundID.Grab);
					}
				};

				depositButton.OnMouseOver += (evt, e) => {
					(e as UIPanel).BackgroundColor = new Color(73, 94, 171);

					string alt = MagicStorageConfig.QuickStackDepositMode ? "Alt" : "";
					MagicUI.mouseText = Language.GetText($"Mods.MagicStorage.DepositTooltip{alt}").Value;
				};

				depositButton.OnMouseOut += (evt, e) => {
					(e as UIPanel).BackgroundColor = new Color(63, 82, 151) * 0.7f;

					MagicUI.mouseText = "";
				};

				depositButton.Left.Set(x, 0f);
				depositButton.Width.Set(128f, 0f);
				depositButton.Height.Set(-2 * StorageGUI.padding, 1f);
				depositButton.PaddingTop = 8f;
				depositButton.PaddingBottom = 8f;
				topBar.Append(depositButton);

				// TODO make this a new variable
				x += depositButton.GetDimensions().Width;

				float depositButtonRight = x;

				searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), StorageGUI.RefreshItems);
				searchBar.Left.Set(depositButtonRight + StorageGUI.padding, 0f);
				searchBar.Width.Set(-depositButtonRight - 2 * StorageGUI.padding, 1f);
				searchBar.Height.Set(0f, 1f);
				topBar.Append(searchBar);

				slotZone = new(/* HoverItemSlot, GetItem, */ StorageGUI.inventoryScale);

				slotZone.InitializeSlot += (slot, scale) => {
					MagicStorageItemSlot itemSlot = new(slot, scale: scale) {
						IgnoreClicks = true  // Purely visual
					};

					itemSlot.OnClick += (evt, e) => {
						Player player = Main.LocalPlayer;

						MagicStorageItemSlot obj = e as MagicStorageItemSlot;
						int objSlot = obj.slot + StorageGUI.numColumns * (int)Math.Round(scrollBar.ViewPosition);

						bool changed = false;
						if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0) {
							if (StorageGUI.TryDeposit(Main.mouseItem))
								changed = true;
						} else if (Main.mouseItem.IsAir && objSlot < StorageGUI.items.Count && !StorageGUI.items[objSlot].IsAir) {
							if (MagicStorageConfig.CraftingFavoritingEnabled && Main.keyState.IsKeyDown(Keys.LeftAlt)) {
								if (Main.netMode == NetmodeID.SinglePlayer) {
									StorageGUI.items[objSlot].favorited = !StorageGUI.items[objSlot].favorited;
									changed = true;
								} else {
									Main.NewTextMultiline(
										"Toggling item as favorite is not implemented in multiplayer but you can withdraw this item, toggle it in inventory and deposit again",
										c: Color.White);
								}
								// there is no item instance id and there is no concept of slot # in heart so we can't send this in operation
								// a workaropund would be to withdraw and deposit it back with changed favorite flag
								// but it still might look ugly for the player that initiates operation
							} else {
								Item toWithdraw = StorageGUI.items[objSlot].Clone();

								if (toWithdraw.stack > toWithdraw.maxStack)
									toWithdraw.stack = toWithdraw.maxStack;
								
								Main.mouseItem = StorageGUI.DoWithdraw(toWithdraw, ItemSlot.ShiftInUse);
								
								if (ItemSlot.ShiftInUse)
									Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, GetItemSettings.InventoryEntityToPlayerInventorySettings);
								
								changed = true;
							}
						}

						if (changed) {
							StorageGUI.needRefresh = true;
							SoundEngine.PlaySound(SoundID.Grab);
						}
					};

					itemSlot.OnMouseOver += (evt, e) => {
						MagicStorageItemSlot obj = e as MagicStorageItemSlot;
						int objSlot = obj.slot + StorageGUI.numColumns * (int)Math.Round(scrollBar.ViewPosition);

						if (objSlot < StorageGUI.items.Count && !StorageGUI.items[objSlot].IsAir)
							StorageGUI.items[objSlot].newAndShiny = false;
					};

					itemSlot.OnRightClick += (evt, e) => {
						MagicStorageItemSlot obj = e as MagicStorageItemSlot;
						int objSlot = obj.slot + StorageGUI.numColumns * (int)Math.Round(scrollBar.ViewPosition);

						if (slot < StorageGUI.items.Count && (Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, StorageGUI.items[objSlot]) && Main.mouseItem.stack < Main.mouseItem.maxStack))
							StorageGUI.slotFocus = objSlot;
					};

					return itemSlot;
				};

				slotZone.Width.Set(0f, 1f);
				slotZone.Top.Set(76f, 0f);
				slotZone.Height.Set(-116f, 1f);
				Append(slotZone);

				scrollBar = new();
				scrollBar.Left.Set(-20f, 1f);
				Append(scrollBar);

				UIElement bottomBar = new();
				bottomBar.Width.Set(0f, 1f);
				bottomBar.Height.Set(32f, 0f);
				bottomBar.Top.Set(-32f, 1f);
				Append(bottomBar);

				capacityText = new UIText("Items");
				capacityText.Left.Set(6f, 0f);
				capacityText.Top.Set(6f, 0f);
				bottomBar.Append(capacityText);

				UpdateZone();
			}

			private void InitFilterButtons() {
				if (MagicStorageConfig.CraftingFavoritingEnabled) {
					if (filterFavorites.Parent is null)
						topBar.Append(filterFavorites);
				} else {
					filterFavorites.Remove();
				}
			}

			public override void Update(GameTime gameTime) {
				base.Update(gameTime);

				if (!Main.mouseRight)
					StorageGUI.ResetSlotFocus();

				if (StorageGUI.slotFocus >= 0)
					StorageGUI.SlotFocusLogic();

				if (lastKnownConfigFavorites != MagicStorageConfig.CraftingFavoritingEnabled) {
					InitFilterButtons();
					lastKnownConfigFavorites = MagicStorageConfig.CraftingFavoritingEnabled;
				}

				TEStorageHeart heart = StorageGUI.GetHeart();
				int numItems = 0;
				int capacity = 0;
				if (heart is not null) {
					foreach (TEAbstractStorageUnit abstractStorageUnit in heart.GetStorageUnits()) {
						if (abstractStorageUnit is TEStorageUnit storageUnit) {
							numItems += storageUnit.NumItems;
							capacity += storageUnit.Capacity;
						}
					}
				}

				capacityText.SetText(Language.GetTextValue("Mods.MagicStorage.Capacity", numItems, capacity));

				Player player = Main.LocalPlayer;

				StorageUIState parent = parentUI as StorageUIState;

				if (Main.mouseX > parent.PanelLeft && Main.mouseX < parent.PanelRight && Main.mouseY > parent.PanelTop && Main.mouseY < parent.PanelBottom) {
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}
			}

			private void UpdateZone() {
				float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * StorageGUI.inventoryScale;

				int numRows = (StorageGUI.items.Count + StorageGUI.numColumns - 1) / StorageGUI.numColumns;
				int displayRows = (int)slotZone.GetDimensions().Height / ((int)itemSlotHeight + StorageGUI.padding);
				slotZone.SetDimensions(StorageGUI.numColumns, displayRows);
				int noDisplayRows = numRows - displayRows;
				if (noDisplayRows < 0)
					noDisplayRows = 0;

				int scrollBarMaxViewSize = 1 + noDisplayRows;
				scrollBar.Height.Set(displayRows * (itemSlotHeight + StorageGUI.padding), 0f);
				scrollBar.SetView(StorageGUI.scrollBarViewSize, scrollBarMaxViewSize);
			}

			public void Refresh() {
				slotZone.SetItemsAndContexts(int.MaxValue, GetItem);
			}

			internal Item GetItem(int slot, ref int context)
			{
				int index = slot + StorageGUI.numColumns * (int)Math.Round(scrollBar.ViewPosition);
				Item item = index < StorageGUI.items.Count ? StorageGUI.items[index] : new Item();

				if (!item.IsAir && !StorageGUI.didMatCheck[index]) {
					item.checkMat();
					StorageGUI.didMatCheck[index] = true;
				}

				return item;
			}
		}
	}
}
