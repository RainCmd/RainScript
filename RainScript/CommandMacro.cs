﻿namespace RainScript
{
    internal enum CommandMacro : byte
    {
        #region base
        BASE_Exit,
        BASE_Finally,
        BASE_ExitJump,
        BASE_Wait,
        BASE_WaitFrame,
        BASE_Stackzero,
        BASE_Jump,
        BASE_ConditionJump,
        BASE_NullJump,

        BASE_Flag_1,
        BASE_Flag_8,

        BASE_CreateObject,
        BASE_CreateDelegate,
        BASE_CreateCoroutine,
        BASE_CreateDelegateCoroutine,
        BASE_CreateArray,

        BASE_SetCoroutineParameter,
        BASE_GetCoroutineResult,
        BASE_CoroutineStart,
        #endregion base

        #region function
        FUNCTION_Entrance,
        FUNCTION_Ensure,
        FUNCTION_CustomCallPretreater,
        FUNCTION_PushReturnPoint,

        FUNCTION_PushParameter_1,
        FUNCTION_PushParameter_4,
        FUNCTION_PushParameter_8,
        FUNCTION_PushParameter_16,
        FUNCTION_PushParameter_24,
        FUNCTION_PushParameter_32,
        FUNCTION_PushParameter_String,
        FUNCTION_PushParameter_Handle,
        FUNCTION_PushParameter_Entity,

        FUNCTION_ReturnPoint_1,
        FUNCTION_ReturnPoint_4,
        FUNCTION_ReturnPoint_8,
        FUNCTION_ReturnPoint_16,
        FUNCTION_ReturnPoint_24,
        FUNCTION_ReturnPoint_32,
        FUNCTION_ReturnPoint_String,
        FUNCTION_ReturnPoint_Handle,
        FUNCTION_ReturnPoint_Entity,
        FUNCTION_Return,

        FUNCTION_Call,
        FUNCTION_MemberCall,
        FUNCTION_MemberVirtualCall,
        FUNCTION_InterfaceCall,
        FUNCTION_CustomCall,
        FUNCTION_NativeCall,
        FUNCTION_KernelCall,
        FUNCTION_KernelMemberCall,
        #endregion function

        #region assignment
        ASSIGNMENT_Const2Local_1,
        ASSIGNMENT_Const2Local_4,
        ASSIGNMENT_Const2Local_8,
        ASSIGNMENT_Const2Local_16,
        ASSIGNMENT_Const2Local_24,
        ASSIGNMENT_Const2Local_32,
        ASSIGNMENT_Const2Local_String,
        ASSIGNMENT_Const2Local_HandleNull,
        ASSIGNMENT_Const2Local_EntityNull,
        ASSIGNMENT_Const2Local_Vector,
        ASSIGNMENT_Local2Local_1,
        ASSIGNMENT_Local2Local_4,
        ASSIGNMENT_Local2Local_8,
        ASSIGNMENT_Local2Local_16,
        ASSIGNMENT_Local2Local_24,
        ASSIGNMENT_Local2Local_32,
        ASSIGNMENT_Local2Local_String,
        ASSIGNMENT_Local2Local_Handle,
        ASSIGNMENT_Local2Local_Entity,
        ASSIGNMENT_Local2Local_Vector,
        ASSIGNMENT_Local2Global_1,
        ASSIGNMENT_Local2Global_4,
        ASSIGNMENT_Local2Global_8,
        ASSIGNMENT_Local2Global_16,
        ASSIGNMENT_Local2Global_24,
        ASSIGNMENT_Local2Global_32,
        ASSIGNMENT_Local2Global_String,
        ASSIGNMENT_Local2Global_Handle,
        ASSIGNMENT_Local2Global_Entity,
        ASSIGNMENT_Local2Handle_1,
        ASSIGNMENT_Local2Handle_4,
        ASSIGNMENT_Local2Handle_8,
        ASSIGNMENT_Local2Handle_16,
        ASSIGNMENT_Local2Handle_24,
        ASSIGNMENT_Local2Handle_32,
        ASSIGNMENT_Local2Handle_String,
        ASSIGNMENT_Local2Handle_Handle,
        ASSIGNMENT_Local2Handle_Entity,
        ASSIGNMENT_Local2Array_1,
        ASSIGNMENT_Local2Array_4,
        ASSIGNMENT_Local2Array_8,
        ASSIGNMENT_Local2Array_16,
        ASSIGNMENT_Local2Array_24,
        ASSIGNMENT_Local2Array_32,
        ASSIGNMENT_Local2Array_String,
        ASSIGNMENT_Local2Array_Handle,
        ASSIGNMENT_Local2Array_Entity,
        ASSIGNMENT_Global2Local_1,
        ASSIGNMENT_Global2Local_4,
        ASSIGNMENT_Global2Local_8,
        ASSIGNMENT_Global2Local_16,
        ASSIGNMENT_Global2Local_24,
        ASSIGNMENT_Global2Local_32,
        ASSIGNMENT_Global2Local_String,
        ASSIGNMENT_Global2Local_Handle,
        ASSIGNMENT_Global2Local_Entity,
        ASSIGNMENT_Handle2Local_1,
        ASSIGNMENT_Handle2Local_4,
        ASSIGNMENT_Handle2Local_8,
        ASSIGNMENT_Handle2Local_16,
        ASSIGNMENT_Handle2Local_24,
        ASSIGNMENT_Handle2Local_32,
        ASSIGNMENT_Handle2Local_String,
        ASSIGNMENT_Handle2Local_Handle,
        ASSIGNMENT_Handle2Local_Entity,
        ASSIGNMENT_Array2Local_1,
        ASSIGNMENT_Array2Local_4,
        ASSIGNMENT_Array2Local_8,
        ASSIGNMENT_Array2Local_16,
        ASSIGNMENT_Array2Local_24,
        ASSIGNMENT_Array2Local_32,
        ASSIGNMENT_Array2Local_String,
        ASSIGNMENT_Array2Local_Handle,
        ASSIGNMENT_Array2Local_Entity,
        #endregion assignment

        #region bool
        BOOL_Not,
        BOOL_Or,
        BOOL_Xor,
        BOOL_And,
        BOOL_Equals,
        BOOL_NotEquals,
        #endregion bool

        #region integer
        INTEGER_Negative,
        INTEGER_Plus,
        INTEGER_Minus,
        INTEGER_Multiply,
        INTEGER_Divide,
        INTEGER_Mod,
        INTEGER_And,
        INTEGER_Or,
        INTEGER_Xor,
        INTEGER_Inverse,
        INTEGER_Equals,
        INTEGER_NotEquals,
        INTEGER_Grater,
        INTEGER_GraterThanOrEquals,
        INTEGER_Less,
        INTEGER_LessThanOrEquals,
        INTEGER_LeftShift,
        INTEGER_RightShift,
        INTEGER_Increment,
        INTEGER_Decrement,
        #endregion integer

        #region real
        REAL_Negative,
        REAL_Plus,
        REAL_Minus,
        REAL_Multiply,
        REAL_Divide,
        REAL_Mod,
        REAL_Equals,
        REAL_NotEquals,
        REAL_Grater,
        REAL_GraterThanOrEquals,
        REAL_Less,
        REAL_LessThanOrEquals,
        REAL_Increment,
        REAL_Decrement,
        #endregion real

        #region real2
        REAL2_Negative,
        REAL2_Plus,
        REAL2_Minus,
        REAL2_Multiply_rv,
        REAL2_Multiply_vr,
        REAL2_Multiply_vv,
        REAL2_Divide_rv,
        REAL2_Divide_vr,
        REAL2_Divide_vv,
        REAL2_Mod_rv,
        REAL2_Mod_vr,
        REAL2_Mod_vv,
        REAL2_Equals,
        REAL2_NotEquals,
        #endregion real2

        #region real3
        REAL3_Negative,
        REAL3_Plus,
        REAL3_Minus,
        REAL3_Multiply_rv,
        REAL3_Multiply_vr,
        REAL3_Multiply_vv,
        REAL3_Divide_rv,
        REAL3_Divide_vr,
        REAL3_Divide_vv,
        REAL3_Mod_rv,
        REAL3_Mod_vr,
        REAL3_Mod_vv,
        REAL3_Equals,
        REAL3_NotEquals,
        #endregion real3

        #region real4
        REAL4_Negative,
        REAL4_Plus,
        REAL4_Minus,
        REAL4_Multiply_rv,
        REAL4_Multiply_vr,
        REAL4_Multiply_vv,
        REAL4_Divide_rv,
        REAL4_Divide_vr,
        REAL4_Divide_vv,
        REAL4_Mod_rv,
        REAL4_Mod_vr,
        REAL4_Mod_vv,
        REAL4_Equals,
        REAL4_NotEquals,
        #endregion real4

        STRING_Release,
        STRING_Element,
        STRING_Combine,
        STRING_Sub,
        STRING_Equals,
        STRING_NotEquals,

        HANDLE_ArrayCut,
        HANDLE_CheckNull,
        HANDLE_Equals,
        HANDLE_NotEquals,

        ENTITY_Equals,
        ENTITY_NotEquals,

        DELEGATE_Equals,
        DELEGATE_NotEquals,

        #region casting
        CASTING,
        CASTING_IS,
        CASTING_AS,
        CASTING_R2I,
        CASTING_I2R,
        #endregion casting
    }
}
