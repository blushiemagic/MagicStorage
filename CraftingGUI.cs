using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;
using MagicStorage.Components;
using MagicStorage.Sorting;

namespace MagicStorage
{
    public static class CraftingGUI
	{
	    const int RecipeButtonsNewChoice = 0;
        const int RecipeButtonsBlacklistChoice = 3;
        const int RecipeButtonsFavoritesChoice = 2;
        private const int padding = 4;
		private const int numColumns = 10;
		private const int numColumns2 = 7;
		private const float inventoryScale = 0.85f;
		private const float smallScale = 0.7f;

	    static HashSet<int> threadCheckListFoundItems;
        static Mod _checkListMod;
	    static volatile bool wasItemChecklistRetrieved;

		public static MouseState curMouse;
		public static MouseState oldMouse;
		public static bool MouseClicked
		{
			get
			{
				return curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;
			}
		}

	    public static bool RightMouseClicked
		{
			get
			{
				return curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;
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
		private static UIButtonChoice recipeButtons;
		private static UIElement topBar2 = new UIElement();
		private static UIButtonChoice filterButtons;
	    internal static UISearchBar searchBar2;
		
		private static UIText stationText;
	    private static UISlotZone stationZone = new UISlotZone(HoverStation, GetStation, inventoryScale);
	    private static UIText recipeText;
		private static UISlotZone recipeZone = new UISlotZone(HoverRecipe, GetRecipe, inventoryScale);

		internal static UIScrollbar scrollBar = new UIScrollbar();
		private static int scrollBarFocus = 0;
		private static int scrollBarFocusMouseStart;
		private static float scrollBarFocusPositionStart;
		private static float scrollBarViewSize = 1f;
		private static float scrollBarMaxViewSize = 2f;

		private static List<Item> items = new List<Item>();
		private static Dictionary<int, int> itemCounts = new Dictionary<int, int>();
		private static bool[] adjTiles = new bool[TileLoader.TileCount];
		private static bool adjWater = false;
		private static bool adjLava = false;
		private static bool adjHoney = false;
		private static bool zoneSnow = false;
		private static bool alchemyTable = false;
		private static List<Recipe> recipes = new List<Recipe>();
		private static List<bool> recipeAvailable = new List<bool>();
		private static Recipe selectedRecipe = null;
		private static int numRows;
		private static int displayRows;
		private static bool slotFocus = false;

		private static UIElement bottomBar = new UIElement();
		private static UIText capacityText;

		private static UIPanel recipePanel = new UIPanel();
		
		private static float recipeTop;
		private static float recipeLeft;
		private static float recipeWidth;
		private static float recipeHeight;

        private static UIText recipePanelHeader;
		private static UIText ingredientText;
		private static UISlotZone ingredientZone = new UISlotZone(HoverItem, GetIngredient, smallScale);
		private static UISlotZone recipeHeaderZone = new UISlotZone(HoverHeader, GetHeader, smallScale);
		private static UIText reqObjText;
		private static UIText reqObjText2;
		private static UIText storedItemsText;

		private static UISlotZone storageZone = new UISlotZone(HoverStorage, GetStorage, smallScale);
		private static int numRows2;
		private static int displayRows2;
		private static List<Item> storageItems = new List<Item>();
		private static List<ItemData> blockStorageItems = new List<ItemData>();

		internal static UIScrollbar scrollBar2 = new UIScrollbar();
		private static float scrollBar2ViewSize = 1f;
		private static float scrollBar2MaxViewSize = 2f;

		internal static UITextPanel<LocalizedText> craftButton;
	    private static Item result = null;
		private static UISlotZone resultZone = new UISlotZone(HoverResult, GetResult, inventoryScale);
		private static int craftTimer = 0;
		private const int startMaxCraftTimer = 20;
		private static int maxCraftTimer = startMaxCraftTimer;
		private static int rightClickTimer = 0;
		private const int startMaxRightClickTimer = 20;
	    
	    private static int maxRightClickTimer = startMaxRightClickTimer;

		private static Object threadLock = new Object();
		private static Object recipeLock = new Object();
		private static Object itemsLock = new Object();
		private static bool threadRunning = false;
		internal static bool threadNeedsRestart = false;
		private static SortMode threadSortMode;
		private static FilterMode threadFilterMode;
		private static List<Recipe> threadRecipes = new List<Recipe>();
		private static List<bool> threadRecipeAvailable = new List<bool>();
		private static List<Recipe> nextRecipes = new List<Recipe>();
		private static List<bool> nextRecipeAvailable = new List<bool>();
        
		public static void Initialize()
		{
			lock (recipeLock)
			{
				recipes = nextRecipes;
				recipeAvailable = nextRecipeAvailable;
			}
            
		    InitLangStuff();
			float itemSlotWidth = Main.inventoryBackTexture.Width * inventoryScale;
			float itemSlotHeight = Main.inventoryBackTexture.Height * inventoryScale;
			float smallSlotWidth = Main.inventoryBackTexture.Width * smallScale;
			float smallSlotHeight = Main.inventoryBackTexture.Height * smallScale;

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

			recipeTop = panelTop;
			recipeLeft = panelLeft + panelWidth;
			recipeWidth = numColumns2 * (smallSlotWidth + padding) + 20f + padding;
			recipeWidth += recipePanel.PaddingLeft + recipePanel.PaddingRight;
			recipeHeight = panelHeight;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);
			recipePanel.Width.Set(recipeWidth, 0f);
			recipePanel.Height.Set(recipeHeight, 0f);
			recipePanel.Recalculate();

			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			basePanel.Append(topBar);

			InitSortButtons();
			topBar.Append(sortButtons);
			float sortButtonsRight = sortButtons.GetDimensions().Width + padding;
			InitRecipeButtons();
			float recipeButtonsLeft = sortButtonsRight + 3 * padding;
			recipeButtons.Left.Set(recipeButtonsLeft, 0f);
			topBar.Append(recipeButtons);
			float recipeButtonsRight = recipeButtonsLeft + recipeButtons.GetDimensions().Width + padding;

			searchBar.Left.Set(recipeButtonsRight + padding, 0f);
			searchBar.Width.Set(-recipeButtonsRight - 2 * padding, 1f);
			searchBar.Height.Set(0f, 1f);
			topBar.Append(searchBar);

			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);
			basePanel.Append(topBar2);

			InitFilterButtons();
			float filterButtonsRight = filterButtons.GetDimensions().Width + padding;
            topBar2.Append(filterButtons);
            searchBar2.Left.Set(filterButtonsRight + padding, 0f);
		    searchBar2.Width.Set(-filterButtonsRight - 2 * padding, 1f);
		    searchBar2.Height.Set(0f, 1f);
		    topBar2.Append(searchBar2);

			stationText.Top.Set(76f, 0f);
			basePanel.Append(stationText);

			stationZone.Width.Set(0f, 1f);
			stationZone.Top.Set(100f, 0f);
			stationZone.Height.Set(90f, 0f);
		    stationZone.SetDimensions(numColumns, 1);
			basePanel.Append(stationZone);
            
		    recipeText.Top.Set(152f, 0f);
		    basePanel.Append(recipeText);

			recipeZone.Width.Set(0f, 1f);
			recipeZone.Top.Set(176f, 0f);
			recipeZone.Height.Set(-216f, 1f);
			basePanel.Append(recipeZone);

			numRows = (recipes.Count + numColumns - 1) / numColumns;
			displayRows = (int)recipeZone.GetDimensions().Height / ((int)itemSlotHeight + padding);
			recipeZone.SetDimensions(numColumns, displayRows);
			int noDisplayRows = numRows - displayRows;
			if (noDisplayRows < 0)
			{
				noDisplayRows = 0;
			}
			scrollBarMaxViewSize = 1 + noDisplayRows;
			scrollBar.Height.Set(displayRows * (itemSlotHeight + padding), 0f);
			scrollBar.Left.Set(-20f, 1f);
			scrollBar.SetView(scrollBarViewSize, scrollBarMaxViewSize);
			recipeZone.Append(scrollBar);

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

            recipePanelHeader.Left.Set(60, 0f);
            recipePanel.Append(recipePanelHeader);

		    ingredientText.Top.Set(30f, 0f);
		    ingredientText.Left.Set(60, 0f);

            recipeHeaderZone.SetDimensions(1, 1);
		    recipePanel.Append(recipeHeaderZone);
		    
            recipePanel.Append(ingredientText);

			ingredientZone.SetDimensions(numColumns2, 2);
			ingredientZone.Top.Set(54f, 0f);
			ingredientZone.Width.Set(0f, 1f);
			ingredientZone.Height.Set(60f, 0f);
			recipePanel.Append(ingredientZone);

			reqObjText.Top.Set(136f, 0f);
			recipePanel.Append(reqObjText);
			reqObjText2.Top.Set(160f, 0f);
			recipePanel.Append(reqObjText2);
			storedItemsText.Top.Set(190f, 0f);
			recipePanel.Append(storedItemsText);

			storageZone.Top.Set(214f, 0f);
			storageZone.Width.Set(0f, 1f);
			storageZone.Height.Set(-214f - 36, 1f);
			recipePanel.Append(storageZone);
			numRows2 = (storageItems.Count + numColumns2 - 1) / numColumns2;
			displayRows2 = (int)storageZone.GetDimensions().Height / ((int)smallSlotHeight + padding);
			storageZone.SetDimensions(numColumns2, displayRows2);
			int noDisplayRows2 = numRows2 - displayRows2;
			if (noDisplayRows2 < 0)
			{
				noDisplayRows2 = 0;
			}
			scrollBar2MaxViewSize = 1 + noDisplayRows2;
			scrollBar2.Height.Set(displayRows2 * (smallSlotHeight + padding), 0f);
			scrollBar2.Left.Set(-20f, 1f);
			scrollBar2.SetView(scrollBar2ViewSize, scrollBar2MaxViewSize);
			storageZone.Append(scrollBar2);
            
			craftButton.Top.Set(-32f, 1f);
			craftButton.Width.Set(100f, 0f);
			craftButton.Height.Set(24f, 0f);
			craftButton.PaddingTop = 8f;
			craftButton.PaddingBottom = 8f;
			recipePanel.Append(craftButton);
            
			resultZone.SetDimensions(1, 1);
			resultZone.Left.Set(-itemSlotWidth, 1f);
			resultZone.Top.Set(-itemSlotHeight, 1f);
			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);
			recipePanel.Append(resultZone);
		}

		private static void InitLangStuff()
		{
			if (searchBar == null)
			{
				searchBar = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), RefreshItems);
			}
		    if (searchBar2 == null)
		    {
		        searchBar2 = new UISearchBar(Language.GetText("Mods.MagicStorage.SearchMod"), RefreshItems);
		    }
			if (stationText == null)
			{
				stationText = new UIText(Language.GetText("Mods.MagicStorage.CraftingStations"));
			}
		    if (recipeText == null)
		    {
		        recipeText = new UIText(Language.GetText("Mods.MagicStorage.Recipes"));
		    }
			if (capacityText == null)
			{
				capacityText = new UIText("Items");
			}
			if (recipePanelHeader == null)
			{
				recipePanelHeader = new UIText(Language.GetText("Mods.MagicStorage.SelectedRecipe"));
			}
			if (ingredientText == null)
			{
				ingredientText = new UIText(Language.GetText("Mods.MagicStorage.Ingredients"));
			}
			if (reqObjText == null)
			{
				reqObjText = new UIText(Language.GetText("LegacyInterface.22"));
			}
			if (reqObjText2 == null)
			{
				reqObjText2 = new UIText("");
			}
			if (storedItemsText == null)
			{
				storedItemsText = new UIText(Language.GetText("Mods.MagicStorage.StoredItems"));
			}
			if (craftButton == null)
			{
				craftButton = new UITextPanel<LocalizedText>(Language.GetText("LegacyMisc.72"), 1f);
			}
		}

		private static void InitSortButtons()
		{
			if (sortButtons == null)
			{
                sortButtons = GUIHelpers.MakeSortButtons(CraftingGUI.RefreshItems);
			}
		}

		private static void InitRecipeButtons()
		{
			if (recipeButtons == null)
			{
			    recipeButtons = new UIButtonChoice(CraftingGUI.RefreshItems, new Texture2D[]
				{
					MagicStorage.Instance.GetTexture("RecipeAvailable"),
					MagicStorage.Instance.GetTexture("RecipeAll"),
				    MagicStorage.Instance.GetTexture("FilterMisc"),
                    MagicStorage.Instance.GetTexture("RecipeAll"),
                },
				new LocalizedText[]
				{
					Language.GetText("Mods.MagicStorage.RecipeAvailable"),
					Language.GetText("Mods.MagicStorage.RecipeAll"),
				    Language.GetText("Mods.MagicStorage.ShowOnlyFavorited"),
                    Language.GetText("Mods.MagicStorage.RecipeBlacklist"),
				    
                }) { Choice = 1 };
			}
		}

		private static void InitFilterButtons()
		{
			if (filterButtons == null)
			{
                filterButtons = GUIHelpers.MakeFilterButtons(false, CraftingGUI.RefreshItems);
			}
		}

		public static void Update(GameTime gameTime)
		{
		    try {
			oldMouse = StorageGUI.oldMouse;
			curMouse = StorageGUI.curMouse;
			if (Main.playerInventory && Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>(MagicStorage.Instance).ViewingStorage().X >= 0 && StoragePlayer.IsStorageCrafting())
			{
				if (curMouse.RightButton == ButtonState.Released)
				{
					ResetSlotFocus();
				}
				
				if (basePanel != null)
				{
					basePanel.Update(gameTime);
				}
				recipePanel.Update(gameTime);
				UpdateRecipeText();
				UpdateScrollBar();
				UpdateCraftButton();
			}
			else
			{
				scrollBarFocus = 0;
				craftTimer = 0;
				maxCraftTimer = startMaxCraftTimer;
				ResetSlotFocus();
			}}catch(Exception e){Main.NewTextMultiline(e.ToString());}
		}

	    public static void Draw(TEStorageHeart heart)
	    {
	        try
	        {
	            Player player = Main.player[Main.myPlayer];
	            Initialize();
	            if (Main.mouseX > panelLeft && Main.mouseX < recipeLeft + panelWidth && Main.mouseY > panelTop && Main.mouseY < panelTop + panelHeight)
	            {
	                player.mouseInterface = true;
	                player.showItemIcon = false;
	                InterfaceHelper.HideItemIconCache();
	            }

	            basePanel.Draw(Main.spriteBatch);
	            recipePanel.Draw(Main.spriteBatch);
	            Vector2 pos = recipeZone.GetDimensions().Position();
	            if (threadRunning)
	            {
	                Utils.DrawBorderString(Main.spriteBatch, "Loading", pos + new Vector2(8f, 8f), Color.White);
	            }

	            stationZone.DrawText();
	            recipeZone.DrawText();
	            ingredientZone.DrawText();
	            recipeHeaderZone.DrawText();
	            storageZone.DrawText();
	            resultZone.DrawText();
	            sortButtons.DrawText();
	            recipeButtons.DrawText();
	            filterButtons.DrawText();
	        }
	        catch (Exception e)
	        {
	            Main.NewTextMultiline(e.ToString());
	        }
	    }

	    private static Item GetStation(int slot, ref int context)
		{
			Item[] stations = GetCraftingStations();
			if (stations == null || slot >= stations.Length)
			{
				return new Item();
			}
			return stations[slot];
		}

		private static Item GetRecipe(int slot, ref int context)
		{
			if (threadRunning)
			{
				return new Item();
			}
			int index = slot + numColumns * (int)Math.Round(scrollBar.ViewPosition);
			Item item = index < recipes.Count ? recipes[index].createItem : new Item();
			if (!item.IsAir)
			{
			    if (recipes[index] == selectedRecipe)
			    {
			        context = 6;
			    }
			    if (!recipeAvailable[index])
			    {
			        context = recipes[index] == selectedRecipe ? 4 : 3;
			    }
                if (ModPlayer.FavoritedRecipes.Contains(item))
                {
                    item = item.Clone();
                    item.favorited = true;
                }

			    if (!ModPlayer.SeenRecipes.Contains(item))
			    {
			        item = item.Clone();
			        item.newAndShiny = true;
			    }
			}

			return item;
		}

        private static Item GetHeader(int slot, ref int context)
        {
            if (selectedRecipe == null)
                return new Item();

            var item = selectedRecipe.createItem;
            if (item.IsAir)
            {
                int t = item.type;
                item = new Item();
                item.SetDefaults(t);
                item.stack = 0;
            }
            return item;
        }
        
        private static Item GetIngredient(int slot, ref int context)
		{
		    if (selectedRecipe == null || slot >= selectedRecipe.requiredItem.Length)
		        return new Item();

		    Item item = selectedRecipe.requiredItem[slot].Clone();
			if (selectedRecipe.anyWood && item.type == ItemID.Wood)
			{
				item.SetNameOverride(Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.Wood));
			}
			if (selectedRecipe.anySand && item.type == ItemID.SandBlock)
			{
				item.SetNameOverride(Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.SandBlock));
			}
			if (selectedRecipe.anyIronBar && item.type == ItemID.IronBar)
			{
				item.SetNameOverride(Lang.misc[37].Value + " " + Lang.GetItemNameValue(ItemID.IronBar));
			}
			if (selectedRecipe.anyFragment && item.type == ItemID.FragmentSolar)
			{
				item.SetNameOverride(Lang.misc[37].Value + " " + Lang.misc[51].Value);
			}
			if (selectedRecipe.anyPressurePlate && item.type == ItemID.GrayPressurePlate)
			{
				item.SetNameOverride(Lang.misc[37].Value + " " + Lang.misc[38].Value);
			}
			string nameOverride;
			if (selectedRecipe.ProcessGroupsForText(item.type, out nameOverride))
			{
				item.SetNameOverride(nameOverride);
			}
			return item;
		}

		private static Item GetStorage(int slot, ref int context)
		{
			int index = slot + numColumns2 * (int)Math.Round(scrollBar2.ViewPosition);
			Item item = index < storageItems.Count ? storageItems[index] : new Item();
			if (blockStorageItems.Contains(new ItemData(item)))
			{
				context = 3;
			}
			return item;
		}

		private static Item GetResult(int slot, ref int context)
		{
			return slot == 0 && result != null ? result : new Item();
		}

		private static void UpdateRecipeText()
		{
			if (selectedRecipe == null)
			{
				reqObjText2.SetText("");
            }
			else
			{
                bool isEmpty = true;
				string text = "";
				for (int k = 0; k < selectedRecipe.requiredTile.Length; k++)
				{
					if (selectedRecipe.requiredTile[k] == -1)
					{
						break;
					}
					if (!isEmpty)
					{
						text += ", ";
					}
					text += Lang.GetMapObjectName(MapHelper.TileToLookup(selectedRecipe.requiredTile[k], 0));
					isEmpty = false;
				}
				if (selectedRecipe.needWater)
				{
					if (!isEmpty)
					{
						text += ", ";
					}
					text += Language.GetTextValue("LegacyInterface.53");
					isEmpty = false;
				}
				if (selectedRecipe.needHoney)
				{
					if (!isEmpty)
					{
						text += ", ";
					}
					text += Language.GetTextValue("LegacyInterface.58");
					isEmpty = false;
				}
				if (selectedRecipe.needLava)
				{
					if (!isEmpty)
					{
						text += ", ";
					}
					text += Language.GetTextValue("LegacyInterface.56");
					isEmpty = false;
				}
				if (selectedRecipe.needSnowBiome)
				{
					if (!isEmpty)
					{
						text += ", ";
					}
					text += Language.GetTextValue("LegacyInterface.123");
					isEmpty = false;
				}
				if (isEmpty)
				{
					text = Language.GetTextValue("LegacyInterface.23");
				}
				reqObjText2.SetText(text);
            }
		}

		private static void UpdateScrollBar()
		{
			if (slotFocus)
			{
				scrollBarFocus = 0;
				return;
			}
			Rectangle dim = scrollBar.GetClippingRectangle(Main.spriteBatch);
			Vector2 boxPos = new Vector2(dim.X, dim.Y + dim.Height * (scrollBar.ViewPosition / scrollBarMaxViewSize));
			float boxWidth = 20f * Main.UIScale;
			float boxHeight = dim.Height * (scrollBarViewSize / scrollBarMaxViewSize);
			Rectangle dim2 = scrollBar2.GetClippingRectangle(Main.spriteBatch);
			Vector2 box2Pos = new Vector2(dim2.X, dim2.Y + dim2.Height * (scrollBar2.ViewPosition / scrollBar2MaxViewSize));
			float box2Height = dim2.Height * (scrollBar2ViewSize / scrollBar2MaxViewSize);
			if (scrollBarFocus > 0)
			{
				if (curMouse.LeftButton == ButtonState.Released)
				{
					scrollBarFocus = 0;
				}
				else
				{
					int difference = curMouse.Y - scrollBarFocusMouseStart;
					if (scrollBarFocus == 1)
					{
						scrollBar.ViewPosition = scrollBarFocusPositionStart + (float)difference / boxHeight;
					}
					else if (scrollBarFocus == 2)
					{
						scrollBar2.ViewPosition = scrollBarFocusPositionStart + (float)difference / box2Height;
					}
				}
			}
			else if (MouseClicked)
			{
				if (curMouse.X > boxPos.X && curMouse.X < boxPos.X + boxWidth && curMouse.Y > boxPos.Y - 3f && curMouse.Y < boxPos.Y + boxHeight + 4f)
				{
					scrollBarFocus = 1;
					scrollBarFocusMouseStart = curMouse.Y;
					scrollBarFocusPositionStart = scrollBar.ViewPosition;
				}
				else if (curMouse.X > box2Pos.X && curMouse.X < box2Pos.X + boxWidth && curMouse.Y > box2Pos.Y - 3f && curMouse.Y < box2Pos.Y + box2Height + 4f)
				{
					scrollBarFocus = 2;
					scrollBarFocusMouseStart = curMouse.Y;
					scrollBarFocusPositionStart = scrollBar2.ViewPosition;
				}
			}
			if (scrollBarFocus == 0)
			{
				int difference = oldMouse.ScrollWheelValue / 250 - curMouse.ScrollWheelValue / 250;
				scrollBar.ViewPosition += difference;
			}
		}

		private static void UpdateCraftButton()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(craftButton);
			bool flag = false;
			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
			{
				craftButton.BackgroundColor = new Color(73, 94, 171);
			    if (curMouse.LeftButton == ButtonState.Pressed && selectedRecipe != null && IsAvailable(selectedRecipe) && PassesBlock(selectedRecipe))
				{
				    if (craftTimer <= 0)
				    {
				        craftTimer = maxCraftTimer;
				        maxCraftTimer = maxCraftTimer * 3 / 4;
				        if (maxCraftTimer <= 0)
				        {
				            maxCraftTimer = 1;
				        }
				        TryCraft();
				        RefreshItems();
				        Main.PlaySound(7, -1, -1, 1);
				    }
				    craftTimer--;
				    flag = true;
				    StoragePlayer modPlayer = Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>();
				    if (modPlayer.AddToCraftedRecipes(selectedRecipe.createItem))
				        RefreshItems();
				}
			}
			else
			{
				craftButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
			}
			if (selectedRecipe == null || !IsAvailable(selectedRecipe) || !PassesBlock(selectedRecipe))
			{
				craftButton.BackgroundColor = new Color(30, 40, 100) * 0.7f;
			}
			if (!flag)
			{
				craftTimer = 0;
				maxCraftTimer = startMaxCraftTimer;
			}
		}

	    private static TEStorageHeart GetHeart()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			return modPlayer.GetStorageHeart();
		}

		private static TECraftingAccess GetCraftingEntity()
		{
			Player player = Main.player[Main.myPlayer];
			StoragePlayer modPlayer = player.GetModPlayer<StoragePlayer>();
			return modPlayer.GetCraftingAccess();
		}

		private static Item[] GetCraftingStations()
		{
			TECraftingAccess ent = GetCraftingEntity();
			return ent == null ? null : ent.stations;
		}

		public static void RefreshItems()
		{
		    var modPlayer = ModPlayer;
		    if (modPlayer.SeenRecipes.Count == 0)
		    {
		        foreach (var item in GetKnownItems())
		            modPlayer.SeenRecipes.Add(item);
		    }
			items.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
			{
				return;
			}

		    items.AddRange(ItemSorter.SortAndFilter(heart.GetStoredItems(), SortMode.Id, FilterMode.All, searchBar2.Text, ""));
			AnalyzeIngredients();
			InitLangStuff();
			InitSortButtons();
			InitRecipeButtons();
			InitFilterButtons();
			SortMode sortMode = (SortMode)sortButtons.Choice;
			FilterMode filterMode = (FilterMode)filterButtons.Choice;
			
			RefreshStorageItems();

		    HashSet<int> foundItems;
		    HashSet<int> hiddenRecipes;
		    HashSet<int> craftedRecipes;
		    HashSet<int> asKnownRecipes;
		    GetKnownItems(out foundItems, out hiddenRecipes, out craftedRecipes, out asKnownRecipes);
		    foundItems.UnionWith(asKnownRecipes);

            var favoritesCopy = new HashSet<int>(modPlayer.FavoritedRecipes.Items.Select(x => x.type));

		    EnsureProductToRecipesInited();

            lock (threadLock)
			{
				threadNeedsRestart = true;
				threadSortMode = sortMode;
				threadFilterMode = filterMode;
                threadCheckListFoundItems = foundItems;
			    if (!threadRunning)
			    {
			        threadRunning = true;
			        Thread thread = new Thread(_ => RefreshRecipes(hiddenRecipes, craftedRecipes, favoritesCopy));
			        thread.Start();
			    }
			}
		}

        public static HashSet<int> GetKnownItems()
        {
            HashSet<int> a, b, c, d;
            GetKnownItems(out a, out b, out c, out d);
            a.UnionWith(b);
            a.UnionWith(c);
            a.UnionWith(d);
            return a;
        }

	    static void GetKnownItems(out HashSet<int> foundItems, out HashSet<int> hiddenRecipes, out HashSet<int> craftedRecipes, out HashSet<int> asKnownRecipes)
	    {
	        foundItems = new HashSet<int>(RetrieveFoundItemsCheckList());

	        StoragePlayer modPlayer = ModPlayer;
	        hiddenRecipes = new HashSet<int>(modPlayer.HiddenRecipes.Select(x => x.type));
	        craftedRecipes = new HashSet<int>(modPlayer.CraftedRecipes.Select(x => x.type));
	        asKnownRecipes = new HashSet<int>(modPlayer.AsKnownRecipes.Items.Select(x => x.type));
	    }

	    static StoragePlayer ModPlayer { get { return Main.player[Main.myPlayer].GetModPlayer<StoragePlayer>(); } }

	    static IEnumerable<int> RetrieveFoundItemsCheckList()
	    {
	        if (_checkListMod == null)
	            _checkListMod = ModLoader.GetMod("ItemChecklist");

	        var foundItems = _checkListMod != null ? _checkListMod.Call("RequestFoundItems") as bool[] : new bool[0];
	        if (foundItems.Length > 0) wasItemChecklistRetrieved = true;
	        return  foundItems.Select((v, type) => new { WasFound = v, type }).Where(x => x.WasFound).Select(x => x.type);
        }

	    static void EnsureProductToRecipesInited()
	    {
	        if (_productToRecipes == null)
	        {
	            var allRecipes = ItemSorter.GetRecipes(SortMode.Id, FilterMode.All, "", "").Where(x => x != null && x.createItem != null && x.createItem.type > 0).ToArray();
	            _productToRecipes = allRecipes.GroupBy(x => x.createItem.type).ToDictionary(x => x.Key, x => x.ToList());
	        }
	    }

	    static Dictionary<int, List<Recipe>> _productToRecipes;

	    /// <summary>
	    /// Checks all crafting tree until it finds already available ingredients
	    /// </summary>
	    static bool IsKnownRecursively(Recipe recipe, HashSet<int> availableSet)
	    {
	        return IsKnownRecursively(recipe, availableSet, new HashSet<int>(), new Dictionary<Recipe, bool>());
	    }

	    /// <summary>
        /// Checks all crafting tree until it finds already available ingredients
        /// </summary>
        static bool IsKnownRecursively(Recipe recipe, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache)
        {
            bool v;
            if (cache.TryGetValue(recipe, out v)) return v;


            foreach (int tile in recipe.requiredTile)
            {
                if (tile == -1)
                    break;

                List<int> possibleItems;
                if (!StorageWorld.TileToCreatingItem.TryGetValue(tile, out possibleItems))
                    continue;

                if (!possibleItems.Any(x => IsKnownRecursively_CheckIngredient(x, availableSet, recursionTree, cache)))
                {
                    cache[recipe] = false;
                    return false;
                }
            }

            int ingredients = 0;
            for (int i = 0; i < Recipe.maxRequirements; i++)
            {
                var t = recipe.requiredItem[i].type;
                if (t <= 0) continue;
                ingredients++;
                if (IsKnownRecursively_CheckIngredient(t, availableSet, recursionTree, cache)) continue;
                if (IsKnownRecursively_CheckAcceptedGroupsForIngredient(recipe, availableSet, recursionTree, cache, t)) continue;
                cache[recipe] = false;
                return false;
            }

            if (ingredients > 0)
            {
                cache[recipe] = true;
                return true;
            }

            cache[recipe] = false;
            return false;
        }

	    static bool IsKnownRecursively_CheckAcceptedGroupsForIngredient(Recipe recipe, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache, int t)
	    {
	        foreach (var g in recipe.acceptedGroups.Select(j => RecipeGroup.recipeGroups[j]))
	        {
	            if (g.ContainsItem(t))
	            {
	                foreach (var groupItemType in g.ValidItems)
	                {
	                    if (groupItemType != t && IsKnownRecursively_CheckIngredient(groupItemType, availableSet, recursionTree, cache))
	                    {
                            return true;
	                    }
	                }
	            }
	        }
            return false;
	    }

	    static bool IsKnownRecursively_CheckIngredient(int t, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache)
	    {
	        if (availableSet.Contains(t)) return true;
	        if (!recursionTree.Add(t)) return false;
	        try
	        {
	            List<Recipe> ingredientRecipes;
	            if (!_productToRecipes.TryGetValue(t, out ingredientRecipes)) return false;
	            if (ingredientRecipes.Count == 0 || ingredientRecipes.All(x => !IsKnownRecursively(x, availableSet, recursionTree, cache))) return false;
	        }
	        finally
	        {
	            recursionTree.Remove(t);
	        }

	        return true;
	    }

	    private static void RefreshRecipes(HashSet<int> hiddenRecipes, HashSet<int> craftedRecipes, HashSet<int> favorited)
        {
            while (true)
            {
                try
                {
                    SortMode sortMode;
                    FilterMode filterMode;
                    HashSet<int> foundItems;
                    lock (threadLock)
                    {
                        threadNeedsRestart = false;
                        sortMode = threadSortMode;
                        filterMode = threadFilterMode;
                        foundItems = threadCheckListFoundItems;
                    }

                    var availableItemsMutable = new HashSet<int>(hiddenRecipes
                        .Concat(craftedRecipes)
                        .Concat(foundItems));

                    var notNewItems = new HashSet<int>(availableItemsMutable);

                    var temp = new HashSet<int>();
                    var tempCache = new Dictionary<Recipe, bool>();

                    var modFilter = searchBar2.Text;

                    IEnumerable<Recipe> filteredRecipes = null;

                    Action doFiltering = () =>
                    {
                        filteredRecipes = ItemSorter.GetRecipes(sortMode, filterMode, modFilter, searchBar.Text).Where(x => x != null)
                            // show only blacklisted recipes only if choice = 2, otherwise show all other
                            .Where(x => (recipeButtons.Choice == RecipeButtonsBlacklistChoice) == hiddenRecipes.Contains(x.createItem.type))
                            // show only new items if selected
                            .Where(x => (recipeButtons.Choice != RecipeButtonsNewChoice) || !notNewItems.Contains(x.createItem.type))
                            // show only favorited items if selected
                            .Where(x => (recipeButtons.Choice != RecipeButtonsFavoritesChoice) || favorited.Contains(x.createItem.type))
                            // hard check if this item can be crafted from available items and their recursive products
                            .Where(x => !wasItemChecklistRetrieved || IsKnownRecursively(x, availableItemsMutable, temp, tempCache));

                        threadRecipes.Clear();
                        threadRecipeAvailable.Clear();
                        try
                        {
                            threadRecipes.AddRange(filteredRecipes);
                            threadRecipeAvailable.AddRange(threadRecipes.Select(IsAvailable));
                        }
                        catch (InvalidOperationException)
                        {
                        }
                        catch (KeyNotFoundException)
                        {
                        }
                    };

                    doFiltering();
                    
                    // now if nothing found we disable filters one by one
                    if (searchBar.Text.Length > 0)
                    {
                        if (threadRecipes.Count == 0 && recipeButtons.Choice == RecipeButtonsNewChoice)
                        {
                            // search old recipes too
                            notNewItems = new HashSet<int>();
                            doFiltering();
                        }

                        if (threadRecipes.Count == 0 && hiddenRecipes.Count > 0)
                        {
                            // search hidden recipes too
                            hiddenRecipes = new HashSet<int>();
                            doFiltering();
                        }

                        if (threadRecipes.Count == 0 && filterMode != FilterMode.All)
                        {
                            // any category
                            filterMode = FilterMode.All;
                            doFiltering();
                        }

                        if (threadRecipes.Count == 0 && modFilter != "")
                        {
                            // search all mods
                            modFilter = "";
                            doFiltering();
                        }
                    }

                    lock (recipeLock)
                    {
                        nextRecipes = new List<Recipe>();
                        nextRecipeAvailable = new List<bool>();
                        nextRecipes.AddRange(threadRecipes);
                        nextRecipeAvailable.AddRange(threadRecipeAvailable);

                    }
                    lock (threadLock)
                    {
                        if (!threadNeedsRestart)
                        {
                            threadRunning = false;
                            return;
                        }
                    }
                }
                catch (Exception e) { Main.NewTextMultiline(e.ToString()); }
            }
        }

        private static void AnalyzeIngredients()
		{
			Player player = Main.player[Main.myPlayer];
			itemCounts.Clear();
			if (adjTiles.Length != player.adjTile.Length)
			{
				Array.Resize(ref adjTiles, player.adjTile.Length);
			}
			for (int k = 0; k < adjTiles.Length; k++)
			{
				adjTiles[k] = false;
			}
			adjWater = false;
			adjLava = false;
			adjHoney = false;
			zoneSnow = false;
			alchemyTable = false;

			foreach (Item item in items)
			{
				if (itemCounts.ContainsKey(item.netID))
				{
					itemCounts[item.netID] += item.stack;
				}
				else
				{
					itemCounts[item.netID] = item.stack;
				}
			}
			foreach (Item item in GetCraftingStations())
			{
				if (item.createTile >= 0)
				{
					adjTiles[item.createTile] = true;
					if (item.createTile == TileID.GlassKiln || item.createTile == TileID.Hellforge || item.createTile == TileID.AdamantiteForge)
					{
						adjTiles[TileID.Furnaces] = true;
					}
					if (item.createTile == TileID.AdamantiteForge)
					{
						adjTiles[TileID.Hellforge] = true;
					}
					if (item.createTile == TileID.MythrilAnvil)
					{
						adjTiles[TileID.Anvils] = true;
					}
					if (item.createTile == TileID.BewitchingTable || item.createTile == TileID.Tables2)
					{
						adjTiles[TileID.Tables] = true;
					}
					if (item.createTile == TileID.AlchemyTable)
					{
						adjTiles[TileID.Bottles] = true;
						adjTiles[TileID.Tables] = true;
						alchemyTable = true;
					}
					bool[] oldAdjTile = player.adjTile;
					bool oldAdjWater = adjWater;
					bool oldAdjLava = adjLava;
					bool oldAdjHoney = adjHoney;
					bool oldAlchemyTable = alchemyTable;
					player.adjTile = adjTiles;
					player.adjWater = false;
					player.adjLava = false;
					player.adjHoney = false;
					player.alchemyTable = false;
					TileLoader.AdjTiles(player, item.createTile);
					if (player.adjWater)
					{
						adjWater = true;
					}
					if (player.adjLava)
					{
						adjLava = true;
					}
					if (player.adjHoney)
					{
						adjHoney = true;
					}
					if (player.alchemyTable)
					{
						alchemyTable = true;
					}
					player.adjTile = oldAdjTile;
					player.adjWater = oldAdjWater;
					player.adjLava = oldAdjLava;
					player.adjHoney = oldAdjHoney;
					player.alchemyTable = oldAlchemyTable;
				}
				if (item.type == ItemID.WaterBucket || item.type == ItemID.BottomlessBucket)
				{
					adjWater = true;
				}
				if (item.type == ItemID.LavaBucket)
				{
					adjLava = true;
				}
				if (item.type == ItemID.HoneyBucket)
				{
					adjHoney = true;
				}
				if (item.type == MagicStorage.Instance.ItemType("SnowBiomeEmulator"))
				{
					zoneSnow = true;
				}
			}
			adjTiles[MagicStorage.Instance.TileType("CraftingAccess")] = true;
		}

		private static bool IsAvailable(Recipe recipe)
		{
			foreach (int tile in recipe.requiredTile)
			{
				if (tile == -1)
				{
					break;
				}
				if (!adjTiles[tile])
				{
					return false;
				}
			}
			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient.type == 0)
				{
					break;
				}
				int stack = ingredient.stack;
				bool useRecipeGroup = false;
				foreach (int type in itemCounts.Keys)
				{
					if (RecipeGroupMatch(recipe, type, ingredient.type))
					{
						stack -= itemCounts[type];
						useRecipeGroup = true;
					}
				}
				if (!useRecipeGroup && itemCounts.ContainsKey(ingredient.netID))
				{
					stack -= itemCounts[ingredient.netID];
				}
				if (stack > 0)
				{
					return false;
				}
			}
			if (recipe.needWater && !adjWater && !adjTiles[TileID.Sinks])
			{
				return false;
			}
			if (recipe.needLava && !adjLava)
			{
				return false;
			}
			if (recipe.needHoney && !adjHoney)
			{
				return false;
			}
			if (recipe.needSnowBiome && !zoneSnow)
			{
				return false;
			}
			try
			{
				BlockRecipes.active = false;
				if (!RecipeHooks.RecipeAvailable(recipe))
				{
					return false;
				}
			}
			finally
			{
				BlockRecipes.active = true;
			}
			return true;
		}

		private static bool PassesBlock(Recipe recipe)
		{
			foreach (Item ingredient in recipe.requiredItem)
			{
				if (ingredient.type == 0)
				{
					break;
				}
				int stack = ingredient.stack;
				bool useRecipeGroup = false;
				foreach (Item item in storageItems)
				{
					ItemData data = new ItemData(item);
					if (!blockStorageItems.Contains(data) && RecipeGroupMatch(recipe, item.netID, ingredient.type))
					{
						stack -= item.stack;
						useRecipeGroup = true;
					}
				}
				if (!useRecipeGroup)
				{
					foreach (Item item in storageItems)
					{
						ItemData data = new ItemData(item);
						if (!blockStorageItems.Contains(data) && item.netID == ingredient.netID)
						{
							stack -= item.stack;
						}
					}
				}
				if (stack > 0)
				{
					return false;
				}
			}
			return true;
		}

		private static void RefreshStorageItems()
		{
			storageItems.Clear();
			result = null;
			if (selectedRecipe != null)
			{
				foreach (Item item in items)
				{
					for (int k = 0; k < selectedRecipe.requiredItem.Length; k++)
					{
						if (selectedRecipe.requiredItem[k].type == 0)
						{
							break;
						}
						if (item.type == selectedRecipe.requiredItem[k].type || RecipeGroupMatch(selectedRecipe, selectedRecipe.requiredItem[k].type, item.type))
						{
							storageItems.Add(item);
						}
					}
					if (item.type == selectedRecipe.createItem.type)
					{
						result = item;
					}
				}
				if (result == null)
				{
					result = new Item();
					result.SetDefaults(selectedRecipe.createItem.type);
					result.stack = 0;
				}
			}
		}

		private static bool RecipeGroupMatch(Recipe recipe, int type1, int type2)
		{
			return recipe.useWood(type1, type2) || recipe.useSand(type1, type2) || recipe.useIronBar(type1, type2) || recipe.useFragment(type1, type2) || recipe.AcceptedByItemGroups(type1, type2) || recipe.usePressurePlate(type1, type2);
		}

		private static void HoverStation(int slot, ref int hoverSlot)
		{
			TECraftingAccess ent = GetCraftingEntity();
			if (ent == null || slot >= ent.stations.Length)
			{
				return;
			}

			Player player = Main.player[Main.myPlayer];
			if (MouseClicked)
			{
				bool changed = false;
				if (!ent.stations[slot].IsAir && ItemSlot.ShiftInUse)
				{
					Item result = player.GetItem(Main.myPlayer, DoWithdraw(slot), false, true);
					if (!result.IsAir && Main.mouseItem.IsAir)
					{
						Main.mouseItem = result;
						result = new Item();
					}
					if (!result.IsAir && Main.mouseItem.type == result.type && Main.mouseItem.stack < Main.mouseItem.maxStack)
					{
						Main.mouseItem.stack += result.stack;
						result = new Item();
					}
					if (!result.IsAir)
					{
						player.QuickSpawnClonedItem(result);
					}
					changed = true;
				}
				else if (player.itemAnimation == 0 && player.itemTime == 0)
				{
					int oldType = Main.mouseItem.type;
					int oldStack = Main.mouseItem.stack;
					Main.mouseItem = DoStationSwap(Main.mouseItem, slot);
					if (oldType != Main.mouseItem.type || oldStack != Main.mouseItem.stack)
					{
						changed = true;
					}
				}
				if (changed)
				{
					RefreshItems();
					Main.PlaySound(7, -1, -1, 1);
				}
			}

			hoverSlot = slot;
		}

		private static void HoverRecipe(int slot, ref int hoverSlot)
		{
			int visualSlot = slot;
			slot += numColumns * (int)Math.Round(scrollBar.ViewPosition);
			if (slot < recipes.Count)
			{
                var recipe = recipes[slot];
			    if (MouseClicked)
                {
                    if (Main.keyState.IsKeyDown(Keys.LeftAlt))
                    {
                        if (!ModPlayer.FavoritedRecipes.Add(recipe.createItem))
                            ModPlayer.FavoritedRecipes.Remove(recipe.createItem);
                    }
                    else if (Main.keyState.IsKeyDown(Keys.LeftControl))
                    {
                        if (recipeButtons.Choice == RecipeButtonsBlacklistChoice)
                        {
                            if (ModPlayer.RemoveFromHiddenRecipes(recipe.createItem))
                                RefreshItems();
                        }
                        else
                        {
                            if (ModPlayer.AddToHiddenRecipes(recipe.createItem))
                                RefreshItems();
                        }
                    }
                    else
                        SetSelectedRecipe(recipe);
                }
			    else if (RightMouseClicked && (recipe == selectedRecipe || recipeButtons.Choice != RecipeButtonsNewChoice))
			    {
			        if (recipeButtons.Choice == RecipeButtonsNewChoice)
			        {
			            ModPlayer.AsKnownRecipes.Add(recipe.createItem);
			            RefreshItems();
			        }
			        else
			            ModPlayer.AsKnownRecipes.Remove(recipe.createItem);
			    }

			    hoverSlot = visualSlot;
			}
		}

	    static void SetSelectedRecipe(Recipe recipe)
	    {
	        if (recipe != null) ModPlayer.SeenRecipes.Add(recipe.createItem);
	        selectedRecipe = recipe;
	        RefreshStorageItems();
	        blockStorageItems.Clear();
	    }

        private static void HoverHeader(int slot, ref int hoverSlot)
        {
            hoverSlot = slot;
        }

        private static void HoverItem(int slot, ref int hoverSlot)
        {
            if (selectedRecipe == null)
            {
                hoverSlot = slot;
                return;
            }
            int visualSlot = slot;
            slot += numColumns2 * (int)Math.Round(scrollBar2.ViewPosition);
            var count = selectedRecipe.requiredItem.Select((x, i) => new { x, i }).First(x => x.x.type == 0).i + 1;

            if (slot < count)
            {
                // select ingredient recipe by right clicking
                if (RightMouseClicked)
                {
                    var item = selectedRecipe.requiredItem[slot];
                    EnsureProductToRecipesInited();
                    List<Recipe> itemRecipes;
                    if (_productToRecipes.TryGetValue(item.type, out itemRecipes))
                    {
                        var knownItems = GetKnownItems();
                        
                        var recursionTree = new HashSet<int>();
                        var cache = new Dictionary<Recipe, bool>();

                        Recipe selected = null;

                        foreach (var r in itemRecipes.Where(x => IsKnownRecursively(x, knownItems, recursionTree, cache)))
                        {
                            if (selected == null) selected = r;
                            if (IsAvailable(r))
                            {
                                selected = r;
                                break;
                            }
                        }
                        if (selected != null) SetSelectedRecipe(selected);
                    }
                }
                hoverSlot = visualSlot;
            }
        }

		private static void HoverStorage(int slot, ref int hoverSlot)
		{
			int visualSlot = slot;
			slot += numColumns2 * (int)Math.Round(scrollBar2.ViewPosition);
			if (slot < storageItems.Count)
			{
			    var item = storageItems[slot];
                item.newAndShiny = false;
                if (MouseClicked)
				{
				    ItemData data = new ItemData(item);
					if (blockStorageItems.Contains(data))
					{
						blockStorageItems.Remove(data);
					}
					else
					{
						blockStorageItems.Add(data);
					}
				}
				hoverSlot = visualSlot;
			}
		}

		private static void HoverResult(int slot, ref int hoverSlot)
		{
			if (slot != 0)
			{
				return;
			}

		    if (Main.mouseItem.IsAir && result != null && !result.IsAir)
		        result.newAndShiny = false;

		    Player player = Main.player[Main.myPlayer];
			if (MouseClicked)
			{
				bool changed = false;
				if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0 && result != null && Main.mouseItem.type == result.type)
				{
					if (TryDepositResult(Main.mouseItem))
					{
						changed = true;
					}
				}
				else if (Main.mouseItem.IsAir && result != null && !result.IsAir)
				{
                    if (Main.keyState.IsKeyDown(Keys.LeftAlt))
                        result.favorited = !result.favorited;
                    else
                    {
                        Item toWithdraw = result.Clone();
                        if (toWithdraw.stack > toWithdraw.maxStack)
                        {
                            toWithdraw.stack = toWithdraw.maxStack;
                        }
                        Main.mouseItem = DoWithdrawResult(toWithdraw, ItemSlot.ShiftInUse);
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

			if (curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released && result != null && !result.IsAir && (Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, items[slot]) && Main.mouseItem.stack < Main.mouseItem.maxStack))
			{
				slotFocus = true;
			}

			hoverSlot = slot;

			if (slotFocus)
			{
				SlotFocusLogic();
			}
		}

		private static void SlotFocusLogic()
		{
			if (result == null || result.IsAir || (!Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack)))
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
					Item toWithdraw = result.Clone();
					toWithdraw.stack = 1;
					Item withdrawn = DoWithdrawResult(toWithdraw);
					if (Main.mouseItem.IsAir)
					{
						Main.mouseItem = withdrawn;
					}
					else
					{
						Main.mouseItem.stack += withdrawn.stack;
					}
					Main.soundInstanceMenuTick.Stop();
					Main.soundInstanceMenuTick = Main.soundMenuTick.CreateInstance();
					Main.PlaySound(12, -1, -1, 1);
					RefreshItems();
				}
				rightClickTimer--;
			}
		}

		private static void ResetSlotFocus()
		{
			slotFocus = false;
			rightClickTimer = 0;
			maxRightClickTimer = startMaxRightClickTimer;
		}

		private static Item DoWithdraw(int slot)
		{
			TECraftingAccess access = GetCraftingEntity();
			if (Main.netMode == 0)
			{
				Item result = access.TryWithdrawStation(slot);
				RefreshItems();
				return result;
			}
			else
			{
				NetHelper.SendWithdrawStation(access.ID, slot);
				return new Item();
			}
		}

		private static Item DoStationSwap(Item item, int slot)
		{
			TECraftingAccess access = GetCraftingEntity();
			if (Main.netMode == 0)
			{
				Item result = access.DoStationSwap(item, slot);
				RefreshItems();
				return result;
			}
			else
			{
				NetHelper.SendStationSlotClick(access.ID, item, slot);
				return new Item();
			}
		}

		private static void TryCraft()
		{
			List<Item> availableItems = new List<Item>(storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).Select(item => item.Clone()));
			List<Item> toWithdraw = new List<Item>();
			for (int k = 0; k < selectedRecipe.requiredItem.Length; k++)
			{
				Item item = selectedRecipe.requiredItem[k];
				if (item.type == 0)
				{
					break;
				}
				int stack = item.stack;
				ModRecipe modRecipe = selectedRecipe as ModRecipe;
				if (modRecipe != null)
				{
					stack = modRecipe.ConsumeItem(item.type, item.stack);
				}
				if (selectedRecipe.alchemy && alchemyTable)
				{
					int save = 0;
					for (int j = 0; j < stack; j++)
					{
						if (Main.rand.Next(3) == 0)
						{
							save++;
						}
					}
					stack -= save;
				}
				if (stack > 0)
				{
					foreach (Item tryItem in availableItems)
					{
						if (item.type == tryItem.type || RecipeGroupMatch(selectedRecipe, item.type, tryItem.type))
						{
							if (tryItem.stack > stack)
							{
								Item temp = tryItem.Clone();
								temp.stack = stack;
								toWithdraw.Add(temp);
								tryItem.stack -= stack;
								stack = 0;
							}
							else
							{
								toWithdraw.Add(tryItem.Clone());
								stack -= tryItem.stack;
								tryItem.stack = 0;
								tryItem.type = 0;
							}
						}
					}
				}
			}
			Item resultItem = selectedRecipe.createItem.Clone();
			resultItem.Prefix(-1);

			RecipeHooks.OnCraft(resultItem, selectedRecipe);
			ItemLoader.OnCraft(resultItem, selectedRecipe);

			if (Main.netMode == 0)
			{
				foreach (Item item in DoCraft(GetHeart(), toWithdraw, resultItem))
				{
					Main.player[Main.myPlayer].QuickSpawnClonedItem(item, item.stack);
				}
			}
			else if (Main.netMode == 1)
			{
				NetHelper.SendCraftRequest(GetHeart().ID, toWithdraw, resultItem);
			}
		}

		internal static List<Item> DoCraft(TEStorageHeart heart, List<Item> toWithdraw, Item result)
		{
			List<Item> items = new List<Item>();
			foreach (Item tryWithdraw in toWithdraw)
			{
				Item withdrawn = heart.TryWithdraw(tryWithdraw);
				if (!withdrawn.IsAir)
				{
					items.Add(withdrawn);
				}
				if (withdrawn.stack < tryWithdraw.stack)
				{
					for (int k = 0; k < items.Count; k++)
					{
						heart.DepositItem(items[k]);
						if (items[k].IsAir)
						{
							items.RemoveAt(k);
							k--;
						}
					}
					return items;
				}
			}
			items.Clear();
			heart.DepositItem(result);
			if (!result.IsAir)
			{
				items.Add(result);
			}
			return items;
		}

		private static bool TryDepositResult(Item item)
		{
			int oldStack = item.stack;
			DoDepositResult(item);
			return oldStack != item.stack;
		}

		private static void DoDepositResult(Item item)
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

		private static Item DoWithdrawResult(Item item, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			if (Main.netMode == 0)
			{
				return heart.TryWithdraw(item);
			}
			else
			{
				NetHelper.SendWithdraw(heart.ID, item, toInventory);
				return new Item();
			}
		}
	}
}