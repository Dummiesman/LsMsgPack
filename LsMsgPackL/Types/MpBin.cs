﻿using System;
using System.Collections.Generic;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpBin : MsgPackVarLen
    {

        private byte[] value = new byte[0];

        public override MsgPackTypeId TypeId
        {
            get
            {
                return GetTypeId(value.LongLength);
            }
        }

        public override int Count
        {
            get { return value.Length; }
        }

        protected override MsgPackTypeId GetTypeId(long len)
        {
            if (len < 256) return MsgPackTypeId.MpBin8;
            if (len <= ushort.MaxValue) return MsgPackTypeId.MpBin16;
            return MsgPackTypeId.MpBin32;
        }

        public override object Value
        {
            get { return value; }
            set { this.value = ReferenceEquals(value, null) ? new byte[0] : (byte[])value; }
        }

        public override long CalcBytesSize()
        {
            return 1 + GetLengthBytesSize(value.LongLength, SupportedLengths.All) + value.LongLength;
        }

        public override void ToStream(Stream stream)
        {
            MsgPackTypeId typeId = GetTypeId(value.LongLength);
            stream.WriteByte((byte)typeId);

            var lenBytes = GetLengthBytes(value.LongLength, SupportedLengths.All);
            stream.Write(lenBytes, 0, lenBytes.Length);
            stream.Write(value, 0, value.Length);
        }

        /*public override byte[] ToBytes()
        {
            List<byte> bytes = new List<byte>(value.Length + 5); // current max length limit is 4 bytes + identifier
            MsgPackTypeId typeId = GetTypeId(value.LongLength);
            bytes.Add((byte)typeId);
            bytes.AddRange(GetLengthBytes(value.LongLength, SupportedLengths.All));
            bytes.AddRange(value);
            return bytes.ToArray();
        }*/

        public override MsgPackItem Read(MsgPackTypeId typeId, Stream data)
        {
            long len;

            switch (typeId)
            {
                case MsgPackTypeId.MpBin8: len = ReadLen(data, 1); break;
                case MsgPackTypeId.MpBin16: len = ReadLen(data, 2); break;
                case MsgPackTypeId.MpBin32: len = ReadLen(data, 4); break;
                default: throw new MsgPackException(string.Concat("MpBin does not support a type ID of ", GetOfficialTypeName(typeId), "."), data.Position - 1, typeId);
            }
            value = ReadBytes(data, len);
            return this;
        }

        public override string ToString()
        {
            return string.Concat("Blob (", GetOfficialTypeName(TypeId), ") of ", value.Length, " bytes.");
        }
    }
}
