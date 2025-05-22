using System.IO;
using System.Linq;

namespace MagicStorage.Common.Systems {
	internal static class StringScrambling {
		internal static byte[] Scramble(string str) {
			if(string.IsNullOrEmpty(str))return[];
			ushort[]c=[..str.Select(U)];
			for(int s=13,d=3,i=0;i<L(c);i++,s=s*3%14,d=16-s){var u=Shuffle(c[i]);u=U(((u<<3)|U((u&57344u)>>13))^42405);c[i]=unchecked(U(((U(u&(((1<<d)-1u)<<s))>>s)|(u<<d))+i*177));}
			byte[]b=AB(c);for(int i=0;i<L(b);i++){b[i]=B(c[i/2]&255);b[++i]=B((c[i/2]&65280)>>8);}return b;
		}

		internal static string Unscramble(byte[] bytes) {
			if(bytes is not {Length:>0})return "";
			ushort[]c=AU(bytes);for(int i=0;i<L(bytes);)c[i/2]=U(bytes[i++]|(bytes[i++]<<8));
			for(int s=13,d=3,i=0;i<L(c);i++,s=s*3%14,d=16-s){var u=unchecked(U(c[i]-i*177));u=U((((u&((1<<d)-1u))<<s)|U(u>>d))^42405);c[i]=Unshuffle(U((u>>3)|((u&7)<<13)));}
			return new([..c.Select(C)]);
		}

		private static ushort[] _shuffle, _unshuffle;

		private static ushort Shuffle(ushort u) {
			if (_shuffle is null) {
				using Stream stream = MagicStorageMod.Instance.Code.GetManifestResourceStream("MagicStorage.Common.Systems.scramble");
				using BinaryReader reader = new(stream);
				_shuffle = new ushort[ushort.MaxValue];
				for (int i = 0; i < ushort.MaxValue; i++)
					_shuffle[i] = reader.ReadUInt16();
			}

			return _shuffle[u];
		}

		private static ushort Unshuffle(ushort u) {
			if (_unshuffle is null) {
				using Stream stream = MagicStorageMod.Instance.Code.GetManifestResourceStream("MagicStorage.Common.Systems.unscramble");
				using BinaryReader reader = new(stream);
				_unshuffle = new ushort[ushort.MaxValue];
				for (int i = 0; i < ushort.MaxValue; i++)
					_unshuffle[i] = reader.ReadUInt16();
			}

			return _unshuffle[u];
		}

		private static ushort[] AU(byte[] arr) => new ushort[arr.Length / 2];
		private static byte[] AB(ushort[] arr) => new byte[arr.Length * 2];
		private static byte B(int i) => (byte)i;
		private static char C(ushort u) => (char)u;
		private static int L(byte[] arr) => arr.Length;
		private static int L(ushort[] arr) => arr.Length;
		private static ushort U(char c) => c;
		private static ushort U(int i) => (ushort)i;
		private static ushort U(uint u) => (ushort)u;
		private static ushort U(long l) => (ushort)l;
	}
}
