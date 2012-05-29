using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public class NullValueToDefaultValueAttribute : ValueConvertAttribute
    {
        object defaultValue;

        public NullValueToDefaultValueAttribute(object defaultValue) : this(defaultValue, ConvertDirections.ResultToModel) { }
        public NullValueToDefaultValueAttribute(object defaultValue, ConvertDirections direction)
            : base(direction)
        {
            this.defaultValue = defaultValue;
        }

        public override object Convert(object obj)
        {
            if (obj == null || obj is DBNull)
                return defaultValue;
            else
                return obj;
        }
    }
}
