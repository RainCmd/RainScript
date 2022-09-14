using RainScript.Compiler.Compiling;

namespace RainScript.Compiler.File
{
    internal partial class Space
    {
        public void Link(DeclarationManager manager, CollectionPool pool, ExceptionCollector exceptions)
        {
            foreach (var item in children) item.Link(manager, pool, exceptions);
            foreach (var item in definitions)
            {
                if (item.inherits.Count == 0) item.compiling.parent = RelyKernel.HANDLE;
                else
                {
                    var inheritContext = new Context(compiling, null, relyCompilings, relyReferences);
                    var inheritDeclaration = inheritContext.FindDeclaration(manager, item.inherits[0], pool, exceptions);
                    if (inheritDeclaration.code == DeclarationCode.Definition)
                    {
                        if (inheritDeclaration.library == LIBRARY.KERNEL) exceptions.Add(item.inherits[0], CompilingExceptionCode.COMPILING_INVALID_INHERIT);
                        else item.compiling.parent = new CompilingDefinition(inheritDeclaration);
                    }
                    else if (inheritDeclaration.code == DeclarationCode.Interface)
                    {
                        item.compiling.parent = RelyKernel.HANDLE;
                        if (inheritDeclaration.library == LIBRARY.KERNEL) exceptions.Add(item.inherits[0], CompilingExceptionCode.COMPILING_INVALID_INHERIT);
                        else item.compiling.inherits.Add(new CompilingDefinition(inheritDeclaration));
                    }
                    else if (inheritDeclaration) exceptions.Add(item.inherits[0], CompilingExceptionCode.COMPILING_INVALID_INHERIT);
                    else exceptions.Add(item.inherits[0], CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    for (var i = 1; i < item.inherits.Count; i++)
                    {
                        inheritDeclaration = inheritContext.FindDeclaration(manager, item.inherits[i], pool, exceptions);
                        if (inheritDeclaration.code == DeclarationCode.Interface && inheritDeclaration.library != LIBRARY.KERNEL) item.compiling.inherits.Add(new CompilingDefinition(inheritDeclaration));
                        else exceptions.Add(item.inherits[i], CompilingExceptionCode.COMPILING_INVALID_INHERIT);
                    }
                }
                var definitionContext = new Context(compiling, item.compiling, relyCompilings, relyReferences);
                foreach (var variable in item.variables)
                {
                    var declaration = definitionContext.FindDeclaration(manager, variable.type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) variable.compiling.type = new CompilingType(definition, variable.type.dimension);
                        else exceptions.Add(variable.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(item.inherits[0], CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
                foreach (var constructor in item.constructors)
                {
                    for (int i = 0; i < constructor.parameters.Count; i++)
                    {
                        var parameter = constructor.parameters[i];
                        var declaration = definitionContext.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                        if (declaration)
                        {
                            var definition = new CompilingDefinition(declaration);
                            if ((bool)definition)
                            {
                                constructor.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                                constructor.compiling.parameterNames[i] = parameter.name;
                            }
                            else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    }
                }
                foreach (var function in item.functions)
                {
                    for (int i = 0; i < function.returns.Count; i++)
                    {
                        var type = function.returns[i];
                        var declaration = definitionContext.FindDeclaration(manager, type.name, pool, exceptions);
                        if (declaration)
                        {
                            var definition = new CompilingDefinition(declaration);
                            if ((bool)definition) function.compiling.returns[i] = new CompilingType(definition, type.dimension);
                            else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                        }
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    }
                    for (int i = 0; i < function.parameters.Count; i++)
                    {
                        var parameter = function.parameters[i];
                        var declaration = definitionContext.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                        if (declaration)
                        {
                            var definition = new CompilingDefinition(declaration);
                            if ((bool)definition)
                            {
                                function.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                                function.compiling.parameterNames[i] = parameter.name;
                            }
                            else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    }
                }
            }
            var context = new Context(compiling, null, relyCompilings, relyReferences);
            foreach (var item in variables)
            {
                var declaration = context.FindDeclaration(manager, item.type.name, pool, exceptions);
                if (declaration)
                {
                    var definition = new CompilingDefinition(declaration);
                    if ((bool)definition) item.compiling.type = new CompilingType(definition, item.type.dimension);
                    else exceptions.Add(item.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                }
                else exceptions.Add(item.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
            }
            foreach (var item in delegates)
            {
                for (int i = 0; i < item.returns.Count; i++)
                {
                    var type = item.returns[i];
                    var declaration = context.FindDeclaration(manager, type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) item.compiling.returns[i] = new CompilingType(definition, type.dimension);
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
                for (int i = 0; i < item.parameters.Count; i++)
                {
                    var parameter = item.parameters[i];
                    var declaration = context.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition)
                        {
                            item.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                            item.compiling.parameterNames[i] = parameter.name;
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
            }
            foreach (var item in coroutines)
            {
                for (int i = 0; i < item.returns.Count; i++)
                {
                    var type = item.returns[i];
                    var declaration = context.FindDeclaration(manager, type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) item.compiling.returns[i] = new CompilingType(definition, type.dimension);
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
            }
            foreach (var item in functions)
            {
                for (int i = 0; i < item.returns.Count; i++)
                {
                    var type = item.returns[i];
                    var declaration = context.FindDeclaration(manager, type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) item.compiling.returns[i] = new CompilingType(definition, type.dimension);
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
                for (int i = 0; i < item.parameters.Count; i++)
                {
                    var parameter = item.parameters[i];
                    var declaration = context.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) 
                        {
                            item.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                            item.compiling.parameterNames[i] = parameter.name;
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
            }
            foreach (var item in interfaces)
            {
                for (var i = 1; i < item.inherits.Count; i++)
                {
                    var inheritDeclaration = context.FindDeclaration(manager, item.inherits[i], pool, exceptions);
                    if (inheritDeclaration.code == DeclarationCode.Interface && inheritDeclaration.library != LIBRARY.KERNEL) item.compiling.inherits[i] = new CompilingDefinition(inheritDeclaration);
                    else exceptions.Add(item.inherits[i], CompilingExceptionCode.COMPILING_INVALID_INHERIT);
                }
                foreach (var function in item.functions)
                {
                    for (int i = 0; i < function.returns.Count; i++)
                    {
                        var type = function.returns[i];
                        var declaration = context.FindDeclaration(manager, type.name, pool, exceptions);
                        if (declaration)
                        {
                            var definition = new CompilingDefinition(declaration);
                            if ((bool)definition) function.compiling.returns[i] = new CompilingType(definition, type.dimension);
                            else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                        }
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    }
                    for (int i = 0; i < function.parameters.Count; i++)
                    {
                        var parameter = function.parameters[i];
                        var declaration = context.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                        if (declaration)
                        {
                            var definition = new CompilingDefinition(declaration);
                            if ((bool)definition) 
                            {
                                function.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                                function.compiling.parameterNames[i] = parameter.name;
                            }
                            else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                    }
                }
            }
            foreach (var item in natives)
            {
                for (int i = 0; i < item.returns.Count; i++)
                {
                    var type = item.returns[i];
                    var declaration = context.FindDeclaration(manager, type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) item.compiling.returns[i] = new CompilingType(definition, type.dimension);
                        else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
                for (int i = 0; i < item.parameters.Count; i++)
                {
                    var parameter = item.parameters[i];
                    var declaration = context.FindDeclaration(manager, parameter.type.name, pool, exceptions);
                    if (declaration)
                    {
                        var definition = new CompilingDefinition(declaration);
                        if ((bool)definition) 
                        {
                            item.compiling.parameters[i] = new CompilingType(definition, parameter.type.dimension);
                            item.compiling.parameterNames[i] = parameter.name;
                        }
                        else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_INVALID_DEFINITION);
                    }
                    else exceptions.Add(parameter.type.name, CompilingExceptionCode.COMPILING_DECLARATION_NOT_FOUND);
                }
            }
        }
    }
}
