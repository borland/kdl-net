using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#nullable enable

namespace kdl_net
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

        public static KDLString Empty => KDLString.From("");

        public KDLString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public KDLString AsString() => this;
        public KDLNumber? AsNumber() => KDLNumber.From(Value);
        public KDLBoolean? AsBoolean() => null;

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
        {
            throw new NotImplementedException();
        }

        public bool IsString => true;
        public bool IsNumber => false;
        public bool IsBoolean => false;
        public bool IsNull => false;

        public override string ToString() => $"KDLString{{value='{Value}'}}";
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
    }

    /**
     * Representation of a KDL number. Numbers may be base 16, 10, 8, or 2 as stored in the radix field. Base 10 numbers may
     * be fractional, but all others are limited to integers.
     */
    public class KDLNumber : IKDLValue // C# Note: TODO: Just int32 for now. Fancy numbers can come later
    {
        public static KDLNumber From(int value) => new KDLNumber(value);

        public static KDLNumber? From(string value) => int.TryParse(value, out int i) ? From(i) : null; // TODO

        public KDLNumber(int value) 
        {
            Value = value;
        }

        public int Value { get; }

        public KDLString AsString() => KDLString.From(Value.ToString());
        public KDLNumber? AsNumber() => this;
        public KDLBoolean? AsBoolean() => null;

        public void WriteKDL(StreamWriter writer, PrintConfig printConfig)
            => writer.Write(Value.ToString());

        public bool IsString => false;
        public bool IsNumber => true;
        public bool IsBoolean => false;
        public bool IsNull => false;

        public override string ToString() => $"KDLNumber{{value='{Value}'}}";
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
           
            // BigNums not suported, just int for now
            if (o is int i)
                return new KDLNumber(i);
            
            if (o is string s)
                return new KDLString(s);
            
            if (o is IKDLValue k) 
                return k;

            throw new ArgumentException($"No KDLValue for object {o}");
        }
    }
}
