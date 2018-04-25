using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IpcSocketServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            NamedPipeServer nps = new NamedPipeServer();
            nps.Run();
            Console.WriteLine("Press enter to exit ...");
            Console.ReadLine();
        }
    }
}
