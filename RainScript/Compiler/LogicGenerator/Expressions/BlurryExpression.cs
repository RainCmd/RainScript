using System;

namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal class BlurryVariableDeclarationExpression : Expression
    {
        public override TokenAttribute Attribute => TokenAttribute.Variable;
        public BlurryVariableDeclarationExpression(Anchor anchor) : base(anchor, RelyKernel.BLURRY_TYPE) { }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    #region Methods
    internal class MethodMemberExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Temporary;
        public MethodMemberExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class MethodVirtualExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Temporary;
        public MethodVirtualExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class MethodQuestionExpression : Expression
    {
        public readonly Expression target;
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Temporary;
        public MethodQuestionExpression(Anchor anchor, Expression target, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.target = target;
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class MethodGlobalExpression : Expression
    {
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Temporary;
        public MethodGlobalExpression(Anchor anchor, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class MethodNativeExpression : Expression
    {
        public readonly Declaration declaration;
        public override TokenAttribute Attribute => TokenAttribute.Method | TokenAttribute.Temporary;
        public MethodNativeExpression(Anchor anchor, Declaration declaration) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.declaration = declaration;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    #endregion Methods
    internal class BlurryCoroutineExpression : Expression
    {
        public readonly Expression invoker;
        public override TokenAttribute Attribute => TokenAttribute.Coroutine;
        public BlurryCoroutineExpression(Anchor anchor, Expression invoker) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.invoker = invoker;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
    internal class BlurryLambdaExpression : Expression
    {
        public readonly Anchor[] parameters;
        public readonly ListSegment<Lexical> body;
        public override TokenAttribute Attribute => TokenAttribute.Invalid;
        public BlurryLambdaExpression(Anchor anchor, Anchor[] parameters, ListSegment<Lexical> body) : base(anchor, RelyKernel.BLURRY_TYPE)
        {
            this.parameters = parameters;
            this.body = body;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
