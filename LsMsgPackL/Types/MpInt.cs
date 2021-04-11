using System;
using System.Collections.Generic;
using System.IO;

namespace LsMsgPack
{
    [Serializable]
    public class MpInt : MsgPackItem
    {
        private ulong uvalue;
        private long svalue;
        private bool isSigned;
        private MsgPackTypeId typeId = MsgPackTypeId.NeverUsed;

        private void UpdateSignedFlag()
        {
            isSigned = svalue < 0;
        }
            
        private void UpdateTypeId()
        {
            if (isSigned)
            {
                if ((svalue >= -0x1F) && ((svalue <= 0x1F))) typeId = MsgPackTypeId.MpSBytePart;
                else if ((svalue >= sbyte.MinValue) && (svalue <= sbyte.MaxValue)) typeId = MsgPackTypeId.MpSByte;
                else if ((svalue >= short.MinValue) && (svalue <= short.MaxValue)) typeId = MsgPackTypeId.MpShort;
                else if ((svalue >= int.MinValue) && (svalue <= int.MaxValue)) typeId = MsgPackTypeId.MpInt;
                else typeId = MsgPackTypeId.MpLong;
            }
            else
            {
                if (uvalue <= 0x7F) typeId = MsgPackTypeId.MpBytePart;
                else if (uvalue <= 255) typeId = MsgPackTypeId.MpUByte;
                else if (uvalue <= 0xFFFF) typeId = MsgPackTypeId.MpUShort;
                else if (uvalue <= 0xFFFFFFFF) typeId = MsgPackTypeId.MpUInt;
                else typeId = MsgPackTypeId.MpULong;
            }
        }

        public override MsgPackTypeId TypeId
        {
            get
            {
                return typeId;
            }
        }

        internal MpInt SetEnumVal(object value)
        {
            Type valuesType = value.GetType();
            Type typ = Enum.GetUnderlyingType(valuesType);
            if (typ == typeof(int)) Value = (int)value;
            else if (typ == typeof(short)) Value = (short)value;
            else if (typ == typeof(byte)) Value = (byte)value;
            else if (typ == typeof(sbyte)) Value = (sbyte)value;
            else if (typ == typeof(long)) Value = (long)value;
            else if (typ == typeof(uint)) Value = (uint)value;
            else if (typ == typeof(ushort)) Value = (ushort)value;
            else if (typ == typeof(ulong)) Value = (ulong)value;
            else throw new MsgPackException(string.Concat("Unable to convert \"", value, "\" (\"", typ, "\") to an integer type"));
            return this;
        }

        public override object Value
        {
            get
            {
                switch (TypeId)
                {
                    case MsgPackTypeId.MpSBytePart:
                    case MsgPackTypeId.MpSByte:
                        return Convert.ToSByte(svalue);
                    case MsgPackTypeId.MpShort:
                        return Convert.ToInt16(svalue);
                    case MsgPackTypeId.MpInt:
                        return Convert.ToInt32(svalue);
                    case MsgPackTypeId.MpLong:
                        return Convert.ToInt64(svalue);
                    case MsgPackTypeId.MpBytePart:
                    case MsgPackTypeId.MpUByte:
                        return Convert.ToByte(uvalue);
                    case MsgPackTypeId.MpUShort:
                        return Convert.ToUInt16(uvalue);
                    case MsgPackTypeId.MpUInt:
                        return Convert.ToUInt32(uvalue);
                    case MsgPackTypeId.MpULong:
                        return uvalue;
                    default:
                        if (svalue != 0) return svalue;
                        return uvalue;
                }
            }
            set
            {
                // preseve original type in typeId
                var valueTypeCode = Type.GetTypeCode(value.GetType());
                switch (valueTypeCode)
                {
                    case TypeCode.SByte:
                        svalue = Convert.ToInt64(value);
                        uvalue = 0;
                        break;
                    case TypeCode.Int16:
                        svalue = Convert.ToInt64(value);
                        uvalue = 0;
                        break;
                    case TypeCode.Int32:
                        svalue = Convert.ToInt64(value);
                        uvalue = 0;
                        break;
                    case TypeCode.Int64:
                        svalue = Convert.ToInt64(value);
                        uvalue = 0;
                        break;
                    case TypeCode.Byte:
                        svalue = 0;
                        uvalue = Convert.ToUInt64(value);
                        break;
                    case TypeCode.UInt16:
                        svalue = 0;
                        uvalue = Convert.ToUInt64(value);
                        break;
                    case TypeCode.UInt32:
                        svalue = 0;
                        uvalue = Convert.ToUInt64(value);
                        break;
                    case TypeCode.UInt64:
                        svalue = 0;
                        uvalue = Convert.ToUInt64(value);
                        break;
                    default:
                        if (value.GetType().IsEnum)
                        {
                            SetEnumVal(value);
                        }
                        else
                        {
                            throw new MsgPackException(string.Concat("Unable to convert \"", value, "\" to an integer type"));
                        }
                        break;
                }

                if (svalue > 0 && uvalue == 0)
                {
                    uvalue = (ulong)svalue;
                }
                
                UpdateSignedFlag();
                UpdateTypeId();
            }
        }

        public override T GetTypedValue<T>()
        {
            Type targetType = typeof(T);
            if (targetType.IsEnum)
            {
                return (T)Enum.ToObject(targetType, Value);
            }

            var targetTypeCode = Type.GetTypeCode(targetType);
            switch (targetTypeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return (T)Convert.ChangeType(Value, typeof(T));
                default:
                    return base.GetTypedValue<T>();
            }
        }

        public override long CalcBytesSize()
        {
            switch (TypeId)
            {
                case MsgPackTypeId.MpBytePart:
                case MsgPackTypeId.MpSBytePart:
                    return 1;
                case MsgPackTypeId.MpSByte:
                case MsgPackTypeId.MpUByte:
                    return 2;
                case MsgPackTypeId.MpUShort:
                case MsgPackTypeId.MpShort:
                    return 3;
                case MsgPackTypeId.MpInt:
                case MsgPackTypeId.MpUInt:
                    return 5;
                case MsgPackTypeId.MpLong:
                case MsgPackTypeId.MpULong:
                    return 9;
                default:
                    return 0;
            }
        }

        public override void ToStream(Stream stream)
        {
            MsgPackTypeId targetType = TypeId;
            byte type = (byte)targetType;
            if (targetType == MsgPackTypeId.MpBytePart || targetType == MsgPackTypeId.MpSBytePart)
            {
                if ((type & 0x7F) == 0)
                {
                    stream.WriteByte((byte)(type | Convert.ToByte(uvalue)));
                    return;
                }
                else if ((type & 0xE0) == 0xE0)
                {
                    stream.WriteByte((byte)(type | BitConverter.GetBytes(svalue)[0]));
                    return;
                }
            }



            stream.WriteByte(type);
            if (targetType == MsgPackTypeId.MpSByte)
            {
                stream.WriteByte((byte)Convert.ToSByte(svalue));
                return;
            }
            else if(targetType == MsgPackTypeId.MpUByte)
            {
                stream.WriteByte(Convert.ToByte(uvalue));
                return;
            }
            
            
            byte[] iBytes = null;
            switch (targetType)
            {
                case MsgPackTypeId.MpShort:
                    iBytes = BitConverter.GetBytes(Convert.ToInt16(svalue));
                    break;
                case MsgPackTypeId.MpInt:
                    iBytes =  BitConverter.GetBytes(Convert.ToInt32(svalue));
                    break;
                case MsgPackTypeId.MpLong:
                    iBytes = BitConverter.GetBytes(Convert.ToInt64(svalue));
                    break;
                case MsgPackTypeId.MpUShort:
                    iBytes = BitConverter.GetBytes(Convert.ToUInt16(uvalue));
                    break;
                case MsgPackTypeId.MpUInt:
                    iBytes = BitConverter.GetBytes(Convert.ToUInt32(uvalue));
                    break;
                case MsgPackTypeId.MpULong:
                    iBytes = BitConverter.GetBytes(Convert.ToUInt64(uvalue));
                    break;
            }

            ReorderIfLittleEndian(iBytes);
            stream.Write(iBytes, 0, iBytes.Length);
        }

        public override MsgPackItem Read(MsgPackTypeId typeId, Stream data)
        {
            svalue = 0; // in case of reuse
            if (((byte)typeId & 0xE0) == 0xE0)
            { // 5-bit negative integer
                svalue = (sbyte)typeId;
                if (svalue > 0) uvalue = (ulong)svalue;
                this.typeId = MsgPackTypeId.MpSBytePart;
                UpdateSignedFlag();
                return this;
            }
            else if (((byte)typeId & 0x80) == 0)
            { // 7-bit positive integer
                uvalue = ((uint)((byte)typeId & 0x7F));
                this.typeId = MsgPackTypeId.MpBytePart;
                UpdateSignedFlag();
                return this;
            }

            byte[] iBytes = new byte[8];
            switch (typeId)
            {
                case MsgPackTypeId.MpSByte:
                    svalue = (sbyte)data.ReadByte();
                    break;
                case MsgPackTypeId.MpUByte:
                    uvalue = (byte)data.ReadByte();
                    break;
                case MsgPackTypeId.MpShort:
                    data.Read(iBytes, 0, 2);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 2);
                    svalue = BitConverter.ToInt16(iBytes, 0);
                    break;
                case MsgPackTypeId.MpUShort:
                    data.Read(iBytes, 0, 2);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 2);
                    uvalue = BitConverter.ToUInt16(iBytes, 0);
                    break;
                case MsgPackTypeId.MpInt:
                    data.Read(iBytes, 0, 4);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 4);
                    svalue = BitConverter.ToInt32(iBytes, 0);
                    break;
                case MsgPackTypeId.MpUInt:
                    data.Read(iBytes, 0, 4);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 4);
                    uvalue = BitConverter.ToUInt32(iBytes, 0);
                    break;
                case MsgPackTypeId.MpLong:
                    data.Read(iBytes, 0, 8);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 8);
                    svalue = BitConverter.ToInt64(iBytes, 0);
                    break;
                case MsgPackTypeId.MpULong:
                    data.Read(iBytes, 0, 8);
                    iBytes = SwapIfLittleEndian(iBytes, 0, 8);
                    uvalue = BitConverter.ToUInt64(iBytes, 0);
                    break;
                default:
                    throw new MsgPackException(string.Concat("The type ", GetOfficialTypeName(typeId), " is not supported."), data.Position - 1, typeId);
            }


            if (svalue > 0) uvalue = (ulong)svalue;
            this.typeId = typeId;
            UpdateSignedFlag();
            
            return this;
        }


        public override string ToString()
        {
            return string.Concat("Integer (", GetOfficialTypeName(TypeId), ") with the value ", isSigned ? svalue.ToString() : uvalue.ToString());
        }
    }
}