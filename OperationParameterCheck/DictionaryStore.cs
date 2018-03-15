using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperationParameterCheck
{
    class DictionaryStore
    {
        private string operationName;
        private int correctParamNum;
        private List<OperationCall> callList;

        public DictionaryStore(string optName, int numParams)
        {
            operationName = optName;
            correctParamNum = numParams;
            callList = new List<OperationCall>();
        }

        public void IncrementParamsCount()
        {
            this.correctParamNum++;
        }

        public void AddCall(OperationCall call)
        {
            this.callList.Add(call);
        }

        public List<OperationCall> GetCallList()
        {
            return callList;
        }

        public int GetCorrectParamCount()
        {
            return correctParamNum;
        }

        public void SetParamCount()
        {
            correctParamNum = 0;
        }
    }
}
