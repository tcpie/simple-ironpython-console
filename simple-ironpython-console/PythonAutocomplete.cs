// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Hosting.Shell;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.Threading;
using System.Reflection;

namespace IronPythonConsole
{
	/// <summary>
	/// Provides code completion for the Python Console window.
	/// </summary>
	public class PythonAutoComplete 
	{
		Microsoft.Scripting.Hosting.ScriptEngine engine;
		Microsoft.Scripting.Hosting.ScriptScope scope;

		bool excludeCallables;
		public bool ExcludeCallables { get { return excludeCallables; } set { excludeCallables = value; } }

		public PythonAutoComplete(Microsoft.Scripting.Hosting.ScriptEngine engine, Microsoft.Scripting.Hosting.ScriptScope scope)
		{
			this.engine = engine;
			this.scope = scope;
			this.excludeCallables = false;
		}

		/// <summary>
		/// Generates completion data for the specified text. The text should be everything before
		/// the dot character that triggered the completion. The text can contain the command line prompt
		/// '>>>' as this will be ignored.
		/// </summary>
		public AutocompleteInfo[] GenerateCompletionData(string line)
		{         
			List<AutocompleteInfo> items = new List<AutocompleteInfo>(); //DefaultCompletionData

			AutocompleteInput name = this.AnalyzeInput (line);
			// A very simple test of callables!
			if (this.excludeCallables && name.name.Contains (')')) {
				return items.ToArray ();
			}

			if (name.name == null) {
				return items.ToArray ();
			}

			// Console.WriteLine("name: " + name.name + " filter: " + name.filter);
			try
			{
				// Another possibility:
				//commandLine.ScriptScope.Engine.Runtime.IO.SetOutput(new System.IO.MemoryStream(), Encoding.UTF8);
				//object value = commandLine.ScriptScope.Engine.CreateScriptSourceFromString(name, SourceCodeKind.Expression).Execute(commandLine.ScriptScope);
				//IList<string> members = commandLine.ScriptScope.Engine.Operations.GetMemberNames(value);
				Type type = (name.name == string.Empty) ? null : this.TryGetType(name.name);
				// Use Reflection for everything except in-built Python types and COM pbjects. 
				if (type != null && type.Namespace != "IronPython.Runtime" && (type.Name != "__ComObject"))
				{
					return this.FromCLRType(type, name);
				}
				else
				{
					return this.FromPythonType(name);
				}
			}
			catch (ThreadAbortException tae)
			{
				if (tae.ExceptionState is Microsoft.Scripting.KeyboardInterruptException) Thread.ResetAbort();
			}
			catch
			{
				// Do nothing.
			}

			return items.ToArray();
		}

		protected Type TryGetType(string name)
		{
			string tryGetType = name + ".GetType()";
			object type = null;
			try
			{
				type = engine.CreateScriptSourceFromString(tryGetType, SourceCodeKind.Expression).Execute(this.scope);
			}
			catch (ThreadAbortException tae)
			{
				if (tae.ExceptionState is Microsoft.Scripting.KeyboardInterruptException) Thread.ResetAbort();
			}
			catch
			{
				// Do nothing.
			}

			return type as Type;
		}

		protected string GetCommonStarter(AutocompleteInfo[] infos)
		{
			if (infos.Length == 0)
				return string.Empty;

			if (infos.Length == 1)
				return infos [0].complete;

			string ret = string.Empty;

			for (int i = 0; i < infos[0].stub.Length; i++) {
				char c = infos [0].stub [i];
				bool failed = false;

				foreach (AutocompleteInfo aci in infos) {
					if (aci.stub.Length <= i)
						failed = true;

					if (aci.stub [i] != c)
						failed = true;
				}

				if (failed)
					break;

				ret += c;
			}

			return ret;
		}

		protected AutocompleteInfo[] FromPythonType(AutocompleteInput input)
		{
			List<AutocompleteInfo> ret = new List<AutocompleteInfo>();

			string dirCommand = "dir(" + input.name + ")";
			object value = engine.CreateScriptSourceFromString(dirCommand, SourceCodeKind.Expression).Execute(this.scope);

			foreach (object member in (value as IronPython.Runtime.List))
			{
				string member_str = (string)member;

				if (input.have_filter && !member_str.StartsWith (input.filter))
					continue;

				AutocompleteInfo temp = new AutocompleteInfo();
				temp.name = input.name;
				temp.stub = member_str;
				temp.complete = member_str.Substring (input.filter.Length);

				ret.Add(temp);
			}

			if (ret.Count != 0 && ret.Count != 1) {
				string common_starter = this.GetCommonStarter (ret.ToArray ());
				AutocompleteInfo temp = ret [0];
				temp.common_starter = common_starter;

				temp.common_starter_complete = common_starter.Substring (input.filter.Length);

				ret [0] = temp;
			}

			return ret.ToArray();
		}

		protected AutocompleteInfo[] FromCLRType(Type type, AutocompleteInput input)
		{
			List<AutocompleteInfo> ret = new List<AutocompleteInfo>();

			List<string> completionsList = new List<string> ();
			MethodInfo[] methodInfo = type.GetMethods ();
			PropertyInfo[] propertyInfo = type.GetProperties ();
			FieldInfo[] fieldInfo = type.GetFields ();
			foreach (MethodInfo methodInfoItem in methodInfo) {
				if ((methodInfoItem.IsPublic)
					&& (methodInfoItem.Name.IndexOf ("get_") != 0) && (methodInfoItem.Name.IndexOf ("set_") != 0)
					&& (methodInfoItem.Name.IndexOf ("add_") != 0) && (methodInfoItem.Name.IndexOf ("remove_") != 0)
					&& (methodInfoItem.Name.IndexOf ("__") != 0))
					completionsList.Add (methodInfoItem.Name);
			}
			foreach (PropertyInfo propertyInfoItem in propertyInfo) {
				completionsList.Add (propertyInfoItem.Name);
			}
			foreach (FieldInfo fieldInfoItem in fieldInfo) {
				completionsList.Add (fieldInfoItem.Name);
			}
			completionsList.Sort ();
			string last = "";
			for (int i = completionsList.Count - 1; i > 0; --i) {
				if (completionsList [i] == last)
					completionsList.RemoveAt (i);
				else
					last = completionsList [i];
			}


			foreach (string completion in completionsList) {
				if (input.have_filter && !completion.StartsWith (input.filter))
					continue;

				AutocompleteInfo temp = new AutocompleteInfo();
				temp.name = input.name;
				temp.stub = completion;
				temp.complete = completion.Substring (input.filter.Length);
				ret.Add (temp);
			}

			if (ret.Count != 0 && ret.Count != 1) {
				string common_starter = this.GetCommonStarter (ret.ToArray ());
				AutocompleteInfo temp = ret [0];
				temp.common_starter = common_starter;

				temp.common_starter_complete = common_starter.Substring (input.filter.Length);

				ret [0] = temp;
			}

			return ret.ToArray ();
		}

		AutocompleteInput AnalyzeInput(string input)
		{
			AutocompleteInput ret = new AutocompleteInput ();

			string text = input.Replace("\t", "   ");
			int startIndex = text.LastIndexOf(' ');
			int end_index = input.LastIndexOf ('.');

			// Console.WriteLine ("startIndex: " + startIndex.ToString () + " end index: " + end_index.ToString ());

			if (end_index == -1) {
				ret.name = string.Empty;
				ret.filter = input.Substring(startIndex + 1);
				ret.have_filter = true;
			} else {
				ret.name = text.Substring (startIndex + 1, (end_index) - (startIndex + 1));

				ret.raw_input = input;
				ret.filter = input.Substring (end_index + 1);
				ret.have_filter = true;

				if (ret.filter == string.Empty) {
					ret.have_filter = false;
				}
			}
			
			return ret;
		}
	}
}


