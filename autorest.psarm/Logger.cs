using System;
using System.IO;
using System.Threading.Tasks;

namespace AutoRest.PSArm
{
    public class Logger : IDisposable
    {
        public static Logger CreateFileLogger(string logFilePath)
        {
            return new Logger(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read));
        }

        private Stream _outStream;

        private TextWriter _outWriter;

        private bool _disposedValue;

        public Logger(Stream outStream)
        {
            _outStream = outStream;
            _outWriter = new StreamWriter(outStream);
        }

        public void Log(ReadOnlySpan<char> message)
        {
            _outWriter.WriteLine(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _outWriter.Dispose();
                    _outStream.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

