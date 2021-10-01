using System.IO;

#nullable enable

namespace KdlDotNet
{
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

        public override bool Equals(object? obj) => obj is KDLString other && other.Value == Value;

        public override int GetHashCode() => Value.GetHashCode();
    }
}
