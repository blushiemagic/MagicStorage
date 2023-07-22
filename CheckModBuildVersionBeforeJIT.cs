using System;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage {
	internal class CheckModBuildVersionBeforeJIT : PreJITFilter {
		// The GetInstance singleton is only loaded after mod JITing was performed, hence the need for a variable
		public static MagicStorageMod Mod;
		public static bool versionChecked;

		public override bool ShouldJIT(MemberInfo member) {
			if (!versionChecked) {
				CheckBuildVersion();
				versionChecked = true;
			}

			return base.ShouldJIT(member);
		}

		private static readonly Version first143Preview = new Version(2022, 10);

		private static void CheckBuildVersion() {
			// Check if the mod version matches the expected tModLoader build
			Version build = Mod.TModLoaderVersion;
			Version current = BuildInfo.tMLVersion;

			if (build < first143Preview) {
				if (current >= first143Preview) {
					// Attempted to load the 1.4.3 build of Magic Storage on a 1.4.4 client/server
					throw new OutdatedModBuildException();
				}
			} else if (current < first143Preview) {
				// Attempted to load the 1.4.4 build of Magic Storage on a 1.4.3 client/server
				throw new IndatedModBuildException();
			}
		}
	}

	internal class OutdatedModBuildException : Exception {
		private const string MESSAGE = "Attempted to load the 1.4.3 build of Magic Storage on a 1.4.4 tModLoader instance\n" +
			"If you installed the mod manually, get the mod file from the \"Steam/steamapps/workshop/content/1281930/2563309347/2022.9\" directory.";

		public OutdatedModBuildException(Exception innerException = null) : base(MESSAGE, innerException) { }
	}

	internal class IndatedModBuildException : Exception {
		private const string MESSAGE = "Attempted to load the 1.4.4 build of Magic Storage on a 1.4.3 tModLoader instance\n" +
			"If you installed the mod manually, get the mod file from the \"Steam/steamapps/workshop/content/1281930/2563309347/" + MagicStorageMod.build144Version + "\" directory.";

		public IndatedModBuildException(Exception innerException = null) : base(MESSAGE, innerException) { }
	}
}
