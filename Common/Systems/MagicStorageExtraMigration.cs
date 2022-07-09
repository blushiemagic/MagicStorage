#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage.Common.Systems;

public class MagicStorageExtraMigration : ModSystem
{
	private const string MagicStorage = "MagicStorage";
	private const string MagicStorageExtra = "MagicStorageExtra";

	private static readonly Type[] ModTypeLookupTypes =
		typeof(Mod).Assembly
			.GetTypes()
			.Where(type => !type.IsGenericType && typeof(IModType).IsAssignableFrom(type))
			.Select(type => typeof(ModTypeLookup<>).MakeGenericType(type))
			.ToArray();

	public override void PostSetupContent()
	{
		foreach (var modTypeLookup in ModTypeLookupTypes)
		{
			var dictField = modTypeLookup.GetField("dict", BindingFlags.NonPublic | BindingFlags.Static)!;
			var dict = (IDictionary) dictField.GetValue(null)!;

			var cache = new List<(string key, IModType value)>();

			foreach (DictionaryEntry entry in dict)
			{
				var key = (string) entry.Key;
				var value = (IModType) entry.Value!;

				if (!key.StartsWith(MagicStorage))
					continue;

				key = MagicStorageExtra + key[MagicStorage.Length..];

				cache.Add((key, value));
			}

			foreach (var (key, value) in cache)
				dict.Add(key, value);

			var tieredDictField = modTypeLookup.GetField("tieredDict", BindingFlags.NonPublic | BindingFlags.Static)!;
			var tieredDict = (IDictionary) tieredDictField.GetValue(null)!;

			var innerDict = (IDictionary) tieredDict[MagicStorage]!;
			tieredDict[MagicStorageExtra] = innerDict;
		}
	}
}
