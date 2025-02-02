﻿using System;
using System.Collections.Generic;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpArray : MsgPackVarLen
    {

        private object[] value = new object[0];

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
            if (len < 16) return MsgPackTypeId.MpArray4;
            if (len <= ushort.MaxValue) return MsgPackTypeId.MpArray16;
            return MsgPackTypeId.MpArray32;
        }

        public override object Value
        {
            get { return value; }
            set { this.value = ReferenceEquals(value, null) ? new object[0] : (object[])value; }
        }

        public override long CalcBytesSize()
        {
            long retVal;

            MsgPackTypeId typeId = GetTypeId(value.LongLength);
            if (typeId == MsgPackTypeId.MpArray4)
                retVal = 1;
            else
            {
                retVal = 1 + GetLengthBytesSize(value.LongLength, SupportedLengths.FromShortUpward);
            }
            for (int t = 0; t < value.Length; t++)
            {
                MsgPackItem item = MsgPackItem.Pack(value[t]);
                retVal += item.CalcBytesSize();
            }
            return retVal;
        }

        public override void ToStream(Stream stream)
        {
            MsgPackTypeId typeId = GetTypeId(value.LongLength);
            if (typeId == MsgPackTypeId.MpArray4)
            {
                stream.WriteByte(GetLengthBytes(typeId, value.Length));
            }
            else
            {
                stream.WriteByte((byte)typeId);

                var lenBytes = GetLengthBytes(value.LongLength, SupportedLengths.FromShortUpward);
                stream.Write(lenBytes, 0, lenBytes.Length);
            }

            for (int t = 0; t < value.Length; t++)
            {
                MsgPackItem item = MsgPackItem.Pack(value[t]);
                item.ToStream(stream);
            }
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, Stream data)
        {
            long len;
            if (!IsMasked(MsgPackTypeId.MpArray4, typeId, 0x0F, out len))
            {
                switch (typeId)
                {
                    case MsgPackTypeId.MpArray16: len = ReadLen(data, 2); break;
                    case MsgPackTypeId.MpArray32: len = ReadLen(data, 4); break;
                    default: throw new MsgPackException(string.Concat("MpArray does not support a type ID of ", GetOfficialTypeName(typeId), "."), data.Position - 1, typeId);
                }
            }

            value = new object[len];
            for (int t = 0; t < len; t++)
            {
                MsgPackItem item = Unpack(data);
                value[t] = item.Value;
            }

            return this;
        }

        public override string ToString()
        {
            return string.Concat("Array (", GetOfficialTypeName(TypeId), ") of ", value.Length, " items.");
        }
    }
}
