### `Remoting`�첽���÷�ʽ
�첽���ñ���ͨ��ί���ṩ��`BeginInvoke`����
``` C#
Func<int, string> @delegate = new Func<int, string>(employeeService.GetName);
IAsyncResult ar = @delegate.BeginInvoke(0, default, default);
string name = @delegate.EndInvoke(ar);
```
`employeeService`��һ��Զ�̶���

### Զ�̶���
������֪��#�ֶ���ͷ����Զ�̶�����һ��`__TransparentProxy`����
``` C#
internal sealed class __TransparentProxy
{
    private RealProxy _rp;
    private object _stubData;
    private IntPtr _pMT;
    private IntPtr _pInterfaceMT;
    private IntPtr _stub;
}
```
������һ��`RealProxy`�����Ǿ���Ĵ������

### Զ�̶���ĵ���
����һ��Զ�̶���ķ���ʱ��ʵ�ʴ����ִ�е�`RealProxy`��`PrivateInvoke`�������÷���ǩ������
``` C#
private void PrivateInvoke(ref MessageData msgData, int type)
```
`MessageData`��������
```C#
internal struct MessageData
{
    internal IntPtr pFrame;
    internal IntPtr pMethodDesc;
    internal IntPtr pDelegateMD;
    internal IntPtr pSig;
    internal IntPtr thGoverningType;
    internal int iFlags;
}
```
����������װ�˵��÷�����һЩ��Ϣ��������Ҫ��ע`iFlags`�����Ա��ͨ������֪����ͬ�����÷���ʱ��Ҳ����`employeeService.GetName(0)`,�ó�ԱֵΪ`0`���첽����ʱ��Ҳ����`@delegate.BeginInvoke(0, default, default)`���ó�ԱֵΪ`1`;`@delegate.EndInvoke(ar)`ʱ���ó�ԱΪ`2`��
��`RealProxy`�����`InternalInvoke`�������жϸó�Աֵ
``` C#
internal virtual IMessage InternalInvoke(IMethodCallMessage reqMcmMsg, bool useDispatchMessage, int callType)
{
    Message message = reqMcmMsg as Message;
    IMessage result = null;

    ...������������

    switch (callType)
    {
        case 0:
        {
            bool bSkippingContextChain = false;
            Context currentContextInternal = currentThread.GetCurrentContextInternal();
            IMessageSink messageSink = identityObject.EnvoyChain;
            if (currentContextInternal.IsDefaultContext && messageSink is EnvoyTerminatorSink)
            {
                bSkippingContextChain = true;
                messageSink = identityObject.ChannelSink;
            }
            result = RemotingProxy.CallProcessMessage(messageSink, reqMcmMsg, identityObject.ProxySideDynamicSinks, currentThread,  currentContextInternal, bSkippingContextChain);
            break;
        }
        case 1:
        case 9:
        {
            logicalCallContext = (LogicalCallContext)logicalCallContext.Clone();
            internalMessage.SetCallContext(logicalCallContext);
            AsyncResult asyncResult = new AsyncResult(message);
            this.InternalInvokeAsync(asyncResult, message, useDispatchMessage, callType);
            result = new ReturnMessage(asyncResult, null, 0, null, message);
            break;
        }
        case 2:
            result = RealProxy.EndInvokeHelper(message, true);
            break;
        case 8:
            logicalCallContext = (LogicalCallContext)logicalCallContext.Clone();
            internalMessage.SetCallContext(logicalCallContext);
            this.InternalInvokeAsync(null, message, useDispatchMessage, callType);
            result = new ReturnMessage(null, null, 0, null, reqMcmMsg);
            break;
        case 10:
            result = new ReturnMessage(null, null, 0, null, reqMcmMsg);
            break;
    }

    return result;
}
```
��`InternalInvokeAsync`�����У�Ҳ�Ǻ�ͬ������һ��������`RealProxy.IdentityObject.EnvoyChain`����
``` C#
internal virtual Identity IdentityObject {get; set;}
```
``` C#
internal IMessageSink EnvoyChain {get; }
```
����������`IMessageSink`���ǲ��Ǻ���Ϥ��`Remoting`[�涨](https://docs.microsoft.com/zh-cn/previous-versions/dotnet/netframework-4.0/tdzwhfy3%28v%3dvs.100%29)�ͻ����ϵĵ�һ���ŵ�������������ʵ��`IMessageSink`��ͨ�������������һ��`IClientFormatterSink `���󣬱���Ϊ��ʽ�������������
�����Ҳ²⣬���`IMessageSink`���������ŵ����������ĵ�һ���ŵ���������

### �Զ����첽�����ŵ�������
``` C#
 public class AsyncClientChannelSink : BaseChannelSinkWithProperties, IClientChannelSink
 {
     public IClientChannelSink NextChannelSink { get; }

     public AsyncClientChannelSink(IClientChannelSink nextSink)
     {
         NextChannelSink = nextSink;
     }

     public Stream GetRequestStream(IMessage message, ITransportHeaders requestHeaders)
     {
         return NextChannelSink.GetRequestStream(message, requestHeaders);
     }

     public void ProcessMessage(
         IMessage message,
         ITransportHeaders requestHeaders,
         Stream requestStream,
         out ITransportHeaders responseHeaders,
         out Stream responseStream)
     {
         NextChannelSink.ProcessMessage(message, requestHeaders, requestStream, out responseHeaders, out responseStream);
     }

     /// <summary>
     /// �첽��������
     /// </summary>
     /// <param name="sinkStack"></param>
     /// <param name="message"></param>
     /// <param name="requestHeaders"></param>
     /// <param name="requestStream"></param>
     public void AsyncProcessRequest(
         IClientChannelSinkStack sinkStack,
         IMessage message,
         ITransportHeaders requestHeaders,
         Stream requestStream)
     {
         // throw new NotImplementedException();
         // ����ʵ�ִ˷�������Ϊ����˽�����ע�ᵽ�����������У����ų��ͻ��˻��첽����

         IMethodCallMessage callMessage = message as IMethodCallMessage;
         Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}    ��ʼ�����첽����{callMessage.MethodName}");

         // ֻ�аѵ�ǰ������ѹ��ջ��ʱ���첽��Ӧ��AsyncProcessResponse����ʱ��Żᱻִ��
         sinkStack.Push(this, callMessage.MethodName);
         NextChannelSink.AsyncProcessRequest(sinkStack, message, requestHeaders, requestStream);
     }

     public void AsyncProcessResponse(
         IClientResponseChannelSinkStack sinkStack,
         object state,
         ITransportHeaders responseHeaders,
         Stream responseStream)
     {
         Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}    �첽��������ϣ�{state}");
         sinkStack.AsyncProcessResponse(responseHeaders, responseStream);
     }
 }
```
�������ϵ�����������Ӧ��ʵ�֣���Ϊÿ�����󶼻ᾭ�����ŵ������������ٷ��ĵ���Ҳ��ȷ˵���ŵ��������в��������쳣������ʹ��`try{...}catch{...}`��
`AsyncProcessRequest`��������һ��`IClientChannelSinkStack`�������ò����ɵ�һ���ŵ���������`IMessageSink`��������
``` C#
//
// ժҪ:
//     �ṩ�Ŀͻ��˶�ջ�����첽��Ϣ��Ӧ��������е��õ��ŵ��������Ĺ��ܡ�
[ComVisible(true)]
public interface IClientChannelSinkStack : IClientResponseChannelSinkStack
{
    //
    // ժҪ:
    //     ������Ϣ������Ľ�������ջ�е����н������ﲢ����ָ���Ľ�������
    //
    // ����:
    //   sink:
    //     Ҫ�Ƴ������شӽ�������ջ��������
    //
    // ���ؽ��:
    //     ����������ɲ���ָ���Ľ��������������Ϣ��
    //
    // �쳣:
    //   T:System.Security.SecurityException:
    //     ֱ�ӵ��÷�û�л����ṹȨ�ޡ�
    [SecurityCritical]
    object Pop(IClientChannelSink sink);
    //
    // ժҪ:
    //     ��ָ���Ľ���������Ϣ������������������ջ�����͡�
    //
    // ����:
    //   sink:
    //     Ҫ���͵���������ջ��������
    //
    //   state:
    //     ����Ӧ��������������������Ϣ��
    //
    // �쳣:
    //   T:System.Security.SecurityException:
    //     ֱ�ӵ��÷�û�л����ṹȨ�ޡ�
    [SecurityCritical]
    void Push(IClientChannelSink sink, object state);
}
```
�����壬��Ӧ����һ��ջ�ṹ��ֻ�����ǽ���ǰ�ŵ�������ѹ��`IClientChannelSinkStack`ʱ��������������󣬲Ż�ͨ��`IClientChannelSinkStack`���ε����ŵ�������������`AsyncProcessResponse`�����������������ŵ�����������ע�첽�����Ƿ�ֻ��Ҫ���첽���󴫲�����һ�������������Ҳ���Ҫ���Լ�ѹ��`IClientChannelSinkStack`�У���ô`AsyncProcessResponse`�����Ͳ���Ҫʵ�֡�
### ���������
����������ǿͻ��������е����һ������������������������`AsyncProcessRequest`����ʹ��`TcpChannel`�ŵ����ڲ���һ��`TcpClientSocketHandler`����������ͨѶ������²������첽���ã�Ӧ�û�ʹ��`IOCP`��

