using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Scripting
{
/*
    JsErrorInvalidArgument          = ArgumentException
    JsErrorNullArgument             = ArgumentException
    JsErrorNoCurrentContext         = JavaScriptNoContextException
    JsErrorInExceptionState         = InvalidOperationException 
    JsErrorNotImplemented           = NotImplementedException
    JsErrorWrongThread              ERROR_WINDOW_OF_OTHER_THREAD, ERROR_WRONG_THREAD
    JsErrorRuntimeInUse             E_UNEXPECTED, ERROR_RUNTIME_IN_USE
    JsErrorBadSerializedScript      = ArgumentException
    JsErrorInDisabledState          = InvalidOperationException
    JsErrorCannotDisableExecution   = InvalidOperationException
    JsErrorHeapEnumInProgress       Should not happen, we don't support currently
    JsErrorArgumentNotObject        = ArgumentOutOfRangeException
    JsErrorInProfileCallback        Should not happen, we don't support
    JsErrorInThreadServiceCallback  Should not happen, we don't support
    JsErrorCannotSerializeDebugScr  Should not happen, we don't support
    JsErrorAlreadyDebuggingContext  = InvalidOperationException
    JsErrorAlreadyProfilingContext  Should not happen, we don't support
    JsErrorIdleNotEnabled           = InvalidOperationException, ERROR_CONFIG_ERROR
    JsCannotSetProjectionEnqueueCa  Not supported in desktop apps
    JsErrorCannotStartProjection    = NotSupportedException
    JsErrorInObjectBeforeCollectCa  ...Do we support this callback?
    JsErrorObjectNotInspectable     Not supported in desktop apps
    JsErrorPropertyNotSymbol        = ArgumentOutOfRangeException, ERROR_PROPERTY_NOT_SYMBOL
    JsErrorPropertyNotString        = ArgumentOutOfRangeException, ERROR_PROPERTY_NOT_STRING

    JsErrorOutOfMemory              OutOfMemoryException

    JsErrorScriptException          Turn into success case
    JsErrorScriptCompile            = ArgumentException, ERROR_COMPILATION_FAILED
    JsErrorScriptTerminated         Turn into success case
    JsErrorScriptEvalDisabled       = InvalidOperationException, ERROR_SCRIPT_ATTEMPTED_EVAL

    JsErrorFatal                    Exception
*/
    internal static class Errors
    {
        public const string NoMutateJsRuntimeSettings = "Can't change JavaScriptRuntimeSettings once it has been used to create a runtime.";
        public const string DefaultFnOverwritten = "The built-in function '{0}' has been overwritten and is no longer a function.";

        public const string ERROR_ENGINE_IN_EXCEPTION_STATE = "The script engine is in an exception state, and additional progress may not be made without clearing the exception.";
        public const string ERROR_WRONG_THREAD = "Could not acquire the runtime host on the current thread.";
        public const string ERROR_RUNTIME_IN_USE = "A runtime that is still in use cannot be disposed.";
        public const string ERROR_BAD_SERIALIZED_SCRIPT = "The serialized script is corrupt or incompatible with the current version.";
        public const string ERROR_DISABLED = "The runtime is disabled.";
        public const string ERROR_CONFIG_ERROR = "The runtime settings provided at initialization prevent the requested operation.";
        public const string ERROR_NOT_OBJECT = "An operation expected an object parameter but was provided a non-object value.";
        public const string ERROR_PROJECTION_NOT_STARTED = "The Windows Runtime projection could not be initialized.";
        public const string ERROR_ARG_NOT_INSPECTABLE = "Object cannot be projected into the script engine because it isn't a Windows Runtime object.  Windows Runtime objects derive from IInspectable.";
        public const string ERROR_PROPERTY_NOT_SYMBOL = "Attempted to get a Symbol for a property name that is actually a string.";
        public const string ERROR_PROPERTY_NOT_STRING = "Attempted to get a property name that is actually a Symbol.";
        public const string ERROR_COMPILATION_FAILED = "A script failed to compile, probably due to a syntax error.";
        public const string ERROR_SCRIPT_ATTEMPTED_EVAL = "A script was terminated because it tried to use eval or Function() and eval was disabled.";
        public const string ERROR_ALREADY_DEBUGGING = "The script engine was already in debugging mode.";
        public const string ERROR_CANNOT_SERIALIZE_DEBUG_SCRIPT = "Can't serialize script while in debugging mode.";

        private static readonly Dictionary<JsErrorCode, Action> ErrorMap = new Dictionary<JsErrorCode, Action>()
        {
            { JsErrorCode.JsCannotSetProjectionEnqueueCallback, () => { Debug.Assert(false, "Should not occur, we don't support."); throw new Exception(); } },
            { JsErrorCode.JsErrorAlreadyDebuggingContext, () => { throw new InvalidOperationException(ERROR_ALREADY_DEBUGGING); } },
            { JsErrorCode.JsErrorAlreadyProfilingContext, () => { Debug.Assert(false, "Should not occur, we don't support."); throw new Exception(); } },
            { JsErrorCode.JsErrorArgumentNotObject, () => { throw new ArgumentException(ERROR_NOT_OBJECT); } },
            { JsErrorCode.JsErrorBadSerializedScript, () => { throw new ArgumentException(ERROR_BAD_SERIALIZED_SCRIPT); } },
            { JsErrorCode.JsErrorCannotDisableExecution, () => { throw new InvalidOperationException(); } },
            { JsErrorCode.JsErrorCannotSerializeDebugScript, () => { throw new InvalidOperationException(ERROR_CANNOT_SERIALIZE_DEBUG_SCRIPT); } },
            { JsErrorCode.JsErrorCannotStartProjection, () => { Debug.Assert(false, "Should not occur, we don't support."); throw new InvalidOperationException(); } },
            { JsErrorCode.JsErrorFatal, () => { throw new Exception("An unknown error occurred in the script engine."); } },
            { JsErrorCode.JsErrorHeapEnumInProgress, () => { Debug.Assert(false, "Should not occur, we don't support."); throw new Exception(); } },
            { JsErrorCode.JsErrorIdleNotEnabled, () => { throw new InvalidOperationException(ERROR_CONFIG_ERROR); } },
        };

        public static void ThrowFor(JsErrorCode errorCode)
        {
            Debug.Assert(errorCode != JsErrorCode.JsNoError);

            Action throwAction;
            if (!ErrorMap.TryGetValue(errorCode, out throwAction))
            {
                throwAction = () => { throw new Exception(errorCode.ToString()); };
            }

            throwAction();
        }

        public static void ThrowIfIs(JsErrorCode errorCode)
        {
            if (errorCode != JsErrorCode.JsNoError)
                ThrowFor(errorCode);
        }

        public static void ThrowIOEFmt(string formatStr, string param)
        {
            string result = string.Format(formatStr, param);
            throw new InvalidOperationException(result);
        }
    }
}
