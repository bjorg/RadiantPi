namespace SampleParser {

    public enum EnumValues {
        First,
        Second
    }

    public class SampleRecord {

        //--- Properties ---
        public bool BoolValue { get; set; }
        public string StringValue { get; set; }
        public EnumValues EnumValue { get; set; }
    }
}