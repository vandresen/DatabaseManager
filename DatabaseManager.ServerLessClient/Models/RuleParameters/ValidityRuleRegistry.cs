namespace DatabaseManager.ServerLessClient.Models.RuleParameters
{
    // Single source of truth for the "simple" validity/prediction rule functions -
    // the ones whose RuleParameters is just a flat set of number/text fields, matching
    // a static method on DatabaseManager.Services.DataQC.Core.QCMethods.
    //
    // To add a new built-in validity rule:
    //   1. Implement the matching static method in QCMethods.cs on the DataQC service.
    //   2. Register the function name in the RuleFunctions table (Manage Rule Functions
    //      page) with FunctionType "V" (or "P" for predictions) so it appears in the
    //      Rule Function dropdown.
    //   3. Add one entry below describing its fields.
    // No new Razor markup or C# parameter class is needed - RuleParametersEditor
    // renders any descriptor here generically.
    //
    // Rule functions with bespoke UI needs (Entirety, Uniqueness, Consistency,
    // Completeness) are handled separately in RuleParametersEditor and are not
    // part of this registry. Anything not found here or in that bespoke set falls
    // back to a raw text box, so external (http...) rule functions keep working.
    public static class ValidityRuleRegistry
    {
        public static readonly Dictionary<string, List<RuleParameterFieldDescriptor>> Descriptors = new()
        {
            ["ValidityRange"] = new List<RuleParameterFieldDescriptor>
            {
                new() { Key = "MinRange", Label = "Min Range", Type = RuleParameterFieldType.Number },
                new() { Key = "MaxRange", Label = "Max Range", Type = RuleParameterFieldType.Number },
            },
            ["CurveSpikes"] = new List<RuleParameterFieldDescriptor>
            {
                new() {
                    Key = "WindowSize", Label = "Window Size", Type = RuleParameterFieldType.Number, DefaultValue = 5d,
                    HelperText = "Number of neighboring points on each side used to detect a spike."
                },
                new() {
                    Key = "SeveritySize", Label = "Severity Size", Type = RuleParameterFieldType.Number, DefaultValue = 4d,
                    HelperText = "Standard deviations from the window average that count as a spike."
                },
            },
            ["StringLength"] = new List<RuleParameterFieldDescriptor>
            {
                new() { Key = "Min", Label = "Min Length", Type = RuleParameterFieldType.Number, DefaultValue = 20d },
                new() { Key = "Max", Label = "Max Length", Type = RuleParameterFieldType.Number, DefaultValue = 20d },
            },
            ["IsEqualTo"] = new List<RuleParameterFieldDescriptor>
            {
                new() {
                    Key = "Value", Label = "Allowed Values", Type = RuleParameterFieldType.Text,
                    HelperText = "Separate multiple allowed values with the delimiter below."
                },
                new() { Key = "Delimiter", Label = "Delimiter", Type = RuleParameterFieldType.Text, DefaultValue = "," },
            },
            ["IsGreaterThan"] = new List<RuleParameterFieldDescriptor>
            {
                new() { Key = "Value", Label = "Minimum Allowed Value", Type = RuleParameterFieldType.Number },
            },
            ["IsNumber"] = new List<RuleParameterFieldDescriptor>(),

            // Example of how a brand new validity rule would be added going forward:
            // ["IsBetweenPercent"] = new List<RuleParameterFieldDescriptor>
            // {
            //     new() { Key = "LowerPercent", Label = "Lower Percent", Type = RuleParameterFieldType.Number },
            //     new() { Key = "UpperPercent", Label = "Upper Percent", Type = RuleParameterFieldType.Number },
            // },
        };
    }

}
