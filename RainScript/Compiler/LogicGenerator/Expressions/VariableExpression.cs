﻿using RainScript.Vector;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

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
        public VariableLocalExpression(Local local, TokenAttribute attribute) : base(local.anchor, local.type)
        {
            declaration = local.Declaration;
            this.attribute = attribute.AddTypeAttribute(local.type);
        }
        public VariableLocalExpression(Anchor anchor, Declaration declaration, TokenAttribute attribute, CompilingType type) : base(anchor, type)
        {
            this.declaration = declaration;
            this.attribute = attribute.AddTypeAttribute(type);
        }
        private Variable GetVariable(GeneratorParameter parameter)
        {
            if (!parameter.variable.TryGetLocal(declaration.index, out var variable))
                variable = parameter.variable.DecareLocal(declaration.index, returns[0]);
            parameter.debug.AddLocalVariable(anchor, parameter.generator.Point, variable.address, parameter.relied.Convert(returns[0]).RuntimeType);
            return variable;
        }
        public override void Generator(GeneratorParameter parameter)
        {
            parameter.results[0] = GetVariable(parameter);
        }
        public override void GeneratorAssignment(GeneratorParameter parameter)
        {
            var variable = GetVariable(parameter);
            if (variable.type.IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_Handle);
            else if (variable.type == RelyKernel.BOOL_TYPE || variable.type == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Local_1);
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
        private void AddDebugInfo(GeneratorParameter parameter)
        {
            if (parameter.command.generatorDebugTable && declaration)
            {
                var declaration = parameter.manager.GetDeclaration(this.declaration);
                var type = parameter.relied.Convert(returns[0]).RuntimeType;
                var relied = parameter.relied.Convert(this.declaration);
                parameter.debug.AddGlobalVariableSegment(declaration, anchor, parameter.generator.Point, relied.library, relied.index, type);
            }
        }
        public override void Generator(GeneratorParameter parameter)
        {
            AddDebugInfo(parameter);
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE || returns[0] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Global2Local_1);
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
            AddDebugInfo(parameter);
            var declaration = parameter.relied.Convert(this.declaration);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE || returns[0] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Global_1);
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
        private bool TryGetAddress(out uint address, EvaluationParameter parameter)
        {
            if (declaration.library == LIBRARY.SELF && Attribute.ContainAny(TokenAttribute.Constant))
            {
                var variable = parameter.manager.library.variables[(int)declaration.index];
                if (variable.calculated)
                {
                    address = variable.address;
                    return true;
                }
            }
            address = default;
            return false;
        }
        public override bool TryEvaluation(out bool value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<bool>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out byte value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<byte>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out long value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<long>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out real value, EvaluationParameter parameter)
        {
            if (declaration.library == LIBRARY.KERNEL)
            {
                value = KernelConstant.constants[declaration.index].value;
                return true;
            }
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<real>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real2 value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<Real2>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real3 value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<Real3>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out Real4 value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetData<Real4>(address);
                return true;
            }
            value = default;
            return false;
        }
        public override bool TryEvaluation(out string value, EvaluationParameter parameter)
        {
            if (TryGetAddress(out var address, parameter))
            {
                value = parameter.generator.GetDataString(address);
                return true;
            }
            value = default;
            return false;
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
            else if (returns[0] == RelyKernel.BOOL_TYPE || returns[0] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_1);
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
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE || returns[0] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Local2Handle_1);
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
            parameter.generator.WriteCode(targetParameter.results[0]);
            parameter.generator.WriteCode(address);
            var declaration = parameter.relied.Convert(this.declaration);
            parameter.results[0] = parameter.variable.DecareTemporary(parameter.pool, returns[0]);
            if (returns[0].IsHandle) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_Handle);
            else if (returns[0] == RelyKernel.BOOL_TYPE || returns[0] == RelyKernel.BYTE_TYPE) parameter.generator.WriteCode(CommandMacro.ASSIGNMENT_Handle2Local_1);
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
