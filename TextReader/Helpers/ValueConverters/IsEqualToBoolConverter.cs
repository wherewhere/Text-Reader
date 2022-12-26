namespace TextReader.Helpers.ValueConverters
{
    /// <summary>
    /// This class converts a value is equal to another value into a Boolean value
    /// </summary>
    public class IsEqualToBoolConverter : IsEqualToObjectConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsEqualToBoolConverter"/> class.
        /// </summary>
        public IsEqualToBoolConverter()
        {
            NotEqualValue = false;
            IsEqualValue = true;
        }
    }
}
