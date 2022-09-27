using RainScript.VirtualMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace RainScriptDebugger
{
    internal struct RKernenl
    {
        private readonly object kernel;
        public RManipulator manipulator { get { return new RManipulator(field_manipulator.GetValue(kernel)); } }
        public RStringAgency stringAgency { get { return new RStringAgency(field_stringAgency.GetValue(kernel)); } }
        public RHeapAgency heapAgency { get { return new RHeapAgency(field_heapAgency.GetValue(kernel)); } }
        public RCoroutineAgency coroutineAgency { get { return new RCoroutineAgency(field_coroutineAgency.GetValue(kernel)); } }
        public RLibraryAgency libraryAgency { get { return new RLibraryAgency(field_libraryAgency.GetValue(kernel)); } }
        public RKernenl(object kernel)
        {
            this.kernel = kernel;
        }
        public void Step(bool on)
        {
            field_step.SetValue(kernel, on);
        }
        private static readonly FieldInfo field_manipulator = typeof(Kernel).GetField("manipulator", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_stringAgency = typeof(Kernel).GetField("stringAgency", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_heapAgency = typeof(Kernel).GetField("heapAgency", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_coroutineAgency = typeof(Kernel).GetField("coroutineAgency", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_libraryAgency = typeof(Kernel).GetField("libraryAgency", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_step = typeof(Kernel).GetField("step", BindingFlags.NonPublic | BindingFlags.Instance);
    }
    internal struct REntity
    {
        private readonly object entity;
        public ulong value { get { return (ulong)field_value.GetValue(entity); } }
        public REntity(object entity)
        {
            this.entity = entity;
        }
        private static readonly Type type = Type.GetType("RainScript.Entity,RainScript");
        private static readonly FieldInfo field_value = type.GetField("entity", BindingFlags.Instance | BindingFlags.Public);
    }
    internal struct RManipulator
    {
        private readonly object manipulator;
        public RManipulator(object manipulator)
        {
            this.manipulator = manipulator;
        }
        public bool Valid(object entity)
        {
            return (bool)method_Valid.Invoke(manipulator, new object[] { entity });
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.EntityManipulator,RainScript");
        private static readonly MethodInfo method_Valid = type.GetMethod("Valid", BindingFlags.Instance | BindingFlags.Public);
    }
    internal struct RStringAgency
    {
        private readonly object agency;
        public RStringAgency(object agency)
        {
            this.agency = agency;
        }
        public string Get(uint id)
        {
            return (string)method_Get.Invoke(agency, new object[] { id });
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.StringAgency,RainScript");
        private static readonly MethodInfo method_Get = type.GetMethod("Get", BindingFlags.Instance | BindingFlags.Public);
    }
    internal unsafe struct RHeapAgency
    {
        private readonly object agency;
        private readonly DTryGetPoint _tryGetPoint;
        public RHeapAgency(object agency)
        {
            this.agency = agency;
            _tryGetPoint = Delegate.CreateDelegate(typeof(DTryGetPoint), method_TryGetPoint) as DTryGetPoint;
        }
        public ExitCode TryGetPoint(uint handle, out byte* point)
        {
            return _tryGetPoint(handle, out point);
        }
        public object GetType(uint handle)
        {
            return method_GetType.Invoke(agency, new object[] { handle });
        }
        public bool IsVaild(uint handle)
        {
            return (bool)method_IsVaild.Invoke(agency, new object[] { handle });
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.HeapAgency,RainScript");
        private static readonly MethodInfo method_TryGetPoint = type.GetMethod("TryGetPoint", BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo method_GetType = type.GetMethod("GetType", BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo method_IsVaild = type.GetMethod("IsVaild", BindingFlags.Instance | BindingFlags.Public);
        private delegate ExitCode DTryGetPoint(uint handle, out byte* point);
    }
    internal struct RCoroutineAgency
    {
        private readonly object agency;
        public RCoroutine invoking { get { return new RCoroutine(field_invoking.GetValue(agency)); } }
        public RCoroutineAgency(object agency)
        {
            this.agency = agency;
        }
        public IEnumerable<RCoroutine> Activities
        {
            get
            {
                var index = field_head.GetValue(agency);
                while (index != null)
                {
                    var result = new RCoroutine(index);
                    yield return result;
                    index = result.next;
                }
            }
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.CoroutineAgency,RainScript");
        private static readonly FieldInfo field_invoking = type.GetField("invoking", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo field_head = type.GetField("head", BindingFlags.NonPublic | BindingFlags.Instance);
    }
    internal unsafe struct RCoroutine
    {
        private readonly object coroutine;
        public object next { get { return field_next.GetValue(coroutine); } }
        public ulong id { get { return (ulong)field_instanceID.GetValue(coroutine); } }
        public byte* stack { get { return (byte*)Pointer.Unbox(field_stack.GetValue(coroutine)) + (uint)field_bottom.GetValue(coroutine); } }
        public uint point { get { return (uint)field_point.GetValue(coroutine); } }
        public RCoroutine(object coroutine)
        {
            this.coroutine = coroutine;
        }
        public StackFrame[] GetStackFrames()
        {
            return (StackFrame[])method_GetStackFrames.Invoke(coroutine, null);
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.Coroutine,RainScript");
        private static readonly FieldInfo field_next = type.GetField("next", BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo field_instanceID = type.GetField("instanceID", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo field_stack = type.GetField("stack", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo field_bottom = type.GetField("bottom", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo field_point = type.GetField("point", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo method_GetStackFrames = type.GetMethod("GetStackFrames", BindingFlags.Instance | BindingFlags.Public);
    }
    internal struct RLibraryAgency
    {
        private readonly object agency;
        public RLibrary this[uint index]
        {
            get
            {
                return new RLibrary(method_get_Item.Invoke(agency, new object[] { index }));
            }
        }
        public int Count { get { return ((IList)field_libraries.GetValue(agency)).Count; } }
        public RLibraryAgency(object agency)
        {
            this.agency = agency;
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.LibraryAgency,RainScript");
        private static readonly FieldInfo field_libraries = type.GetField("libraries", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo method_get_Item = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.NonPublic);
    }
    internal unsafe struct RLibrary
    {
        private readonly object library;
        public string name { get { return (string)field_name.GetValue(library); } }
        public byte* code { get { return (byte*)Pointer.Unbox(field_code.GetValue(library)); } }
        public byte* data { get { return (byte*)Pointer.Unbox(field_data.GetValue(library)); } }
        public RLibrary(object library)
        {
            this.library = library;
        }
        private static readonly Type type = Type.GetType("RainScript.VirtualMachine.RuntimeLibraryInfo,RainScript");
        private static readonly FieldInfo field_code = type.GetField("code", BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo field_data = type.GetField("data", BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo field_name = type.GetField("name", BindingFlags.Instance | BindingFlags.Public);
    }
}
