using MagicStorage.Common.Systems;
using System;

namespace MagicStorage.Common {
	/// <summary>
	/// An interface for objects which can be watched for changes to determine if the UI should be refreshed
	/// </summary>
	public interface IRefreshUIWatchTarget {
		/// <summary>
		/// Determine the current state of the target here.  If the state has changed, the UI will be refreshed.
		/// </summary>
		bool GetCurrentState();

		/// <summary>
		/// This method runs when this target's state has changed.  If <paramref name="forceFullRefresh"/> is <see langword="true"/>, then the UI will be fully refreshed.
		/// </summary>
		/// <param name="forceFullRefresh">Whether a full refresh (<see langword="true"/>) or a partial refresh (<see langword="false"/>) should be performed if the state has changed
		void OnStateChange(out bool forceFullRefresh);
	}

	/// <summary>
	/// A class which can be used to watch for changes to determine if the UI should be refreshed
	/// </summary>
	public sealed class RefreshUIWatchdog {
		private readonly IRefreshUIWatchTarget _target;
		private readonly bool _initialState;
		private bool _hasStateChanged;

		internal RefreshUIWatchdog(IRefreshUIWatchTarget target, bool initialState) {
			_target = target;
			_initialState = initialState;
		}

		internal bool Observe() {
			if (_hasStateChanged) {
				// Target already in a "changed" state, so no need to check again
				return false;
			}

			_hasStateChanged = _target.GetCurrentState() != _initialState;
			return _hasStateChanged;
		}

		internal void OnStateChange(out bool forceFullRefresh) => _target.OnStateChange(out forceFullRefresh);
	}

	/// <summary>
	/// A wrapper for a lambda function which can be used to watch for changes to determine if the UI should be refreshed
	/// </summary>
	public sealed class LambdaWatchTarget<T> : IRefreshUIWatchTarget {
		private readonly Func<T, bool> _observer;
		private readonly Func<T, bool> _forceFullRefresh;
		private readonly T _target;

		/// <summary>
		/// Creates a new <see cref="LambdaWatchTarget{T}"/> which can be used to watch for changes in <paramref name="target"/>
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="observer">The function used to observe changes in the target</param>
		/// <param name="forceFullRefresh">The function used to set whether a full refresh should be performed</param>
		public LambdaWatchTarget(T target, Func<T, bool> observer, Func<T, bool> forceFullRefresh) {
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(observer);

			_target = target;
			_observer = observer;
			_forceFullRefresh = forceFullRefresh;
		}

		public bool GetCurrentState() => _observer(_target);

		public void OnStateChange(out bool forceFullRefresh) => forceFullRefresh = _forceFullRefresh(_target);
	}
}
