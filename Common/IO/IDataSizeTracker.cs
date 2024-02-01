using System;
using System.Runtime.CompilerServices;
using Terraria.ModLoader;

namespace MagicStorage.Common.IO {
	public interface IDataSizeTracker : ILoadable {
		/// <summary>
		/// How many bits are required to store the data for this tracker
		/// </summary>
		int BitCount { get; }

		void ILoadable.Load(Mod mod) { }

		void ILoadable.Unload() { }

		void Receive(ref object value, ValueReader reader);

		void Send(object value, ValueWriter writer);
	}

	public interface IDataSizeTracker<T> : IDataSizeTracker {
		void IDataSizeTracker.Receive(ref object value, ValueReader reader) {
			if (value is not T)
				throw new ArgumentException($"Expected value of type {typeof(T).Name}, but got {value.GetType().Name}", nameof(value));

			Receive(ref Unsafe.As<object, T>(ref value), reader);
		}

		void Receive(ref T value, ValueReader reader);

		void IDataSizeTracker.Send(object value, ValueWriter writer) => Send((T)value, writer);

		void Send(T value, ValueWriter writer);
	}

	public abstract class DataSizeTracker : ModType, IDataSizeTracker {
		public int Type { get; private set; }

		/// <inheritdoc/>
		public abstract int BitCount { get; }

		protected sealed override void Register() {
			Type = NetCompression.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		public abstract void Receive(ref object value, ValueReader reader);
		
		public abstract void Send(object value, ValueWriter writer);
	}

	public abstract class DataSizeTracker<T> : ModType, IDataSizeTracker<T> {
		public int Type { get; private set; }

		/// <inheritdoc/>
		public abstract int BitCount { get; }

		protected sealed override void Register() {
			Type = NetCompression.Add(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();

		public abstract void Receive(ref T value, ValueReader reader);

		public abstract void Send(T value, ValueWriter writer);
	}
}
