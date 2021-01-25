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
    public class KDLString : IKDLValue
    {
        public static KDLString From(string value) => new KDLString(value);

        public static KDLString Empty => From("");

        public KDLString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public KDLString AsString() => this;
        public KDLNumber? AsNumber() => KDLNumber.From(Value);
        public KDLBoolean? AsBoolean() => null;

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
            => PrintUtil.WriteStringQuotedAppropriately(writer, Value, false, printConfig);

        public bool IsString => true;
        public bool IsNumber => false;
        public bool IsBoolean => false;
        public bool IsNull => false;

        public override string ToString() => $"KDLString{{value='{Value}'}}";

        public override bool Equals(object obj) => obj is KDLString x && x.Value == Value;

        public override int GetHashCode() => Value.GetHashCode();
    }

    /**
     * A model object representing the KDL 'null' value.
     */
    public class KDLNull : IKDLValue
    {
        public static KDLNull Instance { get; } = new KDLNull();

        private static readonly KDLString asKdlStr = KDLString.From("null");

        /**
         * New instances should not be created, instead use the INSTANCE constant
         */
        private KDLNull() { }

        public KDLString AsString() => asKdlStr;
        public KDLNumber? AsNumber() => null;
        public KDLBoolean? AsBoolean() => null;

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
            => writer.Write("null");

        public bool IsString => false;
        public bool IsNumber => false;
        public bool IsBoolean => false;
        public bool IsNull => true;

        public override string ToString() => $"KDLNull";

        public override bool Equals(object obj) => obj is KDLNull;

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
    public class KDLNumber : IKDLValue
    {
        public static KDLNumber Zero { get; } = new KDLNumber(0);

        public static KDLNumber From(int value) => new KDLNumber(value);
        public static KDLNumber From(long value) => new KDLNumber(value);
        public static KDLNumber From(double value) => new KDLNumber(value);

        public static KDLNumber? From(string val)
        {
            if (val.Length == 0)
            {
                return null;
            }

            int radix;
            string toParse;
            KDLNumberParseFlags parseFlags = KDLNumberParseFlags.None;
            if (val[0] == '0')
            {
                if (val.Length == 1)
                {
                    return Zero;
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

            return From(toParse, radix, parseFlags);
        }

        // val should be the number AFTER stripping off any 0x or 0b prefix
        internal static KDLNumber? From(string val, int radix, KDLNumberParseFlags flags)
        {
            if (val.Length == 0)
            {
                return null;
            }

            try
            {
                if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasDecimalPoint))
                {
                    return From(double.Parse(val)); // deliberate throw on parse so we can catch formatException
                }
                else if (radix == 10 && flags.HasFlag(KDLNumberParseFlags.HasScientificNotation))
                {
                    return From((double)decimal.Parse(val, System.Globalization.NumberStyles.Float)); // the literal 1e10 is double in C#, so we use that
                }

                try // the vast majority of numbers we are likely to see in a KDL file will be ints, so fast-path that
                {
                    return From(Convert.ToInt32(val, radix));
                }
                catch(OverflowException) // fallback to int64 for bigger numbers
                {
                    return From(Convert.ToInt64(val, radix));
                }
                // NOTE if we have a number bigger than int64 we'll need to put in a BigInteger or something, but we don't support that at this point in time
            }
            catch (FormatException)
            {
                return null;
            }
        }

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

        public KDLNumber(int value) 
        {
            storage.int32Value = value;
            storageType = KDLNumberType.Int32;
        }

        public KDLNumber(long value)
        {
            storage.int64Value = value;
            storageType = KDLNumberType.Int32;
        }

        public KDLNumber(double value)
        {
            storage.doubleValue = value;
            storageType = KDLNumberType.Int32;
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

        public KDLString AsString() => KDLString.From(AsBasicString());

        public KDLNumber? AsNumber() => this;

        public KDLBoolean? AsBoolean() => null;

        public string AsBasicString() => storageType switch {
            KDLNumberType.Int32 => storage.int32Value.ToString(),
            KDLNumberType.Int64 => storage.int64Value.ToString(),
            KDLNumberType.Float64 => storage.doubleValue.ToString(),
            _ => throw new InvalidOperationException($"Unhandled storageType {storageType}")
        };

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
            => writer.Write(AsBasicString());

        public bool IsString => false;
        public bool IsNumber => true;
        public bool IsBoolean => false;
        public bool IsNull => false;

        public override string ToString() => $"KDLNumber{{value='{AsBasicString()}'}}";

        public override bool Equals(object obj) => obj is KDLNumber x && x.storageType == storageType && x.storage == storage;

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
    public class KDLBoolean : IKDLValue
    {
        public static KDLBoolean True { get; } = new KDLBoolean(true);
        public static KDLBoolean False { get; } = new KDLBoolean(false);

        private static readonly KDLString trueStr = KDLString.From("true");
        private static readonly KDLString falseStr = KDLString.From("false");

        private KDLBoolean(bool value)
        {
            Value = value;
        }

        public bool Value { get; }

        public KDLString AsString() => Value ? trueStr : falseStr;
        public KDLNumber? AsNumber() => null;
        public KDLBoolean? AsBoolean() => this;

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
            => writer.Write(Value ? "true" : "false");

        public bool IsString => false;
        public bool IsNumber => false;
        public bool IsBoolean => true;
        public bool IsNull => false;

        public override string ToString() => $"KDLBoolean{{value='{Value}'}}";

        public override bool Equals(object obj) => obj is KDLBoolean b && b.Value == Value;

        public override int GetHashCode() => Value.GetHashCode();
    }

    public interface IKDLValue : IKDLObject
    {
        KDLString AsString();
        KDLNumber? AsNumber();
        KDLBoolean? AsBoolean();

        bool IsString { get; }
        bool IsNumber { get; }
        bool IsBoolean { get; }
        bool IsNull { get; }
    }

    public static class KDLValue
    {
        public static IKDLValue From(object o)
        {
            if (o == null)
                return KDLNull.Instance;
            
            if (o is bool b) 
                return b ? KDLBoolean.True : KDLBoolean.False;
           
            if (o is int i)
                return new KDLNumber(i);

            if (o is long l)
                return new KDLNumber(l);

            if (o is double d)
                return new KDLNumber(d);

            if (o is string s)
                return new KDLString(s);
            
            if (o is IKDLValue k) 
                return k;

            throw new ArgumentException($"No KDLValue for object {o}");
        }
    }
}
