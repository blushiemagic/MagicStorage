using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;

namespace MagicStorage.Common.Systems.Debugging {
	internal class DebuggingConfigurationManager {
		public static readonly string RelativeDestinationFolder = "ModConfigs";
		public static readonly string RelativeDestinationFile = "MagicStorage_Debug.json";

		public static string DestinationFolder => Path.Combine(Main.SavePath, RelativeDestinationFolder);
		public static string DestinationPath => Path.Combine(Main.SavePath, RelativeDestinationFolder, RelativeDestinationFile);

		internal void LoadConfigurations() {
			try {
				Directory.CreateDirectory(DestinationFolder);

				string path = DestinationPath;
				if (!File.Exists(path)) {
					// No file, default to the initial control scheme
					goto UseDefault;
				} else {
					try {
						string contents = File.ReadAllText(path);

						var dictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(contents);

						foreach (var (key, value) in dictionary) {
							if (DebugControls.Has(key))
								DebugControls.Set(key, value);
							else
								MagicStorageMod.Instance.Logger.Warn("Debug configuration file \"" + RelativeDestinationFile + "\" contains an unrecognized control key \"" + key + "\"");
						}

						return;
					} catch (Exception ex) {
						MagicStorageMod.Instance.Logger.Warn("Debug configuration file \"" + RelativeDestinationFile + "\" was malformed", ex);
						goto UseDefault;
					}
				}
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Warn("Debug configuration file \"" + RelativeDestinationFile + "\" could not be loaded", ex);
				goto UseDefault;
			}

			UseDefault:
			Save();
		}

		internal void Save() {
			Dictionary<string, bool> dictionary = [];

			// For better ease-of-use, the controls are serialized in a roughly alphabetical order

			foreach (var (control, value) in DebugControls.Enumerate().OrderBy(kvp => kvp.Key, ControlComparer.Instance))
				dictionary[control] = value;

			string contents = JsonConvert.SerializeObject(dictionary, Formatting.Indented);

			try {
				Directory.CreateDirectory(DestinationFolder);

				File.WriteAllText(DestinationPath, contents);
			} catch (Exception ex) {
				MagicStorageMod.Instance.Logger.Warn("Debug configuration file \"" + RelativeDestinationFile + "\" could not be saved", ex);
			}
		}

		private class ControlComparer : IComparer<string> {
			public static ControlComparer Instance { get; } = new();

			int IComparer<string>.Compare(string x, string y) {
				// "any" should be at the top of the list for any given control group

				bool anyX = x == "any";
				bool anyY = y == "any";

				if (anyX)
					return anyY ? 0 : -1;

				if (anyY)
					return 1;

				// Fall back to standard comparison rules

				return Comparer<string>.Default.Compare(x, y);
			}
		}
	}
}
