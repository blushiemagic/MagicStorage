using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace MagicStorage.UI {
	//Basically a copy of UIList that uses a NewUIScrollbar instead of a UIScrollbar
	internal class NewUIList : UIElement, IEnumerable<UIElement>, IEnumerable {
		public delegate bool ElementSearchMethod(UIElement element);

		private class UIInnerList : UIElement {
			public override bool ContainsPoint(Vector2 point) => true;

			protected override void DrawChildren(SpriteBatch spriteBatch) {
				var parentDims = Parent.GetDimensions();

				Vector2 position = parentDims.Position();
				Vector2 dimensions = new(parentDims.Width, parentDims.Height);

				foreach (UIElement element in Elements) {
					var elementDims = element.GetDimensions();

					Vector2 position2 = elementDims.Position();
					Vector2 dimensions2 = new(elementDims.Width, elementDims.Height);

					if (Collision.CheckAABBvAABBCollision(position, dimensions, position2, dimensions2))
						element.Draw(spriteBatch);
				}
			}

			public override Rectangle GetViewCullingArea() => Parent.GetDimensions().ToRectangle();
		}

		public List<UIElement> _items = new();
		protected NewUIScrollbar _scrollbar;
		internal UIElement _innerList = new UIInnerList();
		private float _innerListHeight;
		public float ListPadding = 5f;
		public Action<List<UIElement>> ManualSortMethod;

		public int Count => _items.Count;

		public float ViewPosition {
			get => _scrollbar.ViewPosition;
			set => _scrollbar.ViewPosition = value;
		}

		public bool DisplayChildrenInReverseOrder;

		public NewUIList() {
			_innerList.OverflowHidden = false;
			_innerList.Width.Set(0f, 1f);
			_innerList.Height.Set(0f, 1f);
			OverflowHidden = true;
			Append(_innerList);
		}

		public float GetTotalHeight() => _innerListHeight;

		public void Goto(ElementSearchMethod searchMethod) {
			int num = 0;
			while (true) {
				if (num < _items.Count) {
					if (searchMethod(_items[num]))
						break;

					num++;
					continue;
				}

				return;
			}

			_scrollbar.ViewPosition = _items[num].Top.Pixels;
		}

		public virtual void Add(UIElement item) {
			_items.Add(item);
			_innerList.Append(item);
			UpdateOrder();
			_innerList.Recalculate();
		}

		public virtual bool Remove(UIElement item) {
			_innerList.RemoveChild(item);
			UpdateOrder();
			return _items.Remove(item);
		}

		public virtual void AddRange(IEnumerable<UIElement> items) {
			foreach (var item in items) {
				//TML bug fix:  duplicate enumerations resulting in separate object instances in "_items" and "_innerList.Children"
				_items.Add(item);
				_innerList.Append(item);
			}

			UpdateOrder();
			_innerList.Recalculate();
		}

		public override void MouseOver(UIMouseEvent evt) {
			base.MouseOver(evt);
			PlayerInput.LockVanillaMouseScroll("MagicStorage/NewUIList");
		}

		public virtual void Clear() {
			_innerList.RemoveAllChildren();
			_items.Clear();
		}

		public override void Recalculate() {
			base.Recalculate();
			UpdateScrollbar();
		}

		public override void ScrollWheel(UIScrollWheelEvent evt) {
			base.ScrollWheel(evt);
			if (_scrollbar != null)
				_scrollbar.ViewPosition -= evt.ScrollWheelValue / _scrollbar.ScrollDividend;
		}

		public override void RecalculateChildren() {
			base.RecalculateChildren();
			float num = 0f;
			if (!DisplayChildrenInReverseOrder) {
				for (int i = 0; i < _items.Count; i++) {
					float num2 = (_items.Count == 1) ? 0f : ListPadding;
					_items[i].Top.Set(num, 0f);
					_items[i].Recalculate();
					CalculatedStyle outerDimensions = _items[i].GetOuterDimensions();
					num += outerDimensions.Height + num2;
				}
			} else {
				for (int i = _items.Count - 1; i >= 0; i--) {
					float num2 = (_items.Count == 1) ? 0f : ListPadding;
					_items[i].Top.Set(num, 0f);
					_items[i].Recalculate();
					CalculatedStyle outerDimensions = _items[i].GetOuterDimensions();
					num += outerDimensions.Height + num2;
				}
			}

			_innerListHeight = num;
		}

		private void UpdateScrollbar() {
			if (_scrollbar != null) {
				float height = GetInnerDimensions().Height;
				_scrollbar.SetView(height, _innerListHeight);
			}
		}

		public void SetScrollbar(NewUIScrollbar scrollbar) {
			_scrollbar = scrollbar;
			UpdateScrollbar();
		}

		public void UpdateOrder() {
			if (ManualSortMethod != null)
				ManualSortMethod(_items);
			else
				_items.Sort(SortMethod);

			UpdateScrollbar();
		}

		public int SortMethod(UIElement item1, UIElement item2) => item1.CompareTo(item2);

		public override List<SnapPoint> GetSnapPoints() {
			List<SnapPoint> list = new();
			if (GetSnapPoint(out SnapPoint point))
				list.Add(point);

			foreach (UIElement item in _items) {
				list.AddRange(item.GetSnapPoints());
			}

			return list;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			if (_scrollbar != null)
				_innerList.Top.Set(0f - _scrollbar.ViewPosition, 0f);

			Recalculate();
		}

		public IEnumerator<UIElement> GetEnumerator() => _items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	}
}
