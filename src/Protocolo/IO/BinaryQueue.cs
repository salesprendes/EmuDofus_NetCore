using System;
using System.IO;
using System.Text;

namespace Protocolo.Framework.IO
{
    /// <summary>
    /// Small binary buffer optimized for append/read workflows used by the socket stack.
    /// </summary>
    public class BinaryQueue
    {
        private const int DefaultCapacity = 256;

        private byte[] m_buffer;
        private int m_readOffset;
        private int m_writeOffset;

        public BinaryQueue(int initialCapacity = DefaultCapacity)
        {
            if (initialCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));

            m_buffer = new byte[initialCapacity];
        }

        public int Count => m_writeOffset - m_readOffset;

        public void Clear()
        {
            m_readOffset = 0;
            m_writeOffset = 0;
        }

        public byte[] ToArray()
        {
            var count = Count;
            if (count == 0)
                return Array.Empty<byte>();

            var data = new byte[count];
            Buffer.BlockCopy(m_buffer, m_readOffset, data, 0, count);
            return data;
        }

        public int ReadInt()
        {
            EnsureReadable(sizeof(int));
            var value = BitConverter.ToInt32(m_buffer, m_readOffset);
            m_readOffset += sizeof(int);
            CompactIfNeeded();
            return value;
        }

        public long ReadLong()
        {
            EnsureReadable(sizeof(long));
            var value = BitConverter.ToInt64(m_buffer, m_readOffset);
            m_readOffset += sizeof(long);
            CompactIfNeeded();
            return value;
        }

        public short ReadShort()
        {
            EnsureReadable(sizeof(short));
            var value = BitConverter.ToInt16(m_buffer, m_readOffset);
            m_readOffset += sizeof(short);
            CompactIfNeeded();
            return value;
        }

        public string ReadString()
        {
            var length = ReadInt();
            if (length < 0)
                throw new InvalidDataException("BinaryQueue::ReadString invalid string length.");

            EnsureReadable(length);
            var value = Encoding.Default.GetString(m_buffer, m_readOffset, length);
            m_readOffset += length;
            CompactIfNeeded();
            return value;
        }

        public void WriteInt(int value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteLong(long value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteShort(short value)
        {
            WriteBytes(BitConverter.GetBytes(value));
        }

        public void WriteString(string value)
        {
            var safeValue = value ?? string.Empty;
            var bytes = Encoding.Default.GetBytes(safeValue);
            WriteInt(bytes.Length);
            WriteBytes(bytes);
        }

        public void WriteBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            WriteBytes(data, 0, data.Length);
        }

        public void WriteBytes(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || count < 0 || offset + count > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count == 0)
                return;

            EnsureWritable(count);
            Buffer.BlockCopy(data, offset, m_buffer, m_writeOffset, count);
            m_writeOffset += count;
        }

        public void WriteBytes(BinaryQueue data, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            data.EnsureReadable(count);
            if (count == 0)
                return;

            EnsureWritable(count);
            Buffer.BlockCopy(data.m_buffer, data.m_readOffset, m_buffer, m_writeOffset, count);
            m_writeOffset += count;
            data.m_readOffset += count;
            data.CompactIfNeeded();
        }

        public void WriteByte(byte data)
        {
            EnsureWritable(1);
            m_buffer[m_writeOffset++] = data;
        }

        public byte[] ReadBytes(int length)
        {
            EnsureReadable(length);

            var data = new byte[length];
            Buffer.BlockCopy(m_buffer, m_readOffset, data, 0, length);
            m_readOffset += length;
            CompactIfNeeded();
            return data;
        }

        public string ReadStringDirect(int length, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            EnsureReadable(length);
            var value = encoding.GetString(m_buffer, m_readOffset, length);
            m_readOffset += length;
            CompactIfNeeded();
            return value;
        }

        public byte ReadByte()
        {
            EnsureReadable(1);

            var value = m_buffer[m_readOffset++];
            CompactIfNeeded();
            return value;
        }

        private void EnsureReadable(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (Count < count)
                throw new EndOfStreamException("BinaryQueue::ReadBytes end of stream.");
        }

        private void EnsureWritable(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var available = m_buffer.Length - m_writeOffset;
            if (available >= count)
                return;

            Compact();

            available = m_buffer.Length - m_writeOffset;
            if (available >= count)
                return;

            var newSize = m_buffer.Length;
            var required = Count + count;
            while (newSize < required)
                newSize *= 2;

            var newBuffer = new byte[newSize];
            Buffer.BlockCopy(m_buffer, m_readOffset, newBuffer, 0, Count);
            m_writeOffset = Count;
            m_readOffset = 0;
            m_buffer = newBuffer;
        }

        private void CompactIfNeeded()
        {
            if (m_readOffset == m_writeOffset)
            {
                Clear();
                return;
            }

            if (m_readOffset >= (m_buffer.Length / 2))
                Compact();
        }

        private void Compact()
        {
            var count = Count;
            if (count > 0)
                Buffer.BlockCopy(m_buffer, m_readOffset, m_buffer, 0, count);

            m_readOffset = 0;
            m_writeOffset = count;
        }
    }
}
