using System;
using HarmonyLib;
using TerrariaModder.Core;
using TerrariaModder.Core.Logging;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace VanillaUISkin
{
    internal static class Log
    {
        private static ILogger _logger;
        public static void Init(ILogger logger) => _logger = logger;
        public static void Info(string msg) => _logger?.Info(msg);
        public static void Warn(string msg) => _logger?.Warn(msg);
        public static void Error(string msg, Exception ex) => _logger?.Error($"{msg}: {ex}");
    }

    /// <summary>
    /// Restyles every TerrariaModder window (Core's mod menu and all mods built on Core's UI widgets)
    /// to look like vanilla / tModLoader UI. Delete the mod folder to go back to the flat style.
    /// </summary>
    public sealed class VanillaUISkinMod : ModBase
    {
        private Harmony _harmony;

        public override void Initialize(ModContext context)
        {
            Log.Init(context.Logger);
            if (context.IsServer) return;

            _harmony = new Harmony("terrariamodder.vanillauiskin");
            int ok = 0;
            ok += Patch(AccessTools.Method(typeof(UIRenderer), "DrawRect",
                    new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte), typeof(byte) }),
                prefix: nameof(Skin.DrawRectPrefix));
            ok += Patch(AccessTools.Method(typeof(UIRenderer), "DrawPanel",
                    new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(byte), typeof(byte), typeof(byte), typeof(byte) }),
                prefix: nameof(Skin.DrawPanelPrefix));
            ok += Patch(AccessTools.Method(typeof(UIRenderer), "DrawPanel",
                    new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(Color4) }),
                prefix: nameof(Skin.DrawPanelPrefix));
            ok += Patch(AccessTools.Method(typeof(DraggablePanel), "BeginDraw"),
                prefix: nameof(Skin.ChromePrefix), finalizer: nameof(Skin.ChromeFinalizer));
            Log.Info($"Vanilla UI skin active ({ok}/4 patches)");

            Preview.Init(context.ModFolder);
        }

        private int Patch(System.Reflection.MethodInfo target, string prefix = null, string finalizer = null)
        {
            if (target == null)
            {
                Log.Warn($"Patch target not found (prefix {prefix}); that part of the skin is skipped");
                return 0;
            }
            try
            {
                _harmony.Patch(target,
                    prefix: prefix != null ? new HarmonyMethod(typeof(Skin), prefix) : null,
                    finalizer: finalizer != null ? new HarmonyMethod(typeof(Skin), finalizer) : null);
                return 1;
            }
            catch (Exception ex)
            {
                Log.Error($"Could not patch {target.DeclaringType?.Name}.{target.Name}", ex);
                return 0;
            }
        }

        public override void Unload()
        {
            Preview.Unload();
            _harmony?.UnpatchAll(_harmony.Id);
        }
    }
}
