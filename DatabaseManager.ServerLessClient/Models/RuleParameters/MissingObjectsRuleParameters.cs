namespace DatabaseManager.ServerLessClient.Models.RuleParameters
{
    // Mirrors MissingObjectsParameters read by PredictionMethods.PredictMissingDataObjects
    // on the Predictions service (DatabaseManager.Services.Predictions/Core/RuleMethodUtilities.cs).
    public class MissingObjectsRuleParameters
    {
        public string DataType { get; set; }
        public List<MissingObjectKeyPart> Keys { get; set; } = new();
        public List<MissingObjectDefaultPart> Defaults { get; set; } = new();
    }

    // One entry in Keys: sets dataObject[Key] = Value, or - if Value starts with "!" -
    // copies the value of that attribute from the parent data object instead.
    public class MissingObjectKeyPart
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    // One entry in Defaults: sets dataObject[Default] = Value after the object is built.
    public class MissingObjectDefaultPart
    {
        public string Default { get; set; }
        public string Value { get; set; }
    }
}
