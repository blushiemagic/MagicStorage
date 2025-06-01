using SerousCommonLib.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Terraria.ModLoader;

namespace MagicStorage.CrossMod.Calls {
	/// <summary>
	/// The base class for <see cref="Mod.Call(object[])"/> handlers
	/// </summary>
	public abstract class BaseCallFunction : ModType {
		private class StaticLoadable : ILoadable {
			void ILoadable.Load(Mod mod) { }

			void ILoadable.Unload() {
				_callFunctionLookup.Clear();
			}
		}

		private static readonly string[] _indexToName = [
			"1st", "2nd", "3rd", "4th", "5th", "6th", "7th", "8th", "9th", "10th",
			"11th", "12th", "13th", "14th", "15th", "16th", "17th", "18th", "19th", "20th",
		];

		public virtual string Function => PrettyPrintName();

		private static readonly Dictionary<string, Dictionary<string, BaseCallFunction>> _callFunctionLookup = [];

		public static BaseCallFunction Find(Mod mod, string function) {
			ArgumentNullException.ThrowIfNull(mod);
			ArgumentNullException.ThrowIfNull(function);

			if (_callFunctionLookup.TryGetValue(mod.Name, out var functionLookup) && functionLookup.TryGetValue(function, out var func))
				return func;

			foreach (var callFunction in mod.GetContent<BaseCallFunction>()) {
				if (callFunction.Function == function) {
					if (!_callFunctionLookup.TryGetValue(mod.Name, out functionLookup))
						_callFunctionLookup[mod.Name] = functionLookup = new();

					return functionLookup[function] = callFunction;
				}
			}

			throw new KeyNotFoundException($"Call function \"{function}\" not found in mod \"{mod.Name}\"");
		}

		/// <summary>
		/// The function which handles the arguments and return value for the call function
		/// </summary>
		public abstract object Call(ReadOnlySpan<object> args);

		protected sealed override void Register() {
			ModTypeLookup<BaseCallFunction>.Register(this);
		}

		protected sealed override void InitTemplateInstance() { }

		protected sealed override void ValidateType() { }

		[StackTraceHidden]
		protected T GetOrThrowIfNot<T>(ReadOnlySpan<object> args, int index) {
			if (args[index] is T value)
				return value;

			string argName = _indexToName.Length > index ? $"the {_indexToName[index]} argument" : $"argument {index + 1}";
			string expectedType = typeof(T).GetSimplifiedGenericTypeName();
			string actualType = args[index]?.GetType().GetSimplifiedGenericTypeName() ?? "null";
			throw new ArgumentException($"Call \"{Function}\" requires {argName} to be of type {expectedType}, but got {actualType} instead");
		}

		[StackTraceHidden]
		protected T GetOrThrowIfNotNull<T>(ReadOnlySpan<object> args, int index) where T : class {
			if (args[index] is T value)
				return value;
			if (args[index] is null)
				return null;

			string argName = _indexToName.Length > index ? $"the {_indexToName[index]} argument" : $"argument {index + 1}";
			string expectedType = typeof(T).GetSimplifiedGenericTypeName();
			string actualType = args[index].GetType().GetSimplifiedGenericTypeName();
			throw new ArgumentException($"Call \"{Function}\" requires {argName} to be of type {expectedType}, but got {actualType} instead");
		}

		[StackTraceHidden]
		protected NothingOr<T> GetOrThrowIfNotNothing<T>(ReadOnlySpan<object> args, int index) {
			if (index >= args.Length || args[index] is null)
				return default;

			if (args[index] is T value)
				return new NothingOr<T>(value);

			string argName = _indexToName.Length > index ? $"the {_indexToName[index]} argument" : $"argument {index + 1}";
			string expectedType = typeof(T).GetSimplifiedGenericTypeName();
			string actualType = args[index].GetType().GetSimplifiedGenericTypeName();
			throw new ArgumentException($"Call \"{Function}\" requires {argName} to be of type {expectedType}, but got {actualType} instead");
		}

		[StackTraceHidden]
		protected Either<TFirst, TSecond> GetOrThrowIfNotEither<TFirst, TSecond>(ReadOnlySpan<object> args, int index) {
			var arg = args[index];

			if (arg is TFirst first)
				return new Either<TFirst, TSecond>(first);
			if (arg is TSecond second)
				return new Either<TFirst, TSecond>(second);

			string argName = _indexToName.Length > index ? $"the {_indexToName[index]} argument" : $"argument {index + 1}";
			string expectedType = $"{typeof(TFirst).GetSimplifiedGenericTypeName()} or {typeof(TSecond).GetSimplifiedGenericTypeName()}";
			string actualType = arg?.GetType().GetSimplifiedGenericTypeName() ?? "null";
			throw new ArgumentException($"Call \"{Function}\" requires {argName} to be of type {expectedType}, but got {actualType} instead");
		}

		[StackTraceHidden]
		protected OneOfMany<T1, T2, T3> GetOrThrowIfNotAny<T1, T2, T3>(ReadOnlySpan<object> args, int index) {
			var arg = args[index];

			if (arg is T1 value1)
				return new OneOfMany<T1, T2, T3>(value1);
			if (arg is T2 value2)
				return new OneOfMany<T1, T2, T3>(value2);
			if (arg is T3 value3)
				return new OneOfMany<T1, T2, T3>(value3);

			string argName = _indexToName.Length > index ? $"the {_indexToName[index]} argument" : $"argument {index + 1}";
			string expectedType = $"{typeof(T1).GetSimplifiedGenericTypeName()}, {typeof(T2).GetSimplifiedGenericTypeName()} or {typeof(T3).GetSimplifiedGenericTypeName()}";
			string actualType = arg?.GetType().GetSimplifiedGenericTypeName() ?? "null";
			throw new ArgumentException($"Call \"{Function}\" requires {argName} to be of type {expectedType}, but got {actualType} instead");
		}

		[StackTraceHidden]
		protected void ThrowIfNotEnoughArgs(ReadOnlySpan<object> args, int minArgs) {
			if (args.Length < minArgs)
				throw new ArgumentException($"Call \"{Function}\" requires at least {minArgs} arguments, but got {args.Length} instead");
		}

		[StackTraceHidden]
		[DoesNotReturn]
		protected void ThrowWithMessage(string message, int argumentIndex) {
			string argName = _indexToName.Length > argumentIndex ? $"the {_indexToName[argumentIndex]} argument" : $"argument {argumentIndex + 1}";
			throw new ArgumentException($"Call \"{Function}\" could not be performed due to {argName} being invalid.\nReason: {message}");
		}
	}

	/// <summary>
	/// The base implementation of a <see cref="BaseCallFunction"/> which does not take any arguments.
	/// </summary>
	public abstract class BaseCallFunctionNoArgs : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) => Handle();

		/// <inheritdoc cref="Call"/>
		protected abstract object Handle();
	}

	public abstract class BaseCallFunctionNoReturn : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			Handle(args);
			return null;
		}

		/// <inheritdoc cref="Call"/>
		protected abstract void Handle(ReadOnlySpan<object> args);
	}

	public abstract class BaseCallFunctionNoReturn<T> : BaseCallFunctionNoReturn {
		protected sealed override void Handle(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 1);
			Handle(
				GetOrThrowIfNot<T>(args, 0)
			);
		}

		/// <inheritdoc cref="Handle(ReadOnlySpan<object>)"/>
		protected abstract void Handle(T arg);
	}

	public abstract class BaseCallFunctionNoReturn<T1, T2> : BaseCallFunctionNoReturn {
		protected sealed override void Handle(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 2);
			Handle(
				GetOrThrowIfNot<T1>(args, 0),
				GetOrThrowIfNot<T2>(args, 1)
			);
		}

		/// <inheritdoc cref="Handle(ReadOnlySpan<object>)"/>
		protected abstract void Handle(T1 arg1, T2 arg2);
	}

	public abstract class BaseCallFunctionNoReturn<T1, T2, T3> : BaseCallFunctionNoReturn {
		protected sealed override void Handle(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 3);
			Handle(
				GetOrThrowIfNot<T1>(args, 0),
				GetOrThrowIfNot<T2>(args, 1),
				GetOrThrowIfNot<T3>(args, 2)
			);
		}

		/// <inheritdoc cref="Handle(ReadOnlySpan<object>)"/>
		protected abstract void Handle(T1 arg1, T2 arg2, T3 arg3);
	}

	public abstract class BaseCallFunctionNoReturn<T1, T2, T3, T4> : BaseCallFunctionNoReturn {
		protected sealed override void Handle(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 4);
			Handle(
				GetOrThrowIfNot<T1>(args, 0),
				GetOrThrowIfNot<T2>(args, 1),
				GetOrThrowIfNot<T3>(args, 2),
				GetOrThrowIfNot<T4>(args, 3)
			);
		}

		/// <inheritdoc cref="Handle(ReadOnlySpan<object>)"/>
		protected abstract void Handle(T1 arg1, T2 arg2, T3 arg3, T4 arg4);
	}

	public abstract class BaseCallFunctionNoReturn<T1, T2, T3, T4, T5> : BaseCallFunctionNoReturn {
		protected sealed override void Handle(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 5);
			Handle(
				GetOrThrowIfNot<T1>(args, 0),
				GetOrThrowIfNot<T2>(args, 1),
				GetOrThrowIfNot<T3>(args, 2),
				GetOrThrowIfNot<T4>(args, 3),
				GetOrThrowIfNot<T5>(args, 4)
			);
		}

		/// <inheritdoc cref="Handle(ReadOnlySpan<object>)"/>
		protected abstract void Handle(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
	}

	public abstract class BaseCallFunction<TArg, TReturn> : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 1);
			return Handle(
				GetOrThrowIfNot<TArg>(args, 0)
			);
		}

		/// <inheritdoc cref="Call"/>
		protected abstract TReturn Handle(TArg arg);
	}

	public abstract class BaseCallFunction<TArg1, TArg2, TReturn> : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 2);
			return Handle(
				GetOrThrowIfNot<TArg1>(args, 0),
				GetOrThrowIfNot<TArg2>(args, 1)
			);
		}

		/// <inheritdoc cref="Call"/>
		protected abstract TReturn Handle(TArg1 arg1, TArg2 arg2);
	}

	public abstract class BaseCallFunction<TArg1, TArg2, TArg3, TReturn> : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 3);
			return Handle(
				GetOrThrowIfNot<TArg1>(args, 0),
				GetOrThrowIfNot<TArg2>(args, 1),
				GetOrThrowIfNot<TArg3>(args, 2)
			);
		}

		/// <inheritdoc cref="Call"/>
		protected abstract TReturn Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3);
	}

	public abstract class BaseCallFunction<TArg1, TArg2, TArg3, TArg4, TReturn> : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 4);
			return Handle(
				GetOrThrowIfNot<TArg1>(args, 0),
				GetOrThrowIfNot<TArg2>(args, 1),
				GetOrThrowIfNot<TArg3>(args, 2),
				GetOrThrowIfNot<TArg4>(args, 3)
			);
		}

		/// <inheritdoc cref="Call"/>
		protected abstract TReturn Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4);
	}

	public abstract class BaseCallFunction<TArg1, TArg2, TArg3, TArg4, TArg5, TReturn> : BaseCallFunction {
		public sealed override object Call(ReadOnlySpan<object> args) {
			ThrowIfNotEnoughArgs(args, 5);
			return Handle(
				GetOrThrowIfNot<TArg1>(args, 0),
				GetOrThrowIfNot<TArg2>(args, 1),
				GetOrThrowIfNot<TArg3>(args, 2),
				GetOrThrowIfNot<TArg4>(args, 3),
				GetOrThrowIfNot<TArg5>(args, 4)
			);
		}

		/// <inheritdoc cref="Call"/>
		protected abstract TReturn Handle(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5);
	}
}
