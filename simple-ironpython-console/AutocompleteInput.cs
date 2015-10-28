using System;

namespace IronPythonConsole
{
	public struct AutocompleteInput
	{
		public string raw_input;
		public string name;
		public string filter;

		public bool have_filter;
	}
}

