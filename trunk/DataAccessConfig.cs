using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace DataAccess
{
    /// <summary>
    /// Represents the DataAccess section within a configuration file.
    /// </summary>
    /// <example>
    /// copy this section at the top of &lt;configuration&gt; section
    /// <code>
    /// &lt;configSections&gt;
    ///     &lt;section name=&quot;dataAccess&quot; type=&quot;DataAccess.DataAccessConfig, DataAccess, Version=1.0.0.0, PublicKeyToken=a9774da76c574d4a&quot; /&gt;
    /// &lt;/configSections&gt;
    /// </code>
    /// Add dataAccess section to the &lt;configuration&gt; section
    /// <code>
    /// &lt;dataAccess connectionString=&quot;a sql connection string&quot; /&gt;
    /// </code>
    /// or
    /// <code>
    /// &lt;dataAccess connectionStringName=&quot;a name of sql connection string&quot; /&gt;
    /// </code>
    /// </example>
    public class DataAccessConfig : ConfigurationSection
    {
        /// <summary>
        /// The name of the connection string.
        /// </summary>
        /// <value>
        /// The name of the connection string.
        /// </value>
        [ConfigurationProperty("connectionStringName", IsRequired = false)]
        public string ConnectionStringName
        {
            get
            {
                return (string)this["connectionStringName"];
            }
        }


        /// <summary>
        /// The connection string.
        /// </summary>
        [ConfigurationProperty("connectionString", IsRequired = false)]
        public string ConnectionString
        {
            get
            {
                return (string)this["connectionString"];
            }
        }

        /// <summary>
        /// Gets the default connetion string.
        /// </summary>
        public string DefaultConnetionString
        {
            get
            {
                ConnectionStringSettingsCollection connStrings = System.Configuration.ConfigurationManager.ConnectionStrings;

                DataAccessConfig customSection = System.Configuration.ConfigurationManager.GetSection("dataAccess") as DataAccessConfig;
                if (customSection != null)
                {
                    if (!string.IsNullOrEmpty(customSection.ConnectionStringName))
                        return connStrings[customSection.ConnectionStringName].ConnectionString;

                    if (!string.IsNullOrEmpty(customSection.ConnectionString))
                        return customSection.ConnectionString;
                }

                if (connStrings.Count > 0)
                    return connStrings[0].ConnectionString;

                return "";
            }
        }
    }
}