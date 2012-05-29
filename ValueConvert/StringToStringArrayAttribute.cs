using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public class StringToStringArrayAttribute : ValueConvertAttribute
    {

        public StringToStringArrayAttribute() : this(ConvertDirections.ResultToModel) { }

        public StringToStringArrayAttribute(ConvertDirections direction)
            : base(direction) { }

        public override object Convert(object obj)
        {
            string item = Cast<string>(obj);

            if (item != null)
                return item.Split(new char[] { ',' }).Where(t => !string.IsNullOrEmpty(t.Trim())).Select(t => t.Trim()).ToArray();

            return null;
        }
    }
}
