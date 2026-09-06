namespace Hexalith.FrontComposer.SourceTools.Tests.Integration;

internal sealed class ConversionProbe(bool returnNull) : IConvertible {
    public TypeCode GetTypeCode() => TypeCode.Object;

    public object ToType(Type conversionType, IFormatProvider? provider)
        => returnNull ? null! : "wrong runtime type";

    public bool ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();
    public byte ToByte(IFormatProvider? provider) => throw new InvalidCastException();
    public char ToChar(IFormatProvider? provider) => throw new InvalidCastException();
    public DateTime ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();
    public decimal ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();
    public double ToDouble(IFormatProvider? provider) => throw new InvalidCastException();
    public short ToInt16(IFormatProvider? provider) => throw new InvalidCastException();
    public int ToInt32(IFormatProvider? provider) => throw new InvalidCastException();
    public long ToInt64(IFormatProvider? provider) => throw new InvalidCastException();
    public sbyte ToSByte(IFormatProvider? provider) => throw new InvalidCastException();
    public float ToSingle(IFormatProvider? provider) => throw new InvalidCastException();
    public string ToString(IFormatProvider? provider) => string.Empty;
    public ushort ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();
    public uint ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();
    public ulong ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
}
