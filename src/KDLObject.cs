using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace KdlDotNet
{
    /**
     * The base interface all objects parsed from a KDL document must implement
     */
    public interface IKDLObject
    {
        /**
         * Write the object to the provided stream.
         *
         * @param writer the Writer to write to
         * @param printConfig a configuration object controlling how the object is printed
         * @throws IOException if there is any issue writing the object
         */
        void WriteKDL(StreamWriter writer, PrintConfig printConfig);
    }

    public static class KDLObjectExtensions
    {
        /**
         * Generate a string with the text representation of the given object.
         *
         * @return the string
         */
        public static string ToKDL(this IKDLObject obj)
        {
            var ms = new MemoryStream();
            var streamWriter = new StreamWriter(ms, Encoding.UTF8);

            obj.WriteKDL(streamWriter, PrintConfig.PrettyDefault);
            streamWriter.Flush();

            return Encoding.UTF8.GetString(ms.ToArray()); // C# Note: copies the buffer, probably not efficient
        }
    }

    /**
     * A model object representing a string in a KDL document. Note that even if quoted, identifiers are not KDLStrings.
     */
    public class KDLString : KDLValue<string>
    {
        public static KDLString From(string value) => new KDLString(value);

        public static KDLString Empty => From("");

        public KDLString(string value) : this(value, null) { }

        public KDLString(string value, string? type) : base(type)
        {
            Value = value;
        }

        public override string Value { get; }

        public override KDLString AsString() => this;
        public override KDLNumber? AsNumber() => KDLNumber.From(Value);
        public override KDLBoolean? AsBoolean() => null;

        protected override void WriteKDLValue(StreamWriter writer, PrintConfig printConfig)
            => PrintUtil.WriteStringQuotedAppropriately(writer, Value, false, printConfig);

        public override bool IsString => true;

        public override string ToString() => $"KDLString{{value='{Value}', type={Type}}}";

        public override bool Equals(object obj) => obj is KDLString x && x.Value == Value;

        public override int GetHashCode() => Value.GetHashCode();
    }

    /**
     * A model object representing the KDL 'null' value.
     */
    public class KDLNull : KDLValue
    {
        public static KDLNull Instance { get; } = new KDLNull(null);

        private static readonly KDLString asKdlStr = KDLString.From("null");

        /**
         * New instances should not be created, instead use the INSTANCE constant
         */
        public KDLNull(string? type) : base(type) { }

        public override KDLString AsString() => asKdlStr;
        public override KDLNumber? AsNumber() => null;
        public override KDLBoolean? AsBoolean() => null;

        protected override void WriteKDLValue(StreamWriter writer, PrintConfig printConfig)
            => writer.Write("null");

        public override bool IsNull => true;

        public override string ToString() => $"KDLNull{{type={Type}}}";

        public override bool Equals(object obj) => obj is KDLNull other && Type == other.Type;

        public override int GetHashCode() => 0;
    }

    /** A KDL Number can be internally stored as either an int or double. This enum tells you the native storage type used by a given KDLNumber instance */
    public enum KDLNumberType
    {
        Int32,
        Int64,
        Float64
    }

    // used to pass context information from KDLParser.ParseNumber into KDLNumber.From to reduce duplicated logic
    [Flags]
    internal enum KDLNumberParseFlags
    {
        None = 0,
        HasDecimalPoint = 1,
        HasScientificNotation = 2,
    }

    /**
     * Representation of a KDL number. Numbers may be base 16, 10, 8, or 2 as stored in the radix field. Base 10 numbers may
     * be fractional, but all others are limited to integers.
     */
    public class KDLNumber : KDLValue // not a KDLValue<T> because of the way we share number storage. Consider splitting into KdlNumberInt, KdlNumberLong, etc subclasses
    {
        public static KDLNumber Zero(int radix) => Zero(radix, null);

        public static KDLNumber Zero(int radix, string? type)
        {
            switch (radix)
            {
                case 2:
                case 8:
                case 10:
                case 16:
                    return new KDLNumber(0, radix, type);
                default:
                    throw new ArgumentException("Radix must be one of: [2, 8, 10, 16]", nameof(radix));
            }
        }

        public static KDLNumber From(int value) => new KDLNumber(value);
        public static KDLNumber From(int value, int radix, string? type) => new KDLNumber(value, radix, type);

        public static KDLNumber From(long value) => new KDLNumber(value);
        public static KDLNumber From(long value, int radix, string? type) => new KDLNumber(value, radix, type);

        public static KDLNumber From(double value) => new KDLNumber(value);
        public static KDLNumber From(double value, int radix, string? type) => new KDLNumber(value, radix, type);

        public static KDLNumber? From(string val) => From(val, null);

        public static KDLNumber? From(string val, string? type)
        {
            if (val.Length == 0)
                return null;

            int radix;
            string toParse;
            KDLNumberParseFlags parseFlags = KDLNumberParseFlags.None;
            if (val[0] == '0')
            {
                if (val.Length == 1)
                {
                    return Zero(10, type);
                }

                switch (val[1])
                {
                    case 'x':
                        radix = 16;
                        toParse = val.Substring(2);
                        break;
                    case 'o':
                        radix = 8;
                        toParse = val.Substring(2);
                        break;
                    case 'b':
                        radix = 2;
                        toParse = val.Substring(2);
                        break;
                    default:
                        radix = 10;
                        toParse = val;
                        break;
                }
            }
            else
            {
                radix = 10;
                toParse = val;
            }

            foreach(var ch in toParse)
            {
                switch(ch)
                {
                    case 'e':
                    case 'E':
                        parseFlags |= KDLNumberParseFlags.HasScientificNotation;
                        break;
                    case '.':
                        parseFlags |= KDLNumberParseFlags.HasDecimalPoint;
                        break;
                }
            }

            return From(toParse, radix, type, parseFlags);
        }

        // val should be the number AFTER stripping off any 0x or 0b prefix
        internal static KDLNumber? From(string val, int radix, string? type, KDLNumberParseFlags flags)
        {
            if (val.Length == 0)
                return null;

            try
            {
                if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasDecimalPoint))
                {
                    return From(double.Parse(val), radix, type); // deliberate throw on parse so we can catch formatException
                }
                else if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasScientificNotation))
                {
                    return From((double)decimal.Parse(val, System.Globalization.NumberStyles.Float), radix, type); // the literal 1e10 is double in C#, so we use that
                }

                try // the vast majority of numbers we are likely to see in a KDL file will be ints, so fast-path that
                {
                    return From(Convert.ToInt32(val, radix), radix, type);
                }
                catch(OverflowException) // fallback to int64 for bigger numbers
                {
                    return From(Convert.ToInt64(val, radix), radix, type);
                }
                // NOTE if we have a number bigger than int64 we'll need to put in a BigInteger or something, but we don't support that at this point in time
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private readonly int radix;
        private readonly Storage storage;
        private readonly KDLNumberType storageType;

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        struct Storage
        {
            [FieldOffset(0)]
            public int int32Value;
            [FieldOffset(0)]
            public long int64Value;
            [FieldOffset(0)]
            public double doubleValue;

            public override bool Equals(object obj) => obj is Storage other && this == other;

            public override int GetHashCode() => int64Value.GetHashCode();

            public static bool operator ==(Storage a, Storage b) => a.int64Value == b.int64Value; // covers all the bits so should be good

            public static bool operator !=(Storage a, Storage b) => !(a == b);
        }

        public KDLNumber(int value, int radix = 10, string? type = null) : base(type)
        {
            this.radix = radix;
            this.storage.int32Value = value;
            this.storageType = KDLNumberType.Int32;
        }

        public KDLNumber(long value, int radix = 10, string? type = null) : base(type)
        {
            this.radix = radix;
            this.storage.int64Value = value;
            this.storageType = KDLNumberType.Int64;
        }

        public KDLNumber(double value, int radix = 10, string? type = null) : base(type)
        {
            this.radix = radix;
            this.storage.doubleValue = value;
            this.storageType = KDLNumberType.Float64;
        }

        public int AsInt() => storageType switch {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => (int)storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => (int)storage.doubleValue, // let the cast throw or truncate if the number is too big
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public long AsLong() => storageType switch {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => (long)storage.doubleValue, // let the cast throw or truncate if the number is too big
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public double AsDouble() => storageType switch {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => storage.doubleValue,
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public override KDLString AsString() => new KDLString(AsBasicString(), type: Type); // TODO radix on ToString?

        public override KDLNumber? AsNumber() => this;

        public override KDLBoolean? AsBoolean() => null;

        public string AsBasicString() => storageType switch {
            KDLNumberType.Int32 => storage.int32Value.ToString(),
            KDLNumberType.Int64 => storage.int64Value.ToString(),
            KDLNumberType.Float64 => storage.doubleValue.ToString(),
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        // TODO this method is doesn't support ShouldRespectRadix yet
        protected override void WriteKDLValue(StreamWriter writer, PrintConfig printConfig)
            => writer.Write(AsBasicString());

        public override bool IsNumber => true;

        public override string ToString() => $"KDLNumber{{value='{AsBasicString()}', type={Type}}}";

        public override bool Equals(object obj)
            => obj is KDLNumber other && other.storageType == storageType && other.storage == storage && other.Type == Type;

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + storageType.GetHashCode();
                hash = hash * 23 + storage.GetHashCode();
                return hash;
            }
        }
    }

    /**
     * A KDL object holding a boolean value. New instances should not be created, instead use the TRUE or FALSE constants
     */
    public class KDLBoolean : KDLValue<bool>
    {
        public static KDLBoolean? FromString(string str, string? type) => str switch
        {
            "true" => type == null ? True : new KDLBoolean(true, type),
            "false" => type == null ? False : new KDLBoolean(false, type),
            _ => null,
        };

        public static KDLBoolean True { get; } = new KDLBoolean(true);
        public static KDLBoolean False { get; } = new KDLBoolean(false);

        private static readonly KDLString trueStr = KDLString.From("true");
        private static readonly KDLString falseStr = KDLString.From("false");

        public KDLBoolean(bool value) : this(value, null) { }

        public KDLBoolean(bool value, string? type) : base(type)
            => Value = value;

        public override bool Value { get; }

        public override KDLString AsString() => Value ? trueStr : falseStr;
        public override KDLNumber? AsNumber() => null;
        public override KDLBoolean? AsBoolean() => this;

        protected override void WriteKDLValue(StreamWriter writer, PrintConfig printConfig)
            => writer.Write(Value ? "true" : "false");

        public override bool IsBoolean => true;

        public override string ToString() => $"KDLBoolean{{value='{Value}', type={Type}}}";

        public override bool Equals(object obj)
            => obj is KDLBoolean other && other.Value == Value && Type == other.Type;

        public override int GetHashCode() => Value.GetHashCode();
    }

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
                return type == null ? KDLNull.Instance : new KDLNull(type);

            if (o is bool b)
                return type == null ?
                    b ? KDLBoolean.True : KDLBoolean.False :
                    new KDLBoolean(b, type);

            if (o is int i)
                return new KDLNumber(i, type: type);

            if (o is long l)
                return new KDLNumber(l, type: type);

            if (o is double d)
                return new KDLNumber(d, type: type);

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
            if(Type != null)
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
        public override bool Equals(object obj)
            => obj is KDLValue other && GetType() == other.GetType() && Type == other.Type;

        // derived classes must override this
        public override int GetHashCode() => GetType().GetHashCode() ^ Type?.GetHashCode() ?? 0;

        public static bool operator == (KDLValue a, KDLValue b)
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

        public static bool operator !=(KDLValue a, KDLValue b) => !(a == b);
    }
}
