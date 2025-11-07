using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria.ModLoader.IO;

namespace MagicStorage.Common.IO {
	partial class SaveCompression {
		private static readonly LengthCompressor<uint> _collectionLengthTiers;

		private static readonly ConditionalWeakTable<ValueReader, GenericKeyLookup> _tagKeyReaderLookup = [];
		private static readonly ConditionalWeakTable<ValueWriter, GenericKeyLookup> _tagKeyWriterLookup = [];

		static SaveCompression() {
			// Many tiers for smaller collections, and fewer for rarer large arrays
			var tier0 = EncodingTier.CreateZero<uint>  (prefix: 0b_00, 2, size: 16);
			var tier1 = tier0.CreateSuccessive         (prefix: 0b_01, 2, size: 64);
			var tier2 = tier1.CreateSuccessive         (prefix: 0b_10, 2, size: 256);
			var tier3 = tier2.CreateSuccessive         (prefix: 0b_11, 2, size: 4096);
			var tier4 = tier3.CreateSuccessive         (prefix: 0b011, 3, size: 131072);
			var tier5 = tier4.CreateSuccessiveUnbounded(prefix: 0b111, 3);

			_collectionLengthTiers = new LengthCompressor<uint>(tier0, tier1, tier2, tier3, tier4, tier5);

			// Since this implementation has handlers which share a PayloadType, the dictionary needs to be created manually
			_payloadIDs = [];
			for (int i = 1; i < _payloadHandlers.Length; i++) {
				var type = _payloadHandlers[i].PayloadType;
				if (!_payloadIDs.ContainsKey(type))
					_payloadIDs[type] = i;
			}
		}

		private static readonly PayloadHandler[] _payloadHandlers = [
			null,
			new PayloadHandler<byte>(r => r.ReadByte(BitBuffer128.MAX_BYTE), (w, v) => w.Write(v, BitBuffer128.MAX_BYTE)),
			new PayloadHandler<short>(r => r.ReadInt16(BitBuffer128.MAX_SHORT), (w, v) => w.Write(v, BitBuffer128.MAX_SHORT)),
			new PayloadHandler<int>(r => r.ReadInt32(BitBuffer128.MAX_INT), (w, v) => w.Write(v, BitBuffer128.MAX_INT)),
			new PayloadHandler<long>(r => r.ReadInt64(BitBuffer128.MAX_LONG), (w, v) => w.Write(v, BitBuffer128.MAX_LONG)),
			new PayloadHandler<float>(
				r => {
					// ValueReader doesn't support floats natively
					uint bits = r.ReadUInt32(BitBuffer128.MAX_INT);
					return BitConverter.UInt32BitsToSingle(bits);
				},
				(w, v) => {
					// ValueWriter doesn't support floats natively
					uint bits = BitConverter.SingleToUInt32Bits(v);
					w.Write(bits, BitBuffer128.MAX_INT);
				}
			),
			new PayloadHandler<double>(
				r => {
					// ValueReader doesn't support doubles natively
					ulong bits = r.ReadUInt64(BitBuffer128.MAX_LONG);
					return BitConverter.UInt64BitsToDouble(bits);
				},
				(w, v) => {
					// ValueWriter doesn't support doubles natively
					ulong bits = BitConverter.DoubleToUInt64Bits(v);
					w.Write(bits, BitBuffer128.MAX_LONG);
				}
			),
			new PayloadHandler<byte[]>(
				r => r.ReadBytes((int)_collectionLengthTiers.ReadFrom(r)),
				(w, v) => {
					_collectionLengthTiers.WriteTo(w, (uint)v.Length);
					w.WriteBytesNoLength(v);
				}
			),
			new PayloadHandler<string>(
				StringCompressor.ReadFrom,
				StringCompressor.WriteTo
			),
			new PayloadHandler<IList>(
				r => GetHandler(r.ReadByte(SIZE_ID)).ReadList(r, (int)_collectionLengthTiers.ReadFrom(r)),
				(w, v) => {
					int id;
					try {
						id = GetPayloadId(GetListElementType(v.GetType()));
					} catch (Exception ex) {
						throw new InvalidOperationException($"Invalid list type: {v.GetType()}", ex);
					}
					w.Write((byte)id, SIZE_ID);
					_collectionLengthTiers.WriteTo(w, (uint)v.Count);
					_payloadHandlers[id].WriteList(w, v);
				}
			),
			new PayloadHandler<TagCompound>(
				r => {
					TagCompound tag = [];
					object obj = null;
					var lookup = _tagKeyReaderLookup.GetValue(r, r => null);

					while ((obj = ReadObject(r, lookup, out string name)) is not null)
						tag.Set(name, obj);

					return tag;
				},
				(w, v) => {
					var lookup = _tagKeyWriterLookup.GetValue(w, w => null);

					foreach ((string key, object value) in v) {
						if (value is not null)
							WriteObject(w, lookup, key, value);
					}

					w.Write((byte)0, SIZE_ID);
				}
			),
			new PayloadHandler<int[]>(
				r => {
					int length = (int)_collectionLengthTiers.ReadFrom(r);
					int[] array = GC.AllocateUninitializedArray<int>(length);
					for (int i = 0; i < length; i++)
						array[i] = r.ReadInt32(BitBuffer128.MAX_INT);
					return array;
				},
				(w, v) => {
					_collectionLengthTiers.WriteTo(w, (uint)v.Length);
					for (int i = 0; i < v.Length; i++)
						w.Write(v[i], BitBuffer128.MAX_INT);
				}
			),
			
			// Extra defined handlers not in TagIO (since this doesn't have to match NBT specs 1:1)

			new PayloadHandler<bool>(  // ID_BOOL
				r => r.ReadBoolean(),
				(w, v) => w.Write(v)
			),
			new PayloadHandler<int>(  // ID_TINY_INT
				r => r.ReadInt32(8),
				(w, v) => w.Write(v, 8)
			),
			new PayloadHandler<short>(  // ID_TINY_SHORT
				r => r.ReadInt16(4),
				(w, v) => w.Write(v, 4)
			),
			new PayloadHandler<long>(  // ID_TINY_LONG
				r => r.ReadInt64(12),
				(w, v) => w.Write(v, 12)
			)
		];

		// Initialization moved to static ctor
	//	private static readonly Dictionary<Type, int> _payloadIDs = Enumerable.Range(1, _payloadHandlers.Length - 1).ToDictionary(i => _payloadHandlers[i].PayloadType);
		private static readonly Dictionary<Type, int> _payloadIDs;

		private static PayloadHandler GetHandler(int id) {
			if (id < 1 || id >= _payloadHandlers.Length)
				throw new ArgumentOutOfRangeException(nameof(id), "Invalid payload ID");

			return _payloadHandlers[id];
		}

		private static int GetPayloadId(Type t) {
			if (_payloadIDs.TryGetValue(t, out int id))
				return id;

			if (typeof(IList).IsAssignableFrom(t))
				return ID_LISTS;

			throw new NotSupportedException($"Unsupported payload type: {t}");
		}

		private static Type GetListElementType(Type type) => type.GetElementType() ?? type.GetGenericArguments()[0];

		private abstract class PayloadHandler {
			public abstract Type PayloadType { get; }
			public abstract object Read(ValueReader r);
			public abstract void Write(ValueWriter w, object v);
			public abstract IList ReadList(ValueReader r, int size);
			public abstract void WriteList(ValueWriter w, IList list);
		}

		private class PayloadHandler<T> : PayloadHandler
			where T : notnull
		{
			internal Func<ValueReader, T> reader;
			internal Action<ValueWriter, T> writer;

			public PayloadHandler(Func<ValueReader, T> reader, Action<ValueWriter, T> writer) {
				this.reader = reader;
				this.writer = writer;
			}

			public override Type PayloadType => typeof(T);
			public override object Read(ValueReader r) => reader(r);
			public override void Write(ValueWriter w, object v) => writer(w, (T)v);

			public override IList ReadList(ValueReader r, int size) {
				var list = new List<T>(size);
				for (int i = 0; i < size; i++)
					list.Add(reader(r));

				return list;
			}

			public override void WriteList(ValueWriter w, IList list) {
				foreach (T t in (IEnumerable<T>)list)
					writer(w, t);
			}
		}
	}
}
