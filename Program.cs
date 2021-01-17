using ClarityDriverDefault;
using ClarityHMI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TestClarity
{
    class Program
    {
        private static JsonSerializer serializer = new JsonSerializer();
        
        static void Main(string[] args)
        {
            var driver = new StandardClarityDriver();

            var debugResult = driver.Read("debug", HMIScope.Session);
            bool debug = debugResult.success && debugResult.warnings == null;

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
                        Console.WriteLine("error: " + "Unspecific command normalization error; Please contact vendor about this Clarity driver implementation");
                    }
                    if (continuingExecution && (command.HasMacro() != HMIScope.Undefined))
                    {
                        var result = driver.Write("clarity.macro." + command.macroName, command.HasMacro(), command.statement.statement);
                        if (result.errors != null)
                        {
                            continuingExecution = false;
                            foreach (var error in result.errors)
                                Console.WriteLine("error: " + error);
                        }
                        else if (!result.success)
                        {
                            continuingExecution = false;
                            Console.WriteLine("error: " + "Unspecific macro error; Please contact vendor about this Clarity driver implementation");
                        }
                        else
                        {
                            continuingExecution = command.macroSimultaneousExecution;
                        }
                        if (result.warnings != null)
                        {
                            foreach (var error in result.errors)
                                Console.WriteLine("error: " + error);
                        }
                    }
                    if (continuingExecution)
                    {
                        var results = new Dictionary<string, List<IClarityResult>>();

                        if (normalized.normalization.ContainsKey(HMISegment.SET))
                        {
                            var list = new List<IClarityResult>();

                            foreach (var segment in normalized.normalization[HMISegment.SET])
                            {
                                list.Add(
                                driver.Write(segment.rawFragments[0], normalized.scope, segment.rawFragments[1])
                                );
                            }
                            results.Add(HMISegment.SET, list);
                        }
                        foreach (var verb in normalized.normalization.Keys)
                        {
                            if (verb != HMISegment.SET) // SET was already processed, so skip here
                            {
                                var explain = HMISegment.IsVerb(verb);

                                if (explain.directive != null)
                                {
                                    bool add = !results.ContainsKey(explain.directive);
                                    var list = add ? new List<IClarityResult>() : results[verb];
                                    foreach (var segment in normalized.normalization[verb])
                                    {
                                        IClarityResult result = null;
                                        switch (explain.directive)
                                        {
                                            case HMISegment.PERSISTENCE:    result = driver.Write(segment.rawFragments[0], normalized.scope, segment.rawFragments[1]); break;
                                            case HMISegment.STATUS:         result = driver.Read(segment.rawFragments[0], normalized.scope); break;
                                            case HMISegment.REMOVAL:        result = driver.Remove(segment.rawFragments[0], normalized.scope); break;
                                            case HMISegment.SEARCH:         result = driver.Search(command.statement); break;
                                            case HMISegment.FILE:           if (verb == HMISegment.EXPORT)
                                                                                result = driver.Export(command.statement);
                                                                            else if (verb == HMISegment.EXPORT)
                                                                                result = driver.Import(command.statement);
                                                                            break;

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
                        debugResult = driver.Read("debug", HMIScope.Session);
                        debug = debugResult.success && debugResult.warnings == null;

                        foreach (var verb in results.Keys)
                        {
                            var error = false;
                            var specificResults = results[verb];
                            var explain = HMISegment.IsVerb(verb);
                            var output = new List<string>();

                            var comma = "";
                            foreach (IClarityResult result in specificResults)
                            {
                                if (result.errors != null)
                                {
                                    error = true;
                                    foreach (var message in result.errors)
                                        Console.WriteLine(message);
                                }
                                if ((explain.directive == HMISegment.STATUS) && !error)
                                {
                                    var resultString = (IClarityResultString) result;
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
                    Console.WriteLine("error: " + "Statement is not expected to be null; Clarity driver implementation error");
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
