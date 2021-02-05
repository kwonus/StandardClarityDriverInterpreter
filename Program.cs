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

                //  Bybass HMIStatement for now
                if (line.Trim().ToLower().StartsWith("@generate"))
                {
                    var expanded = line.ToLower().Replace("!", " ! ").Replace(">", " > ");
                    var tokens = expanded.Split(HMIClause.Whitespace, line.Contains('!') ? 6 : 5, StringSplitOptions.RemoveEmptyEntries);

                    if (tokens[0] == "@generate")
                    {
                        bool valid3 = (tokens.Length == 3);
                        bool valid5 = (tokens.Length == 5) && (tokens[3] == ">");
                        bool valid6 = (tokens.Length == 6)
                                 && ( (tokens[3] == "!") && (tokens[4] == ">")
                                    ||(tokens[3] == ">") && (tokens[4] == "!")
                                    );

                        if (valid3 || valid5 || valid6)
                        {
                            var generator = XGen.Factory(tokens[1].Trim());
                            if (generator != null)
                            {
                                var code = generator.export(tokens[2].Trim());
                                if (valid5 || valid6)
                                {
                                    var file = valid6 ? tokens[5].Trim() : tokens[4].Trim();
                                    if (valid5 && File.Exists(file))
                                        Console.WriteLine("ERROR: File already exists.");
                                    else using (StreamWriter writer = new StreamWriter(file))
                                    {
                                        writer.Write(code);
                                    }
                                }
                                else Console.WriteLine(code);
                            }
                        }
                    }
                    continue;   // no error handling here, just silent failure for now; TODO: fix later
                }
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
