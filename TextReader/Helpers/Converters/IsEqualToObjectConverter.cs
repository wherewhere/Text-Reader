using Microsoft.Toolkit.Uwp.UI.Converters;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace TextReader.Helpers.Converters
{
    /// <summary>
    /// This class converts a value is equal to another value into an other object.
    /// Can be used to convert to visibility, a couple of colors, couple of images, etc.
    /// </summary>
    public class IsEqualToObjectConverter : DependencyObject, IValueConverter
    {
        /// <summary>
        /// Identifies the <see cref="IsEqualValue"/> property.
        /// </summary>
        public static readonly DependencyProperty IsEqualValueProperty =
            DependencyProperty.Register(nameof(IsEqualValue), typeof(object), typeof(BoolToObjectConverter), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="NotEqualValue"/> property.
        /// </summary>
        public static readonly DependencyProperty NotEqualValueProperty =
            DependencyProperty.Register(nameof(NotEqualValue), typeof(object), typeof(BoolToObjectConverter), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the value to be returned when the boolean is equal
        /// </summary>
        public object IsEqualValue
        {
            get { return GetValue(IsEqualValueProperty); }
            set { SetValue(IsEqualValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the value to be returned when the boolean is not equal
        /// </summary>
        public object NotEqualValue
        {
            get { return GetValue(NotEqualValueProperty); }
            set { SetValue(NotEqualValueProperty, value); }
        }

        /// <summary>
        /// Convert a value is equal to another value to an other object.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property, as a type reference.</param>
        /// <param name="parameter">An optional parameter to be used to invert the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isEqualValue = AreValuesEqual(value, parameter, true);

            return ConverterTools.Convert(isEqualValue ? IsEqualValue : NotEqualValue, targetType);
        }

        /// <summary>
        /// Convert back the value
        /// </summary>
        /// <remarks>If the <paramref name="value"/> parameter is a reference type, <see cref="TrueValue"/> must match its reference to return true.</remarks>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The type of the target property, as a type reference (System.Type for Microsoft .NET, a TypeName helper struct for Visual C++ component extensions (C++/CX)).</param>
        /// <param name="parameter">An optional parameter to be used to invert the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the source object.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        internal static bool AreValuesEqual(object value1, object value2, bool convertType)
        {
            if (object.Equals(value1, value2))
            {
                return true;
            }

            // If they are the same type but fail with Equals check, don't bother with conversion.
            if (value1 != null && value2 != null && convertType
                && value1.GetType() != value2.GetType())
            {
                // Try the conversion in both ways:
                return ConvertTypeEquals(value1, value2) || ConvertTypeEquals(value2, value1);
            }

            return false;
        }

        private static bool ConvertTypeEquals(object value1, object value2)
        {
            // Let's see if we can convert:
            value1 = value2 is Enum ? ConvertToEnum(value2.GetType(), value1) : ConverterTools.Convert(value1, value2.GetType());

            return value2.Equals(value1);
        }

        private static object ConvertToEnum(Type enumType, object value)
        {
            try
            {
                return Enum.IsDefined(enumType, value) ? Enum.ToObject(enumType, value) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
