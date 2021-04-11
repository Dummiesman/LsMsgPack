using System;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpNull : MsgPackItem
    {

        public override MsgPackTypeId TypeId
        {
            get { return MsgPackTypeId.MpNull; }
        }

        public override long CalcBytesSize()
        {
            return 1;
        }

        public override void ToStream(Stream stream)
        {
            stream.WriteByte((byte)TypeId);
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, System.IO.Stream data)
        {
            return this;
        }

        public override object Value
        {
            get { return null; }
            set { if (!ReferenceEquals(value, null)) throw new MsgPackException("Cannot assign anything to Null!"); }
        }

        public virtual int Write(System.IO.Stream data)
        {
            byte[] item = ToBytes();
            data.Write(item, 0, item.Length);
            return item.Length;
        }

        public override string ToString()
        {
            return string.Concat("[NULL] (", GetOfficialTypeName(TypeId), ")");
        }
    }
}
