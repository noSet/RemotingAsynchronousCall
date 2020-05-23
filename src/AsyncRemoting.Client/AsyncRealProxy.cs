using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Security;
using System.Threading.Tasks;

namespace AsyncRemoting.Client
{
    public class AsyncRealProxy<T> : RealProxy
    {
        private readonly T _decorated;

        public AsyncRealProxy(T decorated)
            : base(typeof(T))
        {
            _decorated = decorated;
        }

        public override IMessage Invoke(IMessage msg)
        {
            var methodCall = msg as IMethodCallMessage;
            var methodInfo = methodCall.MethodBase as MethodInfo;

            try
            {
                object result;

                if (methodInfo.Name.Contains("Async") || !methodInfo.GetParameters().Any(p => p.IsOut || p.ParameterType.IsByRef))
                {
                    methodInfo = typeof(T).GetMethod(methodInfo.Name.Substring(0, methodInfo.Name.Length - 5));

                    Type delegateType = DelegateHelper.CreateDelegate2(methodInfo);

                    // 上面这行注释掉的代码，会抛出异常。
                    // 如何将Delegate.BeginInvoke和Delegate.EndInvoke转换成Task，写不动了...
                    // Func<int, string> @delegate = Delegate.CreateDelegate(delegateType, _decorated, methodInfo) as Func<int, string>;
                    Func<int, string> @delegate = CreateMethod(delegateType, _decorated, methodInfo) as Func<int, string>;
                    result = Task.Factory.FromAsync(@delegate.BeginInvoke, @delegate.EndInvoke, 1, null);
                }
                else
                {
                    result = methodInfo.Invoke(_decorated, methodCall.InArgs);
                }

                return new ReturnMessage(result, null, 0, methodCall.LogicalCallContext, methodCall);
            }
            catch (Exception ex)
            {
                return new ReturnMessage(ex, methodCall);
            }
        }

        public Delegate CreateMethod(Type type, object instance, MethodInfo methodInfo)
        {
            DynamicMethod dynamic = new DynamicMethod(string.Empty, type, new Type[] { instance.GetType() }, type);

            ILGenerator il = dynamic.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldftn, methodInfo);
            il.Emit(OpCodes.Newobj, type.GetConstructors().First());
            il.Emit(OpCodes.Ret);

            return (Delegate)dynamic.Invoke(null, new object[] { instance });
        }
    }

    public class DelegateHelper
    {
        public static Type CreateDelegate2(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType == typeof(void))
            {
                return Expression.GetActionType(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
            }
            else
            {
                return Expression.GetFuncType(methodInfo.GetParameters().Select(p => p.ParameterType).Append(methodInfo.ReturnType).ToArray());
            }
        }

        public static Type CreateDelegate(MethodInfo methodInfo)
        {
            var @params = methodInfo.GetParameters();

            var hasReturn = methodInfo.ReturnType != typeof(void);

            var openGenericDelegate = GetOpenGenericDelegate(@params.Length, hasReturn);

            Type[] genericTypes = new Type[hasReturn ? @params.Length + 1 : @params.Length];

            if (genericTypes.Length == 0)
            {
                return typeof(Action);
            }

            for (int i = 0; i < genericTypes.Length; i++)
            {
                if (hasReturn && i == genericTypes.Length - 1)
                {
                    genericTypes[i] = methodInfo.ReturnType;
                }
                else
                {
                    genericTypes[i] = @params[i].ParameterType;
                }
            }

            var closeGenericDelegate = openGenericDelegate.MakeGenericType(genericTypes);

            return closeGenericDelegate;
        }

        public static Type GetOpenGenericDelegate(int paramCount, bool hasReturn)
        {
            switch (paramCount)
            {
                case 0:
                    return hasReturn ? typeof(Func<>) : typeof(Action);

                case 1:
                    return hasReturn ? typeof(Func<,>) : typeof(Action<>);

                case 2:
                    return hasReturn ? typeof(Func<,,>) : typeof(Action<,>);

                case 3:
                    return hasReturn ? typeof(Func<,,,>) : typeof(Action<,,>);

                case 4:
                    return hasReturn ? typeof(Func<,,,,>) : typeof(Action<,,,>);

                case 5:
                    return hasReturn ? typeof(Func<,,,,,>) : typeof(Action<,,,,>);

                case 6:
                    return hasReturn ? typeof(Func<,,,,,,>) : typeof(Action<,,,,,>);

                case 7:
                    return hasReturn ? typeof(Func<,,,,,,,>) : typeof(Action<,,,,,,>);

                case 8:
                    return hasReturn ? typeof(Func<,,,,,,,,>) : typeof(Action<,,,,,,,>);

                case 9:
                    return hasReturn ? typeof(Func<,,,,,,,,,>) : typeof(Action<,,,,,,,,>);

                case 10:
                    return hasReturn ? typeof(Func<,,,,,,,,,,>) : typeof(Action<,,,,,,,,,>);

                case 11:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,>);

                case 12:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,>);

                case 13:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,>);

                case 14:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,>);

                case 15:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,,>);

                case 16:
                    return hasReturn ? typeof(Func<,,,,,,,,,,,,,,,,>) : typeof(Action<,,,,,,,,,,,,,,,>);

                default:
                    throw new ArgumentOutOfRangeException(nameof(paramCount), "参数数量最多只能是16个");
            }
        }
    }
}
