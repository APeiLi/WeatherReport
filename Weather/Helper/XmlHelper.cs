using System;
using System.Collections.Generic;
using System.Xml;
using Weather.Model;

namespace Weather.Helper
{
    public static class XmlHelper
    {
        public static bool WriteToXml(List<CityInfo> keyAndValueList, string node = "City", string saveFileName = "ThinkPageSavedCity.xml")
        {
            try
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;
                XmlWriter writer = XmlWriter.Create(saveFileName, settings);
                writer.WriteStartDocument();
                writer.WriteStartElement(node);
                foreach (var kav in keyAndValueList)
                {
                    writer.WriteElementString(kav.CityName, kav.CityCode);
                }
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("操作Xml时城市信息保存失败！" + ex.Message);
            }
        }

        public static bool AddWriteToXml(List<CityInfo> keyAndValueList, string node = "City", string saveFileName = "ThinkPageSavedCity.xml")
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(saveFileName);
                XmlNode root = xmlDoc.SelectSingleNode(node);

                foreach (var value in keyAndValueList)
                {
                    XmlElement xe = xmlDoc.CreateElement(value.CityName);
                    xe.InnerText = value.CityCode;
                    root.AppendChild(xe);
                }

                xmlDoc.Save(saveFileName);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("操作Xml时城市信息保存失败！" + ex.Message);
            }
        }

        public static List<CityInfo> ReadFromXml(string node = "City", string fileName = "ThinkPageSavedCity.xml")
        {
            List<CityInfo> keyAndValueList = new List<CityInfo>();

            try
            {
                XmlReader rdr = XmlReader.Create(fileName);
                while (!rdr.EOF)
                {
                    if (rdr.MoveToContent() == XmlNodeType.Element && rdr.Name != node)
                    {
                        keyAndValueList.Add(new CityInfo()
                        {
                            CityName = rdr.Name,
                            CityCode = rdr.ReadElementString()
                        });
                    }
                    else
                    {
                        rdr.Read();
                    }
                }

                return keyAndValueList;
            }
            catch (Exception ex)
            {
                throw new Exception("操作Xml时读取城市信息失败！" + ex.Message);
            }
        }

        public static bool DeleteFromXml(CityInfo kav, string node = "City", string fileName = "ThinkPageSavedCity.xml")
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(fileName);

                XmlNode xn = doc.SelectSingleNode(string.Format("//{0}",kav.CityName));
                xn.ParentNode.RemoveChild(xn);

                doc.Save(fileName);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("操作Xml时删除城市节点失败！" + ex.Message);
            }
        }

    }//End public class XmlHelper

    //public class KeyAndValue
    //{
    //    public string Key { get; set; }
    //    public string Value { get; set; }
    //    public bool SelectStatus { get; set; }
    //}
}//End namespce
