﻿using System;
using Terraria.UI;

namespace MagicStorage.UI.States {
	public abstract class BaseStorageUIPage : UIElement {
		internal BaseStorageUI parentUI;

		public readonly string Name;

		public event Action OnPageSelected, OnPageDeselected;

		public BaseStorageUIPage(BaseStorageUI parent, string name) {
			parentUI = parent;
			Name = name;

			OnPageSelected += Recalculate;
		}

		public void InvokeOnPageSelected() => OnPageSelected?.Invoke();

		public void InvokeOnPageDeselected() => OnPageDeselected?.Invoke();
	}
}
