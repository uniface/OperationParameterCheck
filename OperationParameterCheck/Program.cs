using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace OperationParameterCheck
{
    class Program
    {
        static int lineCount = 0;
        static void Main(string[] args)
        {
            string component = "";
            string line;
            bool countingParams = false;
            bool foundParamsStart = false;
            DictionaryStore currentStore = null;
            Dictionary<string, DictionaryStore> dict = new Dictionary<string, DictionaryStore>();

            System.IO.StreamReader file = new System.IO.StreamReader("codebase.xml");
            while((line = file.ReadLine()) != null)
            {
                lineCount++;
                string lowerLine = line.ToLower();
                if (countingParams && foundParamsStart)
                {
                    
                    if (lowerLine.Trim().Length >= 9)
                    {
                        if (lowerLine.Contains("endparams") && (lowerLine.Trim()[0] != ';'))
                        {
                            foundParamsStart = false;
                            countingParams = false;
                        }
                        else if (lowerLine.Trim()[0] != ';')
                            currentStore.IncrementParamsCount();
                    }
                    else if (lowerLine != "" && lowerLine.Trim()[0] != ';')
                        currentStore.IncrementParamsCount();
                }

                else
                {
                    if (countingParams && !foundParamsStart && lowerLine.Trim().Length > 2)
                    {
                        if (lowerLine.Trim().Length >= 6)
                        {
                            if (lowerLine.Trim().Substring(0, 6) == "params")
                            {
                                foundParamsStart = true;
                                continue;
                            }
                        }
                        if (lowerLine.Trim().TrimEnd('\r', '\n').Substring(0,3) == "end" || lowerLine.Trim().TrimEnd('\r', '\n') == "end</dat>")
                        {
                            countingParams = false;
                        }
                    }
                    
                    else if (lowerLine.Contains("name=\"ulabel\">") || lowerLine.Contains("name=\"umenu\">"))
                        component = lowerLine.Substring(lowerLine.IndexOf('>') + 1, lowerLine.Length - lowerLine.IndexOf('>') - 7);

                    else if (lowerLine.Contains("operation ") && !lowerLine.Contains("opt_getversioninfo"))
                    {
                        if (lowerLine.Contains("public operation"))
                            lowerLine = lowerLine.Substring(lowerLine.IndexOf("operation"));
                        if (lowerLine.Contains('<') && lowerLine.Contains('>'))
                            lowerLine = lowerLine.Substring(lowerLine.IndexOf('>') + 1);
                        int comment = lowerLine.Replace(" ", "").IndexOf(';');
                        string name = lowerLine.Replace(" ", "").TrimEnd('\r', '\n').Substring(9);
                        if (comment > -1)
                        {
                            if ((comment < lowerLine.Replace(" ", "").IndexOf(name))) //|| !lowerLine.Contains(name))
                                continue;
                            else
                                name = name.Substring(0, name.Length - lowerLine.Replace(" ", "").Substring(comment).Length).TrimEnd();
                        }
                        string compAndOpt = component + " " + name;
                        if (!dict.ContainsKey(compAndOpt))
                        {
                            currentStore = new DictionaryStore(compAndOpt, 0);
                            dict.Add(compAndOpt, currentStore);
                        }
                        else
                        {
                            currentStore = dict[compAndOpt];
                            currentStore.SetParamCount();
                        }
                        countingParams = true;
                    }

                    else if (lowerLine.Contains("activate \"") && lowerLine.Contains(".opt") && !lowerLine.Contains("$concat") && !lowerLine.Contains("$formname"))
                    {
                        if (lowerLine.Contains("%\\"))
                        {
                            lowerLine = CollapseToOneLine(lowerLine, file).ToLower().Replace("%\\", " ");
                        }
                        else if (lowerLine.Length > 475)
                            lowerLine = CollapseLongLine(lowerLine, file).ToLower();
                        string name = lowerLine.Substring(lowerLine.IndexOf("activate \"") + 10, lowerLine.IndexOf(".opt") - 1 - (lowerLine.IndexOf("activate \"") + 10));
                        int comment = lowerLine.IndexOf(';');
                        if ((comment > -1 && comment < lowerLine.IndexOf(name)) || name.Contains("%%") || name.Substring(0, 3) == "idf" || name[0] == 'o' || name[0] == 'm' || name.Substring(0, 4) == "calc" || name.Substring(0, 4) == "auto" || name.Contains('$'))
                            continue;
                        string opt = lowerLine.Replace(" ", "").Substring(lowerLine.Replace(" ", "").IndexOf("opt"), lowerLine.Replace(" ", "").IndexOf('(', lowerLine.IndexOf("activate")) - lowerLine.Replace(" ", "").IndexOf("opt"));
                        if (opt == "opt_validatetable" || opt == "opt_databaseinfo" || opt == "opt_manage")    //all are found in include procs
                            continue;
                        string sub = lowerLine.Substring(lowerLine.IndexOf('(', lowerLine.IndexOf("activate")), lowerLine.LastIndexOf(')') - lowerLine.IndexOf('(', lowerLine.IndexOf("activate")));
                        int numParams = sub.Count(p => p == ',') + 1;
                        numParams -= sub.Select((c, i) => sub.Substring(i)).Count(t => t.StartsWith("%%,"));
                        if (numParams == 1)
                        {
                            if (lowerLine.Trim().Substring(lowerLine.Trim().LastIndexOf('('), lowerLine.Trim().LastIndexOf(')') - lowerLine.Trim().LastIndexOf('(')).Length == 1)
                                numParams = 0;
                        }
                        string compAndOpt = name + " " + opt;
                        if (compAndOpt.Length > 16)
                        {
                            if (compAndOpt.Substring(0, 17) == "svcrebates opt_de" || compAndOpt == "svcmasterfilein opt_verifyformulasyntax")      //opt_de<Entity> in svcrebates dynamically populates the operation name from entity list
                                continue;                                                                                                           //opt_verifyformulasytax passes in string variables with commas inside them
                        }
                        if (!dict.ContainsKey(compAndOpt))
                        {
                            currentStore = new DictionaryStore(compAndOpt, -1);
                            dict.Add(compAndOpt, currentStore);
                        }
                        else
                            currentStore = dict[compAndOpt];
                        currentStore.AddCall(new OperationCall(compAndOpt, component, numParams, lineCount));
                    }
                }
            }
            
            file.Close();

            System.IO.StreamWriter log = new System.IO.StreamWriter("log.txt");
            log.Write("Component        Service          Operation                     Codebase Line #     Correct # of Params           # From this Call\r\n");
            log.Write("__________________________________________________________________________________________________________________________________\r\n");
            foreach(KeyValuePair<string, DictionaryStore> operation in dict)
            {
                string[] name = operation.Key.Split(' ');
                currentStore = operation.Value;
                foreach(OperationCall call in currentStore.GetCallList())
                {
                    if (call.GetParamCount() != currentStore.GetCorrectParamCount())
                        log.Write("{0,-17}{1,-17}{2,-30}{3,-20}{4,-30}{5,-16}\r\n", call.GetComponentCalledFrom(),name[0], name[1], call.GetLineNumber(), OperationExists(currentStore.GetCorrectParamCount()), call.GetParamCount());
                }
            }
            log.Close();
        }

        public static string CollapseToOneLine(string line, System.IO.StreamReader file)
        {
            string nextLine = file.ReadLine();
            lineCount++;
            if (nextLine.Contains("%\\"))
                nextLine = CollapseToOneLine(nextLine, file);
            return (line + nextLine);
        }

        public static string CollapseLongLine(string line, System.IO.StreamReader file)
        {
            string nextLine = file.ReadLine();
            lineCount++;
            if (nextLine.Contains("&uBS;"))
                return (line + nextLine);
            else
                return line;
        }

        public static string OperationExists(int correctParamCount)
        {
            return(correctParamCount == -1 ? "Operation Does Not Exist" : correctParamCount.ToString());
        }

    }
}
