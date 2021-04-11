using System;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpBool : MsgPackItem
    {

        private bool value;

        public override MsgPackTypeId TypeId
        {
            get { return value ? MsgPackTypeId.MpBoolTrue : MsgPackTypeId.MpBoolFalse; }
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
            value = (byte)typeId == 0xc3;
            return this;
        }

        public override object Value
        {
            get { return value; }
            set { this.value = Convert.ToBoolean(value); }
        }

        public override string ToString()
        {
            return string.Concat("Boolean (", GetOfficialTypeName(TypeId), ") with the value \"", value, "\"");
        }
    }
}
