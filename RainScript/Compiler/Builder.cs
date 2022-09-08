using System;
using System.Collections.Generic;

namespace RainScript.Compiler
{
    /// <summary>
    /// 编译状态
    /// </summary>
    public enum CompileState
    {
        /// <summary>
        /// 未开始
        /// </summary>
        Unstart,
        /// <summary>
        /// 正在编译
        /// </summary>
        Compiling,
        /// <summary>
        /// 已失败
        /// </summary>
        Failed,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 编译器内部发生异常
        /// </summary>
        Accident,
    }
    /// <summary>
    /// 文件信息
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        /// 路径
        /// </summary>
        string Path { get; }
        /// <summary>
        /// 内容
        /// </summary>
        string Context { get; }
    }
    /// <summary>
    /// 编译命令选项
    /// </summary>
    public struct CompilerCommand
    {
        /// <summary>
        /// 生成符号表
        /// </summary>
        public readonly bool generatorSymbolTable;
        /// <summary>
        /// 忽略Exit功能
        /// </summary>
        public readonly bool ignoreExit;
    }
    /// <summary>
    /// 编译器
    /// </summary>
    public class Builder
    {
        /// <summary>
        /// 当前编译状态
        /// </summary>
        public CompileState State { get; private set; }
        /// <summary>
        /// 库名
        /// </summary>
        public readonly string name;
        private readonly CollectionPool pool = new CollectionPool();
        /// <summary>
        /// 编译异常
        /// </summary>
        public readonly ExceptionCollector exceptions = new ExceptionCollector();
        private readonly List<TextInfo> textInfos = new List<TextInfo>();
        private readonly RelyLibrary[] relies;
        /// <summary>
        /// 编译完成后的库
        /// </summary>
        public Library Library { get; private set; }
        /// <summary>
        /// 给其他库引用的信息
        /// </summary>
        public ReferenceLibrary ReferenceLibrary { get; private set; }
        /// <summary>
        /// 编译器
        /// </summary>
        /// <param name="name">目标库名</param>
        /// <param name="files">文件集</param>
        /// <param name="references">引用集</param>
        public Builder(string name, IEnumerable<IFileInfo> files, IEnumerable<ReferenceLibrary> references)
        {
            this.name = name;
            foreach (var item in files) textInfos.Add(new TextInfo(item.Path, item.Context));
            relies = InitRelies(references);

            State = CompileState.Unstart;
        }
        /// <summary>
        /// 开始编译
        /// </summary>
        /// <param name="command">编译参数</param>
        public void Compile(CompilerCommand command)
        {
            if (State == CompileState.Compiling) throw ExceptionGeneratorCompiler.InvalidCompilingState(State);
            State = CompileState.Compiling;
            pool.Clear();
            exceptions.Clear();
            try
            {
                using (var manager = new DeclarationManager(name, relies, pool))
                {
                    using (var fileSpaces = pool.GetList<File.Space>())
                    {
                        foreach (var item in textInfos) fileSpaces.Add(new File.Space(manager.library, item, pool, exceptions));
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionParseFail();

                        foreach (var item in fileSpaces) item.Tidy(manager, pool, exceptions);
                        //todo 检查类型的接口是否全部实现过
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionTidyFail();

                        foreach (var item in fileSpaces) item.Link(manager, pool, exceptions);
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionLinkFail();

                        foreach (var item in fileSpaces) item.Dispose();
                    }
                    manager.library.DeclarationValidityCheck(pool, exceptions);
                    if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionIllegal();

                    using (var referenceGenerator = new ReferenceGenerator.Library(manager, pool, exceptions))
                    {
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionReferenceGeneratorFail();
                        ReferenceLibrary = referenceGenerator.GeneratorLibrary(pool);
                    }

                    manager.library.CalculatedVariableAddress();

                    using (var libraryGenerator = new LogicGenerator.Generator(manager.library.DataSize, pool))
                    {
                        var library = libraryGenerator.GeneratorLibrary(new LogicGenerator.GeneratorParameter(command, manager, pool, exceptions));
                        if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DeclaractionLibraryGeneratorFail();
                        Library = library;
                    }
                }
                State = CompileState.Completed;
            }
            catch (ExpectedException)
            {
                State = CompileState.Failed;
                throw;
            }
            catch (Exception)
            {
                State = CompileState.Accident;
                throw;
            }
        }
        private RelyLibrary[] InitRelies(IEnumerable<ReferenceLibrary> sourceReferences)
        {
            using (var references = pool.GetDictionary<string, ReferenceLibrary>())
            {
                foreach (var reference in sourceReferences)
                {
                    if (references.ContainsKey(reference.name)) exceptions.Add(CompilingExceptionCode.COMPILING_DUPLICATE_LIBRARY_NAMES, reference.name);
                    else references.Add(reference.name, reference);
                }
                if (references.ContainsKey(name)) exceptions.Add(CompilingExceptionCode.COMPILING_DUPLICATE_LIBRARY_NAMES, name);
                if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.DuplicateLibraryNames();
                using (var nameSet = pool.GetSet<string>())
                using (var checkedSet = pool.GetSet<ReferenceLibrary>())
                    foreach (var reference in sourceReferences)
                        CheckCircularReference(reference, references, nameSet, checkedSet);
                if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.CircularRely();
            }
            using (var references = pool.GetList<ReferenceLibrary>())
            {
                references.AddRange(sourceReferences);
                using (var completedRelies = pool.GetSet<string>())
                using (var relies = pool.GetList<RelyLibrary>())
                {
                    while (references.Count > 0)
                    {
                        var count = references.Count;
                        for (int i = 0; i < references.Count; i++)
                        {
                            var reference = references[i];
                            foreach (var rely in reference.relies) if (!completedRelies.Contains(rely.name)) goto next;
                            completedRelies.Add(reference.name);
                            references.RemoveAt(i--);
                            relies.Add(new RelyLibrary(reference, relies, pool, exceptions));
                        next:;
                        }
                        if (count == references.Count) throw ExceptionGeneratorCompiler.UnknownRelyError();
                    }
                    if (exceptions.Count > 0) throw ExceptionGeneratorCompiler.RelyInitFail();
                    return relies.ToArray();
                }

            }
        }
        private void CheckCircularReference(ReferenceLibrary library, Dictionary<string, ReferenceLibrary> relies, HashSet<string> nameSet, HashSet<ReferenceLibrary> checkedSet)
        {
            if (nameSet.Add(library.name))
            {
                if (checkedSet.Add(library))
                {
                    foreach (var item in library.relies)
                        if (relies.TryGetValue(item.name, out ReferenceLibrary reference)) CheckCircularReference(reference, relies, nameSet, checkedSet);
                        else exceptions.Add(CompilingExceptionCode.COMPILING_LIBRARY_NOT_FOUND, item.name);
                }
                nameSet.Remove(library.name);
            }
            else
            {
                var nameList = name;
                foreach (var item in nameSet) nameList += " -> " + item;
                exceptions.Add(CompilingExceptionCode.COMPILING_CIRCULAR_RELY, nameList);
            }
        }
    }
}
