using System;
using System.Security.Cryptography;
using System.Threading;

namespace Compress.ThreadReaders
{
    public class ThreadHash : IDisposable
    {
        private Utils.CRC _crc;
        private readonly SHA1 _sha1;
        private readonly MD5 _md5;

        private readonly AutoResetEvent _waitEvent;
        private readonly AutoResetEvent _outEvent;
        private readonly Thread _tWorker;

        private byte[] _buffer;
        private int _size;
        private bool _finished;

        public ThreadHash()
        {
            _crc = new Utils.CRC();
            _sha1 = SHA1.Create();
            _md5 = MD5.Create();

            _waitEvent = new AutoResetEvent(false);
            _outEvent = new AutoResetEvent(false);
            _finished = false;

            _tWorker = new Thread(MainLoop);
            _tWorker.Start();
        }

        public byte[] HashCRC => _crc.Crc32ResultB;
        public byte[] HashSHA1 => _sha1.Hash;
        public byte[] HashMD5 => _md5.Hash;

        public void Dispose()
        {
            _waitEvent?.Dispose();
            _outEvent?.Dispose();
        }

        private void MainLoop()
        {
            while (true)
            {
                _waitEvent.WaitOne();
                if (_finished)
                {
                    break;
                }
                _crc.SlurpBlock(_buffer, 0, _size);
                _sha1.TransformBlock(_buffer, 0, _size, null, 0);
                _md5.TransformBlock(_buffer, 0, _size, null, 0);
                _outEvent.Set();
            }
            
            byte[] tmp = new byte[0];
            _sha1.TransformFinalBlock(tmp, 0, 0);
            _md5.TransformFinalBlock(tmp, 0, 0);
        }

        public void Trigger(byte[] buffer, int size)
        {
            _buffer = buffer;
            _size = size;
            _waitEvent.Set();
        }

        public void Wait()
        {
            _outEvent?.WaitOne();
        }

        public void Finish()
        {
            _finished = true;
            _waitEvent?.Set();
            _tWorker?.Join();
        }
    }
}