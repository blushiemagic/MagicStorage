using System.IO;
using System;
using Terraria.ModLoader;
using Terraria;

namespace MagicStorage.Common.Systems.Auditing {
	internal interface IWriteIntersceptor {
		void Flush();
		void WriteLine();
		void WriteLine(string text);
	}

	internal class StreamWriterIntersceptor(StreamWriter writer) : IWriteIntersceptor {
		private readonly StreamWriter _writer = writer;

		void IWriteIntersceptor.Flush() => _writer.Flush();
		void IWriteIntersceptor.WriteLine() => _writer.WriteLine();
		void IWriteIntersceptor.WriteLine(string text) => _writer.WriteLine(text);
	}

	internal class PacketIntersceptor(int bufferLength, string newline, int toClient, int requestID) : IWriteIntersceptor {
		private readonly char[] _buffer = new char[bufferLength];
		private readonly string _newline = newline;
		private readonly int _toClient = toClient;
		private readonly int _requestID = requestID;
		private int _head = 0;
		private int _currentBuffer = 0;
		private ModPacket _activePacket;

		void IWriteIntersceptor.Flush() {
			if (_head > 0) {
				// A packet is still active, send it
				SendPacket();
			}

			// At this point, there shouldn't be any active packet
			// Send a special packet saying that the content has ended
			InitPacket(AuditSystem.COMMAND_FILE_CONTENT_END);
			_activePacket.Write((ushort)_currentBuffer);
			_activePacket.Write(Path.GetFileNameWithoutExtension(Main.ActiveWorldFileData.Path));
			_activePacket.Send(toClient: _toClient);
		}

		void IWriteIntersceptor.WriteLine() => AddToBuffer(_newline);

		void IWriteIntersceptor.WriteLine(string text) => AddToBuffer(text + _newline);

		private void AddToBuffer(string text) {
			ReadOnlySpan<char> buffer = text;
			while (!buffer.IsEmpty) {
				if (_activePacket is null)
					InitPacket(AuditSystem.COMMAND_FILE_CONTENT);

				// Cut up the buffer into slices
				int maxLength = _buffer.Length - _head;

				if (buffer.Length <= maxLength) {
					// The entire buffer fits in the remaining space
					buffer.CopyTo(_buffer.AsSpan()[_head..]);
					_head += buffer.Length;
					break;
				} else {
					// The buffer is too long, cut it up
					buffer[..maxLength].CopyTo(_buffer.AsSpan()[_head..]);
					buffer = buffer[maxLength..];
					_head = _buffer.Length;
				}

				if (_head >= _buffer.Length) {
					// The buffer is full, send the packet
					SendPacket();
				}
			}
		}

		private void InitPacket(byte command) {
			_activePacket = MagicStorageMod.Instance.GetPacket();
			_activePacket.Write((byte)MessageType.AuditSystemMessage);
			_activePacket.Write(command);
		}

		private void SendPacket() {
			_activePacket.Write(_requestID);
			_activePacket.Write((ushort)_currentBuffer);
			// NOTE: Due to encoding, the written byte count may not match the "_head" value
			_activePacket.Write((ushort)_head);
			_activePacket.Write(_buffer.AsSpan()[.._head]);
			_activePacket.Send(toClient: _toClient);
			
			// Reset the parameters
			_activePacket = null;
			_head = 0;
			_currentBuffer++;
		}
	}
}
