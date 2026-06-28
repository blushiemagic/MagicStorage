using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal interface IAuditable<TSelf> where TSelf : IAuditable<TSelf> {
		static abstract void DeserializeOne(BinaryReader reader, ref TSelf instance);

		void Serialize(BinaryWriter writer);
	}
}
