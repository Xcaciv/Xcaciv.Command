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
    [CommandRegister("ECHOE", "test command that echoes in base64 encoding")]
    public class EchoEncodeCommand : EchoCommand, ICommandDelegate
    {
        public EchoEncodeCommand()
        {
            this.FriendlyName = "echoe";
            this.BaseCommand = "ECHOE";
        }

        public override string FormatEcho(string p)
        {
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(p));
            return $":{encoded}:";
        }
    }
}
