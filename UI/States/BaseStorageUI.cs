using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
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

		public T GetPage<T>(string page) where T : BaseStorageUIPage => pages is null
			? null
			: (pages[page] as T ?? throw new InvalidCastException($"The underlying object for page \"{GetType().Name}:{page}\" cannot be converted to " + typeof(T).FullName));

		public override void OnInitialize() {
			float itemSlotWidth = TextureAssets.InventoryBack.Value.Width * CraftingGUI.InventoryScale;

			panel = new(true, GetMenuOptions().Select(p => (p, Language.GetText("Mods.MagicStorage.UIPages." + p))));

			panel.OnMenuClose += StoragePlayer.LocalPlayer.CloseStorage;

			panel.OnRecalculate += UpdateFields;

			PanelTop = Main.instance.invBottom + 60;
			PanelLeft = 20f;
			float innerPanelWidth = CraftingGUI.RecipeColumns * (itemSlotWidth + CraftingGUI.Padding) + 20f + CraftingGUI.Padding;
			PanelWidth = panel.PaddingLeft + innerPanelWidth + panel.PaddingRight + 2 * UIDragablePanel.cornerPadding;
			PanelHeight = Main.screenHeight - PanelTop + 2 * UIDragablePanel.cornerPadding;

			pages = new();

			foreach ((string key, var tab) in panel.menus) {
				var page = pages[key] = InitPage(key);
				page.Width = StyleDimension.Fill;
				page.Height = StyleDimension.Fill;

				tab.OnClick += (evt, e) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					SetPage((e as UIPanelTab).Name);
				};
			}

			PostInitializePages();

			Append(panel);

			PostAppendPanel();

			//Need to manually activate the pages
			foreach (var page in pages.Values)
				page.Activate();
		}

		private void UpdateFields() {
			PanelLeft = panel.Left.Pixels;
			PanelTop = panel.Top.Pixels;
			PanelWidth = panel.Width.Pixels;
			PanelHeight = panel.Height.Pixels;
		}

		public sealed override void OnActivate() => Open();

		public sealed override void OnDeactivate() => Close();

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

				panel.viewArea.Append(currentPage);

				currentPage.InvokeOnPageSelected();

				return true;
			}

			return false;
		}

		public void Open() {
			if (currentPage is not null)
				return;

			OnOpen();

			SetPage(DefaultPage);
		}

		protected virtual void OnOpen() { }

		public void Close() {
			if (currentPage is not null) {
				OnClose();

				currentPage.InvokeOnPageDeselected();

				currentPage.Remove();
			}

			currentPage = null;
		}

		protected virtual void OnClose() { }
	}
}
