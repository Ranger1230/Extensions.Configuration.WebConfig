using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Extensions.Configuration.WebConfig
{
	public class ConfigFileConfigurationSource : FileConfigurationSource
	{
		internal ILogger Logger { get; set; }
		internal IEnumerable<IConfigurationParser> Parsers { get; set; }

		internal ConfigFileConfigurationSource(string path, bool optional, params IConfigurationParser[] parsers)
			: this(path, optional, null, parsers)
		{ }

		internal ConfigFileConfigurationSource(string path, bool optional, ILogger logger, params IConfigurationParser[] parsers)
		{
			Path = path;
			Optional = optional;
			Logger = logger;

			var parsersToUse = new List<IConfigurationParser> {
				new KeyValueParser(),
				new KeyValueParser("name", "connectionString")
			};

			parsersToUse.AddRange(parsers);

			Parsers = parsersToUse.ToArray();
		}

		public override IConfigurationProvider Build(IConfigurationBuilder builder)
		{
			return new ConfigFileConfigurationProvider(this);
		}
	}
}
