using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Python.Runtime;
using System.IO;
using System.Reflection;

namespace PythonNetTest
{
	/// <summary>
	/// This class tests various features of pythonnet to confirm that it
	/// is working as expected.
	/// </summary>
	[TestClass]
	public class TestLoadScript
	{

		private static string GetAssemblyDir()
		{
			string codeBase = Assembly.GetExecutingAssembly().CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}


		/// <summary>
		/// Get path to a script name.  Scripts are found
		/// in 'scripts' directory where the assembly is loaded.
		/// </summary>
		/// <param name="fileName">Name of the scriptfile</param>
		/// <returns>Path tot he script file.</returns>
		private static string GetScriptPath(string fileName)
		{
			return Path.Combine(GetAssemblyDir(), "scripts", fileName);
		}


		/// <summary>
		/// Select python environment.  Remove other python directoryies from 
		/// the path and add this path.  Set this as PYTHONHOME.
		/// Not well tested.  May not do everything desired.
		/// </summary>
		/// <param name="pythonHome"></param>
		private static void SelectPython(string pythonHome)
		{
			string path = System.Environment.GetEnvironmentVariable("PATH");
			List<string> pp = new List<string>(path.Split(Path.PathSeparator));
			int i = 0;
			while (i < pp.Count)
			{
				if (pp[i].ToUpper().Contains("PYTHON"))
					pp.RemoveAt(i);
				else
					++i;
			}
			pp.Insert(0, pythonHome);
			path = String.Join(Path.PathSeparator.ToString(), pp);
			System.Environment.SetEnvironmentVariable("PATH", path);
			System.Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome);
		}


		/// <summary>
		/// Add a directory to the PYTHONPATH by updating the environment variable.
		/// </summary>
		/// <param name="path"></param>
		public static void AddToPath(string path)
		{
			string pPath = System.Environment.GetEnvironmentVariable("PYTHONPATH");
			if (pPath != null)
				pPath += Path.PathSeparator + path;
			else
				pPath = path;
			System.Environment.SetEnvironmentVariable("PYTHONPATH", pPath);
		}



		[TestInitialize]
		public void Initialize()
		{
			//SelectPython("c:\\python27");
		}




		/// <summary>
		/// A simple class which we will pass to python to confirm it can call back to us.
		/// </summary>
		public class Logger
		{
			public string _lastMesssage = "";

			public Logger()
			{
			}
			public void Write(string msg)
			{
				_lastMesssage = msg;
			}
		}


		/// <summary>
		/// Load a module into a scope.
		/// </summary>
		/// <param name="name">Name to be assigned to the module</param>
		/// <param name="fileName">Name of the code file.</param>
		/// <param name="globals">Globals to be set before execution of the script.</param>
		/// <returns>PyScope with code loaded.</returns>
		private PyScope LoadModule(string name, string fileName, Dictionary<string, object> globals)
		{
			PyScope scope = null;
			using (Py.GIL())
			{
				// Create a new scope
				scope = Py.CreateScope(name);

				// Assign any globals.
				if (globals != null)
				{
					foreach (string gname in globals.Keys)
						scope.Set(gname, globals[gname]);
				}

				// Load python code, compile, then execute it in the scope.
				string scriptText = File.ReadAllText(fileName);
				PyObject code = PythonEngine.Compile(scriptText, fileName);
				dynamic r = scope.Execute(code);
			}
			return scope;
		}


		/// <summary>
		/// Test that module global variables can be set and then used.
		/// </summary>
		[TestMethod]
		public void TestAddToPath()
		{
			// Must add scripts directory so that modules can import the shared module.
			string pathAddition = Path.Combine(GetAssemblyDir(), "scripts");
			AddToPath(pathAddition);
			PythonEngine.Initialize();
			Assert.IsTrue(PythonEngine.PythonPath.Contains(pathAddition));
			PythonEngine.Shutdown();
		}


		/// <summary>
		/// Test that module global variables can be set and then used.
		/// </summary>
		[TestMethod]
		public void SetModuleGlobal()
		{
			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("setglobal.py");
			Logger logger = new Logger();

			Dictionary<string, object> globals = new Dictionary<string, object>
					{
						{ "Logger", logger}
					};

			using (Py.GIL())
			{
				// Load script.  Logger should get called during 
				// execution.  Test that we got execpted message.
				dynamic mod = LoadModule(Path.GetFileNameWithoutExtension(scriptfile), scriptfile, globals);
				Assert.AreEqual("This script got compiled", logger._lastMesssage);

				// Set global then call a function that references that global.
				mod.Count = 1234;
				mod.writeGlobalLogger();
				Assert.AreEqual("Count is 1234", logger._lastMesssage);
			}
			PythonEngine.Shutdown();
		}


		/// <summary>
		/// Test that a stack trace has the file name.
		/// </summary>
		[TestMethod]
		public void StrackTraceHasFileName()
		{
			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("throwserror.py");

			using (Py.GIL())
			{
				dynamic mod = LoadModule(Path.GetFileNameWithoutExtension(scriptfile), scriptfile, null);
				try
				{
					mod.invitationToError();
				}
				catch (PythonException e)
				{
					Assert.IsTrue(e.StackTrace.Contains("throwserror.py"));
				}
			}
			PythonEngine.Shutdown();
		}



		/// <summary>
		/// Test that same file can be loaded as two different modules.
		/// </summary>
		[TestMethod]
		public void TwiceLoadScript()
		{
			// Must add scripts directory so that modules can import the shared module.
			AddToPath(Path.Combine(GetAssemblyDir(), "scripts"));

			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("sharemodule.py");

			using (Py.GIL())
			{
				// Load script as two different modules.
				dynamic modA = LoadModule("moduleA", scriptfile, null);
				dynamic modB = LoadModule("moduleB", scriptfile, null);

				// Call a function in the script that sets a globla in that script.
				modA.setLGlobal("module A");
				modB.setLGlobal("module B");

				// Check that they retain values.  Mainly checking that module A's value
				// has not become "module B"
				dynamic mAValue = modA.getLGlobal();
				dynamic mBValue = modB.getLGlobal();
				Assert.AreEqual("module A", (string)mAValue);
				Assert.AreEqual("module B", (string)mBValue);

				// Both modules import 'sharedmodule'.
				// We can set and retrive a value from that moodule
				modA.setMGlobal("module A");
				dynamic v = modA.getMGlobal();
				Assert.AreEqual("module A", (string)v);

				// We can set sharedmodule global through module B
				// Now the value we get through module A should be that
				// set through module B
				modB.setMGlobal("module B");
				v = modA.getMGlobal();
				Assert.AreEqual("module B", (string)v);
			}
			PythonEngine.Shutdown();
		}



		/// <summary>
		/// Tests call back to .NET objects.
		/// </summary>
		[TestMethod]
		public void CallMultiplier()
		{
			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("callmultiplier.py");


			using (Py.GIL())
			{
				PyScope mod = LoadModule(Path.GetFileNameWithoutExtension(scriptfile), scriptfile, null);

				//
				// Shows how to make call directly using objects.
				//
				PyObject func = mod.Get("multiplyThese");
				dynamic r = func.Invoke(new PyObject[] { new PyFloat(3.0), new PyFloat(1.5) });

				Assert.AreEqual<double>(4.5, (double)r);
			}
			PythonEngine.Shutdown();
		}



		/// <summary>
		/// Test function calls with various parameters and returns
		/// </summary>
		[TestMethod]
		public void FunctionCalls()
		{
			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("functioncalls.py");


			using (Py.GIL())
			{
				dynamic mod = LoadModule(Path.GetFileNameWithoutExtension(scriptfile), scriptfile, null);


				dynamic rs = mod.returnString();
				Assert.AreEqual<string>("This is my return", (string)rs);

				dynamic ri = mod.returnSum(2, 2);
				Assert.AreEqual<int>(4, (int)ri);

				rs = mod.threeParameters("one", "two", 3);
				Assert.AreEqual<string>("Three parameters are: 'one', 'two', 3", (string)rs);

				dynamic ra = mod.returnStringList("a", "b", "c");
				Assert.AreEqual<int>(3, ra.Length());
				Assert.AreEqual<string>("a", (string)ra[0]);
				Assert.AreEqual<string>("b", (string)ra[1]);
				Assert.AreEqual<string>("c", (string)ra[2]);
			}
			PythonEngine.Shutdown();
		}



		/// <summary>
		/// Test function calls with various parameters and returns
		/// </summary>
		[TestMethod]
		[ExpectedException(typeof(PythonException), "The script had a syntax error")]
		public void TestSyntaxError()
		{
			PythonEngine.Initialize();
			string scriptfile = GetScriptPath("syntaxerror.py");


			using (Py.GIL())
			{
				dynamic mod = LoadModule(Path.GetFileNameWithoutExtension(scriptfile), scriptfile, null);
			}
			PythonEngine.Shutdown();
		}
	}
}
