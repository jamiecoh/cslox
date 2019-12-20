﻿using CSLox.Native;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSLox
{
    public class Interpreter : IExprVisitor<object>, IStmtVisitor
    {
        public Environment Globals { get; } = new Environment();
        private Environment environment;

        private readonly Dictionary<Expr, int> locals = new Dictionary<Expr, int>();
        public Interpreter()
        {
            environment = Globals;

            Globals.Define("clock", new Clock());
        }
        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach(var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            catch(RuntimeException ex)
            {
                Program.RuntimeError(ex);
            }
        }

        public void Resolve(Expr expr, int depth) => locals.Add(expr, depth);

        public object VisitAnonymousFunctionExpr(Expr.AnonymousFunction expr)
        {
            return new AnonymousFunction(expr);
        }
        public object VisitAssignExpr(Expr.Assign expr)
        {
            var value = Evaluate(expr.Value);
            if (locals.TryGetValue(expr, out var distance))
                environment.AssignAt(distance, expr.Name, value);
            else
                Globals.Assign(expr.Name, value);
            return value;
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            var left = Evaluate(expr.Left);
            var right = Evaluate(expr.Right);

            switch(expr.Op.Type)
            {
                case TokenType.Greater:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left > (double)right;
                case TokenType.GreaterEqual:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left >= (double)right;
                case TokenType.Less:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left < (double)right;
                case TokenType.LessEqual:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left <= (double)right;
                case TokenType.Minus:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left - (double)right;
                case TokenType.Plus when left is double leftDouble && right is double rightDouble:
                    return leftDouble + rightDouble;
                case TokenType.Plus when left is string leftString && right is string rightString:
                    return leftString + rightString;
                case TokenType.Plus when left is string leftString && right is double rightDouble:
                    return leftString + rightDouble;
                case TokenType.Plus when left is double leftDouble && right is string rightString:
                    return leftDouble + rightString;
                case TokenType.Plus:
                    throw InvalidOperator("Operands must be two numbers or two strings or string and number", expr.Op);
                case TokenType.Slash:
                    CheckNumberOperands(expr.Op, left, right);
                    CheckNonZero(expr.Op, right);
                    return (double)left / (double)right;
                case TokenType.Star:
                    CheckNumberOperands(expr.Op, left, right);
                    return (double)left * (double)right;
                case TokenType.BangEqual:
                    return !IsEqual(left, right);
                case TokenType.EqualEqual:
                    return IsEqual(left, right);
            }

            return null;
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            var callee = Evaluate(expr.Callee);

            var args = new List<object>();
            foreach (var argExpr in expr.Arguments)
                args.Add(Evaluate(argExpr));

            if(callee is ICallable function)
            {
                var arity = function.Arity();
                if (args.Count != arity)
                    throw new RuntimeException($"Expected {arity} arguments, but got {args.Count}", expr.Paren);

                return function.Call(this, args);
            }

            throw new RuntimeException("Can only call functions and classes", expr.Paren);
        }

        public object VisitGroupingExpr(Expr.Grouping expr) => Evaluate(expr.Expression);

        public object VisitLiteralExpr(Expr.Literal expr) => expr.Value;
        
        public object VisitLogicalExpr(Expr.Logical expr)
        {
            var left = Evaluate(expr.Left);
            if(expr.Op.Type == TokenType.Or)
            {
                if (IsTruthy(left)) return left;
            }
            else
            {
                if (!IsTruthy(left)) return left;
            }
            return Evaluate(expr.Right);
        }
        public object VisitUnaryExpr(Expr.Unary expr)
        {
            var right = Evaluate(expr.Right);
            switch(expr.Op.Type)
            {
                case TokenType.Bang:
                    return !IsTruthy(right);
                case TokenType.Minus:
                    CheckNumberOperand(expr.Op, right);
                    return -(double)right;
            }
            return null;
        }
        public object VisitVariableExpr(Expr.Variable expr)
        {
            return LookupVariable(expr.Name, expr);
        }
        public void VisitBlockStmt(Stmt.Block stmt)
        {
            ExecuteBlock(stmt.Statements, new Environment(environment));
        }               

        public void VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.Expr);
        }
        
        public void VisitFunctionStmt(Stmt.Function stmt)
        {
            var function = new Function(stmt, environment);
            environment.Define(stmt.Name.Lexeme, function);
        }
        public void VisitIfStmt(Stmt.If stmt)
        {
            if (IsTruthy(Evaluate(stmt.Condition)))
                Execute(stmt.ThenBranch);
            else if (stmt.ElseBranch != null)
                Execute(stmt.ElseBranch);
        }

        public void VisitPrintStmt(Stmt.Print stmt)
        {
            var value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
        }
        
        public void VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;
            if (stmt.Value != null)
                value = Evaluate(stmt.Value);
            throw new Return(value); 
        }

        public void VisitVarStmt(Stmt.Var stmt)
        {
            if (stmt.Initializer != null)
            {
                environment.Define(stmt.Name.Lexeme, Evaluate(stmt.Initializer));
            }
            else
            {
                environment.Define(stmt.Name.Lexeme);
            }
        }

        public void VisitWhileStmt(Stmt.While stmt)
        {
            while(IsTruthy(Evaluate(stmt.Condition)))
            {
                Execute(stmt.Body);
            }
        }


        private Object Evaluate(Expr expr) => expr.Accept(this);
        private void Execute(Stmt stmt) => stmt.Accept(this);
        public void ExecuteBlock(List<Stmt> statements, Environment environment)
        {
            var previous = this.environment;
            try
            {
                this.environment = environment;
                foreach (var stmt in statements)
                    Execute(stmt);
            }
            finally
            {
                this.environment = previous;
            }
        }

        private bool IsTruthy(object value)
        {
            if (value == null) return false;
            if (value is bool boolValue) return boolValue;
            return true;
        }

        private bool IsEqual(object left, object right)
        {
            if(left == null && right == null) return true;
            if (left == null) return false;
            return left.Equals(right);
        }

        private void CheckNumberOperand(Token op, object operand)
        {
            if (operand is double) return;

            throw new RuntimeException("Operand must be a number", op);
        }
        private void CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return;

            throw new RuntimeException("Operands must be a numbers", op);
        }

        private void CheckNonZero(Token op, object value)
        {
            if (value is double doubleValue && doubleValue != 0) return;

            throw new RuntimeException("Value cannot be zero", op);
        }

        private RuntimeException InvalidOperator(string message, Token op)
        {
            return new RuntimeException(message, op);
        }

        private string Stringify(object value)
        {
            if (value == null) return "nil";
            return value.ToString();
        }

        private object LookupVariable(Token name, Expr expr)
        {
            if(locals.TryGetValue(expr, out int distance))
            {
                return environment.GetAt(distance, name);
            }
            return Globals.Get(name);
        }

    }
}
