using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria.Localization;

namespace MagicStorage.Common.Systems.Shimmering {
	/// <summary>
	/// An interface representing a report of the results of a shimmering operation
	/// </summary>
	public interface IShimmerResultReport : IEquatable<IShimmerResultReport> {
		/// <summary>
		/// The label for this report
		/// </summary>
		LocalizedText Label { get; }

		/// <summary>
		/// The texture that appears next to the label
		/// </summary>
		Asset<Texture2D> Texture { get; }

		/// <summary>
		/// The object that this report is bound to
		/// </summary>
		object Parent { get; set; }

		Rectangle GetAnimationFrame();

		bool Equals(IShimmerResultReport report);

		bool Render(SpriteBatch spriteBatch);

		void Update();
	}
}
