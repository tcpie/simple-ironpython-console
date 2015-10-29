using System;

using System.IO;

using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Collections.Generic;

namespace IronPythonConsole
{
	class IPConsole
	{
		private ScriptEngine engine;
		private ScriptScope scope;

		private string script;
		private string old_code;
		private List<string> old_entries;
		private int old_entry_num;
		private int line;

		private string cmd_prefix;
        private string new_cmd_line_prefix;
        private string identation_indicator;

		private PythonAutoComplete autocompleter;
        
		public IPConsole()
		{
            script = "";
			old_code = "";
			old_entries = new List<string>();
			old_entry_num = 0;
			line = 1;
			cmd_prefix = ">>> ";
            new_cmd_line_prefix = "....";
            identation_indicator = "....";

			engine = Python.CreateEngine ();
			scope = engine.CreateScope ();

			var coll = engine.GetSearchPaths ();

			coll.Add (".");
			coll.Add("./ipython-lib");
			coll.Add("./ipython-dlls");

			engine.SetSearchPaths(coll);
            
			this.autocompleter = new PythonAutoComplete (this.engine, this.scope);
		}

		private void handle_backspace()
		{
			if (script.Length == 0)
				return;

			script = script.Remove(script.Length - 1);
			Console.Write("\b \b");
		}

		private void handle_up_or_downarrow()
		{
			foreach (char c in script)
			{
				Console.Write("\b");
			}

			if (old_entries.Count + old_entry_num >= old_entries.Count || old_entries.Count + old_entry_num < 0)
			{
				old_entry_num = 0;
				script = "";
			} else {
				script = old_entries[old_entries.Count + old_entry_num];
				Console.Write(script);
			}
		}

		private void handle_uparrow()
		{
			old_entry_num--;

			handle_up_or_downarrow ();
		}

		private void handle_downarrow()
		{
			old_entry_num++;

			handle_up_or_downarrow ();
		}

		private void handle_enter()
		{
			old_entry_num = 0;

			Console.Write("\n");
			old_code += script + "\n";
			line++;

			old_entries.Add(script);
			ScriptSource source = engine.CreateScriptSourceFromString (script);

			try
			{
				CompiledCode code = source.Compile ();
				code.Execute (scope);
			}
			catch (System.Exception e)
			{
				Console.WriteLine(e.GetType().Name + ": " + e.Message);
			}

			script = "";
			Console.Write(this.cmd_prefix);
		}

		private void handle_tab()
		{
			try {
				AutocompleteInfo[] ret = this.autocompleter.GenerateCompletionData(script);

				if (ret.Length == 0)
					return;

				if (ret.Length == 1)
				{
					Console.Write(ret[0].complete);
					script += ret[0].complete;
				} else {
					Console.Write("\n");
					foreach (AutocompleteInfo aci in ret)
					{
						Console.WriteLine(aci.stub);
					}

					Console.Write(this.cmd_prefix + script + ret[0].common_starter_complete);
					script += ret[0].common_starter_complete;
				}
			}
			catch (System.Exception e)
			{
				Console.WriteLine(e.GetType().Name + ": " + e.Message);
			}
		}

		public void main_loop()
		{
			Console.WriteLine ("Please type your python code:");

			ConsoleKeyInfo keyinfo;

			Console.Write (this.cmd_prefix);

			do
			{
				keyinfo = Console.ReadKey(true);
                
				if (keyinfo.Key == ConsoleKey.Backspace)
				{
					this.handle_backspace();
				} 
				else if (keyinfo.Key == ConsoleKey.UpArrow)
				{
					this.handle_uparrow();
				}
				else if (keyinfo.Key == ConsoleKey.DownArrow)
				{
					this.handle_downarrow();
				}
				else if (keyinfo.Key == ConsoleKey.Enter && (keyinfo.Modifiers & ConsoleModifiers.Shift) == 0)
				{
					this.handle_enter();
				} 
				else if (keyinfo.Key == ConsoleKey.Tab && (keyinfo.Modifiers & ConsoleModifiers.Shift) == 0)
				{
					this.handle_tab();
				} 
                else if (keyinfo.Key == ConsoleKey.Enter)
                {
                    Console.Write(Environment.NewLine + this.new_cmd_line_prefix);
                    script += "\n";
                }
				else if (keyinfo.Key == ConsoleKey.Tab)
				{
                    string script_trim_res = script.Substring(script.LastIndexOf('\n') + 1).Trim();
                    if (script_trim_res == string.Empty)
                        Console.Write(this.identation_indicator);
                    else
                        Console.Write("    ");
                    
                    script += "    ";
                }
                else if (keyinfo.KeyChar == '\0')
                {
                    continue;
                }
                else
                {
                    Console.Write(keyinfo.KeyChar);
                    script += keyinfo.KeyChar;
                }
			}
			while (keyinfo.Key != ConsoleKey.Escape);
		}
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			IPConsole cs = new IPConsole ();

			cs.main_loop ();
		}
	}
}
