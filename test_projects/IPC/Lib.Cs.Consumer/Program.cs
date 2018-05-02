using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Lib.Cs;
using JJA.Anperi.Lib.Cs.Elements;
using JJA.Anperi.Lib.Cs.Message;
using JJA.Anperi.Utility;

namespace Lib.Cs.Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Anperi anperi = new Anperi();
            anperi.Message += Anperi_Message;

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("type a line to do stuff: periinf, perilay");
                string text = Console.ReadLine();
                switch (text)
                {
                    case "periinf":
                        anperi.RequestPeripheralInfo();
                        break;
                    case "perilay":
                        RootGrid rg = new RootGrid();
                        int rnd = new Random().Next(1, 10);
                        for (int i = 0; i < rnd; i++)
                        {
                            rg.elements.Add(new Button {column = 1, row = i, id = i, text = "button_" + i});
                        }
                        anperi.SetLayout(rg);
                        break;
                    case "exit":
                        exit = true;
                        break;
                }
            }
            
        }

        private static void Anperi_Message(object sender, AnperiMessageEventArgs e)
        {
            switch (e.Message)
            {
                case DebugAnperiMessage _:
                    Trace.TraceInformation($"Got debug: {e.Message.Data.ToDataString()}");
                    break;
                case ErrorAnperiMessage _:
                    Trace.TraceInformation($"Got error: {e.Message.Data.ToDataString()}");
                    break;
                case EventFiredAnperiMessage _:
                    Trace.TraceInformation($"Got event fired: {e.Message.Data.ToDataString()}");
                    break;
                case PeripheralInfoAnperiMessage _:
                    Trace.TraceInformation($"Got peripheral info: {e.Message.Data.ToDataString()}");
                    break;
                default:
                    throw new NotImplementedException("this type is not implemented yet");
            }
        }
    }
}
