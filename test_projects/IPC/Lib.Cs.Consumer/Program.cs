using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Lib.Cs;

namespace Lib.Cs.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Anperi anperi = new Anperi();
            anperi.DebugMessage += Anperi_DebugMessage;
            
            CancellationTokenSource cts = new CancellationTokenSource();

            CancellationToken ct = cts.Token;
            Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(1000, ct);
                    ct.ThrowIfCancellationRequested();
                    anperi.Debug($"This is a very important message: {DateTime.Now.Millisecond}.");
                }
            }, cts.Token);

            Console.WriteLine("Press enter to close ...");
            Console.ReadLine();
        }

        private static void Anperi_DebugMessage(object sender, AnperiMessageEventArgs e)
        {
            Trace.TraceInformation("Got debug message: {0}", e.Message);
        }
    }
}
