using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cameleon.Protocol
{
    public class CameleonClient : ICameleonClient
    {
        //xxxxxxxxxx<XML-RPC formatted request data>
        //where xxxxxxxxxx is the number of bytes in the packet, including these 10 digits.
        //Leading zeros must be used if the packet is less than 1000000000 bytes long.
        private const int MESSAGE_HEADER_LENGTH = 10;
        private string _hostname;
        private int _port;
        public CameleonClient(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
        }

        public MixedResult<ScheduledMessage[]> CancelScheduledMessages(string[] selectedMessages)
        {
            var parameters = new List<object>() { selectedMessages };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessMixedRequest<ScheduledMessage[]>(request).Result;
            return response;
        }

        public MixedResult<ScheduledMessage[]> EditScheduledMessages(string[] selectedMessages, EventTimeStruct newUpdateTime, EventTimeStruct newEndTime)
        {
            var parameters = new List<object>() { selectedMessages, newUpdateTime, newEndTime };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessMixedRequest<ScheduledMessage[]>(request).Result;
            return response;
        }

        public MixedResult<ScheduledMessage[]> EditScheduledMessages(string[] selectedMessages, EventTimeStruct newUpdateTime)
        {
            var parameters = new List<object>() { selectedMessages, newUpdateTime, string.Empty };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessMixedRequest<ScheduledMessage[]>(request).Result;
            return response;
        }

        public GetCurrentMessageResponse GetCurrentMessage(int signID)
        {
            var parameters = new List<object>() { signID };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessRequest<GetCurrentMessageResponse>(request).Result;
            return response;
        }
        /// <summary>
        /// Gets the current messages.
        /// </summary>
        /// <returns></returns>
        public XmlRpcStruct GetCurrentMessages()
        {
            var parameters = new List<object>() { };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessRequest<XmlRpcStruct>(request).Result;
            return response;
        }

        public MixedResult<GetGlobalMessagesResponse[]> GetGlobalMessages()
        {
            var parameters = new List<object>() { };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessMixedRequest<GetGlobalMessagesResponse[]>(request).Result;
            return response;
        }

        public MixedResult<ScheduledMessage[]> GetScheduledMessages()
        {
            var parameters = new List<object>() { };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessMixedRequest<ScheduledMessage[]>(request).Result;
            return response;
        }

        public GetSignIDsResponse[] GetSignIDs()
        {
            var parameters = new List<object>() { };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessRequest<GetSignIDsResponse[]>(request).Result;
            return response;
        }

        public SetMessageResponse SetMessageImmediately(string[] recipients, int messageLevel, string username, string password, string messageName, MessageStruct[] messagePhases = null, DateTime? endTime = null, int? activatePriority = null, int? runPriority = null)
        {
            EventTimeStruct updateTime = new EventTimeStruct();
            updateTime.EventTime =Constants.IMMIDIATELY;
            return SetMessage(recipients, updateTime, messageLevel, username, password, messageName, messagePhases, endTime, activatePriority, runPriority);
        }
        public SetMessageResponse SetMessage(string[] recipients, EventTimeStruct updateTime, int messageLevel, string username, string password, string messageName, MessageStruct[] messagePhases = null, DateTime? endTime = null, int? activatePriority = null, int? runPriority = null)
        {
            recipients.ToList().ForEach(recipient =>
            {
                var parts = recipient.Split('_');
                if (!(parts.Length==2 && (string.Compare(parts[0],Constants.Device,false)==0 || string.Compare(parts[0],Constants.Group,false)==0)))
                {
                    throw new ArgumentOutOfRangeException("recipients", recipient, "Each string in the array must be of the form <recipient type>_<recipient ID> where <recipienttype> is either \"Device\" for a sign or \"Group\" for a sign group and <recipient ID> is the sign or sign group’s ID. <recipient type> is casesensitive");
                }
            });
            var parameters = new List<object>() { recipients };
            if(updateTime.EventTime == Constants.IMMIDIATELY)
            {
                parameters.Add(Constants.IMMIDIATELY);
            }
            else
            {
                parameters.Add(updateTime);
            }
            parameters.AddRange(new List<object>() { messageLevel, username, password, messageName });
            if (messageName !=Constants.DONTCARE && messageName != Constants.BLANK && messagePhases != null)
            { //Add Optional Parameters when passed
                parameters.AddRange(new List<object>() { messagePhases, endTime.Value, activatePriority.Value, runPriority.Value });
            }
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessRequest<SetMessageResponse>(request).Result;
            return response;
        }

        public ValidateUsernamePassworResult ValidateUsernamePassword(string userName, string password)
        {
            var parameters = new List<object>() { };
            var request = createXmlRpcRequest(MethodBase.GetCurrentMethod(), parameters);
            var response = ProcessRequest<ValidateUsernamePassworResult>(request).Result;
            return response;
        }
        private XmlRpcRequest createXmlRpcRequest(MethodBase mb, IEnumerable<Object> parameters)
        {
            return new XmlRpcRequest(mb.Name, parameters.ToArray(), mb as MethodInfo);
        }
        private async Task<T> ProcessRequest<T>(XmlRpcRequest request)
        {
            var responseDeserializer = new XmlRpcResponseDeserializer();
            var outStream = await ProcessRequest(request);
            var resp = responseDeserializer.DeserializeResponse(outStream, typeof(T));
            return (T)resp.retVal;
        }
        private async Task<MixedResult<T>> ProcessMixedRequest<T>(XmlRpcRequest request)
        {
            var responseDeserializer = new XmlRpcResponseDeserializer();
            var outStream = await ProcessRequest(request);
            var resp = responseDeserializer.DeserializeResponse(outStream, typeof(T));
            var mixedResp = new MixedResult<T>();

            if (resp.retVal is string) //if the response is just a string then 
            {
                mixedResp.ErrorMessage = resp.retVal as string;
                mixedResp.Data = default(T);
            }
            else
            {
                mixedResp.Data = (T)resp.retVal;
            }
            return mixedResp;
        }

        private async Task<MemoryStream> ProcessRequest(XmlRpcRequest request)
        {
            var tcpClient = new TcpClient();
            try
            {
                var settings = new XmlRpcFormatSettings() { OmitXmlDeclaration = true, UseEmptyParamsTag = false, UseIntTag = true, UseIndentation = true };
                var serializer = new XmlRpcRequestSerializer(settings);
                var stream = new MemoryStream();
                serializer.SerializeRequest(stream, request);
                //string requestString = Encoding.UTF8.GetString(stream.ToArray());

                var bytesInPacket = (stream.Length + MESSAGE_HEADER_LENGTH).ToString("D" + MESSAGE_HEADER_LENGTH);
                await tcpClient.ConnectAsync(_hostname, _port);
                using (var networkStream = tcpClient.GetStream())
                {
                    var leadingBytes = Encoding.UTF8.GetBytes(bytesInPacket);
                    await networkStream.WriteAsync(leadingBytes, 0, leadingBytes.Length);
                    stream.Position = 0;
                    await stream.CopyToAsync(networkStream);
                    networkStream.Flush();
                    byte[] headerBuffer = new byte[MESSAGE_HEADER_LENGTH];

                    await networkStream.ReadAsync(headerBuffer, 0, MESSAGE_HEADER_LENGTH);
                    var headerString = Encoding.UTF8.GetString(headerBuffer);
                    var bytesInBody = Convert.ToInt64(headerString) - MESSAGE_HEADER_LENGTH;
                    var bodyBytes = new byte[bytesInBody];
                    await networkStream.ReadAsync(bodyBytes, 0, (int)bytesInBody);
                    //string responseString = "<methodResponse><params><param><value><struct><member><name>ServerName</name><value><string>Server</string></value></member><member><name>UpdateStatus</name><value><string>AllUpdated</string></value></member></struct></value></param></params></methodResponse>";

                    var outStream = new MemoryStream(bodyBytes);
                    return outStream;
                }
            }
            finally
            {
                if (tcpClient.Connected) { tcpClient.Close(); }
            }
        }
    }
}
