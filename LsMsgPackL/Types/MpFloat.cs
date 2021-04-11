using System;
using System.Collections.Generic;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpFloat : MsgPackItem
    {

        private MsgPackTypeId typeId = MsgPackTypeId.MpFloat;
        private float f32value;
        private double f64value;

        public override MsgPackTypeId TypeId
        {
            get { return typeId; }
        }

        public override object Value
        {
            get
            {
                switch (typeId)
                {
                    case MsgPackTypeId.MpFloat: return f32value;
                    case MsgPackTypeId.MpDouble: return f64value;
                }
                throw new MsgPackException(string.Concat("Type ", GetOfficialTypeName(typeId), " is not a floating point."), 0, typeId);
            }
            set
            {
                if (value is float single)
                {
                    typeId = MsgPackTypeId.MpFloat;
                    f32value = single;
                    f64value = 0;
                }
                else if (value is double @double)
                {
                    typeId = MsgPackTypeId.MpDouble;
                    f64value = @double;
                    f32value = 0;
                }
                else throw new MsgPackException("Only floating point types are allowed in MpFloat.");
            }
        }


        public override long CalcBytesSize()
        {
            return (typeId == MsgPackTypeId.MpFloat) ? 5 : 9;
        }

        public override void ToStream(Stream stream)
        {
            stream.WriteByte((byte)typeId);

            byte[] fBytes = (typeId == MsgPackTypeId.MpFloat) ? BitConverter.GetBytes(f32value) : BitConverter.GetBytes(f64value);
            ReorderIfLittleEndian(fBytes);
            stream.Write(fBytes, 0, fBytes.Length);
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, System.IO.Stream data)
        {
            this.typeId = typeId;
            byte[] buffer;

            if (this.typeId == MsgPackTypeId.MpFloat)
            {
                buffer = new byte[4];
                data.Read(buffer, 0, 4);
            }
            else
            {
                buffer = new byte[8];
                data.Read(buffer, 0, 8);
            }

            ReorderIfLittleEndian(buffer);

            if (this.typeId == MsgPackTypeId.MpFloat)
            {
                f32value = BitConverter.ToSingle(buffer, 0);
            }
            else
            {
                f64value = BitConverter.ToDouble(buffer, 0);
            }

            return this;
        }

        public override string ToString()
        {
            return string.Concat("Floating point (", GetOfficialTypeName(typeId), ") with the value ", Value);
        }
    }
}
