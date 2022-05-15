using System;
using System.Collections.Generic;
using System.Linq;
using MagicStorage.Components;
using MagicStorage.Items;
using MagicStorage.Sorting;
using MagicStorage.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace MagicStorage
{
	public static class CraftingGUI
	{
		private const int RecipeButtonsAvailableChoice = 0;
		private const int RecipeButtonsBlacklistChoice = 3;
		private const int RecipeButtonsFavoritesChoice = 2;
		private const int Padding = 4;
		private const int RecipeColumns = 10;
		private const int IngredientColumns = 7;
		private const float InventoryScale = 0.85f;
		private const float SmallScale = 0.7f;
		private const int StartMaxCraftTimer = 20;
		private const int StartMaxRightClickTimer = 20;
		private const float ScrollBar2ViewSize = 1f;
		private const float RecipeScrollBarViewSize = 1f;

		private static HashSet<int> threadCheckListFoundItems;
		private static Mod itemChecklistMod;
		private static volatile bool wasItemChecklistRetrieved;

		private static MouseState curMouse;
		private static MouseState oldMouse;

		private static UIPanel basePanel;
		private static float panelTop;
		private static float panelLeft;
		private static float panelWidth;
		private static float panelHeight;

		private static UIElement topBar;

		// TODO take a look at Terraria's UISearchBar
		public static UI.UISearchBar searchBar;
		private static UIButtonChoice sortButtons;
		internal static UIButtonChoice recipeButtons;
		private static UIElement topBar2;
		private static UIButtonChoice filterButtons;

		private static UIText stationText;
		private static readonly UISlotZone stationZone = new(HoverStation, GetStation, InventoryScale / 1.55f);
		private static readonly UISlotZone recipeZone = new(HoverRecipe, GetRecipe, InventoryScale);

		private static readonly UIScrollbar recipeScrollBar = new();
		private static int recipeScrollBarFocus;
		private static int recipeScrollBarFocusMouseStart;
		private static float recipeScrollBarFocusPositionStart;
		private static float recipeScrollBarMaxViewSize = 2f;

		private static readonly List<Item> items = new();
		private static readonly Dictionary<int, int> itemCounts = new();
		private static List<Recipe> recipes = new();
		private static List<bool> recipeAvailable = new();
		private static Recipe selectedRecipe;
		private static int numRows;
		private static int displayRows;
		private static bool slotFocus;

		private static readonly UIElement bottomBar = new();
		private static UIText capacityText;

		private static UIPanel recipePanel;

		private static float recipeTop;
		private static float recipeLeft;
		private static float recipeWidth;
		private static float recipeHeight;

		private static UIText recipePanelHeader;
		private static UIText ingredientText;
		private static readonly UISlotZone ingredientZone = new(HoverItem, GetIngredient, SmallScale);
		private static readonly UISlotZone recipeHeaderZone = new(HoverHeader, GetHeader, SmallScale);
		private static UIText reqObjText;
		private static UIText reqObjText2;
		private static UIText storedItemsText;

		private static readonly UISlotZone storageZone = new(HoverStorage, GetStorage, SmallScale);
		private static int numRows2;
		private static int displayRows2;
		private static readonly List<Item> storageItems = new();
		private static readonly List<ItemData> blockStorageItems = new();

		private static readonly UIScrollbar storageScrollBar = new();
		private static float storageScrollBarMaxViewSize = 2f;

		private static UITextPanel<LocalizedText> craftButton;
		private static readonly ModSearchBox modSearchBox = new(RefreshItems);

		private static Item result;
		private static readonly UISlotZone resultZone = new(HoverResult, GetResult, InventoryScale);
		private static int craftTimer;
		private static int maxCraftTimer = StartMaxCraftTimer;
		private static int rightClickTimer;

		private static int maxRightClickTimer = StartMaxRightClickTimer;

		private static readonly object threadLock = new();
		private static readonly object recipeLock = new();
		private static bool threadRunning;
		private static bool threadNeedsRestart;
		private static SortMode threadSortMode;
		private static FilterMode threadFilterMode;
		private static readonly List<Recipe> threadRecipes = new();
		private static readonly List<bool> threadRecipeAvailable = new();
		private static List<Recipe> nextRecipes = new();
		private static List<bool> nextRecipeAvailable = new();

		private static Dictionary<int, List<Recipe>> _productToRecipes;
		public static bool compoundCrafting;
		public static List<Item> compoundCraftSurplus = new();

		private static bool[] adjTiles = new bool[TileLoader.TileCount];
		private static bool adjWater;
		private static bool adjLava;
		private static bool adjHoney;
		private static bool zoneSnow;
		private static bool alchemyTable;
		private static bool graveyard;
		public static bool Campfire { get; private set; }

		public static bool MouseClicked => curMouse.LeftButton == ButtonState.Pressed && oldMouse.LeftButton == ButtonState.Released;

		public static bool RightMouseClicked => curMouse.RightButton == ButtonState.Pressed && oldMouse.RightButton == ButtonState.Released;

		public static void Initialize()
		{
			lock (recipeLock)
			{
				recipes = nextRecipes;
				recipeAvailable = nextRecipeAvailable;
			}

			InitLangStuff();
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * InventoryScale;
			float itemSlotHeight = TextureAssets.InventoryBack.Value.Height * InventoryScale;
			float smallSlotWidth = TextureAssets.InventoryBack.Value.Width * SmallScale;
			float smallSlotHeight = TextureAssets.InventoryBack.Value.Height * SmallScale;

			panelTop = Main.instance.invBottom + 60;
			panelLeft = 20f;
			basePanel = new UIPanel();
			float innerPanelWidth = RecipeColumns * (itemSlotWidth + Padding) + 20f + Padding;
			panelWidth = basePanel.PaddingLeft + innerPanelWidth + basePanel.PaddingRight;
			panelHeight = Main.screenHeight - panelTop;
			basePanel.Left.Set(panelLeft, 0f);
			basePanel.Top.Set(panelTop, 0f);
			basePanel.Width.Set(panelWidth, 0f);
			basePanel.Height.Set(panelHeight, 0f);
			basePanel.Recalculate();

			recipePanel = new UIPanel();
			recipeTop = panelTop;
			recipeLeft = panelLeft + panelWidth;
			recipeWidth = IngredientColumns * (smallSlotWidth + Padding) + 20f + Padding;
			recipeWidth += recipePanel.PaddingLeft + recipePanel.PaddingRight;
			recipeHeight = panelHeight;
			recipePanel.Left.Set(recipeLeft, 0f);
			recipePanel.Top.Set(recipeTop, 0f);
			recipePanel.Width.Set(recipeWidth, 0f);
			recipePanel.Height.Set(recipeHeight, 0f);
			recipePanel.Recalculate();

			topBar = new UIElement();
			topBar.Width.Set(0f, 1f);
			topBar.Height.Set(32f, 0f);
			basePanel.Append(topBar);

			InitSortButtons();
			topBar.Append(sortButtons);
			float sortButtonsRight = sortButtons.GetDimensions().Width + Padding;
			InitRecipeButtons();
			// TODO consider shortening the search box to fix the pretty 32f gap
			//float recipeButtonsLeft = sortButtonsRight + 32f + 3 * padding; // Original
			float recipeButtonsLeft = sortButtonsRight + 3 * Padding;
			recipeButtons.Left.Set(recipeButtonsLeft, 0f);
			topBar.Append(recipeButtons);
			float recipeButtonsRight = recipeButtonsLeft + recipeButtons.GetDimensions().Width + Padding;

			searchBar.Left.Set(recipeButtonsRight + Padding, 0f);
			searchBar.Width.Set(-recipeButtonsRight - 2 * Padding, 1f);
			searchBar.Height.Set(0f, 1f);
			topBar.Append(searchBar);

			topBar2 = new UIElement();
			topBar2.Width.Set(0f, 1f);
			topBar2.Height.Set(32f, 0f);
			topBar2.Top.Set(36f, 0f);
			basePanel.Append(topBar2);

			InitFilterButtons();
			float filterButtonsRight = filterButtons.GetDimensions().Width + Padding;
			topBar2.Append(filterButtons);

			modSearchBox.Button.Left.Set(filterButtonsRight + Padding, 0f);
			modSearchBox.Button.Width.Set(-filterButtonsRight - 2 * Padding, 1f);
			modSearchBox.Button.Height.Set(0f, 1f);
			modSearchBox.Button.OverflowHidden = true;
			topBar2.Append(modSearchBox.Button);

			stationText.Top.Set(76f, 0f);
			basePanel.Append(stationText);

			stationZone.Width.Set(0f, 1f);
			stationZone.Top.Set(100f, 0f);
			int rows = GetCraftingStations().Count / TECraftingAccess.Columns + 1;
			if (rows > TECraftingAccess.Rows)
			{
				rows = TECraftingAccess.Rows;
			}
			stationZone.SetDimensions(TECraftingAccess.Columns, rows);
			stationZone.Height.Set(stationZone.getHeight(), 1f);
			basePanel.Append(stationZone);

			recipeZone.Width.Set(0f, 1f);
			recipeZone.Top.Set(100 + stationZone.getHeight(), 0f);
			recipeZone.Height.Set(-(100 + stationZone.getHeight()), 1f);
			basePanel.Append(recipeZone);

			numRows = (recipes.Count + RecipeColumns - 1) / RecipeColumns;
			displayRows = (int)recipeZone.GetDimensions().Height / ((int)itemSlotHeight + Padding);
			recipeZone.SetDimensions(RecipeColumns, displayRows);
			int noDisplayRows = numRows - displayRows;
			if (noDisplayRows < 0)
				noDisplayRows = 0;
			recipeScrollBarMaxViewSize = 1 + noDisplayRows;
			recipeScrollBar.Height.Set(displayRows * (itemSlotHeight + Padding), 0f);
			recipeScrollBar.Left.Set(-20f, 1f);
			recipeScrollBar.SetView(RecipeScrollBarViewSize, recipeScrollBarMaxViewSize);
			recipeZone.Append(recipeScrollBar);

			bottomBar.Width.Set(0f, 1f);
			bottomBar.Height.Set(32f, 0f);
			bottomBar.Top.Set(-15f, 1f);
			basePanel.Append(bottomBar);

			capacityText.Left.Set(6f, 0f);
			capacityText.Top.Set(6f, 0f);
			TEStorageHeart heart = GetHeart();
			int numItems = 0;
			int capacity = 0;
			if (heart is not null)
				foreach (TEAbstractStorageUnit abstractStorageUnit in heart.GetStorageUnits())
					if (abstractStorageUnit is TEStorageUnit storageUnit)
					{
						numItems += storageUnit.NumItems;
						capacity += storageUnit.Capacity;
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

			int itemsNeeded = selectedRecipe?.requiredItem.Count ?? IngredientColumns * 2;
			int recipeRows = itemsNeeded / IngredientColumns;
			int extraRow = itemsNeeded % IngredientColumns != 0 ? 1 : 0;
			int totalRows = recipeRows + extraRow;
			if (totalRows < 2)
				totalRows = 2;
			const float ingredientZoneTop = 54f;
			float ingredientZoneHeight = 30f * totalRows;

			ingredientZone.SetDimensions(IngredientColumns, totalRows);
			ingredientZone.Top.Set(ingredientZoneTop, 0f);
			ingredientZone.Width.Set(0f, 1f);
			ingredientZone.Height.Set(ingredientZoneHeight, 0f);
			recipePanel.Append(ingredientZone);

			float reqObjTextTop = ingredientZoneTop + ingredientZoneHeight + 11 * totalRows;
			float reqObjText2Top = reqObjTextTop + 24;

			reqObjText.Top.Set(reqObjTextTop, 0f);
			recipePanel.Append(reqObjText);
			reqObjText2.Top.Set(reqObjText2Top, 0f);
			recipePanel.Append(reqObjText2);

			int reqObjText2Rows = reqObjText2.Text.Count(c => c == '\n') + 1;
			float storedItemsTextTop = reqObjText2Top + 30 * reqObjText2Rows;
			float storageZoneTop = storedItemsTextTop + 24;
			storedItemsText.Top.Set(storedItemsTextTop, 0f);
			recipePanel.Append(storedItemsText);
			storageZone.Top.Set(storageZoneTop, 0f);
			storageZone.Width.Set(0f, 1f);
			storageZone.Height.Set(-storageZoneTop - 36, 1f);
			recipePanel.Append(storageZone);
			numRows2 = (storageItems.Count + IngredientColumns - 1) / IngredientColumns;
			displayRows2 = (int)storageZone.GetDimensions().Height / ((int)smallSlotHeight + Padding);
			storageZone.SetDimensions(IngredientColumns, displayRows2);
			int noDisplayRows2 = numRows2 - displayRows2;
			if (noDisplayRows2 < 0)
				noDisplayRows2 = 0;
			storageScrollBarMaxViewSize = 1 + noDisplayRows2;
			storageScrollBar.Height.Set(displayRows2 * (smallSlotHeight + Padding), 0f);
			storageScrollBar.Left.Set(-20f, 1f);
			storageScrollBar.SetView(ScrollBar2ViewSize, storageScrollBarMaxViewSize);
			storageZone.Append(storageScrollBar);

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
			searchBar ??= new UI.UISearchBar(Language.GetText("Mods.MagicStorage.SearchName"), RefreshItems);
			stationText ??= new UIText(Language.GetText("Mods.MagicStorage.CraftingStations"));
			capacityText ??= new UIText("Items");
			recipePanelHeader ??= new UIText(Language.GetText("Mods.MagicStorage.SelectedRecipe"));
			ingredientText ??= new UIText(Language.GetText("Mods.MagicStorage.Ingredients"));
			reqObjText ??= new UIText(Language.GetText("LegacyInterface.22"));
			reqObjText2 ??= new UIText("");
			storedItemsText ??= new UIText(Language.GetText("Mods.MagicStorage.StoredItems"));
			craftButton ??= new UITextPanel<LocalizedText>(Language.GetText("LegacyMisc.72"));
			modSearchBox.InitLangStuff();
		}

		internal static void Unload()
		{
			sortButtons = null;
			filterButtons = null;
			recipeButtons = null;
			selectedRecipe = null;
		}

		private static void InitSortButtons()
		{
			sortButtons ??= GUIHelpers.MakeSortButtons(RefreshItems);
		}

		private static void InitRecipeButtons()
		{
			if (recipeButtons == null)
			{
				recipeButtons = new UIButtonChoice(RefreshItems, new[]
				{
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/RecipeAvailable", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/RecipeAll", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/FilterMisc", AssetRequestMode.ImmediateLoad),
					MagicStorage.Instance.Assets.Request<Texture2D>("Assets/RecipeAll", AssetRequestMode.ImmediateLoad)
				}, new[]
				{
					Language.GetText("Mods.MagicStorage.RecipeAvailable"),
					Language.GetText("Mods.MagicStorage.RecipeAll"),
					Language.GetText("Mods.MagicStorage.ShowOnlyFavorited"),
					Language.GetText("Mods.MagicStorage.RecipeBlacklist")
				});
				if (MagicStorageConfig.UseConfigFilter)
					recipeButtons.Choice = MagicStorageConfig.ShowAllRecipes ? 1 : 0;
			}
		}

		private static void InitFilterButtons()
		{
			filterButtons ??= GUIHelpers.MakeFilterButtons(false, RefreshItems);
		}

		public static void Update(GameTime gameTime)
		{
			try
			{
				// TODO this needs to be in a better place. What's the hook for doing Keybinds?
				if (MagicStorage.IsItemKnownHotKey is not null &&
					PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus.ContainsKey("Is This Item Known?") &&
					MagicStorage.IsItemKnownHotKey.GetAssignedKeys().Count > 0 &&
					MagicStorage.IsItemKnownHotKey.JustPressed &&
					Main.HoverItem is not null &&
					!Main.HoverItem.IsAir)
				{
					string s = Main.HoverItem.Name + " is ";
					int t = Main.HoverItem.type;
					if (GetKnownItems().Contains(t))
					{
						s += "known";
						int sum = StoragePlayer.LocalPlayer
									  .LatestAccessedStorage?.GetStoredItems()
									  .Where(x => x.type == t)
									  .Select(x => x.stack)
									  .DefaultIfEmpty()
									  .Sum() ??
								  0;
						if (sum > 0)
							s += $" ({sum} in l.a.s.)";
					}
					else
					{
						s += "new";
					}

					Main.NewTextMultiline(s);
				}
			}
			catch (KeyNotFoundException)
			{
				// ignore
			}

			try
			{
				oldMouse = StorageGUI.oldMouse;
				curMouse = StorageGUI.curMouse;
				if (Main.playerInventory && StoragePlayer.LocalPlayer.ViewingStorage().X >= 0 && StoragePlayer.IsStorageCrafting())
				{
					if (curMouse.RightButton == ButtonState.Released)
						ResetSlotFocus();

					basePanel?.Update(gameTime);
					recipePanel?.Update(gameTime);
					UpdateRecipeText();
					UpdateScrollBar();
					UpdateCraftButton();
					modSearchBox.Update(curMouse, oldMouse);
				}
				else
				{
					//Reset the campfire bool since it could be used elswhere
					Campfire = false;

					recipeScrollBarFocus = 0;
					craftTimer = 0;
					maxCraftTimer = StartMaxCraftTimer;
					ResetSlotFocus();
				}
			}
			catch (Exception e)
			{
				Main.NewTextMultiline(e.ToString());
			}
		}

		public static void Draw()
		{
			try
			{
				Player player = Main.LocalPlayer;
				Initialize();
				if (Main.mouseX > panelLeft && Main.mouseX < recipeLeft + recipeWidth && Main.mouseY > panelTop && Main.mouseY < panelTop + panelHeight)
				{
					player.mouseInterface = true;
					player.cursorItemIconEnabled = false;
					InterfaceHelper.HideItemIconCache();
				}

				basePanel.Draw(Main.spriteBatch);
				recipePanel.Draw(Main.spriteBatch);
				Vector2 pos = recipeZone.GetDimensions().Position();
				if (threadRunning)
					Utils.DrawBorderString(Main.spriteBatch, "Loading", pos + new Vector2(8f, 8f), Color.White);
				stationZone.DrawText();
				recipeZone.DrawText();
				ingredientZone.DrawText();
				recipeHeaderZone.DrawText();
				storageZone.DrawText();
				resultZone.DrawText();
				sortButtons.DrawText();
				recipeButtons.DrawText();
				filterButtons.DrawText();
				DrawCraftButton();
			}
			catch (Exception e)
			{
				Main.NewTextMultiline(e.ToString());
			}
		}

		private static void DrawCraftButton()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(craftButton);

			if (Main.netMode == NetmodeID.SinglePlayer)
				if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
					if (selectedRecipe is not null && Main.mouseItem.IsAir && CanItemBeTakenForTest(selectedRecipe.createItem))
						Main.instance.MouseText(Language.GetText("Mods.MagicStorage.CraftTooltip").Value);
		}

		private static Item GetStation(int slot, ref int context)
		{
			List<Item> stations = GetCraftingStations();
			if (stations is not null && slot < stations.Count)
				return stations[slot];
			return new Item();
		}

		private static Item GetRecipe(int slot, ref int context)
		{
			if (threadRunning)
				return new Item();
			int index = slot + RecipeColumns * (int)Math.Round(recipeScrollBar.ViewPosition);
			Item item = index < recipes.Count ? recipes[index].createItem : new Item();
			if (!item.IsAir)
			{
				// TODO can this be nicer?
				if (recipes[index] == selectedRecipe)
					context = 6;
				if (!recipeAvailable[index])
					context = recipes[index] == selectedRecipe ? 4 : 3;
				if (StoragePlayer.LocalPlayer.FavoritedRecipes.Contains(item))
				{
					item = item.Clone();
					item.favorited = true;
				}

				if (!StoragePlayer.LocalPlayer.SeenRecipes.Contains(item))
				{
					item = item.Clone();
					item.newAndShiny = MagicStorageConfig.GlowNewItems;
				}
			}

			return item;
		}

		private static Item GetHeader(int slot, ref int context)
		{
			if (selectedRecipe == null)
				return new Item();

			Item item = selectedRecipe.createItem;
			if (item.IsAir)
				item = new Item(item.type, 0);

			return item;
		}

		private static Item GetIngredient(int slot, ref int context)
		{
			if (selectedRecipe == null || slot >= selectedRecipe.requiredItem.Count)
				return new Item();

			Item item = selectedRecipe.requiredItem[slot].Clone();
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Wood) && item.type == ItemID.Wood)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.Wood));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Sand) && item.type == ItemID.SandBlock)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.SandBlock));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.IronBar) && item.type == ItemID.IronBar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Lang.GetItemNameValue(ItemID.IronBar));
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.Fragment) && item.type == ItemID.FragmentSolar)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.51").Value);
			if (selectedRecipe.HasRecipeGroup(RecipeGroupID.PressurePlate) && item.type == ItemID.GrayPressurePlate)
				item.SetNameOverride(Language.GetText("LegacyMisc.37").Value + " " + Language.GetText("LegacyMisc.38").Value);
			if (ProcessGroupsForText(selectedRecipe, item.type, out string nameOverride))
				item.SetNameOverride(nameOverride);

			Item storageItem;
			int totalGroupStack = 0;
			lock (storageItems)
			{
				storageItem = storageItems.FirstOrDefault(i => i.type == item.type) ?? new Item();

				foreach (RecipeGroup rec in selectedRecipe.acceptedGroups.Select(index => RecipeGroup.recipeGroups[index]))
					if (rec.ValidItems.Contains(item.type))
						foreach (int type in rec.ValidItems)
							totalGroupStack += storageItems.Where(i => i.type == type).Sum(i => i.stack);
			}

			if (!item.IsAir)
			{
				if (storageItem.IsAir && totalGroupStack == 0)
					context = 3; // Unavailable - Red
				else if (storageItem.stack < item.stack && totalGroupStack < item.stack)
					context = 4; // Partially in stock - Pinkish
								 // context == 0 - Available - Default Blue
				if (context != 0)
				{
					bool craftable = _productToRecipes.TryGetValue(item.type, out List<Recipe> r) && r.Any(recipe => IsAvailable(recipe) && AmountCraftable(recipe) > 0);
					if (craftable)
						context = 6; // Craftable - Light green
				}
			}

			return item;
		}

		private static bool ProcessGroupsForText(Recipe recipe, int type, out string theText)
		{
			foreach (int num in recipe.acceptedGroups)
				if (RecipeGroup.recipeGroups[num].ContainsItem(type))
				{
					theText = RecipeGroup.recipeGroups[num].GetText();
					return true;
				}

			theText = "";
			return false;
		}

		// Calculates how many times a recipe can be crafted using available items
		// TODO is this correct?
		private static int AmountCraftable(Recipe recipe)
		{
			if (!IsAvailable(recipe))
				return 0;
			int maxCraftable = int.MaxValue;

			if (RecursiveCraftIntegration.Enabled)
				recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			int GetAmountCraftable(Item requiredItem)
			{
				int total = 0;
				lock (items)
				{
					foreach (Item inventoryItem in items)
						if (inventoryItem.type == requiredItem.type || RecipeGroupMatch(recipe, inventoryItem.type, requiredItem.type))
							total += inventoryItem.stack;
				}

				int craftable = total / requiredItem.stack;
				return craftable;
			}

			maxCraftable = recipe.requiredItem.Select(GetAmountCraftable).Prepend(maxCraftable).Min();

			return maxCraftable;
		}

		private static Item GetStorage(int slot, ref int context)
		{
			int index = slot + IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);
			Item item = index < storageItems.Count ? storageItems[index] : new Item();
			lock (blockStorageItems)
			{
				if (blockStorageItems.Contains(new ItemData(item)))
					context = 3;
			}

			return item;
		}

		private static Item GetResult(int slot, ref int context) => slot == 0 && result is not null ? result : new Item();

		private static void UpdateRecipeText()
		{
			if (selectedRecipe == null)
			{
				reqObjText2.SetText("");
				recipePanelHeader.SetText(Language.GetText("Mods.MagicStorage.SelectedRecipe").Value);
			}
			else
			{
				bool isEmpty = true;
				string text = "";
				int rows = 0;

				void AddText(string s)
				{
					if (!isEmpty)
						text += ", ";

					if ((text.Length + s.Length) / 35 > rows)
					{
						text += "\n";
						++rows;
					}

					text += s;
					isEmpty = false;
				}

				foreach (int tile in selectedRecipe.requiredTile)
					AddText(Lang.GetMapObjectName(MapHelper.TileToLookup(tile, 0)));

				if (selectedRecipe.HasCondition(Recipe.Condition.NearWater))
					AddText(Language.GetTextValue("LegacyInterface.53"));

				if (selectedRecipe.HasCondition(Recipe.Condition.NearHoney))
					AddText(Language.GetTextValue("LegacyInterface.58"));

				if (selectedRecipe.HasCondition(Recipe.Condition.NearLava))
					AddText(Language.GetTextValue("LegacyInterface.56"));

				if (selectedRecipe.HasCondition(Recipe.Condition.InSnow))
					AddText(Language.GetTextValue("LegacyInterface.123"));

				if (selectedRecipe.HasCondition(Recipe.Condition.InGraveyardBiome))
					AddText(Language.GetTextValue("LegacyInterface.124"));

				if (isEmpty)
					text = Language.GetTextValue("LegacyInterface.23");

				reqObjText2.SetText(text);

				double dps = CompareDps.GetDps(selectedRecipe.createItem);
				string dpsText = dps >= 1d ? $"DPS = {dps:F}" : string.Empty;

				recipePanelHeader.SetText(dpsText);
			}
		}

		private static void UpdateScrollBar()
		{
			if (slotFocus)
			{
				recipeScrollBarFocus = 0;
				return;
			}

			Rectangle dim = recipeScrollBar.GetClippingRectangle(Main.spriteBatch);
			Vector2 boxPos = new(dim.X, dim.Y + dim.Height * (recipeScrollBar.ViewPosition / recipeScrollBarMaxViewSize));
			float boxWidth = 20f * Main.UIScale;
			float boxHeight = dim.Height * (RecipeScrollBarViewSize / recipeScrollBarMaxViewSize);
			Rectangle dim2 = storageScrollBar.GetClippingRectangle(Main.spriteBatch);
			Vector2 box2Pos = new(dim2.X, dim2.Y + dim2.Height * (storageScrollBar.ViewPosition / storageScrollBarMaxViewSize));
			float box2Height = dim2.Height * (ScrollBar2ViewSize / storageScrollBarMaxViewSize);
			if (recipeScrollBarFocus > 0)
			{
				if (curMouse.LeftButton == ButtonState.Released)
				{
					recipeScrollBarFocus = 0;
				}
				else
				{
					int difference = curMouse.Y - recipeScrollBarFocusMouseStart;
					if (recipeScrollBarFocus == 1)
						recipeScrollBar.ViewPosition = recipeScrollBarFocusPositionStart + difference / boxHeight;
					else if (recipeScrollBarFocus == 2)
						storageScrollBar.ViewPosition = recipeScrollBarFocusPositionStart + difference / box2Height;
				}
			}
			else if (MouseClicked)
			{
				if (curMouse.X > boxPos.X && curMouse.X < boxPos.X + boxWidth && curMouse.Y > boxPos.Y - 3f && curMouse.Y < boxPos.Y + boxHeight + 4f)
				{
					recipeScrollBarFocus = 1;
					recipeScrollBarFocusMouseStart = curMouse.Y;
					recipeScrollBarFocusPositionStart = recipeScrollBar.ViewPosition;
				}
				else if (curMouse.X > box2Pos.X && curMouse.X < box2Pos.X + boxWidth && curMouse.Y > box2Pos.Y - 3f && curMouse.Y < box2Pos.Y + box2Height + 4f)
				{
					recipeScrollBarFocus = 2;
					recipeScrollBarFocusMouseStart = curMouse.Y;
					recipeScrollBarFocusPositionStart = storageScrollBar.ViewPosition;
				}
			}

			if (recipeScrollBarFocus == 0)
			{
				int difference = oldMouse.ScrollWheelValue / 250 - curMouse.ScrollWheelValue / 250;
				recipeScrollBar.ViewPosition += difference;
			}
		}

		private static void UpdateCraftButton()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(craftButton);
			bool stillCrafting = false;
			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
			{
				craftButton.BackgroundColor = new Color(73, 94, 171);
				//The "Test Item" feature is very cheaty and I don't want that in Magic Storage.
				//However, just deleting the code would be a waste, so it's commented out instead.
				// - absoluteAquarian
				// TODO: make this a config option
				/*
				if (RightMouseClicked && selectedRecipe is not null && Main.mouseItem.IsAir)
				{
					Item item = selectedRecipe.createItem;
					if (CanItemBeTakenForTest(item))
					{
						int type = item.type;
						Item testItem = new();
						testItem.SetDefaults(type, true);
						MarkAsTestItem(testItem);
						Main.mouseItem = testItem;
						StoragePlayer.LocalPlayer.TestedRecipes.Add(selectedRecipe.createItem);
					}
				}
				else */
				if (curMouse.LeftButton == ButtonState.Pressed && selectedRecipe is not null && IsAvailable(selectedRecipe, false) && PassesBlock(selectedRecipe))
				{
					if (craftTimer <= 0)
					{
						craftTimer = maxCraftTimer;
						maxCraftTimer = maxCraftTimer * 3 / 4;
						if (maxCraftTimer <= 0)

							maxCraftTimer = 1;

						TryCraft();
						if (RecursiveCraftIntegration.Enabled)
						{
							RecursiveCraftIntegration.RefreshRecursiveRecipes();
							if (RecursiveCraftIntegration.HasCompoundVariant(selectedRecipe))
								SetSelectedRecipe(selectedRecipe);
						}

						RefreshItems();
						SoundEngine.PlaySound(SoundID.Grab);
					}

					craftTimer--;
					stillCrafting = true;
					if (StoragePlayer.LocalPlayer.AddToCraftedRecipes(selectedRecipe.createItem))
						RefreshItems();
				}
			}

			else
			{
				craftButton.BackgroundColor = new Color(63, 82, 151) * 0.7f;
			}

			if (selectedRecipe == null || !IsAvailable(selectedRecipe, false) || !PassesBlock(selectedRecipe))

				craftButton.BackgroundColor = new Color(30, 40, 100) * 0.7f;

			if (!stillCrafting)
			{
				craftTimer = 0;
				maxCraftTimer = StartMaxCraftTimer;
			}
		}

		private static bool CanItemBeTakenForTest(Item item) =>
			Main.netMode == NetmodeID.SinglePlayer &&
			!item.consumable &&
			(item.mana > 0 ||
			 item.CountsAsClass(DamageClass.Magic) ||
			 item.CountsAsClass(DamageClass.Ranged) ||
			 item.CountsAsClass(DamageClass.Throwing) ||
			 item.CountsAsClass(DamageClass.Melee) ||
			 item.headSlot >= 0 ||
			 item.bodySlot >= 0 ||
			 item.legSlot >= 0 ||
			 item.accessory ||
			 Main.projHook[item.shoot] ||
			 item.pick > 0 ||
			 item.axe > 0 ||
			 item.hammer > 0) &&
			!item.CountsAsClass(DamageClass.Summon) &&
			item.createTile < TileID.Dirt &&
			item.createWall < 0 &&
			!item.potion &&
			item.fishingPole <= 1 &&
			item.ammo == AmmoID.None &&
			!StoragePlayer.LocalPlayer.TestedRecipes.Contains(item);

		public static void MarkAsTestItem(Item testItem)
		{
			testItem.value = 0;
			testItem.shopCustomPrice = 0;
			testItem.material = false;
			testItem.rare = -11;
			testItem.SetNameOverride(Lang.GetItemNameValue(testItem.type) + Language.GetTextValue("Mods.MagicStorage.TestItemSuffix"));
		}

		public static bool IsTestItem(Item item) => item.Name.EndsWith(Language.GetTextValue("Mods.MagicStorage.TestItemSuffix"));


		private static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		private static TECraftingAccess GetCraftingEntity() => StoragePlayer.LocalPlayer.GetCraftingAccess();

		private static List<Item> GetCraftingStations() => GetCraftingEntity()?.stations;

		public static void RefreshItems()
		{
			StoragePlayer modPlayer = StoragePlayer.LocalPlayer;
			if (modPlayer.SeenRecipes.Count == 0)
				foreach (int item in GetKnownItems())
					modPlayer.SeenRecipes.Add(item);

			lock (items)
			{
				items.Clear();
				TEStorageHeart heart = GetHeart();
				if (heart == null)
					return;

				items.AddRange(ItemSorter.SortAndFilter(heart.GetStoredItems(), SortMode.Id, FilterMode.All, ModSearchBox.ModIndexAll, ""));
			}

			AnalyzeIngredients();
			InitLangStuff();
			InitSortButtons();
			InitRecipeButtons();
			InitFilterButtons();

			RefreshStorageItems();

			GetKnownItems(out HashSet<int> foundItems, out HashSet<int> hiddenRecipes, out HashSet<int> craftedRecipes, out HashSet<int> asKnownRecipes);
			foundItems.UnionWith(asKnownRecipes);

			var favoritesCopy = new HashSet<int>(modPlayer.FavoritedRecipes.Items.Select(x => x.type));

			EnsureProductToRecipesInited();
			threadRecipes.Clear();
			lock (threadLock)
			{
				threadNeedsRestart = true;
				threadSortMode = (SortMode)sortButtons.Choice;
				threadFilterMode = (FilterMode)filterButtons.Choice;
				threadCheckListFoundItems = foundItems;
				if (!threadRunning)
				{
					threadRunning = true;
					RefreshRecipes(hiddenRecipes, craftedRecipes, favoritesCopy);
				}
			}
		}

		public static HashSet<int> GetKnownItems()
		{
			GetKnownItems(out HashSet<int> a, out HashSet<int> b, out HashSet<int> c, out HashSet<int> d);
			a.UnionWith(b);
			a.UnionWith(c);
			a.UnionWith(d);
			return a;
		}

		private static void GetKnownItems(out HashSet<int> foundItems, out HashSet<int> hiddenRecipes, out HashSet<int> craftedRecipes, out HashSet<int> asKnownRecipes)
		{
			foundItems = new HashSet<int>(RetrieveFoundItemsCheckList());

			StoragePlayer modPlayer = StoragePlayer.LocalPlayer;
			hiddenRecipes = new HashSet<int>(modPlayer.HiddenRecipes.Select(x => x.type));
			craftedRecipes = new HashSet<int>(modPlayer.CraftedRecipes.Select(x => x.type));
			asKnownRecipes = new HashSet<int>(modPlayer.AsKnownRecipes.Items.Select(x => x.type));
		}

		private static IEnumerable<int> RetrieveFoundItemsCheckList()
		{
			if (itemChecklistMod is null)
				ModLoader.TryGetMod("ItemChecklist", out itemChecklistMod);

			object response = itemChecklistMod?.Call("RequestFoundItems");

			if (response is bool[] { Length: > 0 } found)
			{
				wasItemChecklistRetrieved = true;
				return found.Select((wasFound, type) => (found: wasFound, type)).Where(x => x.found).Select(x => x.type);
			}

			return Array.Empty<int>();
		}

		private static void EnsureProductToRecipesInited()
		{
			if (_productToRecipes is not null)
				return;

			IEnumerable<Recipe> allRecipes = ItemSorter.GetRecipes(SortMode.Id, FilterMode.All, ModSearchBox.ModIndexAll, "")
				.Where(x => x.createItem.type > ItemID.None);
			_productToRecipes = allRecipes.GroupBy(x => x.createItem.type).ToDictionary(x => x.Key, x => x.ToList());
		}

		/// <summary>
		///     Checks all crafting tree until it finds already available ingredients
		/// </summary>
		private static bool IsKnownRecursively(Recipe recipe, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache)
		{
			if (cache.TryGetValue(recipe, out bool v))
				return v;

			foreach (int tile in recipe.requiredTile)
			{
				if (!StorageWorld.TileToCreatingItem.TryGetValue(tile, out List<int> possibleItems))
					continue;

				if (!possibleItems.Any(x => CheckIngredient(x, availableSet, recursionTree, cache)))
				{
					cache[recipe] = false;
					return false;
				}
			}

			int ingredients = 0;
			foreach (Item item in recipe.requiredItem)
			{
				ingredients++;
				if (CheckIngredient(item.type, availableSet, recursionTree, cache))
					continue;
				if (CheckAcceptedGroupsForIngredient(recipe, availableSet, recursionTree, cache, item.type))
					continue;
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

			#region Check Functions

			static bool CheckIngredient(int t, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache)
			{
				if (availableSet.Contains(t))
					return true;
				if (!recursionTree.Add(t))
					return false;
				try
				{
					if (!_productToRecipes.TryGetValue(t, out List<Recipe> ingredientRecipes))
						return false;
					if (ingredientRecipes.Count == 0 || ingredientRecipes.All(x => !IsKnownRecursively(x, availableSet, recursionTree, cache)))
						return false;
				}
				finally
				{
					recursionTree.Remove(t);
				}

				return true;
			}

			static bool CheckAcceptedGroupsForIngredient(Recipe recipe, HashSet<int> availableSet, HashSet<int> recursionTree, Dictionary<Recipe, bool> cache, int t)
			{
				foreach (RecipeGroup g in recipe.acceptedGroups.Select(j => RecipeGroup.recipeGroups[j]))
					if (g.ContainsItem(t))
						foreach (int groupItemType in g.ValidItems)
							if (groupItemType != t && CheckIngredient(groupItemType, availableSet, recursionTree, cache))
								return true;

				return false;
			}

			#endregion
		}

		private static void RefreshRecipes(HashSet<int> hiddenRecipes, HashSet<int> craftedRecipes, HashSet<int> favorited)
		{
			while (true)
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

					var availableItemsMutable = new HashSet<int>(hiddenRecipes.Concat(craftedRecipes).Concat(foundItems));

					var temp = new HashSet<int>();
					var tempCache = new Dictionary<Recipe, bool>();

					int modFilterIndex = modSearchBox.ModIndex;

					void DoFiltering()
					{
						var filteredRecipes = ItemSorter.GetRecipes(sortMode, filterMode, modFilterIndex, searchBar.Text)
							// show only blacklisted recipes only if choice = 2, otherwise show all other
							.Where(x => recipeButtons.Choice == RecipeButtonsBlacklistChoice == hiddenRecipes.Contains(x.createItem.type))
							// show only favorited items if selected
							.Where(x => recipeButtons.Choice != RecipeButtonsFavoritesChoice || favorited.Contains(x.createItem.type))
							// hard check if this item can be crafted from available items and their recursive products
							.Where(x => !wasItemChecklistRetrieved || IsKnownRecursively(x, availableItemsMutable, temp, tempCache))
							// favorites first
							.OrderBy(x => favorited.Contains(x.createItem.type) ? 0 : 1);

						threadRecipes.Clear();
						threadRecipeAvailable.Clear();

						if (recipeButtons.Choice == RecipeButtonsAvailableChoice)
						{
							threadRecipes.AddRange(filteredRecipes.Where(r => IsAvailable(r)));
							threadRecipeAvailable.AddRange(Enumerable.Repeat(true, threadRecipes.Count));
						}
						else
						{
							threadRecipes.AddRange(filteredRecipes);
							threadRecipeAvailable.AddRange(threadRecipes.AsParallel().AsOrdered().Select(r => IsAvailable(r)));
						}
					}

					if (RecursiveCraftIntegration.Enabled)
						RecursiveCraftIntegration.RefreshRecursiveRecipes();

					DoFiltering();

					// now if nothing found we disable filters one by one
					if (searchBar.Text.Length > 0)
					{
						if (threadRecipes.Count == 0 && hiddenRecipes.Count > 0)
						{
							// search hidden recipes too
							hiddenRecipes = new HashSet<int>();
							DoFiltering();
						}

						//if (threadRecipes.Count == 0 && filterMode != FilterMode.All) {
						//	// any category
						//	filterMode = FilterMode.All;
						//	DoFiltering();
						//}

						if (threadRecipes.Count == 0 && modFilterIndex != ModSearchBox.ModIndexAll)
						{
							// search all mods
							modFilterIndex = ModSearchBox.ModIndexAll;
							DoFiltering();
						}
					}

					// TODO is there a better way?
					void GuttedSetSelectedRecipe(Recipe recipe, int index)
					{
						Recipe compound = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);
						if (index != -1)
							threadRecipes[index] = compound;

						selectedRecipe = compound;
						RefreshStorageItems();
						lock (blockStorageItems)
						{
							blockStorageItems.Clear();
						}
					}

					lock (recipeLock)
					{
						if (RecursiveCraftIntegration.Enabled)
							if (selectedRecipe is not null)
							{
								// If the selected recipe is compound, replace the overridden recipe with the compound one so it shows as selected in the UI
								if (RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe))
								{
									Recipe overridden = RecursiveCraftIntegration.GetOverriddenRecipe(selectedRecipe);
									int index = threadRecipes.IndexOf(overridden);
									if (index != -1 && threadRecipeAvailable[index])
										GuttedSetSelectedRecipe(overridden, index);
									else
										GuttedSetSelectedRecipe(overridden, index);
								}
								// If the selectedRecipe(which isn't compound) is uncraftable but is in the available list, this means it's compound version is craftable
								else if (!IsAvailable(selectedRecipe, false))
								{
									int index = threadRecipes.IndexOf(selectedRecipe);
									if (index != -1 && threadRecipeAvailable[index])
										GuttedSetSelectedRecipe(selectedRecipe, index);
								}
							}

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
				catch (Exception e)
				{
					Main.NewTextMultiline(e.ToString());
				}
		}

		private static void AnalyzeIngredients()
		{
			Player player = Main.LocalPlayer;
			if (adjTiles.Length != player.adjTile.Length)
				Array.Resize(ref adjTiles, player.adjTile.Length);

			Array.Clear(adjTiles, 0, adjTiles.Length);
			adjWater = false;
			adjLava = false;
			adjHoney = false;
			zoneSnow = false;
			alchemyTable = false;
			graveyard = false;
			Campfire = false;

			lock (itemCounts)
			{
				itemCounts.Clear();
				foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.Sum())))
					itemCounts[type] = amount;
			}

			foreach (Item item in GetCraftingStations())
			{
				if (item.IsAir)
					continue;

				if (item.createTile >= TileID.Dirt)
				{
					adjTiles[item.createTile] = true;
					switch (item.createTile)
					{
						case TileID.GlassKiln:
						case TileID.Hellforge:
							adjTiles[TileID.Furnaces] = true;
							break;
						case TileID.AdamantiteForge:
							adjTiles[TileID.Furnaces] = true;
							adjTiles[TileID.Hellforge] = true;
							break;
						case TileID.MythrilAnvil:
							adjTiles[TileID.Anvils] = true;
							break;
						case TileID.BewitchingTable:
						case TileID.Tables2:
							adjTiles[TileID.Tables] = true;
							break;
						case TileID.AlchemyTable:
							adjTiles[TileID.Bottles] = true;
							adjTiles[TileID.Tables] = true;
							alchemyTable = true;
							break;
					}

					if (item.createTile == TileID.Tombstones)
					{
						adjTiles[TileID.Tombstones] = true;
						graveyard = true;
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

					if (player.adjTile[TileID.WorkBenches] || player.adjTile[TileID.Tables] || player.adjTile[TileID.Tables2])
						player.adjTile[TileID.Chairs] = true;
					if (player.adjWater || TileID.Sets.CountsAsWaterSource[item.createTile])
						adjWater = true;
					if (player.adjLava || TileID.Sets.CountsAsLavaSource[item.createTile])
						adjLava = true;
					if (player.adjHoney || TileID.Sets.CountsAsHoneySource[item.createTile])
						adjHoney = true;
					if (player.alchemyTable || player.adjTile[TileID.AlchemyTable])
						alchemyTable = true;
					if (player.adjTile[TileID.Tombstones])
						graveyard = true;

					player.adjTile = oldAdjTile;
					player.adjWater = oldAdjWater;
					player.adjLava = oldAdjLava;
					player.adjHoney = oldAdjHoney;
					player.alchemyTable = oldAlchemyTable;
				}

				switch (item.type)
				{
					case ItemID.WaterBucket:
					case ItemID.BottomlessBucket:
						adjWater = true;
						break;
					case ItemID.LavaBucket:
						adjLava = true;
						break;
					case ItemID.HoneyBucket:
						adjHoney = true;
						break;
				}
				if (item.type == ModContent.ItemType<SnowBiomeEmulator>() || item.type == ModContent.ItemType<BiomeGlobe>())
				{
					zoneSnow = true;
				}

				if (item.type == ModContent.ItemType<BiomeGlobe>())
				{
					graveyard = true;
					Campfire = true;
					adjWater = true;
					adjLava = true;
					adjHoney = true;

					adjTiles[TileID.Campfire] = true;
					adjTiles[TileID.DemonAltar] = true;
				}
			}
			adjTiles[ModContent.TileType<Components.CraftingAccess>()] = true;
		}

		public static bool IsAvailable(Recipe recipe, bool checkCompound = true)
		{
			if (RecursiveCraftIntegration.Enabled && checkCompound)
				recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			lock (itemCounts)
			{
				foreach (Item ingredient in recipe.requiredItem)
				{
					int stack = ingredient.stack;
					bool useRecipeGroup = false;
					foreach (int type in itemCounts.Keys)
						if (RecipeGroupMatch(recipe, type, ingredient.type))
						{
							stack -= itemCounts[type];
							useRecipeGroup = true;
						}

					if (!useRecipeGroup && itemCounts.TryGetValue(ingredient.type, out int amount))
						stack -= amount;
					if (stack > 0)
						return false;
				}
			}


			bool retValue = true;

			ExecuteInCraftingGuiEnvironment(() =>
			{
				if (retValue && !RecipeLoader.RecipeAvailable(recipe))
					retValue = false;
			});

			return retValue;
		}

		public static void ExecuteInCraftingGuiEnvironment(Action action)
		{
			ArgumentNullException.ThrowIfNull(action);

			lock (BlockRecipes.ActiveLock)
			{
				Player player = Main.LocalPlayer;
				bool[] origAdjTile = player.adjTile;

				try
				{
					player.adjTile = adjTiles;

					// TODO: test if this allows environmental effects such as nearby water
					if (adjWater)
						player.adjWater = true;
					if (adjLava)
						player.adjLava = true;
					if (adjHoney)
						player.adjHoney = true;
					if (alchemyTable)
						player.alchemyTable = true;
					if (zoneSnow)
						player.ZoneSnow = true;
					if (graveyard)
						player.ZoneGraveyard = true;

					BlockRecipes.Active = false;
					action();
				}
				finally
				{
					BlockRecipes.Active = true;
					player.adjTile = origAdjTile;
				}
			}
		}

		private static bool PassesBlock(Recipe recipe)
		{
			foreach (Item ingredient in recipe.requiredItem)
			{
				int stack = ingredient.stack;
				bool useRecipeGroup = false;
				lock (storageItems)
				{
					lock (blockStorageItems)
					{
						foreach (Item item in storageItems)
						{
							ItemData data = new(item);
							if (!blockStorageItems.Contains(data) && RecipeGroupMatch(recipe, item.type, ingredient.type))
							{
								stack -= item.stack;
								useRecipeGroup = true;
							}
						}

						if (!useRecipeGroup)
							foreach (Item item in storageItems)
							{
								ItemData data = new(item);
								if (!blockStorageItems.Contains(data) && item.type == ingredient.type)
									stack -= item.stack;
							}
					}
				}

				if (stack > 0)
					return false;
			}


			return true;
		}

		private static void RefreshStorageItems()
		{
			lock (storageItems)
			{
				storageItems.Clear();
				result = null;
				if (selectedRecipe is not null)
				{
					lock (items)
					{
						foreach (Item item in items)
						{
							foreach (Item reqItem in selectedRecipe.requiredItem)
								if (item.type == reqItem.type || RecipeGroupMatch(selectedRecipe, item.type, reqItem.type))
									storageItems.Add(item);

							if (item.type == selectedRecipe.createItem.type)
								result = item;
						}
					}

					result ??= new Item(selectedRecipe.createItem.type, 0);
				}
			}
		}

		private static bool RecipeGroupMatch(Recipe recipe, int inventoryType, int requiredType)
		{
			foreach (int num in recipe.acceptedGroups)
			{
				RecipeGroup recipeGroup = RecipeGroup.recipeGroups[num];
				if (recipeGroup.ContainsItem(inventoryType) && recipeGroup.ContainsItem(requiredType))
					return true;
			}

			return false;

			//return recipe.useWood(type1, type2) || recipe.useSand(type1, type2) || recipe.useIronBar(type1, type2) || recipe.useFragment(type1, type2) || recipe.AcceptedByItemGroups(type1, type2) || recipe.usePressurePlate(type1, type2);
		}

		private static void HoverStation(int slot, ref int hoverSlot)
		{
			TECraftingAccess access = GetCraftingEntity();
			if (access == null || slot >= TECraftingAccess.ItemsTotal)
				return;

			Player player = Main.LocalPlayer;
			if (MouseClicked)
			{
				bool changed = false;
				if (slot < access.stations.Count && ItemSlot.ShiftInUse)
				{
					access.TryWithdrawStation(slot, true);
					changed = true;
				}
				else if (player.itemAnimation == 0 && player.itemTime == 0)
				{
					if (Main.mouseItem.IsAir)
					{
						if (!access.TryWithdrawStation(slot).IsAir)
						{
							changed = true;
						}
					}
					else
					{
						int oldType = Main.mouseItem.type;
						int oldStack = Main.mouseItem.stack;
						Main.mouseItem = access.TryDepositStation(Main.mouseItem);
						if (oldType != Main.mouseItem.type || oldStack != Main.mouseItem.stack)
							changed = true;
					}
				}

				if (changed)
				{
					RefreshItems();
					SoundEngine.PlaySound(SoundID.Grab);
				}
			}

			hoverSlot = slot;
		}

		private static void HoverRecipe(int slot, ref int hoverSlot)
		{
			int visualSlot = slot;
			slot += RecipeColumns * (int)Math.Round(recipeScrollBar.ViewPosition);
			if (slot < recipes.Count)
			{
				Recipe recipe = recipes[slot];
				StoragePlayer storagePlayer = StoragePlayer.LocalPlayer;
				if (MouseClicked)
				{
					if (Main.keyState.IsKeyDown(Keys.LeftAlt))
					{
						if (!storagePlayer.FavoritedRecipes.Add(recipe.createItem))
							storagePlayer.FavoritedRecipes.Remove(recipe.createItem);
					}
					else if (Main.keyState.IsKeyDown(Keys.LeftControl))
					{
						if (recipeButtons.Choice == RecipeButtonsBlacklistChoice)
						{
							if (storagePlayer.RemoveFromHiddenRecipes(recipe.createItem))
								RefreshItems();
						}
						else
						{
							if (storagePlayer.AddToHiddenRecipes(recipe.createItem))
								RefreshItems();
						}
					}
					else
					{
						SetSelectedRecipe(recipe);
					}
				}
				else if (RightMouseClicked)
				{
					if (recipe == selectedRecipe || recipeButtons.Choice != RecipeButtonsAvailableChoice)
					{
						if (recipeButtons.Choice == RecipeButtonsAvailableChoice)
						{
							storagePlayer.AsKnownRecipes.Add(recipe.createItem);
							RefreshItems();
						}
						else
						{
							storagePlayer.AsKnownRecipes.Remove(recipe.createItem);
						}
					}
				}

				hoverSlot = visualSlot;
			}
		}

		private static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

			StoragePlayer.LocalPlayer.SeenRecipes.Add(recipe.createItem);

			if (RecursiveCraftIntegration.Enabled)
			{
				int index;
				if (selectedRecipe != null && RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe) && selectedRecipe != recipe)
				{
					Recipe overridden = RecursiveCraftIntegration.GetOverriddenRecipe(selectedRecipe);
					if (overridden != recipe)
					{
						index = recipes.IndexOf(selectedRecipe);
						if (index != -1)
							recipes[index] = overridden;
					}
				}

				index = recipes.IndexOf(recipe);
				if (index != -1)
				{
					recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);
					recipes[index] = recipe;
				}
			}

			selectedRecipe = recipe;
			RefreshStorageItems();
			lock (blockStorageItems)
			{
				blockStorageItems.Clear();
			}
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
			slot += IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);

			if (slot >= selectedRecipe.requiredItem.Count)
				return;

			// select ingredient recipe by right clicking
			if (RightMouseClicked)
			{
				Item item = selectedRecipe.requiredItem[slot];
				EnsureProductToRecipesInited();
				if (_productToRecipes.TryGetValue(item.type, out List<Recipe> itemRecipes))
				{
					HashSet<int> knownItems = GetKnownItems();

					var recursionTree = new HashSet<int>();
					var cache = new Dictionary<Recipe, bool>();

					Recipe selected = null;

					foreach (Recipe r in itemRecipes)
					{
						if (!IsKnownRecursively(r, knownItems, recursionTree, cache))
							continue;

						selected ??= r;
						if (IsAvailable(r))
						{
							selected = r;
							break;
						}
					}

					if (selected is not null)
						SetSelectedRecipe(selected);
				}
			}

			hoverSlot = visualSlot;
		}

		private static void HoverStorage(int slot, ref int hoverSlot)
		{
			int visualSlot = slot;
			slot += IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);
			if (slot >= storageItems.Count)
				return;

			Item item = storageItems[slot];
			item.newAndShiny = false;
			if (MouseClicked)
			{
				ItemData data = new(item);
				lock (blockStorageItems)
				{
					if (blockStorageItems.Contains(data))
						blockStorageItems.Remove(data);
					else
						blockStorageItems.Add(data);
				}
			}

			hoverSlot = visualSlot;
		}

		private static void HoverResult(int slot, ref int hoverSlot)
		{
			if (slot != 0)
				return;

			if (Main.mouseItem.IsAir && result is not null && !result.IsAir)
				result.newAndShiny = false;

			Player player = Main.LocalPlayer;
			if (MouseClicked)
			{
				bool changed = false;
				if (!Main.mouseItem.IsAir && player.itemAnimation == 0 && player.itemTime == 0 && result is not null && Main.mouseItem.type == result.type)
				{
					if (TryDepositResult(Main.mouseItem))
						changed = true;
				}
				else if (Main.mouseItem.IsAir && result is not null && !result.IsAir)
				{
					if (Main.keyState.IsKeyDown(Keys.LeftAlt))
					{
						result.favorited = !result.favorited;
					}
					else
					{
						Item toWithdraw = result.Clone();
						if (toWithdraw.stack > toWithdraw.maxStack)
							toWithdraw.stack = toWithdraw.maxStack;
						Main.mouseItem = DoWithdrawResult(toWithdraw, ItemSlot.ShiftInUse);
						if (ItemSlot.ShiftInUse)
							Main.mouseItem = player.GetItem(Main.myPlayer, Main.mouseItem, GetItemSettings.InventoryEntityToPlayerInventorySettings);
						changed = true;
					}
				}

				if (changed)
				{
					RefreshItems();
					SoundEngine.PlaySound(SoundID.Grab);
				}
			}

			if (RightMouseClicked &&
				result is not null &&
				!result.IsAir &&
				(Main.mouseItem.IsAir || ItemData.Matches(Main.mouseItem, result) && Main.mouseItem.stack < Main.mouseItem.maxStack))
				slotFocus = true;

			hoverSlot = slot;

			if (slotFocus)
				SlotFocusLogic();
		}

		private static void SlotFocusLogic()
		{
			if (result == null || result.IsAir || !Main.mouseItem.IsAir && (!ItemData.Matches(Main.mouseItem, result) || Main.mouseItem.stack >= Main.mouseItem.maxStack))
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
						maxRightClickTimer = 1;
					Item toWithdraw = result.Clone();
					toWithdraw.stack = 1;
					Item withdrawn = DoWithdrawResult(toWithdraw);
					if (Main.mouseItem.IsAir)
						Main.mouseItem = withdrawn;
					else
						Main.mouseItem.stack += withdrawn.stack;
					SoundEngine.PlaySound(SoundID.MenuTick);
					RefreshItems();
				}

				rightClickTimer--;
			}
		}

		private static void ResetSlotFocus()
		{
			slotFocus = false;
			rightClickTimer = 0;
			maxRightClickTimer = StartMaxRightClickTimer;
		}

		private static void TryCraft()
		{
			List<Item> availableItems;
			var toWithdraw = new List<Item>();

			lock (storageItems)
				lock (blockStorageItems)
				{
					availableItems = storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).Select(item => item.Clone()).ToList();
				}

			foreach (Item reqItem in selectedRecipe.requiredItem)
			{
				int stack = reqItem.stack;

				RecipeLoader.ConsumeItem(selectedRecipe, reqItem.type, ref stack);

				if (stack <= 0)
					continue;

				foreach (Item tryItem in availableItems)
					if (reqItem.type == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqItem.type))
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
							tryItem.type = ItemID.None;
						}
					}
			}

			Item resultItem = selectedRecipe.createItem.Clone();
			resultItem.Prefix(-1);
			var resultItems = new List<Item> { resultItem };

			bool isCompound = RecursiveCraftIntegration.Enabled && RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe);
			if (isCompound)
			{
				compoundCrafting = true;
				compoundCraftSurplus.Clear();
			}

			RecipeLoader.OnCraft(resultItem, selectedRecipe);
			ItemLoader.OnCreate(resultItem, new RecipeCreationContext { recipe = selectedRecipe });

			if (isCompound)
			{
				compoundCrafting = false;
				resultItems.AddRange(compoundCraftSurplus);
			}

			if (Main.netMode == NetmodeID.SinglePlayer)
				foreach (Item item in DoCraft(GetHeart(), toWithdraw, resultItems))
					Main.LocalPlayer.QuickSpawnClonedItem(new EntitySource_TileEntity(GetHeart()), item, item.stack);
			else if (Main.netMode == NetmodeID.MultiplayerClient)
				NetHelper.SendCraftRequest(GetHeart().ID, toWithdraw, resultItems);
		}

		internal static List<Item> DoCraft(TEStorageHeart heart, List<Item> toWithdraw, List<Item> results)
		{
			var items = new List<Item>();
			foreach (Item tryWithdraw in toWithdraw)
			{
				Item withdrawn = heart.TryWithdraw(tryWithdraw, false);
				if (!withdrawn.IsAir)
					items.Add(withdrawn);
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
			foreach (Item result in results)
			{
				heart.DepositItem(result);
				if (!result.IsAir)
					items.Add(result);
			}

			return items;
		}

		private static bool TryDepositResult(Item item)
		{
			int oldStack = item.stack;
			TEStorageHeart heart = GetHeart();
			heart.TryDeposit(item);
			return oldStack != item.stack;
		}

		private static Item DoWithdrawResult(Item item, bool toInventory = false)
		{
			TEStorageHeart heart = GetHeart();
			return heart.TryWithdraw(item, false);
		}
	}
}
