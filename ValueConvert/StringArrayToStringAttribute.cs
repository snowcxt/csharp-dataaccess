using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public class StringArrayToStringAttribute : ValueConvertAttribute
    {
        public StringArrayToStringAttribute() : this(ConvertDirections.ModelToParameter) { }

        public StringArrayToStringAttribute(ConvertDirections direction)
            : base(direction)
        {
        }

        public override object Convert(object obj)
        {
            return string.Join(",", Cast<string[]>(obj));
        }
    }
}
