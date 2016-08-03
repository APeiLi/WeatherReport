
namespace Weather.Model
{
    public class RequestAccessToken
    {
        public string corpid;
        public string corpsecret;
    }

    public class ResponsAccessToken
    {
        public string access_token;
        public int expires_in;
        public int errcode;
        public string errmsg;
    }

    public class RequestSentMessage
    {
        public string touser;
        public string toparty;
        public string totag;
        public string msgtype;
        public int agentid;
        public TextContent text;
        public string safe;
    }
    public class TextContent
    {
        public string content;
    }

    public class ResponseSentMessage
    {
        public int errcode;
        public string errmsg;
        public string invaliduser;
        public string invalidparty;
        public string invalidtag;
    }
}
