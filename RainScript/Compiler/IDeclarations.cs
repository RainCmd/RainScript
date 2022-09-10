using System.Collections.Generic;

namespace RainScript.Compiler
{
    internal interface ISpace
    {
        string Name { get; }
        ISpace Parent { get; }
        /// <summary>
        /// 从当前空间开始向上查找所有父空间的子空间
        /// </summary>
        bool TryFindSpace(StringSegment name, out ISpace space);
        /// <summary>
        /// 查找当前空间的直接子空间
        /// </summary>
        bool TryFindChild(StringSegment name, out ISpace child);
        /// <summary>
        /// 只在当前空间中的定义里查找
        /// </summary>
        bool TryFindDeclaration(StringSegment name, out Declaration declaration);
        /// <summary>
        /// 目标空间是当前空间的子集
        /// </summary>
        bool Contain(ISpace space);
    }
    internal interface IDeclaramtion
    {
        Declaration Declaration { get; }
        ISpace Space { get; }
        string Name { get; }
    }
    internal interface IMethod
    {
        Declaration Declaration { get; }
        int FunctionCount { get; }
        string Name { get; }
        IFunction GetFunction(int index);
    }
    internal interface IFunction : IDeclaramtion
    {
        CompilingType[] Parameters { get; }
        CompilingType[] Returns { get; }
    }
    internal interface IInterface : IDeclaramtion
    {
        IList<CompilingDefinition> Inherits { get; }
        int MethodCount { get; }
        IMethod GetMethod(int index);
    }
    internal interface IMemberVariable
    {
        string Name { get; }
        CompilingType Type { get; }
        Declaration Declaration { get; }
    }
    internal interface IDefinition : IInterface
    {
        CompilingDefinition Parent { get; }
        uint Constructor { get; }
        int MemberVaribaleCount { get; }
        IMemberVariable GetMemberVariable(int index);
    }
    internal static class IDeclarationExtension
    {
        private static readonly System.Text.StringBuilder builder = new System.Text.StringBuilder();
        public static string GetFullName(this ISpace space)
        {
            builder.Length = 0;
            builder.Append(space.Name);
            while (space.Parent != null)
            {
                space = space.Parent;
                builder.Insert(0, '.');
                builder.Insert(0, space.Name);
            }
            return builder.ToString();
        }
    }
}
