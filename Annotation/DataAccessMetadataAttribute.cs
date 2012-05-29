using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataAccess.Annotation
{
    /// <summary>
    /// 
    /// </summary>
    public class DataAccessMetadataAttribute : Attribute
    {
        private Type metadataType;
        public Type MetadataClassType
        {
            get
            {
                return metadataType;
            }
        }
        public DataAccessMetadataAttribute(Type metadataType)
        {
            this.metadataType = metadataType;
        }
    }
}
