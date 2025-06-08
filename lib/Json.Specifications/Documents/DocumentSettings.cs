using System.Collections.Generic;

namespace DarkPatterns.Json.Documents;

public class DocumentSettings
{
	public DocumentSettings() { }
	public DocumentSettings(IEnumerable<object> settings)
	{
		SettingObjects = [.. settings];
	}

	public ICollection<object> SettingObjects { get; } = [];
}
