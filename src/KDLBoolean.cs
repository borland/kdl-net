using System.IO;

#nullable enable

namespace KdlDotNet
{
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

        public static KDLBoolean From(bool b, string? type = null)
            => type == null ?
                b ? True : False :
                new KDLBoolean(b, type);

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

        public override bool Equals(object? obj)
            => obj is KDLBoolean other && other.Value == Value && Type == other.Type;

        public override int GetHashCode() => Value.GetHashCode();
    }
}
