using System.IO;
using System.Text;

namespace AutoRest.PSArm
{
    public class DebugStream : Stream
    {
        private readonly Stream _underlyingStream;

        private readonly MemoryStream _inDebugStream;

        private readonly MemoryStream _outDebugStream;

        public DebugStream(
            Stream underlyingStream)
        {
            _underlyingStream = underlyingStream;
            _inDebugStream = new MemoryStream();
            _outDebugStream = new MemoryStream();
        }

        public string InString => Encoding.UTF8.GetString(_inDebugStream.ToArray());

        public string OutString => Encoding.UTF8.GetString(_outDebugStream.ToArray());

        public override bool CanRead => _underlyingStream.CanRead;

        public override bool CanSeek => _underlyingStream.CanSeek;

        public override bool CanWrite => _underlyingStream.CanWrite;

        public override long Length => _underlyingStream.Length;

        public override long Position { get => _underlyingStream.Position; set => _underlyingStream.Position = value; }

        public override void Flush()
        {
            _underlyingStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = _underlyingStream.Read(buffer, offset, count);

            _outDebugStream.Write(buffer, offset, count);

            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _underlyingStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _underlyingStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inDebugStream.Write(buffer, offset, count);
            _underlyingStream.Write(buffer, offset, count);
        }
    }
}
