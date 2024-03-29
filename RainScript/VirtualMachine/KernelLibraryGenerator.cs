﻿using System.Collections.Generic;
using System.IO;
using RainScript.VirtualMachine;
#if FIXED
using real = RainScript.Real.Fixed;
#else
using real = System.Double;
#endif

namespace RainScript
{
    public partial class Library
    {
        private Library(byte[] code, byte[] data, DefinitionInfo[] definitions, VariableInfo[] variables, MethodInfo[] methodInfos, ExportDefinition[] exportDefinitions, ExportIndex[] exportVariables, ExportMethod[] exportMethods) : base(KeyWord.KERNEL, new Space[0], exportDefinitions, exportVariables, new ExportIndex[0], new ExportIndex[0], exportMethods, new ExportInterface[0], new ExportMethod[0])
        {
            this.code = code;
            constantData = data;
            dataSize = (uint)data.Length;
            this.definitions = definitions;
            this.variables = variables;
            delegates = new FunctionInfo[0];
            coroutines = new CoroutineInfo[0];
            methods = methodInfos;
            interfaces = new InterfaceInfo[0];
            natives = new NativeMethodInfo[0];
            imports = new ImportLibraryInfo[0];
            strings = new string[0];
            dataStrings = new Dictionary<string, uint[]>();
        }
        internal static readonly Library kernel;
        private static DefinitionInfo CreateKernelDefinitionInfo(TypeDefinition parent, uint memberMethodStartIndex, uint memberMethodCount)
        {
            var memberMethods = new uint[memberMethodCount];
            while (memberMethodCount > 0) memberMethods[--memberMethodCount] = memberMethodStartIndex++;
            return new DefinitionInfo(parent, new TypeDefinition[0], new Type[0], memberMethods, new Relocation[0], LIBRARY.ENTRY_INVALID);
        }
        private unsafe static VariableInfo CreateKernelVariablleInfo(Stream stream, Type type, real value)
        {
#if MEMORY_ALIGNMENT_4
            if (type == KERNEL_TYPE.REAL || type == KERNEL_TYPE.REAL2 || type == KERNEL_TYPE.REAL3 || type == KERNEL_TYPE.REAL4)
            {
                var position = (uint)stream.Position;
                Tools.MemoryAlignment(ref position);
                while (position > stream.Position)
                    stream.WriteByte(0);
            }
#endif
            var result = new VariableInfo((uint)stream.Position, type);
            stream.Write(*(ulong*)&value);
            return result;
        }
        private static ExportDefinition CreateKernelExportDefinition(string name, uint index, DefinitionInfo info)
        {
            var exportMethods = new ExportMethod[info.methods.Length];
            for (uint memberMethodIndex = 0; memberMethodIndex < info.methods.Length; memberMethodIndex++)
            {
                var method = KernelMethod.memberMethods[info.methods[memberMethodIndex]];
                var functions = new uint[method.functions.Length];
                for (uint i = 0; i < functions.Length; i++) functions[i] = i;
                exportMethods[memberMethodIndex] = new ExportMethod(method.name, memberMethodIndex, functions);
            }
            return new ExportDefinition(name, index, new ExportIndex[0], exportMethods);
        }
        static Library()
        {
            var code = new MemoryStream();
            var data = new MemoryStream();
            var definitions = new DefinitionInfo[]
             {
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 0, 0),//TypeCode.Invalid,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 0, 1),//TypeCode.Bool,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 1, 1),//TypeCode.Byte,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 2, 1),//TypeCode.Integer,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 3, 1),//TypeCode.Real,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 4, 3),//TypeCode.Real2,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 7, 3),//TypeCode.Real3,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 10, 3),//TypeCode.Real4,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 13, 5),//TypeCode.String,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 18, 2),//TypeCode.Handle,
                CreateKernelDefinitionInfo(KERNEL_TYPE.HANDLE.definition, 20, 0),//TypeCode.Interface,
                CreateKernelDefinitionInfo(KERNEL_TYPE.HANDLE.definition, 20, 0),//TypeCode.Function,
                CreateKernelDefinitionInfo(KERNEL_TYPE.HANDLE.definition, 20, 7),//TypeCode.Coroutine,
                CreateKernelDefinitionInfo(TypeDefinition.INVALID, 27, 1),//TypeCode.Entity,
                CreateKernelDefinitionInfo(KERNEL_TYPE.HANDLE.definition, 28, 1),//数组
             };
            var variables = new VariableInfo[KernelConstant.constants.Length];
            for (int i = 0; i < variables.Length; i++)
                variables[i] = CreateKernelVariablleInfo(data, KernelConstant.constants[i].type, KernelConstant.constants[i].value);

            var methodInfos = new MethodInfo[KernelMethod.memberMethods.Length + KernelMethod.methods.Length];
            var methodIndex = 0;
            for (int definitionIndex = 0, index = 0; definitionIndex < definitions.Length; definitionIndex++)
                for (int i = 0, length = definitions[definitionIndex].methods.Length; i < length; i++, index++)
                    methodInfos[methodIndex++] = KernelMemberMethodInvoker.methods[index].CreateMethodInfo(code, KERNEL_TYPE.GetType(definitionIndex), (uint)index);

            for (var i = 0u; i < KernelMethod.methods.Length; i++) methodInfos[methodIndex++] = KernelMethodInvoker.methods[i].CreateMethodInfo(code, i);

            var exportDefinitions = new ExportDefinition[definitions.Length];
            for (var i = 0; i < exportDefinitions.Length; i++) exportDefinitions[i] = CreateKernelExportDefinition(KERNEL_TYPE.GetName(i), (uint)i, definitions[i]);

            var exportVariables = new ExportIndex[variables.Length];
            for (uint i = 0; i < exportVariables.Length; i++) exportVariables[i] = new ExportIndex(KernelConstant.constants[i].name, i);

            var exportMethods = new ExportMethod[KernelMethod.methods.Length];
            for (uint i = 0; i < exportMethods.Length; i++)
            {
                var functions = new uint[KernelMethod.methods[i].functions.Length];
                for (uint index = 0; index < functions.Length; index++) functions[index] = index;
                exportMethods[i] = new ExportMethod(KernelMethod.methods[i].name, (uint)KernelMethod.memberMethods.Length + i, functions);
            }
            kernel = new Library(code.ToArray(), data.ToArray(), definitions, variables, methodInfos, exportDefinitions, exportVariables, exportMethods);
        }
    }
}
