### `Remoting`异步调用方式
异步调用必须通过委托提供的`BeginInvoke`方法
``` C#
Func<int, string> @delegate = new Func<int, string>(employeeService.GetName);
IAsyncResult ar = @delegate.BeginInvoke(0, default, default);
string name = @delegate.EndInvoke(ar);
```
`employeeService`是一个远程对象

### 远程对象
众所周知（#手动狗头），远程对象是一个`__TransparentProxy`对象
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
里面有一个`RealProxy`对象，是具体的代理对象

### 远程对象的调用
调用一个远程对象的方法时，实际代码会执行到`RealProxy`的`PrivateInvoke`方法，该方法签名如下
``` C#
private void PrivateInvoke(ref MessageData msgData, int type)
```
`MessageData`定义如下
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
这个对象里封装了调用方法的一些信息，我们主要关注`iFlags`这个成员。通过调试知道，同步调用方法时候，也就是`employeeService.GetName(0)`,该成员值为`0`；异步调用时，也就是`@delegate.BeginInvoke(0, default, default)`，该成员值为`1`;`@delegate.EndInvoke(ar)`时，该成员为`2`。
在`RealProxy`对象的`InternalInvoke`方法里判断该成员值
``` C#
internal virtual IMessage InternalInvoke(IMethodCallMessage reqMcmMsg, bool useDispatchMessage, int callType)
{
	Message message = reqMcmMsg as Message;
	IMessage result = null;

    ...忽略其他代码

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
在`InternalInvokeAsync`方法中，也是和同步调用一样，都有`RealProxy.IdentityObject.EnvoyChain`对象
``` C#
internal virtual Identity IdentityObject {get; set;}
```
``` C#
internal IMessageSink EnvoyChain {get; }
```
这个对象就是`IMessageSink`。是不是很熟悉，`Remoting`[规定](https://docs.microsoft.com/zh-cn/previous-versions/dotnet/netframework-4.0/tdzwhfy3%28v%3dvs.100%29)客户端上的第一个信道接收器还必须实现`IMessageSink`。通常这个接收器是一个`IClientFormatterSink `对象，被称为格式化程序接收器。
所以我猜测，这个`IMessageSink`就是我们信道接收器链的第一个信道接收器。

### 自定义异步处理信道接收器
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
     /// 异步处理请求
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
         // 尽量实现此方法，因为如果此接收器注册到接收器链当中，不排除客户端会异步调用

         IMethodCallMessage callMessage = message as IMethodCallMessage;
         Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}    开始处理异步请求：{callMessage.MethodName}");

         // 只有把当前接收器压入栈中时，异步响应（AsyncProcessResponse）的时候才会被执行
         sinkStack.Push(this, callMessage.MethodName);
         NextChannelSink.AsyncProcessRequest(sinkStack, message, requestHeaders, requestStream);
     }

     public void AsyncProcessResponse(
         IClientResponseChannelSinkStack sinkStack,
         object state,
         ITransportHeaders responseHeaders,
         Stream responseStream)
     {
         Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}    异步请求处理完毕：{state}");
         sinkStack.AsyncProcessResponse(responseHeaders, responseStream);
     }
 }
```
接收器上的三个方法都应该实现，因为每个请求都会经过该信道接收器处理。官方文档中也明确说明信道接收器中不能引发异常，可以使用`try{...}catch{...}`。
`AsyncProcessRequest`方法中有一个`IClientChannelSinkStack`参数，该参数由第一个信道接收器（`IMessageSink`）创建。
``` C#
//
// 摘要:
//     提供的客户端堆栈必须异步消息响应解码过程中调用的信道接收器的功能。
[ComVisible(true)]
public interface IClientChannelSinkStack : IClientResponseChannelSinkStack
{
    //
    // 摘要:
    //     弹出信息相关联的接收器堆栈中的所有接收器达并包括指定的接收器。
    //
    // 参数:
    //   sink:
    //     要移除并返回从接收器堆栈接收器。
    //
    // 返回结果:
    //     请求端上生成并与指定的接收器相关联的信息。
    //
    // 异常:
    //   T:System.Security.SecurityException:
    //     直接调用方没有基础结构权限。
    [SecurityCritical]
    object Pop(IClientChannelSink sink);
    //
    // 摘要:
    //     将指定的接收器和信息与它关联到接收器堆栈上推送。
    //
    // 参数:
    //   sink:
    //     要推送到接收器堆栈接收器。
    //
    //   state:
    //     在响应端所需的请求端上生成信息。
    //
    // 异常:
    //   T:System.Security.SecurityException:
    //     直接调用方没有基础结构权限。
    [SecurityCritical]
    void Push(IClientChannelSink sink, object state);
}
```
看定义，它应该是一个栈结构。只有我们将当前信道接收器压入`IClientChannelSinkStack`时，在请求处理结束后，才会通过`IClientChannelSinkStack`依次弹出信道接收器并调用`AsyncProcessResponse`方法处理。因此如果该信道接收器不关注异步处理，是否只需要将异步请求传播给下一个接收器，并且不需要将自己压入`IClientChannelSinkStack`中，那么`AsyncProcessResponse`方法就不需要实现。
### 传输接收器
传输接收器是客户端上链中的最后一个接收器。最终是由他处理`AsyncProcessRequest`。若使用`TcpChannel`信道，内部有一个`TcpClientSocketHandler`对象与服务端通讯。这里猜测若是异步调用，应该会使用`IOCP`。

