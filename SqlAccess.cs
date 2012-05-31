using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DataAccess.ValueConvert;
using System.Data.SqlClient;
using System.Reflection;
using System.Data;
using System.Security.Principal;
using System.Data.Common;

namespace DataAccess
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlAccess : ModelAccessBase
    {
        private string defaultConnectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlAccess"/> class with the default connection string. You can config the default connection string with <see cref="DataAccessConfig"/>.
        /// </summary>
        public SqlAccess() : this(new DataAccessConfig().DefaultConnetionString) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlAccess"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlAccess(string connectionString)
        {
            defaultConnectionString = connectionString;
        }

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns></returns>
        public SqlCommand CreateCommand(string commandText, CommandType commandType = CommandType.StoredProcedure)
        {
            SqlCommand command = new SqlCommand(commandText, new SqlConnection(defaultConnectionString));
            command.CommandType = commandType;

            return command;
        }

        #region compose command

        private string GetWhereClause(IEnumerable<string> idList)
        {
            if (idList != null)
                return idList.Count() > 0 ? " WHERE " + string.Join(" AND ", idList.Select(t => string.Format("[{0}]=@{0}", t)).ToArray()) : "";
            return "";
        }

        private string ComposeWhereClauseForSelectDelete(SqlCommand command, object whereClause)
        {
            List<string> idList = new List<string>();
            if (whereClause != null)
            {
                Type type = whereClause.GetType();
                foreach (PropertyInfo info in type.GetProperties())
                {
                    string name = info.Name;
                    object val = info.GetValue(whereClause, null);

                    idList.Add(name);
                    AddInParameter(command, name, val);
                }
            }

            return GetWhereClause(idList);
        }

        private string ComposeSelectClause(string tableName, Type modelType, string[] include = null, string[] exclude = null, Type metadata = null)
        {
            if (exclude == null)
            {
                if (include == null)
                    return string.Format("SELECT * FROM {0}", tableName);
                else
                    return string.Format("SELECT {1} FROM {0}", tableName, string.Join(", ", include.Select(t => "[" + t + "]").ToArray()));
            }
            else
            {
                List<string> fields = new List<string>();
                var mdList = GetMetaData(modelType, ConvertDirections.ResultToModel, metadata);
                foreach (var md in mdList)
                {
                    if ((include == null || include.Contains(md.Name)) && (!exclude.Contains(md.Name)))
                    {
                        fields.Add("[" + md.Name + "]");
                    }
                }

                return string.Format("SELECT {1} FROM {0}", tableName, string.Join(", ", fields.ToArray()));
            }
        }

        /// <summary>
        /// Gets a model from a sql table based on the whereClause
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <param name="include">The include.</param>
        /// <param name="exclude">The exclude.</param>
        /// <param name="metadata">The extra metadata.</param>
        /// <returns></returns>
        /// <example>
        ///   <code>
        /// //get a product model from Product table where ID = 1
        /// ProductModel product = new SqlAccess().GetModel&lt;ProductModel&gt;("Product", new { ID = 1 });
        ///   </code>
        ///   </example>
        public T GetMode<T>(string tableName, object whereClause, string[] include = null, string[] exclude = null, Type metadata = null) where T : new()
        {
            SqlCommand command = new SqlCommand();
            command.Connection = new SqlConnection(defaultConnectionString);

            command.CommandText = string.Format("{0}{1}",
                   ComposeSelectClause(tableName, typeof(T), include, exclude, metadata),
                   ComposeWhereClauseForSelectDelete(command, whereClause));

            return ExecuteCommand<T>(command.Connection, () => QueryModel<T>(command, metadata: metadata).FirstOrDefault());
        }

        /// <summary>
        /// Gets all the rows in a sql table.
        /// </summary>
        /// <typeparam name="T">Model's type.</typeparam>
        /// <param name="tableName">Table's name.</param>
        /// <param name="include">The include.</param>
        /// <param name="exclude">The exclude.</param>
        /// <param name="metadata">The extra metadata.</param>
        /// <returns></returns>
        public IEnumerable<T> GetModelList<T>(string tableName, string[] include = null, string[] exclude = null, Type metadata = null) where T : new()
        {
            SqlCommand command = CreateCommand(ComposeSelectClause(tableName, typeof(T), include, exclude, metadata), CommandType.Text);
            return ExecuteCommand<IEnumerable<T>>(command.Connection, () => QueryModel<T>(command, metadata: metadata));
        }

        /// <summary>
        /// Composes the insert command.
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="model">The model.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="include">The property list.</param>
        /// <param name="exclude">The exceptions.</param>
        /// <param name="metadata">The extra metadata.</param>
        public void ComposeInsertCommand(SqlCommand command, object model, string tableName, string[] include = null, string[] exclude = null, Type metadata = null)
        {
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            if (model != null)
            {
                List<string> fields = new List<string>();
                List<string> para = new List<string>();
                GetFromModel(model, (name, val, isPrimaryKey) =>
                {
                    if ((include == null || include.Contains(name)) && (exclude == null || !exclude.Contains(name)))
                    {
                        AddInParameter(command, name, val);
                        fields.Add("[" + name + "]");
                        para.Add("@" + name);
                    }
                }, metadata);

                command.CommandText = string.Format("INSERT {0}({1})VALUES({2})",
                    tableName,
                    string.Join(",", fields.ToArray()),
                    string.Join(",", para.ToArray())
                );
            }
        }

        /// <summary>
        /// Inserts a model in a sql table
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="include">The properties that are inserted into table.</param>
        /// <param name="exclude">The properties that are NOT inserted into table.</param>
        /// <param name="metadata">The metadata</param>
        /// <example>
        /// <code>
        /// //insert a product model into Product table, except "ModifiedBy" and "Modified" properties
        /// new SqlAccess().InsertModel&lt;ProductModel&gt;(model, "Product", exclude: new string[] { "ModifiedBy", "Modified" });
        /// </code>
        /// </example>
        public void InsertModel(object model, string tableName, string[] include = null, string[] exclude = null, Type metadata = null)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = new SqlConnection(defaultConnectionString);
            ComposeInsertCommand(command, model, tableName, include, exclude, metadata);
            ExecuteCommand<int>(command.Connection, () => command.ExecuteNonQuery());
        }

        /// <summary>
        /// Composes the update command.
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="model">The model.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="include">The property list.</param>
        /// <param name="exclude">The exceptions.</param>
        /// <param name="skipNull">if set to <c>true</c>, the function will skip the null value.</param>
        /// <param name="primaryKeys">The primary keys.</param>
        /// <param name="metadata">The metadata</param>
        public void ComposeUpdateCommand(SqlCommand command, object model, string tableName, string[] include = null, string[] exclude = null, bool skipNull = true, string[] primaryKeys = null, Type metadata = null)
        {
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            if (model != null)
            {
                List<string> fields = new List<string>();
                List<string> idList = new List<string>();

                GetFromModel(model, (name, val, isPrimaryKey) =>
                {
                    if ((include == null || include.Contains(name)) && (exclude == null || !exclude.Contains(name)))
                    {
                        if ((primaryKeys == null && isPrimaryKey) || (primaryKeys != null && primaryKeys.Contains(name)))
                        {
                            AddInParameter(command, name, val);
                            idList.Add(name);
                        }
                        else
                        {
                            if (!skipNull || val != null)
                            {
                                AddInParameter(command, name, val);
                                fields.Add(string.Format("[{0}]=@{0}", name));
                            }
                        }

                    }
                }, metadata);

                command.CommandText = string.Format("UPDATE {0} SET {1}{2}",
                    tableName,
                    string.Join(",", fields.ToArray()),
                    primaryKeys != null ? GetWhereClause(primaryKeys) : GetWhereClause(idList)
                );
            }
        }

        /// <summary>
        /// Updates a row in a sql table based on the values of properties listed in primaryKeys or marked by PrimaryKey if primaryKeys=null
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="include">The properties that are updated.</param>
        /// <param name="exclude">The properties that are NOT updated.</param>
        /// <param name="skipNull">if set to <c>true</c>, the function will skip the null value.</param>
        /// <param name="primaryKeys">The primary keys.</param>
        /// <param name="metadata">The metadata</param>
        /// <example>
        /// <code>
        /// //update the product model into Product table, except "CreatedBy", "Created" properties
        /// new SqlAccess().UpdateModel&lt;ProductModel&gt;(model, "Product", exclude: new string[] { "CreatedBy", "Created" });
        /// </code>
        /// </example>
        public void UpdateModel(object model, string tableName, string[] include = null, string[] exclude = null, bool skipNull = true, string[] primaryKeys = null, Type metadata = null)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = new SqlConnection(defaultConnectionString);
            ComposeUpdateCommand(command, model, tableName, include, exclude, skipNull, primaryKeys, metadata);
            ExecuteCommand<int>(command.Connection, () => command.ExecuteNonQuery());
        }

        /// <summary>
        /// Composes the delete command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereClause">The where clause.</param>
        public void ComposeDeleteCommand(SqlCommand command, string tableName, object whereClause)
        {
            command.CommandType = CommandType.Text;
            command.Parameters.Clear();
            command.CommandText = string.Format("DELETE FROM {0}{1}",
                tableName,
                ComposeWhereClauseForSelectDelete(command, whereClause)
            );
        }

        /// <summary>
        /// a model from a sql table based on the whereClause
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereClause">The where clause.</param>
        /// <example>
        /// <code>
        /// //delete the product where ID = 1
        /// new SqlAccess().DeleteModel("Product", new { ID = 1 });
        /// </code>
        /// </example>
        public void DeleteModel(string tableName, object whereClause)
        {
            SqlCommand command = new SqlCommand();
            command.Connection = new SqlConnection(defaultConnectionString);

            ComposeDeleteCommand(command, tableName, whereClause);
            ExecuteCommand<int>(command.Connection, () => command.ExecuteNonQuery());
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Adds an input parameter to a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        public void AddInParameter(DbCommand command, string name, object value)
        {
            SqlParameter para = new SqlParameter(name, value);
            command.Parameters.Add(para);
        }

        /// <summary>
        /// Adds input parameters to a command based on a model. 
        /// This function fills the public properties as parameters to the command.
        /// (skip the properties marked by NotAnInParameter)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command.</param>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="parameters">The model</param>
        /// <param name="include">The public properties that are the parameters of the command. 
        /// if null - all the public properties are parameters, except the properties maked by NotAnInParameter</param>
        /// <param name="exclude">The public properties that are NOT the parameters of the command.
        /// if null - there is no exception</param>
        /// <param name="metadata">The metadata</param>
        public void AddInParametersByModel(DbCommand command, object parameters, string[] include = null, string[] exclude = null, Type metadata = null)
        {
            if (parameters != null)
            {
                GetFromModel(parameters, (name, val, isPrimaryKey) =>
                {
                    if ((include == null || include.Contains(name)) && (exclude == null || !exclude.Contains(name)))
                        AddInParameter(command, name, val);
                },
                metadata);
            }
        }


        /// <summary>
        /// Adds an output parameter to a command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        public void AddOutParameter(DbCommand command, string name, SqlDbType type)
        {
            SqlParameter para = new SqlParameter(name, type);
            para.Direction = ParameterDirection.Output;
            command.Parameters.Add(para);
        }

        /// <summary>
        /// Adds return parameter.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        public void AddReturnParameter(DbCommand command, string name, SqlDbType type)
        {
            SqlParameter para = new SqlParameter(name, type);
            para.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(para);
        }

        #endregion

        #region Query

        /// <summary>
        /// Executes the query, and returns a strong typed list.
        /// (This function fills the query result to a strong typed list, except the properties marked by NotAResultValue)
        /// </summary>
        /// <typeparam name="T">Model's type.</typeparam>
        /// <param name="command">The command.</param>
        /// <param name="metadata">The extra metadata.</param>
        /// <returns>Strong typed list.</returns>
        public IEnumerable<T> QueryModel<T>(DbCommand command, string[] include = null, string[] exclude = null, Type metadata = null) where T : new()
        {
            List<T> resultList = new List<T>();
            DbDataReader reader = command.ExecuteReader();

            DataTable schemaTable = reader.GetSchemaTable();

            var metaData = GetMetaData(typeof(T), ConvertDirections.ResultToModel, metadata);

            while (reader.Read())
            {
                resultList.Add(SetToModel<T, IDataReader>(
                    reader, metaData,
                    (col, data) =>
                    {
                        if ((include == null || include.Contains(col)) && (exclude == null || !exclude.Contains(col)))
                            return schemaTable.Select("ColumnName='" + col + "'").Count() > 0 ? data[col] : null;
                        else
                            return null;
                    }));
            }

            reader.Close();

            return resultList;
        }

        /// <summary>
        /// Executes the query, and returns a dataset.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public DataSet QueryDataSet(DbCommand command)
        {
            using (DbDataAdapter mySqlDataAdapter = new SqlDataAdapter())
            {
                mySqlDataAdapter.SelectCommand = command;
                DataSet myDataSet = new DataSet();
                mySqlDataAdapter.Fill(myDataSet);

                return myDataSet;
            }
        }

        public ReturnType ExecuteCommand<ReturnType>(DbConnection connection, Func<ReturnType> execution)
        {
            using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                try
                {
                    connection.Open();
                    return execution();
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    connection.Close();
                }
            }
        }


        /// <summary>
        /// Executes a transaction.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <example>
        /// <code>
        /// //create an instance of SqlAccess
        /// SqlAccess sqlAccess = new SqlAccess();
        /// //execute a transaction to update a user's profile and insert a list of products
        /// sqlAccess.ExecuteTransaction(command =>
        ///     {
        ///         //update a user's profile
        ///         //change command's type and text
        ///         command.CommandType = System.Data.CommandType.Text;
        ///         command.CommandText = "UPDATE UserProfile SET Name=@Name, Age=@Age WHERE ID=@ID";
        ///         //add input parameters with a UserModel instance
        ///         sqlAccess.AddInParametersByModel&lt;UserModel&gt;(command, userProfile);
        ///         //execute the command
        ///         command.ExecuteNonQuery();
        ///
        ///         //insert a list of products
        ///         foreach (var p in products)
        ///         {
        ///             //create the insertion command
        ///             sqlAccess.ComposeInsertCommand&lt;ProductModel&gt;(command, p, "Product");
        ///             //execute the command
        ///             command.ExecuteNonQuery();
        ///         }   
        ///     }
        ///  );
        /// </code>
        /// </example>
        public void ExecuteTransaction(Action<SqlCommand> handler)
        {
            using (WindowsImpersonationContext context = WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                using (SqlConnection connection = new SqlConnection(defaultConnectionString))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction = connection.BeginTransaction();
                    command.Connection = connection;
                    command.Transaction = transaction;
                    try
                    {
                        handler(command);
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }
                        throw (ex);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Converts a table to a model list
        /// </summary>
        /// <typeparam name="T">Model's type</typeparam>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        public IEnumerable<T> ConvertTableToModel<T>(DataTable table, Type metadata = null) where T : new()
        {
            if (table != null)
            {
                var defaultmetaData = GetMetaData(typeof(T), ConvertDirections.ResultToModel, metadata);
                foreach (DataRow dr in table.Rows)
                {
                    yield return SetToModel<T, DataRow>(dr, defaultmetaData, (col, data) => data.Table.Columns.Contains(col) ? data[col] : default(T));
                }
            }
        }

    }
}
