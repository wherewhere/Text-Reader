namespace TextReader.Helpers.Converters
{
    /// <summary>
    /// This class converts a object value into a Boolean value (if the value is null or empty returns a false value).
    /// </summary>
    public class EmptyObjectToBoolConverter : EmptyObjectToObjectConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyObjectToBoolConverter"/> class.
        /// </summary>
        public EmptyObjectToBoolConverter()
        {
            NotEmptyValue = true;
            EmptyValue = false;
        }
    }
}
