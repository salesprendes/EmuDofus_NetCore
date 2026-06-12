using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocolo.Framework.Network
{
    public interface IBufferHandler
    {
        int Offset
        {
            get;
        }

        void SetBuffer(byte[] buffer, int offset, int count);
    }
}
