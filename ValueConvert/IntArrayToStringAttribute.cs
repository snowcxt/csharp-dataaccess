using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public class IntArrayToStringAttribute : ValueConvertAttribute
    {
        public IntArrayToStringAttribute()
            : this(ConvertDirections.ModelToParameter) { }

        public IntArrayToStringAttribute(ConvertDirections direction)
            : base(direction) { }

        public override object Convert(object obj)
        {
            int[] items = Cast<int[]>(obj);
            return items == null ? "" : string.Join(",", items.Select(t => t.ToString()).ToArray());
        }
    }
}
