using System;
using System.Collections.Generic;

namespace Eval.Csharp
{
    public class Eval
    {
        private string _code;
        private Dictionary<string, object> _variables;

        public Eval(string code)
        {
            this._code = code;
            this._variables = new Dictionary<string, object>();
        }

        public Eval AddVariable<T>(string identifier, T @object)
        {
            this._variables.Add(identifier, @object);
            return this;
        }

        public Func<object> Evaluate()
        {
            return () => new Object();
        }
    }
}
