using System.Linq;

namespace MagicStorage.Common.Systems {
	//Copied from Terran Automation
	internal static class StringScrambling {
		internal static string Scramble(string orig){
			ushort[] c=orig.Select(a=>(ushort)a).ToArray();int l=c.Length,i=0;ushort d=(ushort)((c[l-1]&3)<<14),s;
			for(;i<l;i++){s=d;d=(ushort)((c[i]&3)<<14);c[i]=(ushort)(((c[i]>>2)|s)^40659);}
			return new string(c.Select(a=>(char)a).ToArray());
		}

		internal static string Unscramble(string orig){
			ushort[] c=orig.Select(a=>(ushort)a).ToArray();int l=c.Length;ushort d=(ushort)(((c[0]^40659)&49152)>>14),s;
			while(l-->0){s=d;d=(ushort)(((c[l]^40659)&49152)>>14);c[l]=(ushort)(((c[l]^40659)<<2)|s);}
			return new string(c.Select(a=>(char)a).ToArray());
		}

		internal static byte[] ToBytes(string str){
			//Can't rely on Encoding.UTF8.GetBytes() here
			byte[] buffer = new byte[str.Length * 2];
			char[] letters = str.ToCharArray();

			for(int i = 0; i < letters.Length; i++){
				ushort u = (ushort)letters[i];
				buffer[i * 2] = (byte)((u & 0xff00) >> 8);
				buffer[i * 2 + 1] = (byte)(u & 0x00ff);
			}

			return buffer;
		}

		internal static string FromBytes(byte[] bytes){
			char[] letters = new char[bytes.Length / 2];

			for(int i = 0; i < letters.Length; i++)
				letters[i] = (char)(((bytes[i * 2]) << 8) | bytes[i * 2 + 1]);

			return new string(letters);
		}
	}
}
