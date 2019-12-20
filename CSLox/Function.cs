﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CSLox
{
    public class Function : ICallable
    {
        private readonly Stmt.Function declaration;
        private readonly Environment closure;
        public Function(Stmt.Function declaration, Environment closure)
        {
            this.declaration = declaration;
            this.closure = closure;
        }
        public int Arity() => declaration.Parms.Count;

        public object Call(Interpreter interpreter, List<object> arguments)
        {
            var environment = new Environment(closure);
            for (var i = 0; i < declaration.Parms.Count; i++)
                environment.Define(declaration.Parms[i].Lexeme, arguments[i]);

            try
            {
                interpreter.ExecuteBlock(declaration.Body, environment);
                return null;
            }
            catch(Return returnValue)
            {
                return returnValue.Value;
            }
        }

        public override string ToString() => $"<fn {declaration.Name.Lexeme}>";
    }
}
