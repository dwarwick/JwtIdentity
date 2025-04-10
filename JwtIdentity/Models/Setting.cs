namespace JwtIdentity.Models
{
    /// <summary>
    /// Represents a system setting stored as a key-value pair in the database
    /// </summary>
    public class Setting : BaseModel
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
        public string DataType { get; set; }
        
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
    }
}