using System.Collections.Generic;
using System.Linq;
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

		public float PanelLeft { get; private set; }
		public float PanelRight { get; private set; }
		public float PanelTop { get; private set; }
		public float PanelBottom { get; private set; }

		protected abstract IEnumerable<string> GetMenuOptions();

		protected abstract BaseStorageUIPage InitPage(string page);

		public abstract string DefaultPage { get; }

		public override void OnActivate() {
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
			if (currentPage is null)
				SetPage(DefaultPage);
		}

		public void Close() {
			if (currentPage is not null) {
				currentPage.InvokeOnPageDeselected();

				currentPage.Remove();
			}

			currentPage = null;
		}
	}
}
