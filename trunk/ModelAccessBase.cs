using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccess.ValueConvert;
using System.Reflection;
using DataAccess.Annotation;
using System.ComponentModel;

namespace DataAccess
{
    /// <summary>
    /// The function that maps a field in the data source with a property in the model
    /// </summary>
    /// <typeparam name="T">Model's type</typeparam>
    /// <param name="fieldName">Name of the field.</param>
    /// <param name="dataSource">The data source.</param>
    public delegate object MappingMethodHandler<T>(string fieldName, T dataSource);

    /// <summary>
    /// The function that sets value to a model by the property's name and if is primary key
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="val">The val.</param>
    /// <param name="isPrimaryKey">if set to <c>true</c> is a primary key.</param>
    public delegate void SetValueHandler(string name, object val, bool isPrimaryKey);

    /// <summary>
    /// Gets or sets value from or to a model
    /// </summary>
    public abstract class ModelAccessBase
    {
        private void ExtendMetadata(Type extraMetadataType, ConvertDirections direction, List<DataAccessMetadataInfo> dic)
        {
            if (extraMetadataType != null)
            {
                foreach (PropertyInfo info in extraMetadataType.GetProperties())
                {

                    if (IsCandidate(info, direction))
                    {
                        DataAccessMetadataInfo mmd = dic.FirstOrDefault(t => t.Info.Name == info.Name);
                        if (mmd != null)
                        {
                            DataAccessMetadataInfo mmd2 = GetMetadataForProp(info, direction);
                            mmd2.Info = mmd.Info;
                            dic.RemoveAll(t => t.Info.Name == info.Name);
                            dic.Add(mmd2);
                        }
                    }
                    else
                    {
                        dic.RemoveAll(t => t.Info.Name == info.Name);
                    }
                }
            }
        }

        private DataAccessMetadataInfo GetMetadataForProp(PropertyInfo info, ConvertDirections direction)
        {
            string name = ConvertName(info, direction);
            return new DataAccessMetadataInfo()
                    {
                        Info = info,
                        Name = name ?? info.Name,
                        ConvertValueAttr = GetConvertAttr(info, direction),
                        IsPrimaryKey = info.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Length > 0
                    };
        }

        protected IEnumerable<DataAccessMetadataInfo> GetMetaData(Type modelType, ConvertDirections direction, Type extraMetadata = null)
        {
            List<DataAccessMetadataInfo> dic = new List<DataAccessMetadataInfo>();
            foreach (PropertyInfo info in modelType.GetProperties())
            {
                if (IsCandidate(info, direction))
                {
                    dic.Add(GetMetadataForProp(info, direction));
                }
            }

            DataAccessMetadataAttribute defaultMetadataTypeAttribute = modelType.GetCustomAttributes(typeof(DataAccessMetadataAttribute), false).FirstOrDefault() as DataAccessMetadataAttribute;
            if (defaultMetadataTypeAttribute != null)
                ExtendMetadata(defaultMetadataTypeAttribute.MetadataClassType, direction, dic);

            ExtendMetadata(extraMetadata, direction, dic);

            return dic;
        }

        protected ModelType SetToModel<ModelType, SourceType>
            (
            SourceType dataRow,
            IEnumerable<DataAccessMetadataInfo> metaData,
            MappingMethodHandler<SourceType> mappingMethod
            ) where ModelType : new()
        {
            ModelType result = new ModelType();

            foreach (DataAccessMetadataInfo ma in metaData)
            {
                object val = mappingMethod(ma.Name, dataRow);
                if (val != null && !(val is DBNull))
                {
                    if (ma.ConvertValueAttr != null)
                        val = ma.ConvertValueFunc.Invoke(ma.ConvertValueAttr, new object[] { val });
                    ma.Info.SetValue(result, val, null);
                }
            }

            return result;
        }


        /// <summary>
        /// Sets value to model.
        /// </summary>
        /// <typeparam name="SourceType">The type of the data source.</typeparam>
        /// <param name="dataRow">A row of data source.</param>
        /// <param name="mappingMethod">The mapping method.</param>
        /// <returns></returns>
        protected ModelType SetToModel<ModelType, SourceType>(
            SourceType dataRow,
            MappingMethodHandler<SourceType> mappingMethod) where ModelType : new()
        {
            var metaData = GetMetaData(typeof(ModelType), ConvertDirections.ResultToModel);
            return SetToModel<ModelType, SourceType>(dataRow, metaData, mappingMethod);
        }

        protected void GetFromModel(object model, IEnumerable<DataAccessMetadataInfo> metaData, SetValueHandler setMethod)
        {
            foreach (DataAccessMetadataInfo ma in metaData)
            {
                object val = ma.Info.GetValue(model, null);
                if (val != null)
                {
                    if (ma.ConvertValueFunc != null)
                        val = ma.ConvertValueFunc.Invoke(ma.ConvertValueAttr, new object[] { val });
                    if (val != null)
                        setMethod(ma.Name, val, ma.IsPrimaryKey);
                }
            }
        }

        /// <summary>
        /// Gets value from model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="setMethod">The set method.</param>
        protected void GetFromModel(object model, SetValueHandler setMethod, Type extraMetadata = null)
        {
            var metaData = GetMetaData(model.GetType(), ConvertDirections.ModelToParameter, extraMetadata);
            GetFromModel(model, metaData, setMethod);
        }

        private bool IsCandidate(PropertyInfo info, ConvertDirections dir)
        {
            switch (dir)
            {
                case ConvertDirections.ResultToModel:
                    return info.CanWrite && info.GetCustomAttributes(typeof(NotAResultValueAttribute), false).Length == 0;
                case ConvertDirections.ModelToParameter:
                    return info.CanRead && info.GetCustomAttributes(typeof(NotAnInParameterAttribute), false).Length == 0;
            }
            return false;
        }

        private string ConvertName(PropertyInfo info, ConvertDirections dir)
        {
            object columnName = null;
            switch (dir)
            {
                case ConvertDirections.ResultToModel:
                    columnName = info.GetCustomAttributes(typeof(FieldNameAttribute), false).FirstOrDefault();
                    break;
                case ConvertDirections.ModelToParameter:
                    columnName = info.GetCustomAttributes(typeof(ParameterNameAttribute), false).FirstOrDefault();
                    break;
            }

            if (columnName != null)
                return ((NameMappingAttribute)(columnName)).Name;
            else
                return null;
        }

        private ValueConvertAttribute GetConvertAttr(PropertyInfo info, ConvertDirections dir)
        {
            return (ValueConvertAttribute)info.GetCustomAttributes(typeof(ValueConvertAttribute), true).FirstOrDefault(t => ((ValueConvertAttribute)t).ConvertDirection == dir);
        }

        private MethodInfo GetConvertFunc(ValueConvertAttribute convertAttr)
        {
            return convertAttr.GetType().GetMethod("Convert");
        }
    }
}
