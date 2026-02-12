using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;
using Xcaciv.Command.Interface.Attributes;

namespace zTestCommandPackage
{
    [CommandRegister("ECHO2", "test command that echoes twice (echo-echo)")]
    public class EchoChamberCommand : EchoCommand, ICommandDelegate
    {
        public EchoChamberCommand() 
        {
            this.FriendlyName = "echo-echo";
            this.BaseCommand = "ECHO2";
        }

        public override string FormatEcho(string p)
        {
            return $"{p}-{p}";
        }
    }
}
