namespace DatabaseManager.ServerLessClient.Models.RuleParameters
{
    public enum RuleParameterFieldType
    {
        Text,
        Number,
        Select
    }

    // Describes a single input field within a validity rule's RuleParameters JSON.
    // Key must match the JSON property name QCMethods.cs on the DataQC service reads
    // for that RuleFunction (e.g. "MinRange", "WindowSize").
    public class RuleParameterFieldDescriptor
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public RuleParameterFieldType Type { get; set; } = RuleParameterFieldType.Text;
        public object DefaultValue { get; set; }
        public string HelperText { get; set; }

        // Only used when Type == Select.
        public List<string> Options { get; set; }
    }

}
