using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.ValueConvert
{
    public interface IValueConvert
    {
        object Convert(object obj);
    }

    /// <summary>
    /// value converting direction
    /// </summary>
    public enum ConvertDirections { ModelToParameter, ResultToModel }

    /// <summary>
    /// The basic ValueConvert class
    /// </summary>
    /// <example>
    /// Define a class that implements IValueConvert
    /// <code>
    /// public class NullValueToZero : IValueConvert
    /// {
    ///     public object Convert(object obj)
    ///     {
    ///         return obj ?? 0;
    ///     }
    /// }
    /// </code>
    /// In the model, mark property with ValueConvert and set set ValueConvertor as <c>typeof(NullValueToZero)</c>
    /// <code>
    /// //When set value to the Age property, the origianl value will be changed according to the Convert function in NullValueToZero class
    /// [ValueConvert(ConvertDirections.ResultToModel, ValueConvertor = typeof(NullValueToZero))]
    /// public int Age { get; set; }
    /// </code>
    /// Or define a class that drives from ValueConvertAttribute
    /// <code>
    /// public class NullValueToZeroAttribute : ValueConvertAttribute
    /// {
    ///     //override the Convert function
    ///     public override object Convert(object obj)
    ///     {
    ///         if (obj == null || obj is DBNull)
    ///             return 0;
    ///         else
    ///             return obj;
    ///     }
    /// }
    /// </code>
    /// In the model, mark property with NullValueToZero
    /// <code>
    /// //When set value to the Age property, the origianl value will be changed according to the Convert function in NullValueToZeroAttribute class
    /// [NullValueToZeroAttribute(ConvertDirections.ResultToModel)]
    /// public int Age { get; set; }
    /// </code>
    /// </example>
    public class ValueConvertAttribute : Attribute
    {
        public Type ValueConvertor = null;

        public ValueConvertAttribute(ConvertDirections direction)
        {
            this.direction = direction;
        }

        ConvertDirections direction;
        internal ConvertDirections ConvertDirection
        {
            get { return direction; }
        }

        public T Cast<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return default(T);

            if (obj is T)
                return (T)obj;

            else
                return default(T);
        }

        public virtual object Convert(object obj)
        {
            if (ValueConvertor == null)
                return obj;
            else
            {
                Object convert = Activator.CreateInstance(ValueConvertor, false);
                IValueConvert vc = convert as IValueConvert;
                if (vc != null)
                    return vc.Convert(obj);
                else
                    throw new Exception("The convertor must implment IValueConvert");
            }
        }
    }
}
