using CSLox;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class LanguageTests : IDisposable
    {
        private TestWriter testWriter;
        private TestWriter testErrors;
        public LanguageTests(ITestOutputHelper outputHelper)
        {
            testWriter = new TestWriter(outputHelper);
            testErrors = new TestWriter(outputHelper);
            Runtime.Writer = testWriter;
            Runtime.Errors = testErrors;
        }
        public void Dispose()
        {
            testWriter.Dispose();
            testErrors.Dispose();
            Runtime.Writer = null;
            Runtime.Errors = null;
        }

        [Theory]
        [InlineData("precedence.lox", new[] { "14","8","4","0","True", "True", "True", "True","0", "0", "0", "0","4" })]
        [InlineData("assignment\\associativity.lox",new[] {"c", "c", "c" })]
        [InlineData("assignment\\global.lox", new[] { "before", "after", "arg","arg" })]
        [InlineData("assignment\\local.lox", new[] { "before", "after", "arg", "arg" })]
        [InlineData("assignment\\syntax.lox", new[] { "var", "var" })]
        [InlineData("block\\empty.lox", new[] { "ok" })]
        [InlineData("block\\scope.lox", new[] { "inner", "outer" })]
        [InlineData("bool\\equality.lox", new[] { "True","False","False", "True", "False", "False", "False", "False", "False", "False", "True","True", "False", "True", "True", "True", "True", "True" })]
        [InlineData("bool\\not.lox", new[] { "False", "True", "True" })]
        [InlineData("class\\empty.lox", new[] { "Foo" })]
        [InlineData("class\\inherited_method.lox", new[] { "in foo", "in bar","in baz" })]
        [InlineData("class\\local_inherit_other.lox", new[] { "B" })]
        [InlineData("class\\local_reference_self.lox", new[] { "Foo" })]
        [InlineData("class\\reference_self.lox", new[] { "Foo" })]
        [InlineData("closure\\assign_to_closure.lox", new[] { "local","after f","after f","after g" })]
        [InlineData("closure\\assign_to_shadowed_later.lox", new[] { "inner","assigned" })]
        [InlineData("closure\\close_over_function_param.lox", new[] { "param" })]
        public void PositiveTest(string file, string[] expectedResults)
        {
            var path = System.Environment.CurrentDirectory;
            Runtime.RunFile(System.IO.Path.Combine(path, $"..\\..\\..\\cases\\{file}"));

            Assert.Equal(expectedResults, testWriter.GetWrittenStrings());
        }

        [Theory]
        [InlineData("unexpected_character.lox", new[] { "[Line 3] Error: Unexpected character '|'", "[Line 3] Error at 'b': Expect ')' after arguments" })]
        [InlineData("assignment\\grouping.lox", new[] { "[Line 2] Error at '=': Invalid assignment target" })]
        [InlineData("assignment\\infix_operator.lox", new[] { "[Line 3] Error at '=': Invalid assignment target" })]
        [InlineData("assignment\\prefix_operator.lox", new[] { "[Line 2] Error at '=': Invalid assignment target" })]
        [InlineData("assignment\\to_this.lox", new[] { "[Line 3] Error at '=': Invalid assignment target" })]
        [InlineData("assignment\\undefined.lox", new[] { "[Line 1] Undefined variable 'unknown'" })]
        [InlineData("call\\bool.lox", new[] { "[Line 1] Can only call functions and classes" })]
        [InlineData("call\\nil.lox", new[] { "[Line 1] Can only call functions and classes" })]
        [InlineData("call\\num.lox", new[] { "[Line 1] Can only call functions and classes" })]
        [InlineData("call\\object.lox", new[] { "[Line 4] Can only call functions and classes" })]
        [InlineData("call\\string.lox", new[] { "[Line 1] Can only call functions and classes" })]
        [InlineData("class\\inherit_self.lox", new[] { "[Line 1] Error at 'Foo': Class cannot inherit from itself" })]
        [InlineData("class\\local_inherit_self.lox", new[] { "[Line 2] Error at 'Foo': Class cannot inherit from itself" })]

        public void NegativeTest(string file, string [] errors)
        {
            var path = System.Environment.CurrentDirectory;
            Runtime.RunFile(System.IO.Path.Combine(path, $"..\\..\\..\\cases\\{file}"));
            Assert.Equal(errors, testErrors.GetWrittenStrings());
        }


    }
}