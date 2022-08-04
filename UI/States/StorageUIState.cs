using MagicStorage.Common.Systems;
using MagicStorage.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public sealed class StorageUIState : BaseStorageUI {
		public override string DefaultPage => "Storage";

		protected override IEnumerable<string> GetMenuOptions() {
			yield return "Storage";
			yield return "Sorting";
			yield return "Filtering";
			yield return "Controls";
		}

		protected override BaseStorageUIPage InitPage(string page)
			=> page switch {
				"Crafting" => new StoragePage(this),
				"Sorting" => new SortingPage(this),
				"Filtering" => new FilteringPage(this),
				"Controls" => new ControlsPage(this),
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

		public class ControlsPage : BaseStorageUIPage {
			public const int SellNoPrefixItems = 0;
			public const int SellAllExceptMostExpensive = 1;
			public const int SellAllExceptLeastExpensive = 2;

			public UITextPanel<LocalizedText> forceRefresh, compactCoins, deleteUnloaded;

			private UIList list;
			private UIScrollbar scroll;

			internal NewUISlotZone sellDuplicatesZone;
			public List<StorageUISellMenuToggleLabel> sellMenuLabels;

			public int SellMenuChoice { get; private set; }

			public ControlsPage(BaseStorageUI parent) : base(parent, "Controls") {
				OnPageSelected += () => SellMenuChoice = 0;
			}

			public override void OnInitialize() {
				list = new();
				list.SetPadding(0);
				list.Width.Set(-20, 1f);
				list.Height.Set(0, 0.9f);
				list.Left.Set(20, 0);
				list.Top.Set(0, 0.05f);
				Append(list);

				scroll = new();
				scroll.Width.Set(20, 0);
				scroll.Height.Set(0, 0.825f);
				scroll.Left.Set(0, 0.95f);
				scroll.Top.Set(0, 0.1f);
				scroll.SetView(viewSize: 1f, maxViewSize: 2f);

				list.SetScrollbar(scroll);
				list.Append(scroll);
				list.ListPadding = 10;

				float top = 0;

				forceRefresh = new(Language.GetText("Mods.MagicStorage.StorageGUI.ForceRefreshButton"));

				forceRefresh.OnClick += (evt, e) => StorageGUI.needRefresh = true;

				InitButtonEvents(forceRefresh);

				forceRefresh.Top.Set(top, 0f);
				list.Add(forceRefresh);

				top += forceRefresh.Height.Pixels + 6;

				compactCoins = new(Language.GetText("Mods.MagicStorage.StorageGUI.CompactCoinsButton"));

				compactCoins.OnClick += (evt, e) => {
					if (StoragePlayer.LocalPlayer.GetStorageHeart() is not TEStorageHeart heart)
						return;

					if (Main.netMode == NetmodeID.SinglePlayer)
						heart.compactCoins = true;
					else
						NetHelper.SendCoinCompactRequest(heart.Position);
				};

				InitButtonEvents(compactCoins);

				compactCoins.Top.Set(top, 0f);
				list.Add(compactCoins);

				top += compactCoins.Height.Pixels + 6;

				deleteUnloaded = new(Language.GetText("Mods.MagicStorage.StorageGUI.DestroyUnloadedButton"));

				deleteUnloaded.OnClick += (evt, e) => {
					if (StoragePlayer.LocalPlayer.GetStorageHeart() is not TEStorageHeart heart)
						return;

					heart.WithdrawManyAndDestroy(ModContent.ItemType<UnloadedItem>());
				};

				InitButtonEvents(deleteUnloaded);

				deleteUnloaded.Top.Set(top, 0f);
				list.Add(deleteUnloaded);

				top += deleteUnloaded.Height.Pixels + 6;

				UIText sellDuplicatesLabel = new(Language.GetText("Mods.MagicStorage.StorageGUI.SellDuplicatesHeader"), 1.1f);
				sellDuplicatesLabel.Top.Set(top, 0f);
				Append(sellDuplicatesLabel);

				UIHorizontalSeparator separator = new();
				separator.Top.Set(top + 30, 0f);
				separator.Width.Set(0, 1f);
				Append(separator);

				top += sellDuplicatesLabel.Height.Pixels + 40;

				string sellMenu = "Mods.MagicStorage.StorageGUI.SellDuplicatesMenu.";

				string[] names = new[] {
					Language.GetTextValue(sellMenu + "NoPrefix"),
					Language.GetTextValue(sellMenu + "KeepMostValue"),
					Language.GetTextValue(sellMenu + "KeepLeastValue")
				};

				sellMenuLabels = new();
				int index = 0;
				foreach (var name in names) {
					StorageUISellMenuToggleLabel label = new(name, index);

					label.OnClick += (evt, e) => {
						StorageUISellMenuToggleLabel obj = e as StorageUISellMenuToggleLabel;

						int whichIsOn = -1;

						foreach (var other in sellMenuLabels) {
							if (other.IsOn) {
								whichIsOn = other.Index;
								break;
							}
						}

						if (whichIsOn == -1) {
							//Set obj back to "on" since the choice didn't change
							obj.SetState(true);
						} else if (whichIsOn != obj.Index) {
							//Set all to "off" except for obj
							foreach (var other in sellMenuLabels) {
								if (other.Index != obj.Index)
									other.SetState(false);
							}
						}

						SellMenuChoice = obj.Index;
					};

					label.Top.Set(top, 0f);
					label.Height.Set(20, 0f);
					label.Width.Set(0, 0.6f);
					list.Append(label);

					sellMenuLabels.Add(label);

					top += label.Height.Pixels + 6;
					index++;
				}

				UITextPanel<LocalizedText> sellMenuButton = new(Language.GetText("Mods.MagiCStorage.StorageGUI.SellDuplicatesButton"));

				sellMenuButton.OnClick += (evt, e) => {
					if (StoragePlayer.LocalPlayer.GetStorageHeart() is not TEStorageHeart heart)
						return;

					if (Main.netMode == NetmodeID.SinglePlayer) {
						DoSell(heart, SellMenuChoice, out long coppersEarned, out var withdrawnItems);

						int sold = withdrawnItems.Values.Select(l => l.Count).Sum();

						if (sold > 0 && coppersEarned > 0) {
							// coins = [ platinum, gold, silver, copper ]
							int[] coins = Utils.CoinsSplit(coppersEarned);

							StringBuilder coinsReport = new();

							if (coins[3] > 0)
								coinsReport.Append($"[i/s{coins[3]}:{ItemID.PlatinumCoin}]");
							if (coins[2] > 0)
								coinsReport.Append($" [i/s{coins[2]}:{ItemID.GoldCoin}]");
							if (coins[1] > 0)
								coinsReport.Append($" [i/s{coins[1]}:{ItemID.SilverCoin}]");
							if (coins[0] > 0)
								coinsReport.Append($" [i/s{coins[0]}:{ItemID.CopperCoin}]");

							Main.NewText($"{sold} duplicates were sold for {coinsReport.ToString().Trim()}");

							if (coins[3] > 0)
								heart.DepositItem(new(ItemID.PlatinumCoin, coins[3]));
							if (coins[2] > 0)
								heart.DepositItem(new(ItemID.GoldCoin, coins[2]));
							if (coins[1] > 0)
								heart.DepositItem(new(ItemID.SilverCoin, coins[1]));
							if (coins[0] > 0)
								heart.DepositItem(new(ItemID.CopperCoin, coins[0]));

							heart.ResetCompactStage();
						} else if (sold > 0 && coppersEarned == 0)
							Main.NewText($"{sold} duplicates were destroyed due to having no value");
						else
							Main.NewText("No duplicates were sold");
					} else
						NetHelper.RequestDuplicateSelling(heart.Position, SellMenuChoice);
				};

				InitButtonEvents(sellMenuButton);

				sellMenuButton.Top.Set(top, 0f);
				list.Add(sellMenuButton);
			}

			private static void InitButtonEvents(UITextPanel<LocalizedText> button) {
				button.OnMouseOver += (evt, e) => (e as UIPanel).BackgroundColor = new Color(73, 94, 171);

				button.OnMouseOut += (evt, e) => (e as UIPanel).BackgroundColor = new Color(63, 82, 151) * 0.7f;
			}

			private delegate bool SelectDuplicate(ref SourcedItem item, ref SourcedItem check, out bool swapHappened);

			private class SourcedItem {
				public TEStorageUnit source;
				public Item item;
				public int indexInSource;

				public SourcedItem(TEStorageUnit source, Item item, int indexInSource) {
					this.source = source;
					this.item = item;
					this.indexInSource = indexInSource;
				}
			}

			private class DuplicateSellingContext {
				public SourcedItem keep;
				public List<SourcedItem> duplicates = new();
			}

			internal static void DoSell(TEStorageHeart heart, int sellOption, out long coppersEarned, out Dictionary<TEStorageUnit, List<int>> withdrawnItems) {
				SelectDuplicate itemEvaluation = sellOption switch {
					SellNoPrefixItems => SellNoPrefix,
					SellAllExceptMostExpensive => SellExceptMostExpensive,
					SellAllExceptLeastExpensive => SellExceptLeastExpensive,
					_ => throw new ArgumentOutOfRangeException(nameof(sellOption))
				};

				//Ignore Creative Storage Units
				IEnumerable<SourcedItem> items = heart.GetStorageUnits().OfType<TEStorageUnit>().SelectMany(s => s.GetItems().Select((i, index) => new SourcedItem(s, i, index)));

				//Filter duplicates to sell
				Dictionary<int, DuplicateSellingContext> duplicatesToSell = new();

				foreach (SourcedItem sourcedItem in items) {
					Item item = sourcedItem.item;

					if (item.IsAir || item.maxStack > 1)  //Only sell duplicates of unstackables
						continue;

					if (!duplicatesToSell.TryGetValue(item.type, out var context))
						context = duplicatesToSell[item.type] = new() { keep = sourcedItem };
					else if (Utility.AreStrictlyEqual(context.keep.item, item)) {
						SourcedItem check = sourcedItem;

						if (itemEvaluation(ref context.keep, ref check, out bool swapHappened)) {
							if (swapHappened)
								context.duplicates.Remove(context.keep);

							context.duplicates.Add(check);
						}
					}
				}

				//Sell the duplicates
				NPC dummy = new();

				HashSet<Point16> unitsUpdated = new();
				withdrawnItems = new();

				int platinum = 0, gold = 0, silver = 0, copper = 0;

				foreach (SourcedItem duplicate in duplicatesToSell.Values.Where(c => c.duplicates.Count > 0).SelectMany(c => c.duplicates)) {
					if (!PlayerLoader.CanSellItem(Main.LocalPlayer, dummy, Array.Empty<Item>(), duplicate.item))
						continue;

					int value = duplicate.item.value;

					// coins = [ platinum, gold, silver, copper ]
					int[] coins = Utils.CoinsSplit(value);
					platinum += coins[3];
					gold += coins[2];
					silver += coins[1];
					copper += coins[0];

					if (unitsUpdated.Add(duplicate.source.Position))
						withdrawnItems.Add(duplicate.source, new());

					withdrawnItems[duplicate.source].Add(duplicate.indexInSource);

					duplicate.source.items.Remove(duplicate.item);

					PlayerLoader.PostSellItem(Main.LocalPlayer, dummy, Array.Empty<Item>(), duplicate.item);
				}

				coppersEarned = platinum * 1000000L + gold * 10000 + silver * 100 + copper;
			}

			private static bool SellNoPrefix(ref SourcedItem item, ref SourcedItem check, out bool swapHappened) {
				swapHappened = false;
				return check.item.prefix == 0;
			}

			private static bool SellExceptMostExpensive(ref SourcedItem item, ref SourcedItem check, out bool swapHappened) {
				swapHappened = false;

				if (check.item.value > item.item.value) {
					Utils.Swap(ref item, ref check);
					swapHappened = true;
				}

				return true;
			}

			private static bool SellExceptLeastExpensive(ref SourcedItem item, ref SourcedItem check, out bool swapHappened) {
				swapHappened = false;

				if (check.item.value < item.item.value) {
					Utils.Swap(ref item, ref check);
					swapHappened = true;
				}

				return true;
			}
		}
	}
}
