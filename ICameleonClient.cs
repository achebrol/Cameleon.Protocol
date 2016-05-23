using CookComputing.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cameleon.Protocol
{
    public interface ICameleonClient
    {
        /// <summary>
        /// Sets the message immediately.
        /// </summary>
        /// <param name="recipients">The recipients.</param>
        /// <param name="messageLevel">The message level.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="messageName">Name of the message.</param>
        /// <param name="messagePhases">The message phases.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="activatePriority">The activate priority.</param>
        /// <param name="runPriority">The run priority.</param>
        /// <returns></returns>
        Task<SetMessageResponse> SetMessageImmediately(string[] recipients, int messageLevel, string username,
                                      string password, string messageName, MessageStruct[] messagePhases,
                                      DateTime? endTime, int? activatePriority, int? runPriority);
        /// <summary>
        /// Sets the message.
        /// </summary>
        /// <param name="recipients">The recipients.</param>
        /// <param name="updateTime">The update time.</param>
        /// <param name="messageLevel">The message level.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="messageName">Name of the message.</param>
        /// <param name="messagePhases">The message phases.</param>
        /// <param name="endTime">The end time.</param>
        /// <param name="activatePriority">The activate priority.</param>
        /// <param name="runPriority">The run priority.</param>
        /// <returns></returns>
        Task<SetMessageResponse> SetMessage(string[] recipients, EventTimeStruct updateTime, int messageLevel, string username,
                                      string password, string messageName, MessageStruct[] messagePhases,
                                      DateTime? endTime, int? activatePriority, int? runPriority);
        /// <summary>
        /// Gets all sign ids.
        /// </summary>
        /// <returns></returns>
        Task<GetSignIDsResponse[]> GetSignIDs();
        /// <summary>
        /// Gets the scheduled messages.
        /// </summary>
        /// <returns></returns>
        Task<MixedResult<ScheduledMessage[]>> GetScheduledMessages();
        /// <summary>
        /// Cancels the scheduled messages.
        /// </summary>
        /// <param name="selectedMessages">The selected messages.</param>
        /// <returns></returns>
        Task<MixedResult<ScheduledMessage[]>> CancelScheduledMessages(string[] selectedMessages);
        /// <summary>
        /// Edits the scheduled messages.
        /// </summary>
        /// <param name="selectedMessages">The selected messages.</param>
        /// <param name="newUpdateTime">The new update time.</param>
        /// <returns></returns>
        Task<MixedResult<ScheduledMessage[]>> EditScheduledMessages(string[] selectedMessages, EventTimeStruct newUpdateTime); //Send an empty string as newUpdateTime
        /// <summary>
        /// Edits the scheduled messages.
        /// </summary>
        /// <param name="selectedMessages">The selected messages.</param>
        /// <param name="newUpdateTime">The new update time.</param>
        /// <param name="newEndTime">The new end time.</param>
        /// <returns></returns>
        Task<MixedResult<ScheduledMessage[]>> EditScheduledMessages(string[] selectedMessages, EventTimeStruct newUpdateTime, EventTimeStruct newEndTime);
        /// <summary>
        /// Gets the current messages.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Returns dymanic member names hence XmlRpcStruct.dynamic also works
        /// </remarks>
        Task<XmlRpcStruct> GetCurrentMessages();
        /// <summary>
        /// Gets the current message.
        /// </summary>
        /// <param name="signID">The sign identifier.</param>
        /// <returns></returns>
        Task<GetCurrentMessageResponse> GetCurrentMessage(int signID);
        /// <summary>
        /// Validates the username password.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        [XmlRpcEnumMapping]
        Task<ValidateUsernamePassworResult> ValidateUsernamePassword(string userName, string password);
        /// <summary>
        /// Gets all global messages.
        /// </summary>
        /// <returns></returns>
        Task<MixedResult<GetGlobalMessagesResponse[]>> GetGlobalMessages();

    }

    public enum UpdateStatus
    {
        InvalidData,
        InvalidUsernamePassword,
        AllUpdated,
        AllUpdatedLater,
        Errors
    }
    public struct SignError
    {
        public string Location;
        public string Error;
    }
    public struct SetMessageResponse
    {
        public string ServerName;
        [XmlRpcEnumMapping]
        public UpdateStatus UpdateStatus;
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public SignError[] SignsNotUpdated;
    }

    public struct GetSignIDsResponse
    {
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string ID;
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string Location;
        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string ScriptTag;//Unique

        [XmlRpcMissingMapping(MappingAction.Ignore)]
        public string SignGroup; //Only for Sign Groups
    }

    public struct ScheduledMessage
    {
        public int MessageID;
        public string[] Recipients;
        public EventTimeStruct UpdateTime;
        public DateTime NextUpdateTime;
        public string UpdateSchedule;
        public int MessageLevel;
        public string UserName;
        public string MessageName;
        public EventTimeStruct EndTime;
        public DateTime NextEndTime;
        public string EndTimeSchedule;
    }

    public enum EventType
    {
        Once = 0,
        Weekly = 1,
        Sunrise = 2,
        Monthly = 4,
        Daily = 5,
        Sunset = 8
    }
    [XmlRpcMissingMapping(MappingAction.Ignore)]
    public struct EventTimeStruct
    {
        //Required Members
        [XmlRpcEnumMapping(EnumMapping.Number)]
        public EventType EventType;
        public const int IsMessage = 1;//always 1
        public int SuspendOnHoliday; //1 if messages are not to be updated on holidays and 0 otherwise
        //Optional based on EventType
        //EventTime should be in the form hh:mm:ss AM/PM
        public string EventTime; //Required when EventType is Once,Daily,Weekly,Monthly,
        //Day should be in the form dd/mm/yyyy
        public string Day;//Required when EventType is Once

        /// <summary>
        /// whichDays is a one byte integer that indicates
        /// which days are included in a daily update.If the most
        /// significant byte is set, the message is sent on Sundays, if the
        /// next most significant byte is set, the message is sent on
        //  Mondays, etc.The least significant byte is not used
        /// </summary>
        [XmlRpcEnumMapping(EnumMapping.Number)]
        public Days? whichDays;//Required when EventType is Daily        
        /// <summary>
        /// Latitude and Longitude are the decimal degree representations of the Latitude and Longitude of the sign the message is being sent
        ///to; they are used to calculate the sunrise and/or sunset times
        ///for each day.
        /// </summary>
        public double? Latitude;
        /// <summary>
        /// Latitude and Longitude are the decimal degree representations of the Latitude and Longitude of the sign the message is being sent
        ///to; they are used to calculate the sunrise and/or sunset times
        ///for each day.
        /// </summary>
        public double? Longitude;
        /// <summary>
        /// Minutes is the number of minutes after sunrise or sunset that the message is sent.
        /// </summary>
        public int? Minutes;
        /// <summary>
        ///DayOfWeek is an integer that specifies which day of the week a weekly update gets sent. 1 = Sunday, 2 = Monday, etc.
        /// </summary>
        public int? DayOfWeek;
        /// <summary>
        /// DayOfMonth is an integer between 1 and 31 that indicates when a montly update gets sent.If DayOfMonth is greater than the number of days in a given month, the update is sent on the last day.
        /// </summary>
        public int? DayOfMonth;
    }
    public struct GetCurrentMessageResponse
    {
        public string ServerName;
        public int Color;
        public bool FullMatrix;
        public MessageStruct[] message;
        public int NumPhases;
        public int NumRows;
        public string MessageName;
    }
    public struct MessageStruct
    {
        public string[] LineText;
        public int DwellTime;//4
        public double BlankTime;//0.1;
    }

    public enum ValidateUsernamePassworResult
    {
        Valid,
        Invalid
    }
    public struct GetGlobalMessagesResponse
    {
        public int ID;
        public string Message;

    }

    public struct MixedResult<T>
    {
        public string ErrorMessage;
        public T Data;
    }

    [Flags]
    public enum Days
    {
        Sunday = 128,
        Monday = 64,
        Tuesday = 32,
        Wednesday = 16,
        Thursday = 8,
        Friday = 4,
        Saturday = 2,
        None = 0
    }


}
