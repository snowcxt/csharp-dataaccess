using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;
using System.Xml;
using System.Security.Principal;

namespace DataAccess
{
    /// <summary>
    /// Represents a Transact-SQL statement or stored procedure to execute against a SQL Server database.
    /// </summary>
    public class SqlExecution
    {
        private SqlCommand command;
        private SqlAccess sqlAccess;


        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecution"/> class with the default connection string. You can config the default connection string with <see cref="DataAccessConfig"/>.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="commandType">Type of the command.</param>
        public SqlExecution(string commandText, CommandType commandType = CommandType.StoredProcedure)
        {
            sqlAccess = new SqlAccess();
            this.command = sqlAccess.CreateCommand(commandText, commandType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlExecution"/> class.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="commandType">Type of the command.</param>
        public SqlExecution(string commandText, string connectionString, CommandType commandType = CommandType.StoredProcedure)
        {
            sqlAccess = new SqlAccess(connectionString);
            this.command = sqlAccess.CreateCommand(commandText, commandType);
        }

        /// <summary>
        /// Gets the parameters of command.
        /// </summary>
        public DbParameterCollection Parameters
        {
            get { return command.Parameters; }
        }

        #region Parameters block

        /// <summary>
        /// Adds an out parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        public void AddOutParameter(string name, SqlDbType type)
        {
            sqlAccess.AddOutParameter(command, name, type);
        }


        /// <summary>
        /// Adds return parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="type">Parameter type.</param>
        public void AddReturnParameter(string name, SqlDbType type)
        {
            sqlAccess.AddReturnParameter(command, name, type);
        }

        /// <summary>
        /// Adds an input parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        public void AddInParameter(string name, object value)
        {
            sqlAccess.AddInParameter(command, name, value);
        }

        /// <summary>
        /// Adds input parameters based on a model. 
        /// This function fills the public properties as parameters to the command.
        /// (skip the properties marked by NotAnInParameter)
        /// </summary>
        /// <param name="parameters">The model</param>
        /// <param name="include">The public properties that are the parameters of the command. 
        /// if null - all the public properties are parameters, except the properties maked by NotAnInParameter</param>
        /// <param name="exclude">The public properties that are NOT the parameters of the command.
        /// if null - there is no exception</param>
        /// <param name="metadata">The metadata</param>
        /// <example>
        /// <code>
        /// //create an instance of SqlExecution with the stored proc "InsertProduct"
        /// SqlExecution db = new SqlExecution("InsertProduct");
        /// //fill a ProductModel to the command.
        /// db.AddInParametersByModel&lt;ProductModel&gt;(product);
        /// //Execute the command
        /// db.ExecuteNonQuery();
        /// </code>
        /// </example>
        public void AddInParametersByModel(object parameters, string[] include = null, string[] exclude = null, Type metadata = null)
        {
            sqlAccess.AddInParametersByModel(command, parameters, include, exclude, metadata);
        }

        #endregion

        #region Query methods

        /// <summary>
        /// Executes the command and returns the number of rows affected.
        /// </summary>
        /// <returns></returns>
        public int ExecuteNonQuery()
        {
            return sqlAccess.ExecuteCommand<int>(command.Connection, () => command.ExecuteNonQuery());
        }

        /// <summary>
        /// Executes the query, and returns the first column of the first row in the result set returned by the query. Additional columns or rows are ignored.
        /// </summary>
        /// <returns></returns>
        public object ExecuteScalar()
        {
            return sqlAccess.ExecuteCommand<object>(command.Connection, () => command.ExecuteScalar());
        }

        /// <summary>
        /// Executes the query, and returns a strong typed list.
        /// (This function fills the query result to a strong typed list, except the properties marked by NotAResultValue)
        /// </summary>
        /// <typeparam name="T">Model's type.</typeparam>
        /// <returns>Strong typed list.</returns>
        /// <example>
        /// <code>
        /// //create an instance of SqlExecution with the stored proc "SearchProducts"
        /// SqlExecution db = new SqlExecution("SearchProducts");
        /// //get product list
        /// IEnumerable&lt;ProductModel&gt; productList = db.ExecuteModel&lt;ProductModel&gt;();
        /// </code>
        /// </example>
        public IEnumerable<T> ExecuteModel<T>(Type metadata = null) where T : new()
        {
            return sqlAccess.ExecuteCommand<IEnumerable<T>>(command.Connection, () => sqlAccess.QueryModel<T>(command, metadata: metadata));
        }

        /// <summary>
        /// Executes the query, and returns a dataset.
        /// </summary>
        /// <returns>dataset</returns>
        public DataSet ExecuteDataSet()
        {
            return sqlAccess.ExecuteCommand<DataSet>(command.Connection, () => sqlAccess.QueryDataSet(command));
        }

        #endregion
    }
}
