using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace MagicStorage.UI.States {
	/// <summary>
	/// A base class for common elements in Magic Storage's GUIs
	/// </summary>
	public abstract class BaseStorageUI : UIState {
		protected UIDragablePanel panel;

		protected Dictionary<string, BaseStorageUIPage> pages;

		public BaseStorageUIPage currentPage;
		
		public float PanelLeft { get; protected set; }
		
		public float PanelTop { get; protected set; }
		
		protected float PanelWidth { get; set; }
		
		protected float PanelHeight { get; set; }

		public float PanelRight {
			get => PanelLeft + PanelWidth;
			set => PanelLeft = value - PanelWidth;
		}
		
		public float PanelBottom {
			get => PanelTop + PanelHeight;
			set => PanelTop = value - PanelHeight;
		}

		protected abstract IEnumerable<string> GetMenuOptions();

		protected abstract BaseStorageUIPage InitPage(string page);

		public abstract string DefaultPage { get; }

		public BaseStorageUIPage GetPage(string page) => pages[page];

		public T GetPage<T>(string page) where T : BaseStorageUIPage => pages[page] as T;

		public override void OnActivate() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;

			PanelTop = Main.instance.invBottom + 60;
			PanelLeft = 20f;
			float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight;
			PanelHeight = Main.screenHeight - PanelTop;

			panel = new(true, GetMenuOptions().ToArray());

			panel.OnMenuClose += Close;

			pages = new();

			foreach (UITextPanel<string> page in panel.menus.Values)
				pages[page.Text] = InitPage(page.Text);

			PostInitializePages();

			Append(panel);

			PostAppendPanel();
		}

		protected virtual void PostInitializePages() { }

		protected virtual void PostAppendPanel() { }

		public bool SetPage(string page) {
			BaseStorageUIPage newPage = pages[page];

			if (!object.ReferenceEquals(currentPage, newPage)) {
				panel.SetActivePage(page);

				if (currentPage is not null) {
					currentPage.InvokeOnPageDeselected();

					currentPage.Remove();
				}

				currentPage = newPage;

				currentPage.InvokeOnPageSelected();

				panel.viewArea.Append(currentPage);

				return true;
			}

			return false;
		}

		public void Open() {
			OnOpen();

			if (currentPage is null)
				SetPage(DefaultPage);
		}

		protected virtual void OnOpen() { }

		public void Close() {
			OnClose();

			if (currentPage is not null) {
				currentPage.InvokeOnPageDeselected();

				currentPage.Remove();
			}

			currentPage = null;
		}

		protected virtual void OnClose() { }
	}
}
