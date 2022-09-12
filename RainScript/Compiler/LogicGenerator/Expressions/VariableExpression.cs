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
            parameter.variable.TryGetLocal(declaration.index, out parameter.results[0]);
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            parameter.variable.TryGetLocal(declaration.index, out var variable);
            if (variable.type.IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Handle);
            else if (variable.type == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_1);
            else if (variable.type == RelyKernel.INTEGER_TYPE || variable.type == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_8);
            else if (variable.type == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_16);
            else if (variable.type == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_24);
            else if (variable.type == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_32);
            else if (variable.type == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_String);
            else if (variable.type == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(variable);
            parameter.generator.WriteCode(parameter.results[0]);
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
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_24);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_32);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_String);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(declaration.library);
            parameter.generator.WriteCode(declaration.index);
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_24);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_32);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_String);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(declaration.library);
            parameter.generator.WriteCode(declaration.index);
            parameter.generator.WriteCode(parameter.results[0]);
        }
    }
    internal class VariableMemberExpression : VariableExpression
    {
        private readonly Expression target;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableMemberExpression(Anchor anchor, Declaration declaration, Expression target, CompilingType type) : base(anchor, type)
        {
            this.target = target;
            this.declaration = declaration;
            attribute = TokenAttribute.Value.AddTypeAttribute(type) | TokenAttribute.Assignable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_24);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_32);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_String);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(declaration.library);
            parameter.generator.WriteCode(new MemberVariable(declaration.definitionIndex, declaration.index));
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_24);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_32);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_String);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(declaration.library);
            parameter.generator.WriteCode(new MemberVariable(declaration.definitionIndex, declaration.index));
            parameter.generator.WriteCode(parameter.results[0]);
        }
    }
    internal class VariableQuestionMemberExpression : Expression
    {
        private readonly Expression target;
        private readonly Declaration declaration;
        private readonly TokenAttribute attribute;
        public override TokenAttribute Attribute => attribute;
        public VariableQuestionMemberExpression(Anchor anchor, Declaration declaration, Expression target, CompilingType type) : base(anchor, type)
        {
            this.target = target;
            this.declaration = declaration;
            attribute = TokenAttribute.Value.AddTypeAttribute(type);
        }
        public override void Generator(GeneratorParameter parameter)
        {
            var address = new Referencable<CodeAddress>(parameter.pool);
            var targetParameter = new GeneratorParameter(parameter, 1);
            target.Generator(targetParameter);
            parameter.generator.WriteCode(CommandMacro.BASE_NullJump);
            parameter.generator.WriteCode(address);
            parameter.generator.WriteCode(targetParameter.results[0]);
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_1);
            else if (returns[0] == RelyKernel.INTEGER_TYPE || returns[0] == RelyKernel.REAL_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_8);
            else if (returns[0] == RelyKernel.REAL2_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_16);
            else if (returns[0] == RelyKernel.REAL3_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_24);
            else if (returns[0] == RelyKernel.REAL4_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_32);
            else if (returns[0] == RelyKernel.STRING_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_String);
            else if (returns[0] == RelyKernel.ENTITY_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Entity);
            else throw ExceptionGeneratorCompiler.Unknown();
            parameter.generator.WriteCode(parameter.results[0]);
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(declaration.library);
            parameter.generator.WriteCode(new MemberVariable(declaration.definitionIndex, declaration.index));
            parameter.generator.SetCodeAddress(address);
            address.Dispose();
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
            if (left is VariableExpression variable)
            {
                right.Generator(parameter);
                variable.GeneratorAssignment(parameter);
            }
            else parameter.exceptions.Add(anchor, CompilingExceptionCode.GENERATOR_INVALID_OPERATION);
        }
    }
}
