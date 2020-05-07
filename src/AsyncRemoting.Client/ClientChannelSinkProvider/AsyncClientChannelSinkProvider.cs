using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;

namespace AsyncRemoting.Client.ClientChannelSinkProvider
{
    public class AsyncClientChannelSinkProvider : IClientChannelSinkProvider
    {
        public IClientChannelSinkProvider Next { get; set; }

        public IClientChannelSink CreateSink(IChannelSender channel, string url, object remoteChannelData)
        {
            // Create the next sink in the chain.
            IClientChannelSink nextSink = Next.CreateSink(channel, url, remoteChannelData);

            // Hook our sink up to it.
            return (new AsyncClientChannelSink(nextSink));
        }
    }

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
}
