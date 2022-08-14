﻿using Microsoft.Toolkit.Uwp.UI.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace TextReader.Helpers.ValueConverters
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
