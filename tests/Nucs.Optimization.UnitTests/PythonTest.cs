using System;
using System.Runtime.CompilerServices;
using Python.Runtime;
using Xunit;

namespace Nucs.Essentials.UnitTests;

public abstract class PythonTest {
    [MethodImpl(MethodImplOptions.Synchronized)]
    protected PythonTest() {
        if (PythonEngine.IsInitialized) return;
        Runtime.PythonDLL = Environment.ExpandEnvironmentVariables("%APPDATA%\\..\\Local\\Programs\\Python\\Python38\\python38.dll");
        PythonEngine.Initialize();
        PythonEngine.BeginAllowThreads();

    }

}