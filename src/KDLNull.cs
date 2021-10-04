using System.IO;

#nullable enable

namespace KdlDotNet
{
    /**
     * A model object representing the KDL 'null' value.
     */
    public class KDLNull : KDLValue
    {
        public static KDLNull Instance { get; } = new KDLNull(null);

        public static KDLNull From(string? type)
            => type == null ? Instance : new KDLNull(type);

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

        public override bool Equals(object? obj) => obj is KDLNull other && Type == other.Type;

        public override int GetHashCode() => 0;
    }
}
