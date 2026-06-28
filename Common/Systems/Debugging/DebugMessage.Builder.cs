using System;

namespace MagicStorage.Common.Systems.Debugging {
	partial class DebugMessage {
		public static Builder Create() {
			ReserveThreadContext();
			BeginReportGroup();
			RememberCurrentGroup();
			return new(BuilderKind.FullContext);
		}

		public static Builder Create(bool condition) {
			if (condition) {
				ReserveThreadContext();
				BeginReportGroup();
				RememberCurrentGroup();
			}

			return new(condition ? BuilderKind.FullContext : BuilderKind.NotDebugging);
		}

		public static Builder CreateIf(string control) => Create(DebugControls.Get(control));

		public static Builder CreateIf(DebugControls.Combination combination) => Create(combination.Result());

		public static Builder CreateIfAll(params ReadOnlySpan<string> controls) => Create(DebugControls.All(controls));

		public static Builder CreateIfAny(params ReadOnlySpan<string> controls) => Create(DebugControls.Any(controls));

		public static Builder Chain() => new(BuilderKind.NoCleanup);

		public static Builder Chain(bool condition) => new(condition ? BuilderKind.NoCleanup : BuilderKind.NotDebugging);

		public static Builder ChainIf(string control) => Chain(DebugControls.Get(control));

		public static Builder ChainIf(DebugControls.Combination combination) => Chain(combination.Result());

		public static Builder ChainIfAll(params ReadOnlySpan<string> controls) => Chain(DebugControls.All(controls));

		public static Builder ChainIfAny(params ReadOnlySpan<string> controls) => Chain(DebugControls.Any(controls));

		public enum BuilderKind {
			NotDebugging,
			FullContext,
			NoCleanup
		}

		public readonly ref struct Builder : IDisposable {
			private readonly BuilderKind _kind;

			public bool IsDebugging => (int)_kind > (int)BuilderKind.NotDebugging;

			internal Builder(BuilderKind kind) => _kind = kind;

			public Builder Create() => IsDebugging ? DebugMessage.Create() : this;

			public Builder Create(bool condition) => IsDebugging ? DebugMessage.Create(condition) : this;

			public Builder CreateIf(string control) => IsDebugging ? DebugMessage.CreateIf(control) : this;

			public Builder CreateIfAll(params string[] controls) => IsDebugging ? DebugMessage.CreateIfAll(controls) : this;

			public Builder CreateIfAny(params string[] controls) => IsDebugging ? DebugMessage.CreateIfAny(controls) : this;

			public Builder Chain() => IsDebugging ? DebugMessage.Chain() : this;

			public Builder Chain(bool condition) => IsDebugging ? DebugMessage.Chain(condition) : this;

			public Builder ChainIf(string control) => IsDebugging ? DebugMessage.ChainIf(control) : this;

			public Builder ChainIfAll(params string[] controls) => IsDebugging ? DebugMessage.ChainIfAll(controls) : this;

			public Builder ChainIfAny(params string[] controls) => IsDebugging ? DebugMessage.ChainIfAny(controls) : this;

			public void Dispose() {
				if (_kind == BuilderKind.FullContext) {
					DebugMessage.RecallSavedGroup();
					DebugMessage.EndReportGroup();
					DebugMessage.FreeThreadContext();
				}
			}

			public Builder Report(bool reportTime, string msg) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, msg);
				return this;
			}

			public Builder Report(bool reportTime, string fmt, object arg) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, fmt, arg);
				return this;
			}

			public Builder Report(bool reportTime, string fmt, object arg0, object arg1) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, fmt, arg0, arg1);
				return this;
			}

			public Builder Report(bool reportTime, string fmt, object arg0, object arg1, object arg2) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, fmt, arg0, arg1, arg2);
				return this;
			}

			public Builder Report(bool reportTime, string fmt, params object[] args) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, fmt, args);
				return this;
			}

			public Builder Report(bool reportTime, NetmodeContextMessage contextMessages) {
				if (IsDebugging)
					DebugMessage.Report(reportTime, contextMessages);
				return this;
			}

			public Builder BeginReportGroup() {
				if (IsDebugging)
					DebugMessage.BeginReportGroup();
				return this;
			}

			public Builder EndReportGroup() {
				if (IsDebugging)
					DebugMessage.EndReportGroup();
				return this;
			}

			public Builder Indent() {
				if (IsDebugging)
					DebugMessage.Indent();
				return this;
			}

			public Builder Unindent() {
				if (IsDebugging)
					DebugMessage.Unindent();
				return this;
			}

			public Builder RememberCurrentGroup() {
				if (IsDebugging)
					DebugMessage.RememberCurrentGroup();
				return this;
			}

			public Builder RecallSavedGroup() {
				if (IsDebugging)
					DebugMessage.RecallSavedGroup();
				return this;
			}

			public Builder ReserveThreadContext() {
				if (IsDebugging)
					DebugMessage.ReserveThreadContext();
				return this;
			}

			public Builder FreeThreadContext() {
				if (IsDebugging)
					DebugMessage.FreeThreadContext();
				return this;
			}
		}
	}
}
