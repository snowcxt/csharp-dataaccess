using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using DataAccess.ValueConvert;
using DataAccess.Annotation;

namespace DataAccess
{
    public class DataAccessMetadataInfo
    {
        public PropertyInfo Info;
        public string Name;
        public bool IsPrimaryKey;
        private ValueConvertAttribute _convertValueAttr;
        public ValueConvertAttribute ConvertValueAttr
        {
            get
            {
                return _convertValueAttr;
            }
            set
            {
                _convertValueAttr = value;
                if (_convertValueAttr != null)
                    _convertValueFunc = _convertValueAttr.GetType().GetMethod("Convert");
                else
                    _convertValueFunc = null;
            }
        }
        private MethodInfo _convertValueFunc;
        public MethodInfo ConvertValueFunc { get { return _convertValueFunc; } }

    }
}
