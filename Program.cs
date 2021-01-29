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
        
        static void Main(string[] args)
        {
            var driver = new QuelleDriver();
            HMICommand.Intitialize(driver);

            var debugResult = driver.ReadInt("debug", HMIScope.Statement);
            bool debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);

            Console.Write("> ");
            for (string line = Console.ReadLine(); /**/; line = Console.ReadLine())
            {
                if (line.Trim().ToLower() == "@exit")
                    break;

                HMICommand command = new HMICommand(line);

                if (command.statement != null && command.statement.segmentation != null && command.statement.segmentation.Count >= 1 && command.errors.Count == 0)
                {
                    var ok = command.statement.Execute();

                    if (ok)
                    {
                        //  Reset debuf variables incase there was a change
                        debugResult = driver.ReadInt("debug", HMIScope.Statement);
                        debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);
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
                Console.WriteLine();
                Console.Write("> ");
            }
            Console.WriteLine("done");
        }
    }
}
