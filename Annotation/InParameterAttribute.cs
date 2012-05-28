using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.Annotation
{
    /// <summary>
    /// Marks a property that is not an input parameter of a query.
    /// </summary>
    public class NotAnInParameterAttribute : Attribute
    {
    }
}
