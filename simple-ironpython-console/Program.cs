using System;

using System.IO;

using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace monotest
{
	class key_test
	{

	}

	class MainClass
	{
		public static void Main (string[] args)
		{

			Console.WriteLine ("Please type your python code:");

			ScriptEngine engine = Python.CreateEngine ();
			var coll = engine.GetSearchPaths ();
			ScriptScope scope = engine.CreateScope ();

			coll.Add (".");
			coll.Add("./ipython-lib");
			coll.Add("./ipython-dlls");

			engine.SetSearchPaths(coll);

			try
			{
				scope.ImportModule("jedi");
			}
			catch (System.Exception e)
			{
				Console.WriteLine(e.GetType().Name + ": " + e.Message);
			}

			ScriptScope python_jedi = scope.GetVariable ("jedi");

			var jedi_script = python_jedi.GetVariable ("Script");

			ConsoleKeyInfo keyinfo;

			string script = "";
			string old_code = "";
			int line = 1;
			int column = 0;

			Console.Write (">> ");
			do
			{
				keyinfo = Console.ReadKey(true);

				if (keyinfo.Key == ConsoleKey.Backspace)
				{
					if (script.Length == 0)
						continue;

					script = script.Remove(script.Length - 1);
					Console.Write("\b");

					continue;
				}

				Console.Write(keyinfo.KeyChar);

				script += keyinfo.KeyChar;
				column++;

				if (keyinfo.Key == ConsoleKey.Enter)
				{
					old_code += script;
					line++;
					column = 0;

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
					Console.Write(">> ");
				}

				// Console.Write(script);
			}
			while (keyinfo.Key != ConsoleKey.Escape);
		}
	}
}
