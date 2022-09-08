namespace RainScript.Compiler.LogicGenerator.Expressions
{
    internal abstract class VariableExpression : Expression
    {
        protected VariableExpression(Anchor anchor, CompilingType type) : base(anchor, type) { }
        public abstract void GeneratorAssignment(GeneratorParameter parameter);
    }
    internal class VariableLocalExpression : VariableExpression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableLocalExpression(Anchor anchor, Declaration declaration, TokenAttribute attribute, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            this.attribute = attribute.AddTypeAttribute(type) | TokenAttribute.Value;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class VariableGlobalExpression : VariableExpression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableGlobalExpression(Anchor anchor, Declaration declaration, bool constant, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            attribute = constant ? TokenAttribute.Constant : (TokenAttribute.Assignable | TokenAttribute.Value);
            attribute = attribute.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class VariableMemberExpression : VariableExpression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableMemberExpression(Anchor anchor, Declaration declaration, Expression target, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            attribute = TokenAttribute.Value.AddTypeAttribute(type) | TokenAttribute.Assignable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class VariableQuestionMemberExpression : Expression
    {
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableQuestionMemberExpression(Anchor anchor, Declaration declaration, Expression target, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class VariableAssignmentExpression : Expression
    {
        private readonly Expression left;
        private readonly Expression right;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableAssignmentExpression(Anchor anchor, Expression left, Expression right, CompilingType type) : base(anchor, type)
        {
            this.left = left;
            this.right = right;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            throw new System.NotImplementedException();
        }
    }
}
