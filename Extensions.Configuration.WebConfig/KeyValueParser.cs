using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Extensions.Configuration.WebConfig
{
	public class KeyValueParser : IConfigurationParser
	{
		private readonly ILogger _logger;
		private readonly string _keyName = "key";
		private readonly string _valueName = "value";
		private readonly string[] _supportedActions = Enum.GetNames(typeof(ConfigurationAction)).Select(x => x.ToLowerInvariant()).ToArray();

		public KeyValueParser()
			: this("key", "value")
		{ }

		/// <summary>
		/// The key/value attribute names.
		/// </summary>
		public KeyValueParser(string key, string value)
			: this(key, value, null)
		{ }

		public KeyValueParser(string key, string value, ILogger logger)
		{
			_keyName = key;
			_valueName = value;
			_logger = logger;
		}

		public bool CanParseElement(XElement element)
		{
			var hasKeyAttribute = element.DescendantsAndSelf().Any(x => x.Attribute(_keyName) != null);

			return hasKeyAttribute;
		}

		public bool ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results)
		{
			if (!CanParseElement(element))
			{
				return false;
			}

			if (!element.Elements().Any())
			{
				AddToDictionary(element, context, results);
			}

			context.Push(element.Name.ToString());

			bool couldParseAll = true;

			foreach (XElement node in element.Elements())
			{
				var hasSupportedAction = node.DescendantsAndSelf().Any(x => _supportedActions.Contains(x.Name.ToString().ToLowerInvariant()));

				if (!hasSupportedAction)
				{
					if (_logger != null)
					{
						_logger.LogWarning($"Contains an unsupported config element. [{node.ToString()}]");
					}

					continue;
				}

				ParseElement(node, context, results);
			}

			context.Pop();

			return couldParseAll;
		}

		private void AddToDictionary(XElement element, Stack<string> context, SortedDictionary<string, string> results)
		{

			if (!Enum.TryParse(element.Name.ToString(), true, out ConfigurationAction action))
			{
				if (_logger != null)
				{
					_logger.LogInformation($"Element with an unsupported action. [{element.ToString()}]");
				}

				return;
			}

			XAttribute key = element.Attribute(_keyName);
			XAttribute value = element.Attribute(_valueName);

			if (key == null)
			{
				if (_logger != null)
				{
					_logger.LogInformation($"[{element.ToString()}] is not supported because it does not have an attribute with {_keyName}");
				}

				return;
			}

			string fullkey = GetKey(context, key.Value);

			switch (action)
			{
				case ConfigurationAction.Add:
					string valueToAdd = null;

					if (value == null)
					{
						if (_logger != null)
						{
							_logger.LogWarning($"Could not parse the value attribute [{_valueName}] from [{element.ToString()}]. Using null as value...");
						}
						return;
					}
					else
					{
						valueToAdd = value.Value;
					}

					if (results.ContainsKey(fullkey))
					{
						if (_logger != null)
						{
							_logger.LogWarning($"{fullkey} exists. Replacing existing value [{results[fullkey]}] with {valueToAdd}");
						}

						results[fullkey] = valueToAdd;
					}
					else
					{
						results.Add(fullkey, valueToAdd);
					}
					break;
				case ConfigurationAction.Remove:
					results.Remove(fullkey);
					break;
				default:
					throw new NotSupportedException($"Unsupported action: [{action}]");
			}
		}

		private static string GetKey(Stack<string> context, string name)
		{
			return string.Join(ConfigurationPath.KeyDelimiter, context.Reverse().Concat(new[] { name }));
		}
	}
}
