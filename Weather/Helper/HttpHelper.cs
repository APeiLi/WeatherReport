using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Weather.Helper
{
    public static class HttpHelper
    {
        public static string HttpPost(string url, string postDataStr)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "Post";
                request.ContentType = "application/json";
                request.Timeout = 10000;
                byte[] requestBytes = Encoding.UTF8.GetBytes(postDataStr);//ASCII.GetBytes(postDataStr);
                request.ContentLength = requestBytes.Length;

                Stream myRequestStream = request.GetRequestStream();
                myRequestStream.Write(requestBytes, 0, requestBytes.Length);
                myRequestStream.Close();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
                string returnString = myStreamReader.ReadToEnd();
                //myStreamReader.Close();
                //myResponseStream.Close();

                return returnString;
            }
            catch (Exception ex)
            {
                throw new Exception("POST获取数据过程中发生错误！\n" + ex.Message);
            }
        }

        public static string HttpGet(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = WebRequestMethods.Http.Get;
                request.Timeout = 10000;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReamder = new StreamReader(myResponseStream, Encoding.UTF8);
                string returnString = myStreamReamder.ReadToEnd();
                //myStreamReamder.Close();
                //myResponseStream.Close();

                return returnString;
            }
            catch (Exception ex)
            {
                throw new Exception("GET获取数据过程中发生错误！\n" + ex.Message);
            }
        }
    }//End public static class
}//End namespace
