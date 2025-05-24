using System.IO;

namespace MagicStorage.Common.Systems.Auditing {
	internal interface IAuditable<TSelf> where TSelf : IAuditable<TSelf> {
		static abstract void DeserializeOne<T>(BinaryReader reader, ref T instance) where T : TSelf;

		void Serialize(BinaryWriter writer);
	}
}
