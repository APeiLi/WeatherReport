using System;
using Newtonsoft.Json;
using Weather.Model;

namespace Weather.Helper
{
    public static class WeiXinHelper
    {
        /// <summary>
        /// 微信推送信息
        /// </summary>
        /// <param name="messageStr"></param>
        public static void WeiXinSentMessage(string messageStr)
        {
            try
            {
                string accessToken = HttpHelper.HttpGet(BackAccessTokenUrl());
                var responseAccesstoken = JsonConvert.DeserializeObject<ResponsAccessToken>(accessToken);

                if (responseAccesstoken.errcode != 0)
                {
                    throw new Exception(string.Format("AccessToken 获取失败！\n错误代码：{0}\t错误信息：{1}", responseAccesstoken.errcode, responseAccesstoken.errmsg));
                }

                string responseMessage = HttpHelper.HttpPost(BackSentMessageUrl(responseAccesstoken.access_token), CreateMessage(messageStr));
                var responseSentMessage = JsonConvert.DeserializeObject<ResponseSentMessage>(responseMessage);

                if (responseSentMessage.errcode != 0)
                {
                    throw new Exception(string.Format("信息推送失败！\n错误代码：{0}\t错误信息：{1}", responseSentMessage.errcode, responseSentMessage.errmsg));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("微信推送过程中发生一个错误！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 初始化获得AccessToken的Url链接
        /// </summary>
        /// <returns></returns>
        private static string BackAccessTokenUrl()
        {
            try
            {
                string corpid = System.Configuration.ConfigurationManager.AppSettings["corpid"];
                string corpsecret = System.Configuration.ConfigurationManager.AppSettings["corpsecret"];

                if (string.IsNullOrEmpty(corpid) || string.IsNullOrEmpty(corpsecret))
                {
                    throw new Exception("配置文件丢失！");
                }

                string url = string.Format("https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={0}&corpsecret={1}", corpid,
                    corpsecret);

                return url;
            }
            catch (Exception ex)
            {
                throw new Exception("微信程序初始化错误！\n" + ex.Message);
            }
        }

        /// <summary>
        /// 生成推送链接Url
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private static string BackSentMessageUrl(string accessToken)
        {
            try
            {
                return string.Format("https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={0}",
                    accessToken);
            }
            catch (Exception ex)
            {
                throw new Exception("微信推送链接生成失败！\n" + ex.Message);
            }
        }

        private static string CreateMessage(string text, string toParty = "", string toTag = "", string toUser = "@all", string msgType = "text", int agentId = 0, string safe = "0")
        {
            try
            {
                TextContent tc = new TextContent() { content = text };
                RequestSentMessage request = new RequestSentMessage()
                {
                    touser = toUser,
                    toparty = toParty,
                    totag = toTag,
                    msgtype = msgType,
                    agentid = agentId,
                    text = tc,
                    safe = safe
                };

                return JsonConvert.SerializeObject(request);
            }
            catch (Exception ex)
            {
                throw new Exception("微信推送文本信息生成失败！\n" + ex.Message);
            }
        }

    }
}
