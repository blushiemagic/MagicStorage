using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;

namespace MagicStorageTM.Tiles
{
    /// <summary>
    /// Grows every array whose length equals vanilla TileID.Count so custom tile types can index it.
    /// The same old array always maps to the same new array, so aliases stay aliases.
    /// New slots get the array's most common existing value (Factory sets with a "true" default
    /// keep that default, Main.tileGlowMask keeps -1, etc.).
    /// </summary>
    internal static class ArrayResizer
    {
        private const BindingFlags StaticFields = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static int ResizeStaticArrays(Assembly asm, int oldLen, int newLen, Dictionary<Array, Array> map)
        {
            int count = 0;
            var names = new List<string>();
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            foreach (Type type in types)
            {
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters) continue;
                FieldInfo[] fields;
                try { fields = type.GetFields(StaticFields); } catch { continue; }
                foreach (FieldInfo field in fields)
                {
                    if (field.IsLiteral || !field.FieldType.IsArray) continue;
                    object value;
                    try { value = field.GetValue(null); } catch { continue; }
                    if (!(value is Array arr)) continue;
                    Array replaced = TryResize(arr, oldLen, newLen, map);
                    if (replaced == null) continue;
                    try
                    {
                        field.SetValue(null, replaced);
                        count++;
                        names.Add(type.Name + "." + field.Name);
                    }
                    catch (Exception ex) { Log.Warn($"[Tiles] Could not resize {type.FullName}.{field.Name}: {ex.Message}"); }
                }
            }
            Log.Debug("[Tiles] Resized static arrays: " + string.Join(", ", names));
            return count;
        }

        /// <summary>Resize arrays held by long-lived vanilla objects that exist before our setup runs.</summary>
        public static int ResizeKnownInstances(int oldLen, int newLen, Dictionary<Array, Array> map)
        {
            int count = 0;
            var visited = new HashSet<object>(new RefEq());

            // Every player slot (adjTile etc.)
            if (Main.player != null)
                foreach (Player p in Main.player)
                    if (p != null) count += ResizeInstanceFields(p, oldLen, newLen, map, visited);

            // Singletons hanging off static fields of Terraria types (SceneMetrics, PylonSystem, renderers...).
            Assembly asm = typeof(Main).Assembly;
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
            foreach (Type type in types)
            {
                if (type.IsGenericTypeDefinition || type.ContainsGenericParameters) continue;
                FieldInfo[] fields;
                try { fields = type.GetFields(StaticFields); } catch { continue; }
                foreach (FieldInfo field in fields)
                {
                    if (field.FieldType.IsArray || field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string)) continue;
                    if (field.FieldType.Assembly != asm) continue;
                    object value;
                    try { value = field.GetValue(null); } catch { continue; }
                    if (value == null || value is Player) continue;
                    count += ResizeInstanceFields(value, oldLen, newLen, map, visited);
                }
            }
            if (Main.instance != null) count += ResizeInstanceFields(Main.instance, oldLen, newLen, map, visited);
            return count;
        }

        /// <summary>Resize array fields (one level deep) of a single object.</summary>
        public static int ResizeInstanceFields(object obj, int oldLen, int newLen, Dictionary<Array, Array> map, HashSet<object> visited = null)
        {
            if (obj == null) return 0;
            if (visited != null && !visited.Add(obj)) return 0;
            int count = 0;
            for (Type t = obj.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo[] fields;
                try { fields = t.GetFields(InstanceFields | BindingFlags.DeclaredOnly); } catch { continue; }
                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsArray) continue;
                    object value;
                    try { value = field.GetValue(obj); } catch { continue; }
                    if (!(value is Array arr)) continue;
                    Array replaced = TryResize(arr, oldLen, newLen, map);
                    if (replaced == null) continue;
                    try { field.SetValue(obj, replaced); count++; } catch { }
                }
            }
            return count;
        }

        /// <summary>Returns the resized replacement for <paramref name="arr"/>, or null if it should not change.</summary>
        public static Array TryResize(Array arr, int oldLen, int newLen, Dictionary<Array, Array> map)
        {
            if (arr.Rank != 1) return null;
            if (map != null && map.TryGetValue(arr, out Array known)) return known;
            if (arr.Length != oldLen) return null;

            Type elem = arr.GetType().GetElementType();
            Array next = Array.CreateInstance(elem, newLen);
            Array.Copy(arr, next, oldLen);
            object fill = MostCommonValue(arr, elem);
            if (fill != null)
                for (int i = oldLen; i < newLen; i++) next.SetValue(fill, i);

            // Jagged arrays (e.g. Main.tileMerge): grow inner arrays too.
            if (elem.IsArray)
            {
                for (int i = 0; i < newLen; i++)
                {
                    if (next.GetValue(i) is Array inner)
                    {
                        Array innerNext = TryResize(inner, oldLen, newLen, map);
                        if (innerNext != null) next.SetValue(innerNext, i);
                    }
                    else if (i >= oldLen && elem.GetElementType().IsValueType)
                    {
                        next.SetValue(Array.CreateInstance(elem.GetElementType(), newLen), i);
                    }
                }
            }

            if (map != null) map[arr] = next;
            return next;
        }

        private static object MostCommonValue(Array arr, Type elem)
        {
            if (!elem.IsPrimitive) return null; // reference types / structs default (null / zero)
            var counts = new Dictionary<object, int>();
            foreach (object v in arr)
            {
                counts.TryGetValue(v, out int c);
                counts[v] = c + 1;
            }
            object best = null; int bestCount = -1;
            foreach (var kv in counts)
                if (kv.Value > bestCount) { best = kv.Key; bestCount = kv.Value; }
            return best;
        }

        private sealed class RefEq : IEqualityComparer<object>
        {
            public new bool Equals(object a, object b) => ReferenceEquals(a, b);
            public int GetHashCode(object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o);
        }
    }
}
