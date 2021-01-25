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

            var debugResult = driver.ReadInt("debug", HMIScope.Statement);
            bool debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);

            Console.Write("> ");
            for (string line = Console.ReadLine(); line.Length > 0; line = Console.ReadLine())
            {
                HMICommand command = new HMICommand(line);
                bool continuingExecution = true;

                if (command.warnings != null)
                    foreach (string message in command.warnings)
                        Console.WriteLine("warning: " + message);

                if (command.errors != null)
                    foreach (string message in command.errors)
                        Console.WriteLine("error: " + message);

                else if (command.statement != null && command.statement.segments != null && command.statement.segments.Count >= 1)
                {
                    var normalized = command.statement.NormalizeStatement();

                    if (normalized.errors != null)
                    {
                        continuingExecution = false;
                        foreach (var error in normalized.errors)
                            Console.WriteLine("error: " + error);
                    }
                    else if (normalized.scope == HMIScope.Undefined)
                    {
                        continuingExecution = false;
                        Console.WriteLine("error: " + "Unspecific command normalization error; Please contact vendor about this Quelle driver implementation");
                    }
                    if (continuingExecution && (command.HasMacro() != HMIScope.Undefined))
                    {
                        var macroDef = (HMIMacroDefintion)command.dependentClause;
                        var result = driver.Write("quelle.macro." + macroDef.macroName, macroDef.macroScope, command.statement.statement);
                        if (result.errors != null)
                        {
                            continuingExecution = false;
                            foreach (var error in result.errors)
                                Console.WriteLine("error: " + error);
                        }
                        else if (!result.success)
                        {
                            continuingExecution = false;
                            Console.WriteLine("error: " + "Unspecific macro error; Please contact vendor about this Quelle driver implementation");
                        }
                        else
                        {
                            continuingExecution = false;    // We used to allow ececution of macros and defintion of macros at the same time (now it is always two steps)
                        }
                        if (result.warnings != null)
                        {
                            foreach (var error in result.errors)
                                Console.WriteLine("error: " + error);
                        }
                    }
                    if (continuingExecution)
                    {
                        var results = new Dictionary<string, List<IQuelleResult>>();

                        if (normalized.normalization.ContainsKey(HMIPhrase.SET))
                        {
                            var list = new List<IQuelleResult>();

                            foreach (var segment in normalized.normalization[HMIPhrase.SET])
                            {
                                list.Add(
                                driver.Write(segment.rawFragments[0], normalized.scope, segment.rawFragments[1])
                                );
                            }
                            results.Add(HMIPhrase.SET, list);
                        }
                        foreach (var verb in normalized.normalization.Keys)
                        {
                            if (verb != HMIPhrase.SET) // SET was already processed, so skip here
                            {
                                var explain = HMIPhrase.IsVerb(verb);

                                if (explain.directive != null)
                                {
                                    bool add = !results.ContainsKey(explain.directive);
                                    var list = add ? new List<IQuelleResult>() : results[verb];
                                    foreach (var segment in normalized.normalization[verb])
                                    {
                                        IQuelleResult result = null;
                                        switch (explain.directive)
                                        {
                                            case HMIPhrase.SETTERS:        result = driver.Write(segment.rawFragments[0], normalized.scope, segment.rawFragments[1]); break;
                                            case HMIPhrase.GETTERS:         result = driver.Read(segment.rawFragments[0], normalized.scope); break;
                                            case HMIPhrase.REMOVAL:        result = driver.Remove(segment.rawFragments[0], normalized.scope); break;
                                            case HMIPhrase.SEARCH:         result = driver.Search(command.statement); break;
                                            case HMIPhrase.DISPLAY:        result = driver.Display(command.statement, "*"); break;

                                            default: continue;

                                        }
                                        list.Add(result);
                                    }
                                    if (add)
                                        results[explain.directive] = list;
                                }
                            }
                        }

                        //  Reset debuf variables incase there was a change
                        debugResult = driver.ReadInt("debug", HMIScope.Statement);
                        debug = debugResult.success && (debugResult.warnings == null) && (debugResult.result == 1);

                        foreach (var directive in results.Keys)
                        {
                            var error = false;
                            var specificResults = results[directive];
                            var output = new List<string>();

                            var comma = "";
                            foreach (IQuelleResult result in specificResults)
                            {
                                if (result.errors != null)
                                {
                                    error = true;
                                    foreach (var message in result.errors)
                                        Console.WriteLine(message);
                                }
                                if ((directive == HMIPhrase.GETTERS) && !error)
                                {
                                    var resultString = (IQuelleResultString) result;
                                    Console.WriteLine(comma + resultString.result);
                                    comma = ", ";
                                }
                                // TODO: HAndle FILE and SEARCH directives here:
                                //

                                //
                                if (result.warnings != null)
                                {
                                    foreach (var message in result.warnings)
                                        Console.WriteLine(message);
                                }
                            }
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
                Console.WriteLine();
                Console.Write("> ");
            }
            Console.WriteLine("done");
        }
    }
}
