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

		public static int craftAmountTarget;
		private static UITextPanel<LocalizedText> craftButton;
		private static UITextPanel<LocalizedText> craftP1, craftP10, craftP100, craftM1, craftM10, craftM100, craftMax, craftReset;
		private static UIText craftAmount;
		private static readonly ModSearchBox modSearchBox = new(RefreshItems);

		private static Item result;
		private static readonly UISlotZone resultZone = new(HoverResult, GetResult, InventoryScale);
		private static int craftTimer;
		private static int maxCraftTimer = StartMaxCraftTimer;
		private static int rightClickTimer;

		private static int maxRightClickTimer = StartMaxRightClickTimer;

		public static bool CatchDroppedItems;
		public static List<Item> DroppedItems = new();

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

			capacityText.SetText(Language.GetTextValue("Mods.MagicStorage.Capacity", numItems, capacity));
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
			storageZone.Height.Set(-storageZoneTop - 200, 1f);
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

			craftButton.Top.Set(-48f, 1f);
			craftButton.Width.Set(100f, 0f);
			craftButton.Height.Set(24f, 0f);
			craftButton.PaddingTop = 8f;
			craftButton.PaddingBottom = 8f;
			recipePanel.Append(craftButton);

			resultZone.SetDimensions(1, 1);
			resultZone.Left.Set(-itemSlotWidth - 15, 1f);
			resultZone.Top.Set(storageZoneTop + storageZone.GetDimensions().Height + 40, 0f);
			resultZone.Width.Set(itemSlotWidth, 0f);
			resultZone.Height.Set(itemSlotHeight, 0f);
			recipePanel.Append(resultZone);

			craftAmount.Top.Set(craftButton.Top.Pixels - 20, 1f);
			craftAmount.Left.Set(12, 0f);
			craftAmount.Width.Set(250f, 0f);
			craftAmount.Height.Set(24f * SmallScale, 0f);
			craftAmount.PaddingTop = 0;
			craftAmount.PaddingBottom = 0;
			craftAmount.SetText(Language.GetTextValue("Mods.MagicStorage.Crafting.Amount", craftAmountTarget));
			craftAmount.TextOriginX = 0f;
			recipePanel.Append(craftAmount);

			craftP1.Top.Set(resultZone.Top.Pixels - 30, 0f);
			craftP1.Width.Set(60, 0f);
			craftP1.Height.Set(24f * SmallScale, 0f);
			craftP1.PaddingTop = 8f;
			craftP1.PaddingBottom = 8f;
			recipePanel.Append(craftP1);

			craftP10.Top = craftP1.Top;
			craftP10.Left.Set(craftP1.Left.Pixels + craftP1.Width.Pixels + 10, 0f);
			craftP10.Width.Set(60, 0f);
			craftP10.Height.Set(24f * SmallScale, 0f);
			craftP10.PaddingTop = 8f;
			craftP10.PaddingBottom = 8f;
			recipePanel.Append(craftP10);

			craftP100.Top = craftP1.Top;
			craftP100.Left.Set(craftP10.Left.Pixels + craftP10.Width.Pixels + 10, 0f);
			craftP100.Width.Set(60, 0f);
			craftP100.Height.Set(24f * SmallScale, 0f);
			craftP100.PaddingTop = 8f;
			craftP100.PaddingBottom = 8f;
			recipePanel.Append(craftP100);

			craftM1.Top.Set(craftP1.Top.Pixels + craftP1.Height.Pixels + 15, 0f);
			craftM1.Width.Set(60, 0f);
			craftM1.Height.Set(24f * SmallScale, 0f);
			craftM1.PaddingTop = 8f;
			craftM1.PaddingBottom = 8f;
			recipePanel.Append(craftM1);

			craftM10.Top = craftM1.Top;
			craftM10.Left.Set(craftM1.Left.Pixels + craftM1.Width.Pixels + 10, 0f);
			craftM10.Width.Set(60, 0f);
			craftM10.Height.Set(24f * SmallScale, 0f);
			craftM10.PaddingTop = 8f;
			craftM10.PaddingBottom = 8f;
			recipePanel.Append(craftM10);

			craftM100.Top = craftM1.Top;
			craftM100.Left.Set(craftM10.Left.Pixels + craftM10.Width.Pixels + 10, 0f);
			craftM100.Width.Set(60, 0f);
			craftM100.Height.Set(24f * SmallScale, 0f);
			craftM100.PaddingTop = 8f;
			craftM100.PaddingBottom = 8f;
			recipePanel.Append(craftM100);

			craftMax.Top.Set(craftM1.Top.Pixels + craftM1.Height.Pixels + 15, 0f);
			craftMax.Width.Set(160f * SmallScale, 0f);
			craftMax.Height.Set(24f * SmallScale, 0f);
			craftMax.PaddingTop = 8f;
			craftMax.PaddingBottom = 8f;
			recipePanel.Append(craftMax);

			craftReset.Top = craftMax.Top;
			craftReset.Left.Set(craftMax.Left.Pixels + craftMax.Width.Pixels + 10, 0f);
			craftReset.Width.Set(100f * SmallScale, 0f);
			craftReset.Height.Set(24f * SmallScale, 0f);
			craftReset.PaddingTop = 8f;
			craftReset.PaddingBottom = 8f;
			recipePanel.Append(craftReset);
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
			craftP1 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus1"));
			craftP10 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus10"));
			craftP100 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Plus100"));
			craftM1 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus1"));
			craftM10 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus10"));
			craftM100 ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Minus100"));
			craftMax ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.MaxStack"), SmallScale);
			craftReset ??= new UITextPanel<LocalizedText>(Language.GetText("Mods.MagicStorage.Crafting.Reset"), SmallScale);
			craftAmount ??= new UIText(Language.GetText("Mods.MagicStorage.Crafting.Amount"), SmallScale);
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
					UpdateCraftButtons();
					modSearchBox.Update(curMouse, oldMouse);
				}
				else
				{
					//Reset the campfire bool since it could be used elswhere
					Campfire = false;

					recipeScrollBarFocus = 0;
					craftTimer = 0;
					maxCraftTimer = StartMaxCraftTimer;
					craftAmountTarget = 1;
					ResetSlotFocus();
				}
			}
			catch (Exception e)
			{
				Main.NewTextMultiline(e.ToString(), c: Color.White);
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

				stationZone.DrawText();
				recipeZone.DrawText();

				ingredientZone.DrawText();
				recipeHeaderZone.DrawText();

				storageZone.DrawText();
				resultZone.DrawText();

				sortButtons.DrawText();
				recipeButtons.DrawText();
				filterButtons.DrawText();

				DrawCraftButtonHoverText();
			}
			catch (Exception e)
			{
				Main.NewTextMultiline(e.ToString(), c: Color.White);
			}
		}

		private static void DrawCraftButtonHoverText()
		{
			Rectangle dim = InterfaceHelper.GetFullRectangle(craftButton);

			if (Main.netMode == NetmodeID.SinglePlayer)
				if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height)
					if (selectedRecipe is not null && IsAvailable(selectedRecipe, false) && PassesBlock(selectedRecipe))
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
			}

			return item;
		}

		private static Item GetHeader(int slot, ref int context)
		{
			if (selectedRecipe == null)
				return new Item();

			// TODO: Can we simply return `selectedRecipe.createItem`
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

			int totalGroupStack = 0;
			Item storageItem = storageItems.FirstOrDefault(i => i.type == item.type) ?? new Item();

			foreach (RecipeGroup rec in selectedRecipe.acceptedGroups.Select(index => RecipeGroup.recipeGroups[index]))
				if (rec.ValidItems.Contains(item.type))
					foreach (int type in rec.ValidItems)
						totalGroupStack += storageItems.Where(i => i.type == type).Sum(i => i.stack);

			if (!item.IsAir)
			{
				if (storageItem.IsAir && totalGroupStack == 0)
					context = 3; // Unavailable - Red // ItemSlot.Context.ChestItem
				else if (storageItem.stack < item.stack && totalGroupStack < item.stack)
					context = 4; // Partially in stock - Pinkish // ItemSlot.Context.BankItem
				// context == 0 - Available - Default Blue
				if (context != 0)
				{
					bool craftable = MagicCache.ResultToRecipe.TryGetValue(item.type, out var r) && r.Any(recipe => AmountCraftable(recipe) > 0);
					if (craftable)
						context = 6; // Craftable - Light green // ItemSlot.Context.TrashItem
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
				foreach (Item inventoryItem in items)
					if (inventoryItem.type == requiredItem.type || RecipeGroupMatch(recipe, inventoryItem.type, requiredItem.type))
						total += inventoryItem.stack;

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
			if (blockStorageItems.Contains(new ItemData(item)))
				context = 3; // Red // ItemSlot.Context.ChestItem

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

		private static void UpdateCraftButtons()
		{
			ClampCraftAmount();

			bool stillCrafting = false;
			HandleCraftButton(craftButton, false, () => ClickCraftButton(ref stillCrafting));
			HandleCraftButton(craftP1, true, () => ClickAmountButton(1, true));
			HandleCraftButton(craftP10, true, () => ClickAmountButton(10, true));
			HandleCraftButton(craftP100, true, () => ClickAmountButton(100, true));
			HandleCraftButton(craftM1, true, () => ClickAmountButton(-1, true));
			HandleCraftButton(craftM10, true, () => ClickAmountButton(-10, true));
			HandleCraftButton(craftM100, true, () => ClickAmountButton(-100, true));
			HandleCraftButton(craftMax, true, () => ClickAmountButton(int.MaxValue, false));
			HandleCraftButton(craftReset, true, () => ClickAmountButton(1, false));

			if (!stillCrafting)
			{
				craftTimer = 0;
				maxCraftTimer = StartMaxCraftTimer;
			}
		}

		private static void HandleCraftButton(UITextPanel<LocalizedText> button, bool clickOnly, Action onClicked) {
			Rectangle dim = InterfaceHelper.GetFullRectangle(button);

			if (curMouse.X > dim.X && curMouse.X < dim.X + dim.Width && curMouse.Y > dim.Y && curMouse.Y < dim.Y + dim.Height) {
				button.BackgroundColor = new Color(73, 94, 171);

				if (curMouse.LeftButton == ButtonState.Pressed && (!clickOnly || oldMouse.LeftButton == ButtonState.Released) && IsAvailable(selectedRecipe, false) && PassesBlock(selectedRecipe))
					onClicked();
			} else
				button.BackgroundColor = new Color(63, 82, 151) * 0.7f;

			if (!IsAvailable(selectedRecipe, false) || !PassesBlock(selectedRecipe))
				button.BackgroundColor = new Color(30, 40, 100) * 0.7f;
		}

		private static void ClickCraftButton(ref bool stillCrafting) {
			if (craftTimer <= 0)
			{
				craftTimer = maxCraftTimer;
				maxCraftTimer = maxCraftTimer * 3 / 4;
				if (maxCraftTimer <= 0)
					maxCraftTimer = 1;

				Craft(craftAmountTarget);
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
		}

		private static void ClickAmountButton(int amount, bool offset) {
			if (offset && (amount == 1 || craftAmountTarget > 1))
				craftAmountTarget += amount;
			else
				craftAmountTarget = amount;  //Snap directly to the amount if the amount target was 1 (this makes clicking 10 when at 1 just go to 10 instead of 11)

			ClampCraftAmount();

			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		private static void ClampCraftAmount() {
			if (craftAmountTarget < 1)
				craftAmountTarget = 1;
			else if (!IsAvailable(selectedRecipe, false) || !PassesBlock(selectedRecipe))
				craftAmountTarget = 1;
			else if (craftAmountTarget > selectedRecipe.createItem.maxStack)
				craftAmountTarget = selectedRecipe.createItem.maxStack;
		}

		private static TEStorageHeart GetHeart() => StoragePlayer.LocalPlayer.GetStorageHeart();

		private static TECraftingAccess GetCraftingEntity() => StoragePlayer.LocalPlayer.GetCraftingAccess();

		private static List<Item> GetCraftingStations() => GetCraftingEntity()?.stations;

		public static void RefreshItems()
		{
			items.Clear();
			TEStorageHeart heart = GetHeart();
			if (heart == null)
				return;

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			IEnumerable<Item> itemsWithSimulators = heart.GetStoredItems()
				.Concat(heart.GetModules()
					.SelectMany(m => m.GetAdditionalItems(sandbox)));

			items.AddRange(ItemSorter.SortAndFilter(itemsWithSimulators, SortMode.Id, FilterMode.All, ModSearchBox.ModIndexAll, ""));

			AnalyzeIngredients();
			InitLangStuff();
			InitSortButtons();
			InitRecipeButtons();
			InitFilterButtons();

			RefreshStorageItems();

			RefreshRecipes();
		}

		private static void RefreshRecipes()
		{
			try
			{
				static void DoFiltering(SortMode sortMode, FilterMode filterMode, int modFilterIndex, ItemTypeOrderedSet hiddenRecipes, ItemTypeOrderedSet favorited)
				{
					var filteredRecipes = ItemSorter.GetRecipes(sortMode, filterMode, modFilterIndex, searchBar.Text, out var sortComparer)
						// show only blacklisted recipes only if choice = 2, otherwise show all other
						.Where(x => recipeButtons.Choice == RecipeButtonsBlacklistChoice == hiddenRecipes.Contains(x.createItem))
						// show only favorited items if selected
						.Where(x => recipeButtons.Choice != RecipeButtonsFavoritesChoice || favorited.Contains(x.createItem))
						// favorites first
						.OrderBy(r => favorited.Contains(r.createItem) ? 0 : 1)
						.ThenBy(r => r.createItem, sortComparer);

					recipes.Clear();
					recipeAvailable.Clear();

					if (recipeButtons.Choice == RecipeButtonsAvailableChoice)
					{
						recipes.AddRange(filteredRecipes.Where(r => IsAvailable(r)));
						recipeAvailable.AddRange(Enumerable.Repeat(true, recipes.Count));
					}
					else
					{
						recipes.AddRange(filteredRecipes);
						recipeAvailable.AddRange(recipes.AsParallel().AsOrdered().Select(r => IsAvailable(r)));
					}
				}

				if (RecursiveCraftIntegration.Enabled)
					RecursiveCraftIntegration.RefreshRecursiveRecipes();

				SortMode sortMode = (SortMode) sortButtons.Choice;
				FilterMode filterMode = ItemFilter.GetFilter(filterButtons.Choice);
				int modFilterIndex = modSearchBox.ModIndex;

				var hiddenRecipes = StoragePlayer.LocalPlayer.HiddenRecipes;
				var favorited = StoragePlayer.LocalPlayer.FavoritedRecipes;

				DoFiltering(sortMode, filterMode, modFilterIndex, hiddenRecipes, favorited);

				// now if nothing found we disable filters one by one
				if (searchBar.Text.Length > 0)
				{
					if (recipes.Count == 0 && hiddenRecipes.Count > 0)
					{
						// search hidden recipes too
						hiddenRecipes = ItemTypeOrderedSet.Empty;
						DoFiltering(sortMode, filterMode, modFilterIndex, hiddenRecipes, favorited);
					}

					/*
					if (recipes.Count == 0 && filterMode != FilterMode.All)
					{
						// any category
						filterMode = FilterMode.All;
						DoFiltering(sortMode, filterMode, modFilterIndex, hiddenRecipes, favorited);
					}
					*/

					if (recipes.Count == 0 && modFilterIndex != ModSearchBox.ModIndexAll)
					{
						// search all mods
						modFilterIndex = ModSearchBox.ModIndexAll;
						DoFiltering(sortMode, filterMode, modFilterIndex, hiddenRecipes, favorited);
					}
				}

				// TODO is there a better way?
				void GuttedSetSelectedRecipe(Recipe recipe, int index)
				{
					Recipe compound = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);
					if (index != -1)
						recipes[index] = compound;

					selectedRecipe = compound;
					RefreshStorageItems();
					blockStorageItems.Clear();
				}

				if (RecursiveCraftIntegration.Enabled)
					if (selectedRecipe is not null)
					{
						// If the selected recipe is compound, replace the overridden recipe with the compound one so it shows as selected in the UI
						if (RecursiveCraftIntegration.IsCompoundRecipe(selectedRecipe))
						{
							Recipe overridden = RecursiveCraftIntegration.GetOverriddenRecipe(selectedRecipe);
							int index = recipes.IndexOf(overridden);
							GuttedSetSelectedRecipe(overridden, index);
						}
						// If the selectedRecipe(which isn't compound) is uncraftable but is in the available list, this means it's compound version is craftable
						else if (!IsAvailable(selectedRecipe, false))
						{
							int index = recipes.IndexOf(selectedRecipe);
							if (index != -1 && recipeAvailable[index])
								GuttedSetSelectedRecipe(selectedRecipe, index);
						}
					}
			}
			catch (Exception e)
			{
				Main.NewTextMultiline(e.ToString(), c: Color.White);
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

			itemCounts.Clear();
			foreach ((int type, int amount) in items.GroupBy(item => item.type, item => item.stack, (type, stacks) => (type, stacks.Sum())))
				itemCounts[type] = amount;

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
					case ItemID.BottomlessLavaBucket:
						adjLava = true;
						break;
					case ItemID.HoneyBucket:
						adjHoney = true;
						break;
				}
				if (item.type == ModContent.ItemType<SnowBiomeEmulator>())
				{
					zoneSnow = true;
				}

				if (item.type == ModContent.ItemType<BiomeGlobe>())
				{
					zoneSnow = true;
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
			if (recipe is null)
				return false;

			if (RecursiveCraftIntegration.Enabled && checkCompound)
				recipe = RecursiveCraftIntegration.ApplyCompoundRecipe(recipe);

			if (recipe.requiredTile.Any(tile => !adjTiles[tile]))
				return false;

			foreach (Item ingredient in recipe.requiredItem)
			{
				int stack = ingredient.stack;
				bool useRecipeGroup = false;
				foreach (var (type, count) in itemCounts)
					if (RecipeGroupMatch(recipe, type, ingredient.type))
					{
						stack -= count;
						useRecipeGroup = true;
					}

				if (!useRecipeGroup && itemCounts.TryGetValue(ingredient.type, out int amount))
					stack -= amount;
				if (stack > 0)
					return false;
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

			Player player = Main.LocalPlayer;
			bool[] origAdjTile = player.adjTile;
			bool oldAdjWater = player.adjWater;
			bool oldAdjLava = player.adjLava;
			bool oldAdjHoney = player.adjHoney;
			bool oldAlchemyTable = player.alchemyTable;
			bool oldSnow = player.ZoneSnow;
			bool oldGraveyard = player.ZoneGraveyard;
			bool oldCampfire = Campfire;

			TEStorageHeart heart = GetHeart();

			try
			{
				EnvironmentSandbox sandbox = new(player, heart);
				CraftingInformation information = new(Campfire, zoneSnow, graveyard, adjWater, adjLava, adjHoney, alchemyTable, adjTiles);

				if (heart is not null) {
					foreach (TEEnvironmentAccess environment in heart.GetEnvironmentSimulators())
						environment.ModifyCraftingZones(sandbox, ref information);
				}

				player.adjTile = information.adjTiles;
				player.adjWater = information.water;
				player.adjLava = information.lava;
				player.adjHoney = information.honey;
				player.alchemyTable = information.alchemyTable;
				player.ZoneSnow = information.snow;
				player.ZoneGraveyard = information.graveyard;
				Campfire = information.campfire;

				action();
			}
			finally
			{
				player.adjTile = origAdjTile;
				player.adjWater = oldAdjWater;
				player.adjLava = oldAdjLava;
				player.adjHoney = oldAdjHoney;
				player.alchemyTable = oldAlchemyTable;
				player.ZoneSnow = oldSnow;
				player.ZoneGraveyard = oldGraveyard;
				Campfire = oldCampfire;

				EnvironmentSandbox sandbox = new(player, heart);

				if (heart is not null) {
					foreach (TEEnvironmentAccess environment in heart.GetEnvironmentSimulators())
						environment.ResetPlayer(sandbox);
				}
			}
		}

		private static bool PassesBlock(Recipe recipe)
		{
			foreach (Item ingredient in recipe.requiredItem)
			{
				int stack = ingredient.stack;
				bool useRecipeGroup = false;

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

				if (stack > 0)
					return false;
			}


			return true;
		}

		private static void RefreshStorageItems()
		{
			storageItems.Clear();
			result = null;
			if (selectedRecipe is null)
				return;

			foreach (Item item in items)
			{
				foreach (Item reqItem in selectedRecipe.requiredItem)
					if (item.type == reqItem.type || RecipeGroupMatch(selectedRecipe, item.type, reqItem.type))
						storageItems.Add(item);

				if (item.type == selectedRecipe.createItem.type)
					result = item;
			}

			result ??= new Item(selectedRecipe.createItem.type, 0);
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
							if (storagePlayer.HiddenRecipes.Remove(recipe.createItem)) {
								Main.NewText(Language.GetTextValue("Mods.MagicStorage.RecipeRevealed", Lang.GetItemNameValue(recipe.createItem.type)));

								RefreshItems();
							}
						}
						else
						{
							if (storagePlayer.HiddenRecipes.Add(recipe.createItem)) {
								Main.NewText(Language.GetTextValue("Mods.MagicStorage.RecipeHidden", Lang.GetItemNameValue(recipe.createItem.type)));

								RefreshItems();
							}
						}
					}
					else
					{
						SetSelectedRecipe(recipe);
					}
				}

				hoverSlot = visualSlot;
			}
		}

		private static void SetSelectedRecipe(Recipe recipe)
		{
			ArgumentNullException.ThrowIfNull(recipe);

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
			slot += IngredientColumns * (int)Math.Round(storageScrollBar.ViewPosition);

			if (slot >= selectedRecipe.requiredItem.Count)
				return;

			// select ingredient recipe by right clicking
			if (RightMouseClicked)
			{
				Item item = selectedRecipe.requiredItem[slot];
				if (MagicCache.ResultToRecipe.TryGetValue(item.type, out var itemRecipes) && itemRecipes.Length > 0)
				{
					Recipe selected = itemRecipes[0];

					foreach (Recipe r in itemRecipes[1..])
					{
						if (IsAvailable(r))
						{
							selected = r;
							break;
						}
					}

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
				if (blockStorageItems.Contains(data))
					blockStorageItems.Remove(data);
				else
					blockStorageItems.Add(data);
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

		private static List<Item> CompactItemList(List<Item> items) {
			List<Item> compacted = new();

			for (int i = 0; i < items.Count; i++) {
				Item item = items[i];

				if (item.IsAir)
					continue;

				bool fullyCompacted = false;
				for (int j = 0; j < compacted.Count; j++) {
					Item existing = compacted[j];

					if (MagicCache.CanCombine(item, existing)) {
						if (existing.stack + item.stack <= existing.maxStack) {
							existing.stack += item.stack;
							item.stack = 0;

							fullyCompacted = true;
						} else {
							int diff = existing.maxStack - existing.stack;
							existing.stack = existing.maxStack;
							item.stack -= diff;
						}

						break;
					}
				}

				if (item.IsAir)
					continue;

				if (!fullyCompacted)
					compacted.Add(item);
			}

			return compacted;
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from a Crafting Access
		/// </summary>
		/// <param name="craftingAccess">The tile entity for the Crafting Access to craft items from</param>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(TECraftingAccess craftingAccess, int toCraft) {
			if (craftingAccess is null)
				return;

			StoragePlayer.StorageHeartAccessWrapper wrapper = new(craftingAccess);

			//OpenStorage() handles setting the CraftingGUI to use the new storage and Dispose()/CloseStorage() handles reverting it back
			if (wrapper.Valid) {
				using (wrapper.OpenStorage())
					Craft(toCraft);
			}
		}

		/// <summary>
		/// Attempts to craft a certain amount of items from the currently assigned Crafting Access.
		/// </summary>
		/// <param name="toCraft">How many items should be crafted</param>
		public static void Craft(int toCraft) {
			var sourceItems = storageItems.Where(item => !blockStorageItems.Contains(new ItemData(item))).ToList();
			var availableItems = sourceItems.Select(item => item.Clone()).ToList();
			List<Item> toWithdraw = new(), results = new();

			TEStorageHeart heart = GetHeart();
			List<EnvironmentModule> modules = heart?.GetModules().ToList() ?? new();

			EnvironmentSandbox sandbox = new(Main.LocalPlayer, heart);

			while (toCraft > 0) {
				if (!AttemptSingleCraft(availableItems, sourceItems, toWithdraw, results, modules, sandbox))
					break;  // Could not craft any more items

				Item resultItem = selectedRecipe.createItem.Clone();
				toCraft -= resultItem.stack;

				resultItem.Prefix(-1);
				results.Add(resultItem);

				CatchDroppedItems = true;
				DroppedItems.Clear();

				RecipeLoader.OnCraft(resultItem, selectedRecipe);
				ItemLoader.OnCreate(resultItem, new RecipeCreationContext { recipe = selectedRecipe });

				CatchDroppedItems = false;

				results.AddRange(DroppedItems);
			}

			toWithdraw = CompactItemList(toWithdraw);
			results = CompactItemList(results);

			if (Main.netMode == NetmodeID.SinglePlayer) {
				foreach (Item item in HandleCraftWithdrawAndDeposit(GetHeart(), toWithdraw, results))
					Main.LocalPlayer.QuickSpawnClonedItem(new EntitySource_TileEntity(GetHeart()), item, item.stack);
			} else if (Main.netMode == NetmodeID.MultiplayerClient)
				NetHelper.SendCraftRequest(GetHeart().ID, toWithdraw, results);
		}

		private static bool AttemptSingleCraft(List<Item> available, List<Item> source, List<Item> withdraw, List<Item> results, List<EnvironmentModule> modules, EnvironmentSandbox sandbox) {
			int index = -1;

			foreach (Item reqItem in selectedRecipe.requiredItem)
			{
				index++;

				int stack = reqItem.stack;

				RecipeLoader.ConsumeItem(selectedRecipe, reqItem.type, ref stack);

				if (stack <= 0)
					continue;

				foreach (var module in modules)
					module.OnConsumeItemForRecipe(sandbox, source[index]);

				bool AttemptToConsumeItem(List<Item> list, bool addToWithdraw) {
					foreach (Item tryItem in list)
					{
						if (reqItem.type == tryItem.type || RecipeGroupMatch(selectedRecipe, tryItem.type, reqItem.type))
						{
							if (tryItem.stack > stack)
							{
								Item temp = tryItem.Clone();
								temp.stack = stack;

								if (addToWithdraw)
									withdraw.Add(temp);
								
								tryItem.stack -= stack;
								stack = 0;
							}
							else
							{
								if (addToWithdraw)
									withdraw.Add(tryItem.Clone());
								
								stack -= tryItem.stack;
								tryItem.stack = 0;
								tryItem.type = ItemID.None;
							}

							if (stack <= 0)
								break;
						}
					}

					return stack <= 0;
				}

				if (!AttemptToConsumeItem(available, addToWithdraw: true))
					AttemptToConsumeItem(results, addToWithdraw: false);

				if (stack > 0)
					return false;  // Did not have enough items
			}

			return true;
		}

		internal static List<Item> HandleCraftWithdrawAndDeposit(TEStorageHeart heart, List<Item> toWithdraw, List<Item> results)
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
			return heart.TryWithdraw(item, false, toInventory);
		}
	}
}
