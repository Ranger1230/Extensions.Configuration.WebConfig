using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Extensions.Configuration.WebConfig
{
	public static class ConfigFileConfigurationExtensions
	{
		/// <summary>
		/// Adds configuration values for a *.config file to the ConfigurationBuilder
		/// </summary>
		/// <param name="builder">Builder to add configuration values to</param>
		/// <param name="path">Path to *.config file</param>
		public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string path)
		{
			return builder.AddConfigFile(path, optional: false);
		}

		/// <summary>
		/// Adds configuration values for a *.config file to the ConfigurationBuilder
		/// </summary>
		/// <param name="builder">Builder to add configuration values to</param>
		/// <param name="path">Path to *.config file</param>
		/// <param name="optional">true if file is optional; false otherwise</param>
		/// <param name="parsers">Additional parsers to use to parse the config file</param>
		public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string path, bool optional, params IConfigurationParser[] parsers)
		{
			return builder.AddConfigFile(path, optional, false, parsers);
		}

		/// <summary>
		/// Adds configuration values for a *.config file to the ConfigurationBuilder
		/// </summary>
		/// <param name="builder">Builder to add configuration values to</param>
		/// <param name="path">Path to *.config file</param>
		/// <param name="optional">true if file is optional; false otherwise</param>
		/// <param name="reloadOnChange">Whether the configuration should be reloaded if the file changes.</param>
		/// <param name="parsers">Additional parsers to use to parse the config file</param>
		public static IConfigurationBuilder AddConfigFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange, params IConfigurationParser[] parsers)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}
			else if (string.IsNullOrEmpty(path))
			{
				throw new ArgumentException("Path for configuration cannot be null/empty.", nameof(path));
			}

			if (!optional && !File.Exists(path))
			{
				throw new FileNotFoundException($"Could not find configuration file. File: [{path}]", path);
			}

			var provider = new PhysicalFileProvider(new FileInfo(path).Directory.FullName);

			path = new FileInfo(path).Name;

			return builder.Add(new ConfigFileConfigurationSource(path, optional, parsers) { ReloadOnChange = reloadOnChange, FileProvider = provider });
		}
	}
}
