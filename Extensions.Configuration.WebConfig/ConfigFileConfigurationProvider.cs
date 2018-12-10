using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Extensions.Configuration.WebConfig
{
	public class ConfigFileConfigurationProvider : FileConfigurationProvider
	{
		private readonly ILogger _logger;

		private readonly IEnumerable<IConfigurationParser> _parsers;

		internal ConfigFileConfigurationProvider(ConfigFileConfigurationSource source): base(source)
		{
			_logger = source.Logger;
			_parsers = source.Parsers;
		}

		public override void Load(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			{
				string configFile = reader.ReadToEnd();
				var document = XDocument.Parse(configFile);

				var context = new Stack<string>();
				var dictionary = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				ParseDocument(document.Root, context, dictionary);

				Data = dictionary;
			}
			
		}

		private void ParseDocument(XElement document, Stack<string> context, SortedDictionary<string, string> dictionary)
		{
			foreach (XElement child in document.Elements())
			{
				if (child.Name.ToString().Equals("location", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!ShouldParseLocation(child))
					{
						continue;
					}
				}
				if (child.DescendantsAndSelf().Any(x => x.Attribute("configSource") != null))
				{
					ParseConfigSource(child, context, dictionary);
				}
				ParseElement(child, context, dictionary);
			}
		}

		/// <summary>
		/// Given a location element tries to see if the configs with in it should be loaded.
		/// Because ASP.NET Core apps don't run in IIS, they best we can do is for return true
		/// when child applications are true and the path attribute is for all paths.
		/// </summary>
		/// <param name="element">the location element</param>
		/// <returns></returns>
		private bool ShouldParseLocation(XElement element)
		{
			string currentDirectory = Directory.GetCurrentDirectory();
			string configFilesRoot = ((PhysicalFileProvider)Source.FileProvider).Root;
			bool isChildApp = !currentDirectory.Equals(configFilesRoot, StringComparison.CurrentCultureIgnoreCase);
			return element.Attribute("path")?.Value == "." && (element.Attribute("inheritInChildApplications")?.Value == "true" || !isChildApp);
		}

		/// <summary>
		/// If the given XElement has a configSource attribute, it will attempt to load in the file specified in the configSource.
		/// </summary>
		/// <param name="element">The element containing the configSource attribute.</param>
		/// <param name="context">The stack of element contexts (AppSettings, ConnectionStrings, etc...)</param>
		/// <param name="dictionary">The KeyValue pair for all the settings currently loaded.</param>
		private void ParseConfigSource(XElement element, Stack<string> context, SortedDictionary<string, string> dictionary)
		{
			foreach (XElement child in element.Elements().Where(x => x.Attribute("configSource") != null))
			{
				string configSource = child.Attribute("configSource").Value;

				string configSourcePath = Source.FileProvider.GetFileInfo(configSource).PhysicalPath;
				if (configSourcePath == null)
				{
					throw new FileNotFoundException($"Could not find configuration file to load at [{configSource}].", configSource);
				}

				var document = XDocument.Load(configSourcePath);

				ParseElement(document.Root, context, dictionary);
			}
		}

		/// <summary>
		/// Given an XElement tries to parse that element using any of the KeyValueParsers
		/// and adds it to the results dictionary
		/// </summary>
		private void ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
		{
			bool parsed = false;
			foreach (IConfigurationParser parser in _parsers)
			{
				if (parser.CanParseElement(element))
				{
					parsed = true;
					parser.ParseElement(element, context, results);
					break;
				}
			}

			if (!parsed && _logger != null)
			{
				_logger.LogWarning($"None of the parsers could parse [{element.ToString()}]!");
			}
		}
	}
}
