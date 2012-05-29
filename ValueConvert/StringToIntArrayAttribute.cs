using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public class StringToIntArrayAttribute : ValueConvertAttribute
    {
        public StringToIntArrayAttribute() : this(ConvertDirections.ResultToModel) { }

        public StringToIntArrayAttribute(ConvertDirections direction)
            : base(direction) { }

        public override object Convert(object obj)
        {
            string item = Cast<string>(obj);
            List<int> result = new List<int>();
            if (item != null)
            {
                string[] strList = item.Split(new char[] { ',' });
                foreach (string str in strList)
                {
                    if (str.Trim() != "")
                    {
                        int output;
                        if (int.TryParse(str.Trim(), out output))
                            result.Add(output);
                    }
                }
            }
            return result.ToArray();
        }
    }
}
