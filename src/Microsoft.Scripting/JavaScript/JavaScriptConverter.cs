using Microsoft.Scripting.JavaScript.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Scripting.JavaScript
{
    public sealed class JavaScriptConverter
    {
        private class JavaScriptProjection
        {
            public volatile int RefCount;
            public JavaScriptObject Prototype;
        }

        private WeakReference<JavaScriptEngine> engine_;
        private ChakraApi api_;
        private Dictionary<Type, JavaScriptProjection> projectionTypes_;

        public JavaScriptConverter(JavaScriptEngine engine)
        {
            engine_ = new WeakReference<JavaScriptEngine>(engine);
            api_ = engine.Api;
            projectionTypes_ = new Dictionary<Type, JavaScriptProjection>();
        }

        private JavaScriptEngine GetEngine()
        {
            JavaScriptEngine result;
            if (!engine_.TryGetTarget(out result))
                throw new ObjectDisposedException(nameof(JavaScriptEngine));

            return result;
        }

        private JavaScriptEngine GetEngineAndClaimContext()
        {
            var result = GetEngine();
            result.ClaimContext();

            return result;
        }

        public bool ToBoolean(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            bool result;
            
            if (value.Type == JavaScriptValueType.Boolean)
            {
                Errors.ThrowIfIs(api_.JsBooleanToBool(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempBool;
                Errors.ThrowIfIs(api_.JsConvertValueToBoolean(value.handle_, out tempBool));
                using (tempBool)
                {
                    Errors.ThrowIfIs(api_.JsBooleanToBool(tempBool, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromBoolean(bool value)
        {
            var eng = GetEngine();
            if (value)
                return eng.TrueValue;

            return eng.FalseValue;
        }

        public double ToDouble(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            double result;

            if (value.Type == JavaScriptValueType.Number)
            {
                Errors.ThrowIfIs(api_.JsNumberToDouble(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempVal;
                Errors.ThrowIfIs(api_.JsConvertValueToNumber(value.handle_, out tempVal));
                using (tempVal)
                {
                    Errors.ThrowIfIs(api_.JsNumberToDouble(tempVal, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromDouble(double value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            Errors.ThrowIfIs(api_.JsDoubleToNumber(value, out result));

            return eng.CreateValueFromHandle(result);
        }

        public int ToInt32(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();

            int result;

            if (value.Type == JavaScriptValueType.Number)
            {
                Errors.ThrowIfIs(api_.JsNumberToInt(value.handle_, out result));
            }
            else
            {
                JavaScriptValueSafeHandle tempVal;
                Errors.ThrowIfIs(api_.JsConvertValueToNumber(value.handle_, out tempVal));
                using (tempVal)
                {
                    Errors.ThrowIfIs(api_.JsNumberToInt(tempVal, out result));
                }
            }

            return result;
        }

        public JavaScriptValue FromInt32(int value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            Errors.ThrowIfIs(api_.JsIntToNumber(value, out result));

            return eng.CreateValueFromHandle(result);
        }

        public unsafe string ToString(JavaScriptValue value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var eng = GetEngineAndClaimContext();
            if (value.Type == JavaScriptValueType.String)
            {
                void* str;
                uint len;
                Errors.ThrowIfIs(api_.JsStringToPointer(value.handle_, out str, out len));
                if (len > int.MaxValue)
                    throw new OutOfMemoryException("Exceeded maximum string length.");

                return Marshal.PtrToStringUni(new IntPtr(str), unchecked((int)len));
            }
            else if (value.Type == JavaScriptValueType.Symbol)
            {
                // Presently, JsRT doesn't have a way for the host to query the description of a Symbol
                // Using JsConvertValueToString resulted in putting the runtime into an exception state
                // Thus, detect the condition and just return a known string.

                return "(Symbol)";
            }
            else
            {
                JavaScriptValueSafeHandle tempStr;
                Errors.ThrowIfIs(api_.JsConvertValueToString(value.handle_, out tempStr));
                using (tempStr)
                {
                    void* str;
                    uint len;
                    Errors.ThrowIfIs(api_.JsStringToPointer(value.handle_, out str, out len));
                    if (len > int.MaxValue)
                        throw new OutOfMemoryException("Exceeded maximum string length.");

                    return Marshal.PtrToStringUni(new IntPtr(str), unchecked((int)len));
                }
            }
        }

        public unsafe JavaScriptValue FromString(string value)
        {
            var eng = GetEngineAndClaimContext();

            JavaScriptValueSafeHandle result;
            var encoded = Encoding.Unicode.GetBytes(value);
            fixed (byte* ptr = &encoded[0])
            {
                Errors.ThrowIfIs(api_.JsPointerToString(ptr, value.Length, out result));
            }

            return eng.CreateValueFromHandle(result);
        }

        public JavaScriptValue FromObject(object o)
        {
            var eng = GetEngine();
            if (o == null)
            {
                return eng.NullValue;
            }

            var jsVal = o as JavaScriptValue;
            if (jsVal != null)
                return jsVal;

            Type t = o.GetType();
            if (t == typeof(string))
            {
                return FromString((string)o);
            }
            else if (t == typeof(double) || t == typeof(float))
            {
                return FromDouble((double)o);
            }
            else if (t == typeof(int) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte))
            {
                return FromInt32((int)o);
            }
            else if (t == typeof(uint))
            {
                return FromDouble((uint)o);
            }
            else if (t == typeof(long))
            {
                return FromDouble((long)o);
            }
            else if (t == typeof(bool))
            {
                bool b = (bool)o;
                return b ? eng.TrueValue : eng.FalseValue;
            }
            else
            {
                var result = InitializeProjectionForObject(o);
                return result;
            }
        }

        public object ToObject(JavaScriptValue val)
        {
            switch (val.Type)
            {
                case JavaScriptValueType.Boolean:
                    return val.IsTruthy;
                case JavaScriptValueType.Number:
                    return ToDouble(val);
                case JavaScriptValueType.String:
                    return ToString(val);
                case JavaScriptValueType.Undefined:
                    return null;
                case JavaScriptValueType.Array:
                    JavaScriptArray arr = val as JavaScriptArray;
                    Debug.Assert(arr != null);

                    return arr.Select(v => ToObject(v)).ToArray();
                case JavaScriptValueType.ArrayBuffer:
                    var ab = val as JavaScriptArrayBuffer;
                    Debug.Assert(ab != null);

                    return ab.GetUnderlyingMemory();

                case JavaScriptValueType.DataView:
                    var dv = val as JavaScriptDataView;
                    Debug.Assert(dv != null);

                    return dv.GetUnderlyingMemory();

                case JavaScriptValueType.TypedArray:
                    var ta = val as JavaScriptTypedArray;
                    Debug.Assert(ta != null);

                    return ta.GetUnderlyingMemory();

                case JavaScriptValueType.Object:
                    var obj = val as JavaScriptObject;
                    var external = obj.ExternalObject;
                    return external ?? obj;

                // Unsupported marshaling types
                case JavaScriptValueType.Function:
                case JavaScriptValueType.Date:
                case JavaScriptValueType.Symbol:
                default:
                    throw new NotSupportedException("Unsupported type marshaling value from JavaScript to host: " + val.Type);
            }
        }

        private JavaScriptProjection InitializeProjectionForType(Type t)
        {
            if (t.GenericTypeArguments.Length > 0 && !t.IsConstructedGenericType)
                throw new InvalidOperationException("The specified type is not a constructed generic types.  Only fully constructed types may be projected to JavaScript.");

            JavaScriptObject result;
            var eng = GetEngineAndClaimContext();

            var publicConstructors = t.GetConstructors(BindingFlags.Public);
            var publicInstanceMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            var publicStaticMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            var publicInstanceProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            var publicStaticProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
            if (AnyHaveSameArity(publicConstructors, publicInstanceMethods, publicStaticMethods, publicInstanceProperties, publicStaticProperties))
                throw new InvalidOperationException("The specified type cannot be marshaled; some publicly accessible members have the same arity.  Projected methods can't differentiate only by type (e.g., Add(int, int) and Add(float, float) would cause this error).");

            // todo:
            // fields
            // events
            if (publicConstructors.Length > 0)
            {
                // marshal as function
                var fn = eng.CreateFunction((engine, ctor, thisObj, args) =>
                {
                    return engine.UndefinedValue;
                }, t.Name);
                result = fn.Prototype;

                ProjectMethods(t.Name, fn, eng, publicStaticMethods);
                ProjectProperties(t.Name, fn, eng, publicStaticProperties);
            }
            else
            {
                result = eng.CreateObject();
            }

            ProjectMethods(t.Name + ".prototype", result, eng, publicInstanceMethods);
            ProjectProperties(t.Name + ".prototype", result, eng, publicInstanceProperties);

            JavaScriptProjection projection = new JavaScriptProjection { Prototype = result, RefCount = 0, };
            return projection;
        }

        private void ProjectMethods(string owningTypeName, JavaScriptObject target, JavaScriptEngine engine, IEnumerable<MethodInfo> methods)
        {
            var methodsByName = methods.GroupBy(m => m.Name);
            foreach (var group in methodsByName)
            {
                var method = engine.CreateFunction((eng, ctor, thisObj, args) =>
                {
                    var @this = thisObj as JavaScriptObject;
                    if (@this == null)
                    {
                        eng.SetException(eng.CreateTypeError("Could not call method '" + group.Key + "' because there was an invalid 'this' context."));
                        return eng.UndefinedValue;
                    }

                    var argsArray = args.ToArray();
                    var candidate = GetBestFitMethod(group, thisObj, argsArray);
                    if (candidate == null)
                    {
                        eng.SetException(eng.CreateReferenceError("Could not find suitable method or not enough arguments to invoke '" + group.Key + "'."));
                        return eng.UndefinedValue;
                    }

                    List<object> argsToPass = new List<object>();
                    for (int i = 0; i < candidate.GetParameters().Length; i++)
                    {
                        argsToPass.Add(ToObject(argsArray[i]));
                    }

                    try
                    {
                        return FromObject(candidate.Invoke(@this.ExternalObject, argsToPass.ToArray()));
                    }
                    catch (Exception ex)
                    {
                        eng.SetException(FromObject(ex));
                        return eng.UndefinedValue;
                    }
                }, owningTypeName + "." + group.Key);
            }
        }

        private MethodInfo GetBestFitMethod(IEnumerable<MethodInfo> methodCandidates, JavaScriptValue thisObj, JavaScriptValue[] argsArray)
        {
            JavaScriptObject @this = thisObj as JavaScriptObject;
            if (@this == null)
                return null;

            var external = @this.ExternalObject;
            if (external == null)
                return null;

            MethodInfo most = null;
            int arity = -1;
            foreach (var candidate in methodCandidates)
            {
                if (candidate.DeclaringType != external.GetType())
                    continue;

                var paramCount = candidate.GetParameters().Length;
                if (argsArray.Length == paramCount)
                {
                    return candidate;
                }
                else if (argsArray.Length < paramCount)
                {
                    if (paramCount > arity)
                    {
                        arity = paramCount;
                        most = candidate;
                    }
                }
            }

            return most;
        }

        private void ProjectProperties(string owningTypeName, JavaScriptObject target, JavaScriptEngine engine, IEnumerable<PropertyInfo> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.GetIndexParameters().Length > 0)
                    throw new NotSupportedException("Index properties not supported for projecting CLR to JavaScript objects.");

                JavaScriptFunction jsGet = null, jsSet = null;
                if (prop.GetMethod != null)
                {
                    jsGet = engine.CreateFunction((eng, ctor, thisObj, args) =>
                    {
                        var @this = thisObj as JavaScriptObject;
                        if (@this == null)
                        {
                            eng.SetException(eng.CreateTypeError("Could not retrieve property '" + prop.Name + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        try
                        {
                            return FromObject(prop.GetValue(@this.ExternalObject));
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, owningTypeName + "." + prop.Name + ".get");
                }
                if (prop.SetMethod != null)
                {
                    jsSet = engine.CreateFunction((eng, ctor, thisObj, args) =>
                    {
                        var @this = thisObj as JavaScriptObject;
                        if (@this == null)
                        {
                            eng.SetException(eng.CreateTypeError("Could not retrieve property '" + prop.Name + "' because there was an invalid 'this' context."));
                            return eng.UndefinedValue;
                        }

                        try
                        {
                            var val = ToObject(args.First());
                            prop.SetValue(@this.ExternalObject, val);
                            return eng.UndefinedValue;
                        }
                        catch (Exception ex)
                        {
                            eng.SetException(FromObject(ex));
                            return eng.UndefinedValue;
                        }
                    }, owningTypeName + "." + prop.Name + ".set");
                }

                var descriptor = engine.CreateObject();
                if (jsGet != null)
                    descriptor.SetPropertyByName("get", jsGet);
                if (jsSet != null)
                    descriptor.SetPropertyByName("set", jsSet);
                descriptor.SetPropertyByName("enumerable", engine.TrueValue);
                target.DefineProperty(prop.Name, descriptor);
            }
        }

        private static bool AnyHaveSameArity(params MemberInfo[][] members)
        {
            foreach (MemberInfo[] memberset in members)
            {
                ConstructorInfo[] ctors = memberset as ConstructorInfo[];
                MethodInfo[] methods = memberset as MethodInfo[];
                PropertyInfo[] props = memberset as PropertyInfo[];
                HashSet<int> arities = new HashSet<int>();

                if (ctors != null)
                {
                    foreach (var ctor in ctors)
                    {
                        int arity = ctor.GetParameters().Length;
                        if (arities.Contains(arity))
                            return true;
                        arities.Add(arity);
                    }
                }
                else if (methods != null)
                {
                    foreach (var methodGroup in methods.GroupBy(m => m.Name))
                    {
                        arities.Clear();
                        foreach (var method in methodGroup)
                        {
                            int arity = method.GetParameters().Length;
                            if (arities.Contains(arity))
                                return true;
                            arities.Add(arity);
                        }
                    }
                }
                else if (props != null)
                {
                    //foreach (var prop in props)
                    //{
                    //    int arity = prop.GetIndexParameters().Length;
                    //    if (arities.Contains(arity))
                    //        return true;
                    //    arities.Add(arity);
                    //}
                }
                else
                {
                    throw new InvalidOperationException("Unrecognized member type");
                }
            }

            return false;
        }

        private JavaScriptObject InitializeProjectionForObject(object target)
        {
            Type t = target.GetType();
            JavaScriptProjection projection;
            if (!projectionTypes_.TryGetValue(t, out projection))
            {
                projection = InitializeProjectionForType(t);
            }

            Interlocked.Increment(ref projection.RefCount);
            // Avoid race condition in which projectionTypes_ has had projection removed
            projectionTypes_[t] = projection;

            var eng = GetEngineAndClaimContext();
            var result = eng.CreateExternalObject(target, externalData =>
            {
                if (Interlocked.Decrement(ref projection.RefCount) <= 0)
                {
                    projectionTypes_.Remove(t);
                }
            });
            result.Prototype = projection.Prototype;

            return result;
        }
    }
}
