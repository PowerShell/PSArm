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

            var outStream = new DebugStream(Console.OpenStandardOutput());
            var inStream = new DebugStream(Console.OpenStandardInput());

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

            var loopTask = (Task)typeof(Connection).GetField("_loop", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(connection);

            await loopTask.ConfigureAwait(false);

            Console.Error.WriteLine("Shutting down");
            return 0;
        }

        public Program(Connection connection, string plugin, string sessionId)
            : base(connection, plugin, sessionId)
        {
        }

        protected override async Task<bool> ProcessInternal()
        {
            var files = await ListInputs();

            if (files.Length != 1)
            {
                throw new Exception($"Generator received incorrect number of inputs: {files.Length} : {string.Join(",", files)}");
            }

            var modelAsJson = (await ReadFile(files[0])).EnsureYamlIsJson();

            // build settings
            
            new Settings
            {
                Host = this
            };

            // process
            var plugin = new PluginPSArm();
            
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
                WriteFile(outFile, outFS.ReadAllText(outFile), null);
            }

            return true;
        }
    }
}
