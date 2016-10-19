using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TestApplication
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var doc = new XDocument();
            Console.WriteLine(Library.Class.Method());
            Console.ReadKey(true);
        }
    }
}
