using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace LsMsgPack
{
    [Serializable]
    public class MpString : MsgPackVarLen
    {

        private string value = string.Empty;

        public override MsgPackTypeId TypeId
        {
            get
            {
                return GetTypeId(StrAsBytes.LongLength);
            }
        }

        public override int Count
        {
            get { return value.Length; }
        }

        protected override MsgPackTypeId GetTypeId(long len)
        {
            if (len < 32) return MsgPackTypeId.MpStr5;
            if (len < 256) return MsgPackTypeId.MpStr8;
            if (len <= ushort.MaxValue) return MsgPackTypeId.MpStr16;
            return MsgPackTypeId.MpStr32;
        }

        public override object Value
        {
            get { return value; }
            set { this.value = ReferenceEquals(value, null) ? string.Empty : value.ToString(); }
        }

        private static Encoding defaultEncoding = Encoding.UTF8;
        /// <summary>
        /// Default string encoding will be UTF8 if this property is not changed
        /// </summary>
        public static Encoding DefaultEncoding
        {
            get { return defaultEncoding; }
            set { defaultEncoding = value; }
        }

        private Encoding encoding = defaultEncoding;
        /// <summary>
        /// will initially be the statically defined DefaultEncoding, but may also be dynamically changed per instance (note that the chosen encoding will not be persisted)
        /// </summary>
        [XmlIgnore]
        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                if (ReferenceEquals(value, null)) return;
                encoding = value;
            }
        }

        private byte[] StrAsBytes
        {
            get
            {
                return encoding.GetBytes(value);
            }
            set
            {
                this.value = encoding.GetString(value);
            }
        }

        public override long CalcBytesSize()
        {
            long bytesCount = encoding.GetByteCount(value);
            MsgPackTypeId typeId = GetTypeId(bytesCount);
            if (typeId == MsgPackTypeId.MpStr5)
                bytesCount++;
            else
            {
                bytesCount += GetLengthBytesSize(bytesCount, SupportedLengths.All) + 1;
            }
            return bytesCount;
        }

        public override void ToStream(Stream stream)
        {
            byte[] strBytes = StrAsBytes;
            MsgPackTypeId typeId = GetTypeId(strBytes.LongLength);
            if (typeId == MsgPackTypeId.MpStr5)
            {
                stream.WriteByte(GetLengthBytes(typeId, strBytes.Length));
            }
            else
            {
                stream.WriteByte((byte)typeId);
                
                var lenBytes =  GetLengthBytes(strBytes.LongLength, SupportedLengths.All);
                stream.Write(lenBytes, 0, lenBytes.Length);
            }
            stream.Write(strBytes, 0, strBytes.Length);
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, Stream data)
        {
            long len;
            if (!IsMasked(MsgPackTypeId.MpStr5, typeId, 0x1F, out len))
            {
                switch (typeId)
                {
                    case MsgPackTypeId.MpStr8: len = ReadLen(data, 1); break;
                    case MsgPackTypeId.MpStr16: len = ReadLen(data, 2); break;
                    case MsgPackTypeId.MpStr32: len = ReadLen(data, 4); break;
                    default: throw new MsgPackException(string.Concat("MpString does not support a type ID of ", GetOfficialTypeName(typeId), "."), data.Position - 1, typeId);
                }
            }
            StrAsBytes = ReadBytes(data, len);
            return this;
        }

        public override string ToString()
        {
            return string.Concat("String (", GetOfficialTypeName(TypeId), ") with the value \"", value, "\"");
        }
    }
}
