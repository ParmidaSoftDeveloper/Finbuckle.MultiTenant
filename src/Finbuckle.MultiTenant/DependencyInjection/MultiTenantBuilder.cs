// Copyright Finbuckle LLC, Andrew White, and Contributors.
// Refer to the solution LICENSE file for more inforation.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class FinbuckleMultiTenantBuilder<TTenantInfo> where TTenantInfo : class, ITenantInfo, new()
    {
        public IServiceCollection Services { get; set; }

        public FinbuckleMultiTenantBuilder(IServiceCollection services)
        {
            Services = services;
        }

        /// <summary>
        /// Adds per-tenant configuration for an options class.
        /// </summary>
        /// <param name="tenantConfigureOptions">The configuration action to be run for each tenant.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        /// <remarks>This is similar to `ConfigureAll` in that it applies to all named and unnamed options of the type.</remarks>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithPerTenantOptions<TOptions>(
            Action<TOptions, TTenantInfo> tenantConfigureOptions) where TOptions : class, new()
        {
            // if (tenantConfigureOptions == null)
            // {
            //     throw new ArgumentNullException(nameof(tenantConfigureOptions));
            // }
            //
            // // Handles multiplexing cached options.
            // Services.TryAddSingleton<IOptionsMonitorCache<TOptions>, MultiTenantOptionsCache<TOptions, TTenantInfo>>();
            //
            // // Necessary to apply tenant options in between configuration and postconfiguration
            // Services
            //     .AddSingleton<ITenantConfigureOptions<TOptions, TTenantInfo>,
            //         TenantConfigureOptions<TOptions, TTenantInfo>>(sp =>
            //         new TenantConfigureOptions<TOptions, TTenantInfo>(tenantConfigureOptions));
            // Services.TryAddTransient<IOptionsFactory<TOptions>, MultiTenantOptionsFactory<TOptions, TTenantInfo>>();
            // Services.TryAddScoped<IOptionsSnapshot<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));
            // Services.TryAddSingleton<IOptions<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            return WithPerTenantNamedOptions(null, tenantConfigureOptions);
        }

        /// <summary>
        /// Adds per-tenant configuration for an named options class.
        /// </summary>
        /// <param name="name">The option name.</param>
        /// <param name="tenantConfigureNamedOptions">The configuration action to be run for each tenant.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithPerTenantNamedOptions<TOptions>(string? name,
            Action<TOptions, TTenantInfo> tenantConfigureNamedOptions) where TOptions : class, new()
        {
            if (tenantConfigureNamedOptions == null)
            {
                throw new ArgumentNullException(nameof(tenantConfigureNamedOptions));
            }

            // Handles multiplexing cached options.
            Services.TryAddSingleton<IOptionsMonitorCache<TOptions>, MultiTenantOptionsCache<TOptions, TTenantInfo>>();

            // Necessary to apply tenant named options in between configuration and post configuration
            Services.AddSingleton<ITenantConfigureNamedOptions<TOptions, TTenantInfo>,
                TenantConfigureNamedOptions<TOptions, TTenantInfo>>(sp => new TenantConfigureNamedOptions<TOptions,
                TTenantInfo>(name, tenantConfigureNamedOptions));
            Services.TryAddTransient<IOptionsFactory<TOptions>, MultiTenantOptionsFactory<TOptions, TTenantInfo>>();
            Services.TryAddScoped<IOptionsSnapshot<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));
            Services.TryAddSingleton<IOptions<TOptions>>(sp => BuildOptionsManager<TOptions>(sp));

            return this;
        }

        // TODO consider per tenant AllOptions variation

        private static MultiTenantOptionsManager<TOptions> BuildOptionsManager<TOptions>(IServiceProvider sp)
            where TOptions : class, new()
        {
            var cache = (IOptionsMonitorCache<TOptions>)ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsCache<TOptions, TTenantInfo>));
            return (MultiTenantOptionsManager<TOptions>)
                ActivatorUtilities.CreateInstance(sp, typeof(MultiTenantOptionsManager<TOptions>), cache);
        }

        /// <summary>
        /// Adds and configures a IMultiTenantStore to the application using default dependency injection.
        /// </summary>>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="parameters">a parameter list for any constructor parameters not covered by dependency injection.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithStore<TStore>(ServiceLifetime lifetime,
            params object[] parameters)
            where TStore : IMultiTenantStore<TTenantInfo>
            => WithStore<TStore>(lifetime, sp => ActivatorUtilities.CreateInstance<TStore>(sp, parameters));

        /// <summary>
        /// Adds and configures a IMultiTenantStore to the application using a factory method.
        /// </summary>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="factory">A delegate that will create and configure the store.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithStore<TStore>(ServiceLifetime lifetime,
            Func<IServiceProvider, TStore> factory)
            where TStore : IMultiTenantStore<TTenantInfo>
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // Note: can't use TryAddEnumerable here because ServiceDescriptor.Describe with a factory can't set implementation type.
            Services.Add(
                ServiceDescriptor.Describe(typeof(IMultiTenantStore<TTenantInfo>), sp => factory(sp), lifetime));

            return this;
        }

        /// <summary>
        /// Adds and configures a IMultiTenantStrategy to the application using default dependency injection.
        /// </summary>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="parameters">a parameter list for any constructor parameters not covered by dependency injection.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithStrategy<TStrategy>(ServiceLifetime lifetime,
            params object[] parameters) where TStrategy : IMultiTenantStrategy
            => WithStrategy(lifetime, sp => ActivatorUtilities.CreateInstance<TStrategy>(sp, parameters));

        /// <summary>
        /// Adds and configures a IMultiTenantStrategy to the application using a factory method.
        /// </summary>
        /// <param name="lifetime">The service lifetime.</param>
        /// <param name="factory">A delegate that will create and configure the strategy.</param>
        /// <returns>The same MultiTenantBuilder passed into the method.</returns>
        public FinbuckleMultiTenantBuilder<TTenantInfo> WithStrategy<TStrategy>(ServiceLifetime lifetime,
            Func<IServiceProvider, TStrategy> factory)
            where TStrategy : IMultiTenantStrategy
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            // Potential for multiple entries per service is intended.
            Services.Add(ServiceDescriptor.Describe(typeof(IMultiTenantStrategy), sp => factory(sp), lifetime));

            return this;
        }
    }
}