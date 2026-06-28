using Microsoft.Xna.Framework;
using ReLogic.Content;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace MagicStorage.Common.Systems.Debugging {
	internal record struct NetmodeContextMessage(FormattedString ChatMessage, FormattedString ConsoleOrLogMessage);

	internal readonly record struct FormattedString {
		public string Format { get; }

		public string Result { get; }

		public FormattedString(string Format) {
			this.Format = Format;
			this.Result = Format;
		}

		public FormattedString(string Format, object Arg) {
			this.Format = Format;
			this.Result = string.Format(Format, Arg);
		}

		public FormattedString(string Format, object Arg0, object Arg1) {
			this.Format = Format;
			this.Result = string.Format(Format, Arg0, Arg1);
		}

		public FormattedString(string Format, object Arg0, object Arg1, object Arg2) {
			this.Format = Format;
			this.Result = string.Format(Format, Arg0, Arg1, Arg2);
		}

		public FormattedString(string Format, params object[] Args) {
			this.Format = Format;
			this.Result = string.Format(Format, Args);
		}

		public static implicit operator string(FormattedString str) => str.Result;

		public static implicit operator FormattedString(string str) => new(str);
	}

	internal static partial class DebugMessage {
		private static readonly ThreadState _mainThreadState = new();
		private static readonly ConcurrentDictionary<int, ThreadState> _threadStates = [];

		public static void Report(bool reportTime, string msg) => ReportString(reportTime, msg);

		public static void Report(bool reportTime, string fmt, object arg) => ReportString(reportTime, string.Format(fmt, arg));

		public static void Report(bool reportTime, string fmt, object arg0, object arg1) => ReportString(reportTime, string.Format(fmt, arg0, arg1));

		public static void Report(bool reportTime, string fmt, object arg0, object arg1, object arg2) => ReportString(reportTime, string.Format(fmt, arg0, arg1, arg2));

		public static void Report(bool reportTime, string fmt, params object[] args) => ReportString(reportTime, string.Format(fmt, args));

		private static void ReportString(bool reportTime, string msg) {
			// Always forward messages to a queued action.
			// Before, reports could run immediately if done on the main thread.
			// However, this led to incorrect report orders, which is undesirable.
			ServerActionsQueue.QueueActionBasedOnClientPresence(new DeferredMessage(ThreadReference.Current(), DateTime.Now, reportTime, msg).HandleMessage);
		}

		public static void Report(bool reportTime, NetmodeContextMessage contextMessages) {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new DeferredChatMessage(ThreadReference.Current(), DateTime.Now, reportTime, contextMessages.ChatMessage, contextMessages.ConsoleOrLogMessage).HandleMessage);
		}

		private record class DeferredMessage(ThreadReference Thread, DateTime Time, bool ReportTime, string Message) {
			public void HandleMessage() {
				var state = Thread.GetState();

				int totalIndent = state.activeGroup is MessageGroup group
					? group.GetBaseIndent() + state.localIndent
					: state.localIndent;

				new ContextualMessage(Time, ReportTime, totalIndent, Message).Print();
			}
		}

		private record class DeferredChatMessage(ThreadReference Thread, DateTime Time, bool ReportTime, string ChatMessage, string ConsoleOrLogMessage) {
			public void HandleMessage() {
				var state = Thread.GetState();

				int totalIndent = state.activeGroup is MessageGroup group
					? group.GetBaseIndent() + state.localIndent
					: state.localIndent;

				var plainMsg = new ContextualMessage(Time, ReportTime, totalIndent, ConsoleOrLogMessage);

				if (Main.netMode != NetmodeID.Server)
					new ContextualMessage(Time, ReportTime, totalIndent, ChatMessage).PrintChat();
				else if (Main.dedServ)
					plainMsg.PrintServer();

				plainMsg.PrintLog();
			}
		}

		private record class ContextualMessage(DateTime Time, bool ReportTime, int Indent, string Message) {
			public void Print() {
				if (Main.netMode != NetmodeID.Server)
					PrintChat();
				else if (Main.dedServ)
					PrintServer();

				PrintLog();
			}

			public void PrintChat() {
				if (MagicStorageConfig.PrintDebuggingTextToChat) {
					if (ReportTime)
						Main.NewText($"Time: {Time.Ticks}", color: Color.Orange);

					Main.NewTextMultiline(Message, c: Color.White);
				}
			}

			public void PrintServer() {
				if (ReportTime)
					Utility.PrettyWriteLineToConsole($"Time: {Time.Ticks}", ConsoleColor.Red, ConsoleColor.Black);

				Utility.WriteLineSafely(Message);
			}

			public void PrintLog() {
				MagicStorageMod.Instance.Logger.Debug($"{DebugHelper.BuildIndentString(Indent)}{Message}");
			}
		}

		public static void Indent() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new IndentationChange(ThreadReference.Current()).Increment);
		}

		public static void Unindent() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new IndentationChange(ThreadReference.Current()).Decrement);
		}

		private record class IndentationChange(ThreadReference Thread) {
			public void Increment() {
				var state = Thread.GetState();
				state.localIndent++;
			}

			public void Decrement() {
				var state = Thread.GetState();
				if (state.localIndent > 0)
					state.localIndent--;
			}
		}

		public static void BeginReportGroup() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new GroupChange(ThreadReference.Current()).HandleCreation);
		}

		public static void EndReportGroup() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new GroupChange(ThreadReference.Current()).HandleTermination);
		}

		private record class GroupChange(ThreadReference Thread) {
			public void HandleCreation() {
				var state = Thread.GetState();

				state.activeGroup = new(state.activeGroup, state.localIndent);
				state.localIndent = 0;
			}

			public void HandleTermination() {
				var state = Thread.GetState();
				
				MessageGroup group = state.activeGroup;
				if (group is null)
					return;

				state.activeGroup = group.parent;
				state.localIndent = group.localIndent;
			}
		}

		private class MessageGroup(MessageGroup parent, int localIndent) {
			public readonly int localIndent = localIndent;
			public readonly MessageGroup parent = parent;

			public int GetBaseIndent() {
				int totalIndent = 0;
				MessageGroup current = this;

				while (current is not null) {
					totalIndent += current.localIndent;
					current = current.parent;
				}

				return totalIndent;
			}
		}

		public static void RememberCurrentGroup() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new GroupMemoryUpdate(ThreadReference.Current()).Remember);
		}

		public static void RecallSavedGroup() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new GroupMemoryUpdate(ThreadReference.Current()).Recall);
		}

		private record class GroupMemoryUpdate(ThreadReference Thread) {
			public void Remember() {
				// Store the group so that exception clauses can properly end all groups that were created within the caller's context
				var state = Thread.GetState();

				state.memoryGroups.Push(state.activeGroup);
			}

			public void Recall() {
				/*
				   Attempt to unwind to the most recently remembered group
				   This method can encounter the following cases:
			
					 1) All created groups were ended.
						In this case, the top of the stack will already the current group.

					 2) An exception occurred, causing some groups to not be ended.
						In this case, the top of the stack will not be the current group, and we will need to end groups until we reach it.
				*/
				var state = Thread.GetState();

				if (state.memoryGroups.Count == 0) {
					MagicStorageMod.Instance.Logger.Warn("DebugMessage attempted to perform RecallSavedGroup() when no groups were saved by RememberCurrentGroup().");
					return;
				}

				var top = state.memoryGroups.Pop();

				var changeAction = new GroupChange(Thread);

				// End groups until we reach the remembered group
				while (!object.ReferenceEquals(state.activeGroup, top))
					changeAction.HandleTermination();
			}
		}

		public static void ReserveThreadContext() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new ThreadReservation(ThreadReference.Current()).Reserve);
		}

		public static void FreeThreadContext() {
			ServerActionsQueue.QueueActionBasedOnClientPresence(new ThreadReservation(ThreadReference.Current()).Free);
		}

		private record class ThreadReservation(ThreadReference Thread) {
			public void Reserve() {
				if (Thread.mainThread) {
					_mainThreadState.reservations++;
					return;
				}

				// If the thread already has a state, just increment the reservation count
				if (_threadStates.TryGetValue(Thread.threadID, out ThreadState state)) {
					state.reservations++;
					return;
				}

				// Otherwise, create a new state for the thread
				state = new ThreadState() { reservations = 1 };
				_threadStates[Thread.threadID] = state;
			}

			public void Free() {
				if (Thread.mainThread) {
					_mainThreadState.reservations--;

					// If reservations drop to 0, reset the main thread state
					if (_mainThreadState.reservations <= 0) {
						_mainThreadState.localIndent = 0;
						_mainThreadState.activeGroup = null;
						_mainThreadState.memoryGroups.Clear();
					}

					return;
				}

				if (_threadStates.TryGetValue(Thread.threadID, out ThreadState state)) {
					state.reservations--;

					// If reservations drop to 0, remove the thread state
					if (state.reservations <= 0)
						_threadStates.TryRemove(Thread.threadID, out _);

					return;
				}

				MagicStorageMod.Instance.Logger.Warn($"DebugMessage attempted to perform FreeThreadContext() when thread ID {Thread.threadID} had not reserved a thread through ReserveThreadContext().");
			}
		}

		private class ThreadState {
			public int localIndent = 0;
			public MessageGroup activeGroup;
			public readonly Stack<MessageGroup> memoryGroups = [];
			public int reservations = 0;
		}

		private class ThreadReference {
			public readonly bool mainThread;
			public readonly int threadID;

			private ThreadReference(bool mainThread, int threadID) {
				this.mainThread = mainThread;
				this.threadID = threadID;
			}

			public ThreadState GetState() {
				if (mainThread)
					return _mainThreadState;

				if (_threadStates.TryGetValue(threadID, out ThreadState state))
					return state;

				// Fallback to main thread state
				MagicStorageMod.Instance.Logger.Warn($"DebugMessage attempted to access the context for thread ID {threadID}, but none was reserved through ReserveThreadContext().");
				return _mainThreadState;
			}

			public static ThreadReference Current() => new(AssetRepository.IsMainThread, Environment.CurrentManagedThreadId);
		}
	}

	file static class DebugHelper {
		public static string BuildIndentString(int indent) {
			// Reduce GC strain by hardcoding lengths up to 10 indents
			return indent switch {
				0  => "",
				1  => "  ",
				2  => "    ",
				3  => "      ",
				4  => "        ",
				5  => "          ",
				6  => "            ",
				7  => "              ",
				8  => "                ",
				9  => "                  ",
				10 => "                    ",
				_ => new string(' ', indent * 2)
			};
		}
	}
}
