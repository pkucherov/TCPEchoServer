using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Common
{
    public class BufferManager : IDisposable
    {        
        private readonly byte[] _block;
        private volatile bool _disposed;
        private readonly BlockingCollection<int> _freeSegments;
        private readonly int _segmentSize;
             
        public byte[] Block { get { return _block; } }
        public int BlockSize { get { return _block.Length; } }
        public int BufferSize { get { return _segmentSize; } }
        public int FreeBuffers { get { return _freeSegments.Count; } }

        // Constructors
        public BufferManager(int bufferCount, int bufferSize)
        {
            int blockSize = bufferCount * bufferSize;
            int [] freeSegments = new int[bufferCount];

            int freeSegment = 0;
            for (int i = 0; i < freeSegments.Length; i++)
            {
                freeSegments[i] = freeSegment;
                freeSegment += bufferSize;
            }
                        
            _block = new byte[blockSize];
            _freeSegments = new BlockingCollection<int>
            (                
                new ConcurrentBag<int>(freeSegments),
                freeSegments.Length
            );
            _segmentSize = bufferSize;
        }
               
        private int getOffset()
        {            
            return _freeSegments.Take();
        }
        private void freeOffset(int offset)
        {            
            _freeSegments.Add(offset);
        }

        public ArraySegment<byte> GetBuffer()
        {            
            return new ArraySegment<byte>(Block, getOffset(), BufferSize);            
        }
        public void FreeBuffer(ArraySegment<byte> buffer)
        {            
            freeOffset(buffer.Offset);
        }

        public void Dispose()
        {
            lock (_freeSegments)
                if (_disposed) return;
                else _disposed = true;

            _freeSegments.Dispose();
        }
    }
}
