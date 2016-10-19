using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace ReferenceDiagnostics
{
    [Serializable]
    class Program
    {
        // http://stackoverflow.com/questions/4298913/static-fields-in-appdomain
        static void Main(string[] args)
        {
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.EnableVisualStyles();
            //Application.Run(new MainForm(@"C:\Users\310169965\Documents\Visual Studio 2010\tfs\GeneralRepository\Dev\ReferenceDiagnostics\TestApplication\bin\Debug\TestApplication.exe"));

            //return;

            bool verbose = false;
            if (args.Length <= 0)
            {
                Console.WriteLine("Please specify the path of the assembly that should be scanned:");
                args = new[] { Console.ReadLine() };
                
                Console.Write("Use verbose mode (y/n)? ");
                var key = Console.ReadKey();
                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                    verbose = true;

                Console.WriteLine();
                Console.WriteLine();
            }

            var loader = new Loader();
            try
            {
                var filePath = args.Length >= 2 ? args[1] : args[0];
                if (args[0] == "-verbose")
                    verbose = true;

                var success = loader.RunInNewDomain(filePath, verbose);
                
                //if (loader.TotalFailed > 0)
                //    Console.WriteLine("DONE with errors. {0} references could not be resolved.", loader.TotalFailed);
                //else
                    //Console.WriteLine("DONE. Everything is fine :)");

                Environment.Exit(success ? 0 : 1);
            }
            catch (FileNotFoundException)
            {
                Loader.WriteLine("Cannot find the specified file.", ConsoleColor.Red);
            }
            catch (BadImageFormatException)
            {
                Loader.WriteLine("The selected file is not a .NET assembly.", ConsoleColor.Red);
            }
        }

        private static void GenerateAppConfig()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Console.WriteLine(config.Sections["runtime"].SectionInformation.GetRawXml());
            config.Sections["runtime"].SectionInformation.SetRawXml("<runtime>\n    <NetFx40_LegacySecurityPolicy   enabled=\"false\"/>\n  </runtime>");
            config.Save();
        }
    }
}
