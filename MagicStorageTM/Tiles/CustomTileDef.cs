using System;

namespace MagicStorageTM.Tiles
{
    /// <summary>
    /// Describes one custom tile type. Runtime type IDs are assigned above vanilla's
    /// TileID.Count when the definition is registered (see <see cref="CustomTiles.Register"/>).
    /// All coordinates passed to callbacks are the top-left tile of the placed object.
    /// </summary>
    public sealed class CustomTileDef
    {
        /// <summary>Stable identifier written to the world sidecar. Never rename once shipped.</summary>
        public string Name;

        /// <summary>Runtime tile type (assigned on registration).</summary>
        public int Type = -1;

        public int Width = 2;
        public int Height = 2;
        public int OriginX = 1;
        public int OriginY = 1;

        /// <summary>Pixel height of each tile row in the sprite sheet (excluding the 2px padding). Defaults to 16 for every row.</summary>
        public int[] CoordinateHeights;

        /// <summary>Texture path relative to the mod folder, e.g. "Assets/Tiles/StorageHeart.png".</summary>
        public string Texture;

        // Placement style layout (same meaning as TileObjectData)
        public bool StyleHorizontal = true;
        public int StyleMultiplier = 1;
        public int StyleWrapLimit = 0;

        // Tile properties
        public bool SolidTop = true;
        public bool LavaDeath = false;
        /// <summary>If true the object needs a solid floor; otherwise it may also be placed floating (MagicStorage components behave this way).</summary>
        public bool RequiresFloor = false;
        public int DustType = 7;

        /// <summary>Vanilla tile whose map color this tile borrows (keeps .map files vanilla-compatible).</summary>
        public int MapColorFrom = 21;

        // Crafting-station behaviour (combined stations)
        public int[] CountsAsTiles;
        public bool CountsAsWater;
        public bool CountsAsHoney;
        public bool CountsAsLava;

        /// <summary>1x1 tiles framed by their neighbours (Storage Connector). Called from the frame hook.</summary>
        public Action<int, int> CustomFrame;

        // Behaviour callbacks (all optional)
        /// <summary>(topLeftX, topLeftY) → false to reject placement.</summary>
        public Func<int, int, bool> CanPlace;
        /// <summary>(topLeftX, topLeftY) after the local player placed the object.</summary>
        public Action<int, int> OnPlaced;
        /// <summary>(topLeftX, topLeftY) → false to prevent the object from being broken.</summary>
        public Func<int, int, bool> CanKill;
        /// <summary>(topLeftX, topLeftY, frameX, frameY) after the object was removed from the world.</summary>
        public Action<int, int, int, int> OnKilled;
        /// <summary>(frameX, frameY) of the top-left tile → item type dropped when broken (0 = nothing).</summary>
        public Func<int, int, int> GetDropItem;
        /// <summary>(topLeftX, topLeftY) → true if the right-click was handled.</summary>
        public Func<int, int, bool> OnRightClick;
        /// <summary>(topLeftX, topLeftY) → item type shown next to the cursor while hovering (0 = none).</summary>
        public Func<int, int, int> HoverIcon;
        /// <summary>(topLeftX, topLeftY) → extra hover text (null = none).</summary>
        public Func<int, int, string> HoverText;

        public int FrameWidth => Width * 18;
        public int FrameHeight
        {
            get
            {
                if (CoordinateHeights == null) return Height * 18;
                int h = 0;
                foreach (int c in CoordinateHeights) h += c + 2;
                return h;
            }
        }

        /// <summary>Pixel offset of tile row <paramref name="row"/> inside one style frame.</summary>
        public int RowOffset(int row)
        {
            if (CoordinateHeights == null) return row * 18;
            int y = 0;
            for (int r = 0; r < row && r < CoordinateHeights.Length; r++) y += CoordinateHeights[r] + 2;
            return y;
        }

        /// <summary>Which row of the object a frameY (relative to the style's top) falls into.</summary>
        public int RowFromFrameY(int localFrameY)
        {
            for (int r = Height - 1; r >= 0; r--)
                if (localFrameY >= RowOffset(r)) return r;
            return 0;
        }
    }
}
