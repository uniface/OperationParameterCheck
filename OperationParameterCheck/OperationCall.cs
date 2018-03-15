using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperationParameterCheck
{
    class OperationCall
    {
        private string operationName;
        private string componentCalledFrom;
        private int paramCount;
        private int codebaseLineNum;

        public OperationCall(string optName, string component, int numParams, int codebaseLine)
        {
            operationName = optName;
            componentCalledFrom = component;
            paramCount = numParams;
            codebaseLineNum = codebaseLine;
        }

        public int GetParamCount()
        {
            return paramCount;
        }

        public int GetLineNumber()
        {
            return codebaseLineNum;
        }

        public string GetComponentCalledFrom()
        {
            return componentCalledFrom;
        }
    }
}
