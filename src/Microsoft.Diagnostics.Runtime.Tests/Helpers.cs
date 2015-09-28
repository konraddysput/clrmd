﻿
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Diagnostics.Runtime.Utilities;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime.Interop;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    static class Helpers
    {
        public static IEnumerable<T> Unique<T>(this IEnumerable<T> self)
        {
            HashSet<T> set = new HashSet<T>();
            foreach (T t in self)
                set.Add(t);

            return set;
        }

        public static ClrAppDomain GetDomainByName(this ClrRuntime runtime, string domainName)
        {
            return runtime.AppDomains.Where(ad => ad.Name == domainName).Single();
        }

        public static ClrModule GetModule(this ClrRuntime runtime, string filename)
        {
            return (from module in runtime.EnumerateModules()
                    let file = Path.GetFileName(module.FileName)
                    where file.Equals(filename, StringComparison.OrdinalIgnoreCase)
                    select module).Single();
        }


        public static ClrThread GetMainThread(this ClrRuntime runtime)
        {
            ClrThread thread = runtime.Threads.Where(t => !t.IsFinalizer).Single();
            return thread;
        }

        public static ClrStackFrame GetFrame(this ClrThread thread, string functionName)
        {
            return thread.StackTrace.Where(sf => sf.Method != null ? sf.Method.Name == functionName : false).Single();
        }

        public static string TestWorkingDirectory { get { return _workingPath.Value; } }

        #region Working Path Helpers
        static Lazy<string> _workingPath = new Lazy<string>(() => CreateWorkingPath(), true);
        private static string CreateWorkingPath()
        {
            Random r = new Random();
            string path;
            do
            {
                path = Path.Combine(Environment.CurrentDirectory, TempRoot + r.Next().ToString());
            } while (Directory.Exists(path));

            Directory.CreateDirectory(path);
            return path;
        }


        internal static readonly string TempRoot = "clrmd_removeme_";
        #endregion
    }

    [TestClass]
    public class GlobalCleanup
    {

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (string directory in Directory.GetDirectories(Environment.CurrentDirectory))
                if (directory.Contains(Helpers.TempRoot))
                    Directory.Delete(directory, true);
        }
    }
}