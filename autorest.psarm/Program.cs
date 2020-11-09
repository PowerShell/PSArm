using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoRest.Core;
using AutoRest.Modeler;
using AutoRest.Core.Parsing;
using Microsoft.Perks.JsonRPC;
using System.Diagnostics;
using System.Threading;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;

namespace AutoRest.PSArm
{
    public class Program : NewPlugin
    {
        public static async Task<int> Main(string[] args)
        {
            if (args == null || args.Length == 0 || args[0] != "--server")
            {
                Console.WriteLine("This is an autorest plugin, and must be invoked through autorest.");
                return 1;
            }

            while (!Debugger.IsAttached)
            {
                Console.Error.WriteLine($"PID: {System.Diagnostics.Process.GetCurrentProcess().Id}");
                Thread.Sleep(2000);
            }

            Console.Error.WriteLine("Running the PSArm autorest plugin");

            var outStream = Console.OpenStandardOutput(); //new DebugStream(Console.OpenStandardOutput());
            var inStream = Console.OpenStandardInput(); //new DebugStream(Console.OpenStandardInput());

            var connection = new Connection(outStream, inStream);
            connection.Dispatch<IEnumerable<string>>("GetPluginNames", async () => new []{ "azureresourceschema", "imodeler2" });

            connection.Dispatch<string, string, bool>("Process", (plugin, sessionId) => {

                Console.Error.WriteLine("Process handler triggered");
                if (plugin == "imodeler2")
                {
                    Console.Error.WriteLine("Creating an imodeler2 plugin");
                    return new ModelerPlugin(connection, plugin, sessionId).Process();
                }
                Console.Error.WriteLine("Creating a plugin from the program object");
                return new Program(connection, plugin, sessionId).Process();
            });

            connection.DispatchNotification("Shutdown", connection.Stop);

            // Evil reflection so we can use a real await
            var loopTask = (Task)typeof(Connection).GetField("_loop", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);
            await loopTask.ConfigureAwait(false);

            Console.Error.WriteLine("Shutting down");
            return 0;
        }

        private Connection _connection;

        public Program(Connection connection, string plugin, string sessionId)
            : base(connection, plugin, sessionId)
        {
            _connection = connection;
        }

        protected override async Task<bool> ProcessInternal()
        {
            var files = await ListInputs();

            if (files.Length != 1)
            {
                throw new Exception($"Generator received incorrect number of inputs: {files.Length} : {string.Join(",", files)}");
            }

            var modelAsJson = (await ReadFile(files[0])).EnsureYamlIsJson();

            string outputPath = await GetValue<string>("output-directory").ConfigureAwait(false);
            string logPath = await GetValue<string>("log-path").ConfigureAwait(false) ?? "C:\\temp\\psarm-autorest-log.txt";

            // build settings
            
            new Settings
            {
                Host = this
            };

            // process
            Console.Error.WriteLine($"LOG PATH: {logPath}");
            using (Logger logger = Logger.CreateFileLogger(logPath))
            {
                var plugin = new PluginPSArm(logger, outputPath);

                using (plugin.Activate())
                {
                    var codeModel = plugin.Serializer.Load(modelAsJson);
                    codeModel = plugin.Transformer.TransformCodeModel(codeModel);
                    await plugin.CodeGenerator.Generate(codeModel);
                }

                // write out files
                var outFS = Settings.Instance.FileSystemOutput;
                var outFiles = outFS.GetFiles("", "*", System.IO.SearchOption.AllDirectories);
                foreach (var outFile in outFiles)
                {
                    //string actualPath = SubstituteVariables(outFile, pathVariables);
                    WriteFile(outFile, outFS.ReadAllText(outFile), null);
                }
            }

            return true;
        }

        private string SubstituteVariables(string input, IDictionary<string, string> values)
        {
            var sb = new StringBuilder(input.Length);
            int lastEnd = 0;
            for (int i = input.IndexOf("$("); i >= 0 && i < input.Length; i = input.IndexOf("$(", i + 1))
            {
                // Add the string up to the variable
                sb.Append(input.AsSpan(lastEnd, i - lastEnd));

                // Skip over the "$("
                i += 2;

                // Substitute the variable value
                int closeParenIdx = input.IndexOf(')', i);
                if (closeParenIdx < 0)
                {
                    throw new InvalidOperationException($"Variable syntax error at index {i} in input '{input}'");
                }

                string variableName = input.Substring(i, closeParenIdx - i);
                if (!values.TryGetValue(variableName, out string value))
                {
                    throw new ArgumentException($"No value provided for argument '{variableName}' with input '{input}'");
                }

                sb.Append(value);

                // Advance over the ")"
                i = lastEnd = closeParenIdx + 1;
            }

            // Append the tail of the input
            sb.Append(input.AsSpan(lastEnd));

            return sb.ToString();
        }
    }
}
