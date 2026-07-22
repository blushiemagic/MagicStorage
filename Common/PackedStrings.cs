using System;
using System.IO;
using Terraria;

namespace MagicStorage.Common {
	/// <summary>
	/// Stores up to eight optional strings in a compact serializable value.
	/// </summary>
	public struct PackedStrings {
		private string _s0, _s1, _s2, _s3, _s4, _s5, _s6, _s7;

		/// <summary>
		/// Gets or sets a string by slot index.
		/// </summary>
		/// <param name="index">The slot index, from 0 through 7.</param>
		/// <returns>The string in the specified slot, or <see langword="null" /> when the slot is empty.</returns>
		public string this[int index] {
			readonly get {
				return index switch {
					0 => _s0,
					1 => _s1,
					2 => _s2,
					3 => _s3,
					4 => _s4,
					5 => _s5,
					6 => _s6,
					7 => _s7,
					_ => null
				};
			}
			set {
				switch (index) {
					case 0:
						_s0 = value;
						break;
					case 1:
						_s1 = value;
						break;
					case 2:
						_s2 = value;
						break;
					case 3:
						_s3 = value;
						break;
					case 4:
						_s4 = value;
						break;
					case 5:
						_s5 = value;
						break;
					case 6:
						_s6 = value;
						break;
					case 7:
						_s7 = value;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(index));
				}
			}
		}

		/// <summary>
		/// Creates a packed value with one string slot.
		/// </summary>
		/// <param name="s0">The first string.</param>
		public PackedStrings(string s0) {
			_s0 = s0;
			_s1 = _s2 = _s3 = _s4 = _s5 = _s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with two string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		public PackedStrings(string s0, string s1) {
			_s0 = s0;
			_s1 = s1;
			_s2 = _s3 = _s4 = _s5 = _s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with three string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		public PackedStrings(string s0, string s1, string s2) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = _s4 = _s5 = _s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with four string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		public PackedStrings(string s0, string s1, string s2, string s3) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = s3;
			_s4 = _s5 = _s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with five string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		public PackedStrings(string s0, string s1, string s2, string s3, string s4) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = s3;
			_s4 = s4;
			_s5 = _s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with six string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		public PackedStrings(string s0, string s1, string s2, string s3, string s4, string s5) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = s3;
			_s4 = s4;
			_s5 = s5;
			_s6 = _s7 = null;
		}

		/// <summary>
		/// Creates a packed value with seven string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		/// <param name="s6">The seventh string.</param>
		public PackedStrings(string s0, string s1, string s2, string s3, string s4, string s5, string s6) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = s3;
			_s4 = s4;
			_s5 = s5;
			_s6 = s6;
			_s7 = null;
		}

		/// <summary>
		/// Creates a packed value with eight string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		/// <param name="s6">The seventh string.</param>
		/// <param name="s7">The eighth string.</param>
		public PackedStrings(string s0, string s1, string s2, string s3, string s4, string s5, string s6, string s7) {
			_s0 = s0;
			_s1 = s1;
			_s2 = s2;
			_s3 = s3;
			_s4 = s4;
			_s5 = s5;
			_s6 = s6;
			_s7 = s7;
		}

		/// <summary>
		/// Retrieves the first string slot.
		/// </summary>
		/// <param name="s0">The first string.</param>
		public readonly void Retrieve(out string s0) {
			s0 = _s0;
		}

		/// <summary>
		/// Retrieves the first two string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		public readonly void Retrieve(out string s0, out string s1) {
			s0 = _s0;
			s1 = _s1;
		}

		/// <summary>
		/// Retrieves the first three string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
		}

		/// <summary>
		/// Retrieves the first four string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2, out string s3) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
			s3 = _s3;
		}

		/// <summary>
		/// Retrieves the first five string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2, out string s3, out string s4) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
			s3 = _s3;
			s4 = _s4;
		}

		/// <summary>
		/// Retrieves the first six string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2, out string s3, out string s4, out string s5) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
			s3 = _s3;
			s4 = _s4;
			s5 = _s5;
		}

		/// <summary>
		/// Retrieves the first seven string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		/// <param name="s6">The seventh string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2, out string s3, out string s4, out string s5, out string s6) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
			s3 = _s3;
			s4 = _s4;
			s5 = _s5;
			s6 = _s6;
		}

		/// <summary>
		/// Retrieves all eight string slots.
		/// </summary>
		/// <param name="s0">The first string.</param>
		/// <param name="s1">The second string.</param>
		/// <param name="s2">The third string.</param>
		/// <param name="s3">The fourth string.</param>
		/// <param name="s4">The fifth string.</param>
		/// <param name="s5">The sixth string.</param>
		/// <param name="s6">The seventh string.</param>
		/// <param name="s7">The eighth string.</param>
		public readonly void Retrieve(out string s0, out string s1, out string s2, out string s3, out string s4, out string s5, out string s6, out string s7) {
			s0 = _s0;
			s1 = _s1;
			s2 = _s2;
			s3 = _s3;
			s4 = _s4;
			s5 = _s5;
			s6 = _s6;
			s7 = _s7;
		}

		/// <summary>
		/// Writes this packed value to a binary stream.
		/// </summary>
		/// <param name="writer">The binary writer to write to.</param>
		public readonly void Write(BinaryWriter writer) {
			BitsByte bb = default;
			bb[0] = _s0 is not null;
			bb[1] = _s1 is not null;
			bb[2] = _s2 is not null;
			bb[3] = _s3 is not null;
			bb[4] = _s4 is not null;
			bb[5] = _s5 is not null;
			bb[6] = _s6 is not null;
			bb[7] = _s7 is not null;

			writer.Write(bb);
			if (_s0 is not null)
				writer.Write(_s0);
			if (_s1 is not null)
				writer.Write(_s1);
			if (_s2 is not null)
				writer.Write(_s2);
			if (_s3 is not null)
				writer.Write(_s3);
			if (_s4 is not null)
				writer.Write(_s4);
			if (_s5 is not null)
				writer.Write(_s5);
			if (_s6 is not null)
				writer.Write(_s6);
			if (_s7 is not null)
				writer.Write(_s7);
		}

		/// <summary>
		/// Reads a packed string value from a binary stream.
		/// </summary>
		/// <param name="reader">The binary reader to read from.</param>
		/// <returns>The packed strings read from the stream.</returns>
		public static PackedStrings Read(BinaryReader reader) {
			BitsByte bb = reader.ReadByte();
			return new PackedStrings(
				bb[0] ? reader.ReadString() : null,
				bb[1] ? reader.ReadString() : null,
				bb[2] ? reader.ReadString() : null,
				bb[3] ? reader.ReadString() : null,
				bb[4] ? reader.ReadString() : null,
				bb[5] ? reader.ReadString() : null,
				bb[6] ? reader.ReadString() : null,
				bb[7] ? reader.ReadString() : null
			);
		}
	}
}
