using QuelleDriverDefault;
using QuelleHMI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuelleDriverInterpreter
{
    class Program
    {
        private static JsonSerializer serializer = new JsonSerializer();
        
        static void prompt()
        {
            Console.WriteLine();
            Console.Write("> ");
        }
        static void Main(string[] args)
        {
            var driver = new QuelleDriver();
            HMICommand.Intitialize(driver);

            var debugResult = driver.ReadInt("debug");
            bool debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);

            Console.Write("> ");
            for (string line = Console.ReadLine(); /**/; line = Console.ReadLine())
            {
                line = line.Trim();
                if (line.Length < 0)
                {
                    prompt();
                    continue;
                }
                if (line.ToLower() == "@exit")
                {
                    break;
                }
                //  Bybass HMIStatement only for @Help and @Exit
                //
                if (line.Trim().ToLower().StartsWith("@help"))
                {
                    if (line.ToLower() == "@help")
                        Console.WriteLine(driver.Help());
                    else
                        Console.WriteLine(driver.Help(line.Substring("@help".Length)));
                    prompt();
                    continue;
                }
                HMICommand command = new HMICommand(line);

                if (command.statement != null && command.statement.segmentation != null && command.statement.segmentation.Count >= 1 && command.errors.Count == 0)
                {
                    var ok = command.statement.Execute();

                    if (ok)
                    {
                        //  Reset debuf variables incase there was a change
                        debugResult = driver.ReadInt("debug");
                        debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);

                        foreach (var message in command.warnings)
                        {
                            Console.WriteLine("WARNING: " + message);
                        }
                    }
                    else
                    {
                        foreach (var message in command.errors)
                        {
                            Console.WriteLine("ERROR: " + message);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("error: " + "Statement is not expected to be null; Quelle driver implementation error");
                }
                if (debug)
                {
                    using (JsonWriter writer = new JsonTextWriter(Console.Out))
                    {
                        serializer.Serialize(writer, command);
                    }
                    using (StreamWriter sw = new StreamWriter("./result.json"))
                    using (JsonWriter writer = new JsonTextWriter(sw))
                    {
                        serializer.Serialize(writer, command);
                    }
                }
                prompt();
            }
            Console.WriteLine("done.");
        }
    }
}
