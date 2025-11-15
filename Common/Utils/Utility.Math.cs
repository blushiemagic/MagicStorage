using System.Numerics;

namespace MagicStorage {
	partial class Utility {
		public static T CeilingMultiple<T>(T value, T size) where T : IUnsignedNumber<T>
			=> value == T.Zero ? T.Zero : ((value - T.One) / size + T.One) * size;
	}
}
