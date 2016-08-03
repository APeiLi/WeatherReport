using System;
using System.IO;
using Application = System.Windows.Forms.Application;

namespace Weather.Helper
{
    public static class LogHelper
    {
        private static readonly string LogFilePath = Application.StartupPath + @"\Log.txt";
        public static void SpecialWriteToLog(string logStr)
        {
            if (string.IsNullOrEmpty(logStr))
            {
                return;
            }

            try
            {
                StreamWriter sw = new StreamWriter(LogFilePath,true);
                sw.Write("\r\n{0}\t{1}\r\n",logStr,DateTime.Now);
                sw.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("追加写入日志时发生错误！\n" + ex.Message);
            }
        }

    }//End public static class
}
