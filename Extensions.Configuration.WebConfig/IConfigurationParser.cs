using System.Collections.Generic;
using System.Xml.Linq;

namespace Extensions.Configuration.WebConfig
{
	public interface IConfigurationParser
	{
		bool CanParseElement(XElement element);
		bool ParseElement(XElement element, Stack<string> context, SortedDictionary<string, string> results);
	}
}
