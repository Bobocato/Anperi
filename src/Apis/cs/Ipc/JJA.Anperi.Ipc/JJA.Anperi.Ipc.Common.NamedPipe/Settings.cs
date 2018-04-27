using System;
using System.Collections.Generic;
using System.Text;

namespace JJA.Anperi.Ipc.Common.NamedPipe
{
    public static class Settings
    {
        private static string PipeName => "anperi.lib.ipc.server";
        public static string ServerInputPipeName => PipeName + ".input";
        public static string ServerOutputPipeName => PipeName + ".output";
        public static string ConnectionSuccessString => "connection_success";
    }
}
