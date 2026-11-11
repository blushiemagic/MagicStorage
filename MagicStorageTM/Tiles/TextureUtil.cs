using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace MagicStorageTM.Tiles
{
    internal static class TextureUtil
    {
        /// <summary>Load a PNG and convert it to premultiplied alpha (what Terraria's sprite batches expect).</summary>
        public static Texture2D LoadPremultiplied(GraphicsDevice gd, string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                Texture2D tex;
                using (var fs = File.OpenRead(path))
                    tex = Texture2D.FromStream(gd, fs);
                var data = new Color[tex.Width * tex.Height];
                tex.GetData(data);
                for (int i = 0; i < data.Length; i++)
                {
                    Color c = data[i];
                    if (c.A == 255) continue;
                    data[i] = new Color(c.R * c.A / 255, c.G * c.A / 255, c.B * c.A / 255, c.A);
                }
                tex.SetData(data);
                return tex;
            }
            catch (Exception ex)
            {
                Log.Error($"[Tiles] Failed to load texture {path}", ex);
                return null;
            }
        }

        /// <summary>Wrap a texture in an Asset&lt;Texture2D&gt; that reports itself as already loaded.</summary>
        public static Asset<Texture2D> CreateLoadedAsset(Texture2D texture, string name)
        {
            try
            {
                Type t = typeof(Asset<Texture2D>);
                var asset = (Asset<Texture2D>)Activator.CreateInstance(t, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { name }, null);
                const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
                t.GetField("<Value>k__BackingField", F).SetValue(asset, texture);
                t.GetField("<State>k__BackingField", F).SetValue(asset, AssetState.Loaded);
                return asset;
            }
            catch (Exception ex)
            {
                Log.Error("[Tiles] Could not create texture asset", ex);
                return null;
            }
        }
    }
}
