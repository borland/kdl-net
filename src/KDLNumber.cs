using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace KdlDotNet
{
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

            foreach (var ch in toParse)
            {
                switch (ch)
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
                catch (OverflowException) // fallback to int64 for bigger numbers
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

        public int AsInt() => storageType switch
        {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => (int)storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => (int)storage.doubleValue, // let the cast throw or truncate if the number is too big
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public long AsLong() => storageType switch
        {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => (long)storage.doubleValue, // let the cast throw or truncate if the number is too big
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public double AsDouble() => storageType switch
        {
            KDLNumberType.Int32 => storage.int32Value,
            KDLNumberType.Int64 => storage.int64Value, // let the cast throw or truncate if the number is too big
            KDLNumberType.Float64 => storage.doubleValue,
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public override KDLString AsString() => new KDLString(AsBasicString(), type: Type); // TODO radix on ToString?

        public override KDLNumber? AsNumber() => this;

        public override KDLBoolean? AsBoolean() => null;

        public string AsBasicString() => storageType switch
        {
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

        public override bool Equals(object? obj)
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
}
