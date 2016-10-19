using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ReferenceDiagnostics
{
    //http://www.martinwilley.com/net/code/reflection/createdomain.html

    [Serializable]
    public class Loader :  MarshalByRefObject
    {
        private static readonly Output Out = Output.Current;

        public bool RunInNewDomain(string assemblyPath, bool verbose)
        {
            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("The assembly could not been found.", assemblyPath);

            var processedAssemblies = new HashSet<string>();
            var assemblies = new Queue<string>();
            assemblies.Enqueue(assemblyPath);

            Console.WriteLine("Start searching for references for assembly " + Path.GetFileName(assemblyPath) + "...");

            var allSuccess = true;

            while (assemblies.Count > 0)
            {
                assemblyPath = assemblies.Dequeue();

                var domainInfo = new AppDomainSetup
                {
                    ApplicationBase = Path.GetDirectoryName(assemblyPath),
                    ApplicationName = Path.GetFileNameWithoutExtension(assemblyPath)
                };
                var domain = AppDomain.CreateDomain(domainInfo.ApplicationName, null, domainInfo);

                try
                {
                    Out.WriteAction("Processing " + Path.GetFileName(assemblyPath));

                    var otherLoader = (Loader)domain.CreateInstanceFromAndUnwrap(typeof(Program).Assembly.Location, typeof(Loader).FullName);
                    var childLoad = otherLoader.LoadChilds(assemblyPath, verbose);

                    Out.WriteStatus(childLoad.Item2);

                    allSuccess = allSuccess & childLoad.Item2;
                    foreach (var a in childLoad.Item1)
                    {
                        if (processedAssemblies.Contains(a))
                            continue;

                        assemblies.Enqueue(a);
                        processedAssemblies.Add(a);
                    }
                }
                finally
                {
                    AppDomain.Unload(domain);
                }
            }

            return allSuccess;
        }

        private Tuple<string[], bool> LoadChilds(string assemblyPath, bool verbose)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            if (verbose)
                WriteLine(assembly.FullName, ConsoleColor.Yellow);

            var names = assembly.GetReferencedAssemblies();

            var childs = new Collection<string>();
            bool allSuccess = true;

            foreach (var name in names)
            {
                // Skip basic .NET library
                if (name.Name.StartsWith("mscorlib"))
                    continue;
                // Skip the System assemblies
                if (name.Name.StartsWith("System"))
                    continue;
                // Skip WPF
                if (name.GetPublicKeyToken().SequenceEqual(new byte[] { 0x31, 0xbf, 0x38, 0x56, 0xad, 0x36, 0x4e, 0x35 }))
                    continue;
                // Skip some extensability libraries
                if (name.GetPublicKeyToken().SequenceEqual(new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }))
                    continue;

                Assembly child;
                try
                {
                    child = AppDomain.CurrentDomain.Load(name.FullName, assembly.Evidence);

                    if (verbose)
                    {
                        Out.WriteAction("Testing " + name.FullName);
                        Out.WriteStatus(true);

                        //WriteLine(" => OK " + (child.GlobalAssemblyCache ? "[GAC] " : string.Empty) + name.FullName, ConsoleColor.Green);
                    }

                    childs.Add(child.Location);
                }
                catch (FileNotFoundException)
                {
                    allSuccess = false;

                    Out.WriteAction("Testing " + name.FullName);
                    Out.WriteStatus(false);

                    //WriteLine(" => FAILED " + name.FullName, ConsoleColor.Red);
                }
                catch (Exception ex)
                {
                    allSuccess = false;

                    Out.WriteAction("Testing " + name.FullName);
                    Out.WriteEntry(ex);
                    Out.WriteStatus(false);

                    //WriteLine(" => FAILED " + name.FullName, ConsoleColor.Red);
                    //WriteLine("    " + ex.Message, ConsoleColor.Red);
                }
            }

            return new Tuple<string[], bool>(childs.ToArray(), allSuccess);
        }


        public static void WriteLine(string text, ConsoleColor color)
        {
            var o = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = o;
        }
    }
}
