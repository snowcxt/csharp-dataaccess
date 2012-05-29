using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.Annotation
{
    public class NameMappingAttribute : Attribute
    {
        private string name;
        public string Name
        {
            get { return name; }
        }
        public NameMappingAttribute(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// The parameter name in a query
    /// </summary>
    public class ParameterNameAttribute : NameMappingAttribute
    {
        public ParameterNameAttribute(string name) : base(name) { }
    }

    /// <summary>
    /// The field name in a result set
    /// </summary>
    public class FieldNameAttribute : NameMappingAttribute
    {
        public FieldNameAttribute(string name) : base(name) { }
    }
}
