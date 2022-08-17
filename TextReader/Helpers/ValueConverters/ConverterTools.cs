﻿using System;
using System.Globalization;
using System.Reflection;
using Windows.UI.Xaml.Markup;

namespace TextReader.Helpers.ValueConverters
{
    /// <summary>
    /// Static class used to provide internal tools
    /// </summary>
    internal static class ConverterTools
    {
        /// <summary>
        /// Helper method to safely cast an object to a boolean
        /// </summary>
        /// <param name="parameter">Parameter to cast to a boolean</param>
        /// <returns>Bool value or false if cast failed</returns>
        internal static bool TryParseBool(object parameter)
        {
            var parsed = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out parsed);
            }

            return parsed;
        }

        /// <summary>
        /// Helper method to convert a value from a source type to a target type.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="targetType">The target type</param>
        /// <returns>The converted value</returns>
        internal static object Convert(object value, Type targetType)
        {
            if (targetType.IsInstanceOfType(value))
            {
                return value;
            }
            else
            {
                return XamlBindingHelper.ConvertValue(targetType, value);
            }
        }
    }
}
