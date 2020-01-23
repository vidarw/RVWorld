using System;
using System.IO;
using System.IO.IsolatedStorage;

namespace Dir2Dat
{
    public class Reader : Stream
    {
        public static object lockObj = null;

        private long maxBufLength = 1024 * 1024 * 1024;

        private byte[] memReader;

        private Stream inStr;
        private long pos = 0;
        private long len;

        public Reader(string filename)
        {
            RVIO.FileStream.OpenFileRead(filename, out inStr);
            len = inStr.Length;
            if (len > maxBufLength)
            {
                memReader = null;
                return;
            }

            lock (lockObj)
            {
                memReader = new byte[len];
                inStr.Read(memReader, 0, (int)len);
            }
            inStr.Close();
            inStr = null;
        }

        ~Reader()
        {
            memReader = null;
            inStr?.Close();
            inStr = null;
        }

        public override void Flush()
        {

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (inStr != null)
            {
                inStr.Seek(offset, origin);
                return inStr.Position;
            }

            switch (origin)
            {
                case SeekOrigin.Begin:
                    pos = offset;
                    return pos;
                case SeekOrigin.Current:
                    pos = pos + offset;
                    return pos;
                case SeekOrigin.End:
                    pos = Length + offset;
                    return pos;
            }
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (inStr != null)
                return inStr.Read(buffer, offset, count);

            long rPos = pos + count;
            if (rPos > len) rPos = len;
            int lCount=(int) (rPos - pos);

            Buffer.BlockCopy(memReader, (int)pos, buffer, offset, lCount);

            pos = rPos;
            return lCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => len;

        public override long Position
        {
            get
            {
                if (inStr != null)
                    return inStr.Position;
                else
                    return pos;
            }
            set
            {
                if (inStr != null)
                    inStr.Position = value;
                else
                    pos = value;
            }
        }
    }
}
