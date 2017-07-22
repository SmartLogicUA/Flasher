using System;
using System.Collections.Generic;
using System.Text;

namespace Flasher
{
    class CRCcalc
    {
        short crc;

        public CRCcalc()
        {
            crc = 0;
        }

        public short CRC
        {
            get
            {
                return crc;
            }
        }

        public void AddData(string data)
        {
            short C;
            for (int i = 0; i < data.Length; i += 2)
            {
                C = (short)(((crc >> 8) ^ (byte.Parse(data.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))) << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((C & 0x8000) == 0)
                        C = (short)(C << 1);
                    else
                        C = (short)((C << 1) ^ 0x1021);
                }
                crc = (short)(C ^ (crc << 8));
            }
        }

        public void FlushCRC()
        {
            crc = 0;
        }
    }
}
