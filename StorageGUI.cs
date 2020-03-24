using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using MagicStorage.Components;
using MagicStorage.Sorting;
using Terraria.ID;

namespace MagicStorage
{
	public static class StorageGUI
	{
		private const int padding = 4;
		private const int numColumns = 10;
		public const float inventoryScale = 0.85f;

		public static MouseState curMouse;
		public static MouseState oldMouse;
		public static bool MouseClicked
		{
			get
			{
				return curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
			}
		}

		private static UIPanel basePanel = new UIPanel();
		private static float panelTop;
		private static float panelLeft;
		private static float panelWidth;
		private static float panelHeight;

		private static UIElement topBar = new UIElement();
		internal static UISearchBar searchBar;
		private static UIButtonChoice sortButtons;
		private static UIToggleButton favoritedOnlyButton;
		internal static UITextPanel<LocalizedText> depositButton;
		internal static UITextPanel<LocalizedText> restockButton;
		private static UIElement topBar2 = new UIElement();
		private static UIButtonChoice filterButtons;

	    public static readonly ModSearchBox modSearchBox = new ModSearchBox(RefreshItems);

        private static UISlotZone slotZone = new UISlotZone(HoverItemSlot, GetItem, inventoryScale);
		private static int slotFocus = -1;
		private static int rightClickTimer = 0;
		private const int startMaxRightClickTimer = 20;
		private static int maxRightClickTimer = startMaxRightClickTimer;

		internal static UIScrollbar scrollBar = new UIScrollbar();
		private static bool scrollBarFocus = false;
		private static int scrollBarFocusMouseStart;
		private static float scrollBarFocusPositionStart;
		private static float scrollBarViewSize = 1f;
		private static float scrollBarMaxViewSize = 2f;

		private static List<Item> items = new List<Item>();
		private static List<bool> didMatCheck = new List<bool>();
		private static int numRows;
		private static int displayRows;

		private static UIElement bottomBar = new UIElement();
		private static UIText capacityText;
        
		public static void Initialize()
		{
			InitLangStuff();
			float itemSlotWidth = Main.inventoryBackTexture.Width * inventoryScale;
			float itemSlotHeight = Main.inventoryBackTexture.Height * inventoryScale;

			panelTop = Main.instance.invBottom + 60;
			panelLeft = 20f;
			float innerPanelLeft = panelLeft + basePanel.PaddingLeft;
			float innerPanelWidth = numColumns * (itemSlotWidth + padding) + 20f + padding;
			panelWidth = basePanel.PaddingLeft + innerPanelWidth + basePanel.PaddingRight;
			panelHeight = Main.screenHeight - panelTop - 40f;
			basePanel.Left.Set(panelLeft, 0f);
			basePanel.Top.Set(panelTop, 0f);
			basePanel.Width.Set(panelWidth, 0f);
			basePanel.Height.Set(panelHeight, 0f);
			basePanel.Recalculate();

			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			basePanel.Append(topBar);

			InitSortButtons();
			topBar.Append(sortButtons);

		    var x = sortButtons.GetDimensions().Width + 2 * padding;
            favoritedOnlyButton.Left.Set(x, 0f);
		    topBar.Append(favoritedOnlyButton);

		    x += favoritedOnlyButton.GetDimensions().Width + 2 * padding;

		    depositButton.Left.Set(x, 0f);
			depositButton.Width.Set(128f, 0f);
			depositButton.Height.Set(-2 * padding, 1f);
			depositButton.PaddingTop = 8f;
			depositButton.PaddingBottom = 8f;
			topBar.Append(depositButton);

		    x += depositButton.GetDimensions().Width;
            
            float depositButtonRight = x;
			searchBar.Left.Set(depositButtonRight + padding, 0f);
			searchBar.Width.Set(-depositButtonRight - 2 * padding, 1f);
			searchBar.Height.Set(0f, 1f);
			topBar.Append(searchBar);

			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);
			basePanel.Append(topBar2);

			InitFilterButtons();

		    float filterButtonsRight = filterButtons.GetDimensions().Width + padding;
		    topBar2.Append(filterButtons);

		    modSearchBox.Button.Left.Set(filterButtonsRight + padding, 0f);
		    modSearchBox.Button.Width.Set(-filterButtonsRight - 2 * padding, 1f);
		    modSearchBox.Button.Height.Set(0f, 1f);
		    modSearchBox.Button.OverflowHidden = true;
		    topBar2.Append(modSearchBox.Button);

			slotZone.Width.Set(0f, 1f);
			slotZone.Top.Set(76f, 0f);
			slotZone.Height.Set(-116f, 1f);
			basePanel.Append(slotZone);

			numRows = (items.Count + numColumns - 1) / numColumns;
			displayRows = (int)slotZone.GetDimensions().Height / ((int)itemSlotHeight + padding);
			slotZone.SetDimensions(numColumns, displayRows);
			int noDisplayRows = numRows - displayRows;
			if (noDisplayRows < 0)
			{
				noDisplayRows = 0;
			}
			scrollBarMaxViewSize = 1 + noDisplayRows;
			scrollBar.Height.Set(displayRows * (itemSlotHeight + padding), 0f);
			scrollBar.Left.Set(-20f, 1f);
			scrollBar.SetView(scrollBarViewSize, scrollBarMaxViewSize);
			slotZone.Append(scrollBar);

			bottomBar.Width.Set(0f, 1f);
			bottomBar.Height.Set(32f, 0f);
			bottomBar.Top.Set(-32f, 1f);
			basePanel.Append(bottomBar);

			capacityText.Left.Set(6f, 0f);
			capacityText.Top.Set(6f, 0f);
			TEStorageHeart heart = GetHeart();
			int numItems = 0;
			int capacity = 0;
			if (heart != null)
			{
				foreach (TEAbstractStorageUnit abstractStorageUnit in heart.GetStorageUnits())
				{
					if (abstractStorageUnit is TEStorageUnit)
					{
						TEStorageUnit storageUnit = (TEStorageUnit)abstractStorageUnit;
						numItems += storageUnit.NumItems;
						capacity += storageUnit.Capacity;
					}
				}
			}
			capacityText.SetText(numItems + "/" + capacity + " Items");
			bottomBar.Append(capacityText);

        }

		private static void InitLangStuff()
		{
			if (depositButton == null)
			{
				depositButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.DepositAll"), 1f);
			}
			if (searchBar == null)
			{
				searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), RefreshItems);
			}
            modSearchBox.InitLangStuff();
			if (capacityText == null)
			{
				capacityText = new UIText("Items");
			}
		}

		private static void InitSortButtons()
		{
			if (sortButtons == null)
			{
				sortButtons = GUIHelpers.MakeSortButtons(StorageGUI.RefreshItems);
			}
			if (favoritedOnlyButton == null)
			{
			    favoritedOnlyButton = new UIToggleButton(RefreshItems, MagicStorage.Instance.GetTexture("FilterMisc"), Language.GetText("Mods.MagicStorage.ShowOnlyFavorited"));
			}
		}

	    private static void InitFilterButtons()
		{
			if (filterButtons == null)
			{
			    filterButtons = GUIHelpers.MakeFilterButtons(true, StorageGUI.RefreshItems);
			}
		}

	    public static void Update(GameTime gameTime)
		{
			oldMouse = curMouse;
			curMouse = Mouse.GetState();
			if (Main.playerInventory && Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>().ViewingStorage().X >= 0 && !StoragePlayer.IsStorageCrafting())
			{
				if (StorageGUI.curMouse.RightButton == ButtonState.Released)
				{
					ResetSlotFocus();
				}
				if(basePanel != null)
					basePanel.Update(gameTime);
				UpdateScrollBar();
				UpdateDepositButton();
                modSearchBox.Update(curMouse, oldMouse);
			}
			else
			{
				scrollBarFocus = false;
				ResetSlotFocus();
			}
		}

		public static void Draw(TEStorageHeart heart)
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			Initialize();
			if (Main.mouseX > panelLeft && Main.mouseX < panelLeft + panelWidth && Main.mouseY > panelTop && Main.mouseY < panelTop + panelHeight)
			{
				player.mouseInterface = true;
				player.showItemIcon = false;
				InterfaceHelper.HideItemIconCache();
			}
			basePanel.Draw(Main.spriteBatch);
			slotZone.DrawText();
			sortButtons.DrawText();
			favoritedOnlyButton.DrawText();
			filterButtons.DrawText();
            DrawDepositButton();
		}

		private static Item GetItem(int slot, ref int context)
		{
			int index = slot + numColumns * (int)Math.Round(scrollBar.ViewPosition);
			Item item = index < items.Count ? items[index] : new Item();
			if (!item.IsAir && !didMatCheck[index])
			{
				item.checkMat();
				didMatCheck[index] = true;
			}
			return item;
		}

		private static void UpdateScrollBar()
		{
			if (slotFocus >= 0)
			{
				scrollBarFocus = false;
				return;
			}
			Rectangle dim = scrollBar.GetClippingRectangle(Main.spriteBatch);
			Vector2 boxPos = new Vector2(dim.X, dim.Y + dim.Height * (scrollBar.ViewPosition / scrollBarMaxViewSize));
			float boxWidth = 20f * Main.UIScale;
			float boxHeight = dim.Height * (scrollBarViewSize / scrollBarMaxViewSize);
			if (scrollBarFocus)
			{
				if (curMouse.LeftButton == ButtonState.Released)
				{
					scrollBarFocus = false;
				}
				else
				{
					int difference = curMouse.Y - scrollBarFocusMouseStart;
					scrollBar.ViewPosition = scrollBarFocusPositionStart + (float)difference / boxHeight;
				}
			}
			else if (MouseClicked)
			{
				if (curMouse.X > boxPos.X && curMouse.X < boxPos.X + boxWidth && curMouse.Y > boxPos.Y - 3f && curMouse.Y < boxPos.Y + boxHeight + 4f)
				{
					scrollBarFocus = true;
					scrollBarFocusMouseStart = curMouse.Y;
					scrollBarFocusPositionStart = scrollBar.ViewPosition;
				}
			}
			if (!scrollBarFocus)
			{
				int difference = oldMouse.ScrollWheelValue / 250 - curMouse.ScrollWheelValue / 250;
				scrollBar.ViewPosition += difference;
			}
		}

		private static TEStorageHeart GetHeart()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			return modPlayer.GetStorageHeart();
		}

	    public static void RefreshItems()
	    {
	        if (StoragePlayer.IsStorageCrafting())
	        {
	            CraftingGUI.RefreshItems();
	            return;
	        }

	        items.Clear();
	        didMatCheck.Clear();
	        TEStorageHeart heart = GetHeart();
	        if (heart == null)
	        {
	            return;
	        }

	        InitLangStuff();
	        InitSortButtons();
	        InitFilterButtons();
	        SortMode sortMode = (SortMode) sortButtons.Choice;

	        FilterMode filterMode = (FilterMode) filterButtons.Choice;
            var modFilterIndex = modSearchBox.ModIndex;

	        Action doFiltering = () =>
	        {
	            IEnumerable<Item> itemsLocal;
	            if (filterMode == FilterMode.Recent)
	            {
	                var stored = heart.GetStoredItems().GroupBy(x => x.type).ToDictionary(x => x.Key, x => x.First());

	                var toFilter = heart.UniqueItemsPutHistory.Reverse().Where(x => stored.ContainsKey(x.type)).Select(x => stored[x.type]);
	                itemsLocal = ItemSorter.SortAndFilter(toFilter, sortMode == SortMode.Default ? SortMode.AsIs : sortMode,
	                    FilterMode.All, modFilterIndex, searchBar.Text, 100);
	            }
	            else
	                itemsLocal = ItemSorter.SortAndFilter(heart.GetStoredItems(), sortMode, filterMode, modFilterIndex, searchBar.Text)
	                    .OrderBy(x => x.favorited ? 0 : 1);

	            items.AddRange(itemsLocal.Where(x => !favoritedOnlyButton.Value || x.favorited));
	        };

	        doFiltering();

	        // now if nothing found we disable filters one by one
	        if (searchBar.Text.Trim().Length > 0)
	        {
	            if (items.Count == 0 && filterMode != FilterMode.All)
	            {
                    // search all categories
	                filterMode = FilterMode.All;
	                doFiltering();
	            }
                
	            if (items.Count == 0 && modFilterIndex != ModSearchBox.ModIndexAll)
	            {
	                // search all mods
	                modFilterIndex = ModSearchBox.ModIndexAll;
	                doFiltering();
	            }
	        }

	        for (int k = 0; k < items.Count; k++)
	        {
	            didMatCheck.Add(false);
	        }
	    }

	    private static void UpdateDepositButton()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(depositButton);
			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
			{
				depositButton.BackgroundColor = new Color(73, 94, 171);
				if (MouseClicked)
				{
					if (TryDepositAll(!Main.keyState.IsKeyDown(Keys.LeftControl) && !Main.keyState.IsKeyDown(Keys.RightControl)))
					{
						RefreshItems();
						Main.PlaySound(7, -1, -1, 1);
					}
				}
                else if (CraftingGUI.RightMouseClicked)
				{
				    if (TryRestock())
				    {
				        RefreshItems();
				        Main.PlaySound(7, -1, -1, 1);
				    }
                }
			}
			else
			{
				depositButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
			}
		}
        
		private static void DrawDepositButton()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(depositButton);
            if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
            {
                Main.instance.MouseText(Language.GetText("Mods.MagicStorage.DepositTooltip").Value);
            }
		}

		private static void ResetSlotFocus()
		{
			slotFocus = -1;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		private static void HoverItemSlot(int slot, ref int hoverSlot)
		{
			Player player = Main.player[Main.myPlayer];
			int visualSlot = slot;
			slot += numColumns * (int)Math.Round(scrollBar.ViewPosition);
            
            if (MouseClicked)
			{
				bool changed = false;
				if (!Main.mouseItem.IsAir && (player.itemAnimation == 0 && player.itemTime == 0))
				{
					if (TryDeposit(Main.mouseItem))
					{
						changed = true;
					}
				}
				else if (Main.mouseItem.IsAir && slot < items.Count && !items[slot].IsAir)
				{
				    if (Main.keyState.IsKeyDown(Keys.LeftAlt))
				    {
				        if (Main.netMode == NetmodeID.SinglePlayer)
				            items[slot].favorited = !items[slot].favorited;
				        else
				            Main.NewTextMultiline("TOggling item as favorite is not implemented in multiplayer but you can withdraw this item, toggle it in inventory and deposit again");
                        // there is no item instance id and there is no concept of slot # in heart so we can't send this in operation
                        // a workaropund would be to withdraw and deposit it back with changed favorite flag
                        // but it still might look ugly for the player that initiates operation
				    }
				    else
                    {
                        Item toWithdraw = items[slot].Clone();
                        if (toWithdraw.stack > toWithdraw.maxStack)
                        {
                            toWithdraw.stack = toWithdraw.maxStack;
                        }
                        Main.mouseItem = DoWithdraw(toWithdraw, ItemSlot.ShiftInUse);
                        if (ItemSlot.ShiftInUse)
                        {
                            Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, false, true);
                        }
                        changed = true;
                    }
				}
				if (changed)
				{
					RefreshItems();
					Main.PlaySound(7, -1, -1, 1);
				}
			}

			if (curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released && slot < items.Count && (Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, items[slot]) && Main.mouseItem.stack < Main.mouseItem.maxStack))
			{
				slotFocus = slot;
			}
			
			if (slot < items.Count && !items[slot].IsAir)
			{
				hoverSlot = visualSlot;
                items[slot].newAndShiny = false;

            }

			if (slotFocus >= 0)
			{
				SlotFocusLogic();
			}
		}

		private static void SlotFocusLogic()
		{
			if (slotFocus >= items.Count || (!Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, items[slotFocus]) || Main.mouseItem.stack >= Main.mouseItem.maxStack)))
			{
				ResetSlotFocus();
			}
			else
			{
				if (rightClickTimer <= 0)
				{
					rightClickTimer = maxRightClickTimer;
					maxRightClickTimer = maxRightClickTimer * 3 / 4;
					if (maxRightClickTimer <= 0)
					{
						maxRightClickTimer = 1;
					}
					Item toWithdraw = items[slotFocus].Clone();
					toWithdraw.stack = 1;
					Item result = DoWithdraw(toWithdraw);
					if (Main.mouseItem.IsAir)
					{
						Main.mouseItem = result;
					}
					else
					{
						Main.mouseItem.stack += result.stack;
					}
					Main.soundInstanceMenuTick.Stop();
					Main.soundInstanceMenuTick = Main.soundMenuTick.CreateInstance();
					Main.PlaySound(12, -1, -1, 1);
					RefreshItems();
				}
				rightClickTimer--;
			}
		}

		private static bool TryDeposit(Item item)
		{
			int oldStack = item.stack;
			DoDeposit(item);
			return oldStack != item.stack;
		}

		private static void DoDeposit(Item item)
		{
			TEStorageHeart heart = GetHeart();
			if (Main.netMode == 0)
			{
				heart.DepositItem(item);
			}
			else
			{
				NetHelper.SendDeposit(heart.ID, item);
				item.SetDefaults(0, true);
			}
		}

		private static bool TryDepositAll(bool quickStack)
		{
			Player player = Main.player[Main.myPlayer];
			TEStorageHeart heart = GetHeart();
			bool changed = false;
		    Predicate<Item> filter = item => !item.IsAir && !item.favorited && (!quickStack || heart.HasItem(item, true));
			if (Main.netMode == 0)
			{
				for (int k = 10; k < 50; k++)
				{
				    var item = player.inventory[k];
				    if (filter(item))
					{
						int oldStack = item.stack;
                        heart.DepositItem(item);
						if (oldStack != item.stack)
						{
							changed = true;
						}
					}
				}
			}
			else
			{
				List<Item> items = new List<Item>();
				for (int k = 10; k < 50; k++)
				{
				    var item = player.inventory[k];
					if (filter(item))
					{
					    items.Add(item);
					}
				}
				NetHelper.SendDepositAll(heart.ID, items);
				foreach (Item item in items)
				{
					item.SetDefaults(0, true);
				}
				changed = true;
			}
			return changed;
		}

        private static bool TryRestock()
        {
            Player player = Main.player[Main.myPlayer];
            TEStorageHeart heart = GetHeart();
            bool changed = false;

            foreach (var item in player.inventory)
            {
                if (item != null && !item.IsAir && item.stack < item.maxStack)
                {
                    var toWithdraw = item.Clone();
                    toWithdraw.stack = item.maxStack - item.stack;
                    toWithdraw = DoWithdraw(toWithdraw, true, true);
                    if (!toWithdraw.IsAir)
                    {
                        item.stack += toWithdraw.stack;
                        toWithdraw.TurnToAir();
                        changed = true;
                    }
                }

            }
            return changed;
        }

		private static Item DoWithdraw(Item item, bool toInventory = false, bool keepOneIfFavorite = false)
		{
			TEStorageHeart heart = GetHeart();
			if (Main.netMode == 0)
			{
				return heart.TryWithdraw(item, keepOneIfFavorite);
			}
			else
			{
				NetHelper.SendWithdraw(heart.ID, item, toInventory, keepOneIfFavorite);
				return new Item();
			}
		}

	}
}
