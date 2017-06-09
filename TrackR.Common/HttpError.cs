namespace TrackR.Common
{
    public class HttpError
    {
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public string MessageDetail { get; set; }
        public string StackTrace { get; set; }

        public HttpError InnerException { get; set; }
    }
}
