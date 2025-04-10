namespace JwtIdentity.Common.ViewModels
{
    public class SettingViewModel : BaseViewModel
    {
        /// <summary>
        /// The unique identifier for the setting
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The setting key (name)
        /// </summary>
        public string Key { get; set; }
        
        /// <summary>
        /// The setting value stored as a string
        /// </summary>
        public string Value { get; set; }
        
        /// <summary>
        /// The data type of the setting value
        /// </summary>
        public string DataType { get; set; } = "String";
        
        /// <summary>
        /// Optional description of what the setting is for
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Whether this setting is editable through the UI
        /// </summary>
        public bool IsEditable { get; set; } = true;
        
        /// <summary>
        /// Category/group for the setting to organize settings in the UI
        /// </summary>
        public string Category { get; set; } = "General";

        /// <summary>
        /// Returns a list of available data types for settings
        /// </summary>
        public static List<string> AvailableDataTypes => new List<string>
        {
            "String",
            "Int",
            "Long",
            "Decimal",
            "Double", 
            "Boolean",
            "DateTime"
        };
    }
}