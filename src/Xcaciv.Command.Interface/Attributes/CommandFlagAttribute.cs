﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Command.Interface.Attributes
{
    /// <summary>
    /// this attribute indicates a toggle flag for a command
    /// if the flag is present the value is true
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class CommandFlagAttribute : AbstractCommandParameter
    {
        /// <summary>
        /// this attribute indicates a toggle flag for a command
        /// if the flag is present the value is true
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        public CommandFlagAttribute(string name, string description) 
        { 
            this.Name = name;
            this.ValueDescription = description;
        }

        public string ShortAlias { get; } = "";

    }
}
