using System;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;

namespace FluentNHibernate.Cfg
{
    /// <summary>
    /// Fluent configuration API for NHibernate
    /// </summary>
    public class FluentConfiguration
    {
        private const string ExceptionMessage = "An invalid or incomplete configuration was used while creating a SessionFactory. Check PotentialReasons collection, and InnerException for more detail.";
        private const string ExceptionDatabaseMessage = "Database was not configured through Database method.";
        private const string ExceptionMappingMessage = "No mappings were configured through the Mappings method.";

        private readonly Configuration cfg;
        private readonly MappingConfiguration mappingCfg;
        private bool dbSet;
        private bool mappingsSet;
        private Action<Configuration> configAlteration;

        internal FluentConfiguration()
        {
            cfg = new Configuration();
            mappingCfg = new MappingConfiguration();
        }

        /// <summary>
        /// Apply database settings
        /// </summary>
        /// <param name="config">Lambda returning database configuration</param>
        /// <returns>Fluent configuration</returns>
        public FluentConfiguration Database(Func<IPersistenceConfigurer> config)
        {
            return Database(config());
        }

        /// <summary>
        /// Apply database settings
        /// </summary>
        /// <param name="config">Database configuration instance</param>
        /// <returns>Fluent configuration</returns>
        public FluentConfiguration Database(IPersistenceConfigurer config)
        {
            config.ConfigureProperties(cfg);
            dbSet = true;
            return this;
        }

        /// <summary>
        /// Apply mappings to NHibernate
        /// </summary>
        /// <param name="mappings">Lambda used to apply mappings</param>
        /// <returns>Fluent configuration</returns>
        public FluentConfiguration Mappings(Action<MappingConfiguration> mappings)
        {
            mappings(mappingCfg);
            mappingsSet = mappingCfg.WasUsed;
            return this;
        }

        /// <summary>
        /// Allows altering of the raw NHibernate Configuration object before creation
        /// </summary>
        /// <param name="config">Lambda used to alter Configuration</param>
        /// <returns>Fluent configuration</returns>
        public FluentConfiguration ExposeConfiguration(Action<Configuration> config)
        {
            configAlteration = config;
            return this;
        }

        /// <summary>
        /// Verify's the configuration and instructs NHibernate to build a SessionFactory.
        /// </summary>
        /// <returns>ISessionFactory from supplied settings.</returns>
        public ISessionFactory BuildSessionFactory()
        {
            try
            {
                mappingCfg.Apply(cfg);

                if (configAlteration != null)
                    configAlteration(cfg);

                return cfg.BuildSessionFactory();
            }
            catch (Exception ex)
            {
                throw CreateConfigurationException(ex);
            }
        }

        /// <summary>
        /// Creates an exception based on the current state of the configuration.
        /// </summary>
        /// <param name="innerException">Inner exception</param>
        /// <returns>FluentConfigurationException with state</returns>
        private FluentConfigurationException CreateConfigurationException(Exception innerException)
        {
            var ex = new FluentConfigurationException(ExceptionMessage, innerException);

            if (!dbSet)
                ex.PotentialReasons.Add(ExceptionDatabaseMessage);
            if (!mappingsSet)
                ex.PotentialReasons.Add(ExceptionMappingMessage);

            return ex;
        }
    }
}