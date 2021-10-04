using System;
using System.IO;
using System.Numerics;

#nullable enable

namespace KdlDotNet
{
    public abstract class KDLValue<T> : KDLValue
    {
        protected KDLValue(string? type) : base(type)
        { }

        public abstract T Value { get; }
    }

    public abstract class KDLValue : IKDLObject
    {
        public static KDLValue From(object o) => From(o, null);

        public static KDLValue From(object o, string? type)
        {
            if (o == null)
                return KDLNull.From(type);

            if (o is bool b)
                return KDLBoolean.From(b, type);

            if (o is int i)
                return KDLNumber.From(i, radix: 10, type: type);

            if (o is long l)
                return KDLNumber.From(l, radix: 10, type: type);

            if (o is double d)
                return KDLNumber.From(d, radix: 10, type: type);

            if (o is BigInteger bi)
                return KDLNumber.From(bi, radix: 10, type: type);

            if (o is string s)
                return new KDLString(s, type);

            if (o is KDLValue k)
                return k;

            throw new ArgumentException($"No KDLValue for object {o}");
        }

        public KDLValue(string? type)
        {
            Type = type;
        }

        public string? Type { get; }

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
        {
            if (Type != null)
            {
                writer.Write('(');
                PrintUtil.WriteStringQuotedAppropriately(writer, Type, true, printConfig);
                writer.Write(')');
            }
            WriteKDLValue(writer, printConfig);
        }

        protected abstract void WriteKDLValue(StreamWriter writer, PrintConfig printConfig); // throws IOException;

        public abstract KDLString AsString();
        public abstract KDLNumber? AsNumber();
        public abstract KDLBoolean? AsBoolean();

        public virtual bool IsString => false;
        public virtual bool IsNumber => false;
        public virtual bool IsBoolean => false;
        public virtual bool IsNull => false;

        // we expect derived classes to override this
        public override bool Equals(object? obj)
            => obj is KDLValue other && GetType() == other.GetType() && Type == other.Type;

        // derived classes must override this
        public override int GetHashCode() => GetType().GetHashCode() ^ Type?.GetHashCode() ?? 0;

        public static bool operator ==(KDLValue? a, KDLValue? b)
        {
            if (a is null)
            {
                if (b is null)
                    return true;

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return a.Equals(b);
        }

        public static bool operator !=(KDLValue? a, KDLValue? b) => !(a == b);
    }
}
