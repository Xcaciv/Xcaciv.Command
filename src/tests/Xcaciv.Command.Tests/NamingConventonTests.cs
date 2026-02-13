using Xunit;
using Xcaciv.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xcaciv.Command.Interface;

namespace Xcaciv.Command.Tests
{
    public class NamingConventonTests
    {
        [Fact()]
        public void GetCommandTest()
        {
            var expected = "DIR";
            var commandLine = $"{expected} - some options here";
            var manager = new CommandController();

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }
        [Fact()]
        public void GetCommandFilterTest()
        {
            var expected = "DIR";
            var commandLine = $"-{expected} - some options here";
            var manager = new CommandController();

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }
        [Fact()]
        public void GetCommandFilterAllowInnerDashTest()
        {
            var expected = "DIR-IT";
            var commandLine = $"-{expected} - some options here";
            var manager = new CommandController();

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }
        [Fact()]
        public void GetCommandFilterUnderscoreTest()
        {
            var expected = "_DIR";
            var commandLine = $"{expected} - some options here";
            var manager = new CommandController();

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }
        [Fact()]
        public void GetCommandFilterAllowInnerUnderscoreTest()
        {
            var expected = "DIR_IT";
            var commandLine = $"-{expected} - some options here";
            var manager = new CommandController();

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void PrepareArgsHandleQuotesTest()
        {
            var expected = new[] { "search", "nuget", "-name", "some text" };
            var commandLine = "package search nuget -name \"some text\"";
            var manager = new CommandController();

            var actual = NamesValidator.GetArgumentsFromCommandline(commandLine);
            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void PrepareArgsTest()
        {
            var command = "DIR";
            var expected = new[] { "-some", "--options", "are-here", "and_here"};
            var commandLine = $"{command} " + string.Join(' ', expected);
            var manager = new CommandController();

            var actual = NamesValidator.GetArgumentsFromCommandline(commandLine);
            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void PrepareArgsNotSupportedTest()
        {
            var command = "DIR";
            var input = new[] { "-some", "options", "are-here", "and_here", "/not:this" };
            var expected = new[] { "-some", "options", "are-here", "and_here", "not", "this" };
            var commandLine = $"{command} " + string.Join(' ', expected);
            var manager = new CommandController();

            var actual = NamesValidator.GetArgumentsFromCommandline(commandLine);
            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void GetCommand_HostileInput_Filters()
        {
            var expected = "DIR";
            var commandLine = $"{expected}*'`%^! -some options here";

            var actual = NamesValidator.GetValidCommandName(commandLine);

            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void PrepareArgs_HostileInput_Filters()
        {
            var command = "DIR";
            var expected = new[] { "-some", "options", "here" };
            var commandLine = $"{command} *'`%^!" + string.Join(' ', expected);

            var actual = NamesValidator.GetArgumentsFromCommandline(commandLine);
            Assert.Equal(expected, actual);
        }

        [Fact()]
        public void PrepareArgs_Quotes_Groups()
        {
            var command = "DIR";
            var expected = new[] { "-some", @"two word", "and_three_word", "options" , ".*? [0-9|a-z] ~!@#$%^&*()" };
            var commandLine = $@"{command} """ + string.Join(@""" """, expected) + '"';

            var actual = NamesValidator.GetArgumentsFromCommandline(commandLine);
            Assert.Equal(expected, actual);
        }
    }
}
