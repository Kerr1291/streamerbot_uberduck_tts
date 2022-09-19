using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Net;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;

namespace Xytio
{
    public class XMLUtils
    {

        /*
        Usage Example:

        [XmlRoot("AppSettings")]
        public class ExampleData
        {
            [XmlElement("Data")]
            public string data;

            [XmlElement("MoreData")]
            public string moreData;

            [XmlArray("ListOfData")]
            public List<string> someListOfData;

            [XmlElement(ElementName ="OptionalData", IsNullable = true)]
            public bool? someOptionalData;
        }
        */


        public static bool WriteDataToFile<T>(string path, T data) where T : class
        {

            bool result = false;

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            FileStream fstream = null;

            try
            {

                fstream = new FileStream(path, FileMode.Create);

                serializer.Serialize(fstream, data);

                result = true;
            }

            catch (System.Exception e)
            {

                Console.WriteLine(e.Message);

                //System.Windows.Forms.MessageBox.Show("Error creating/saving file "+ e.Message);
            }

            finally
            {
                fstream.Close();
            }

            return result;
        }


        public static bool ReadDataFromFile<T>(string path, out T data) where T : class
        {
            data = null;

            if (!File.Exists(path))
            {
                //System.Windows.Forms.MessageBox.Show("No file found at " + path );

                return false;
            }

            bool returnResult = true;

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            FileStream fstream = null;

            try
            {
                fstream = new FileStream(path, FileMode.Open);

                data = serializer.Deserialize(fstream) as T;
            }

            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);

                //System.Windows.Forms.MessageBox.Show("Error loading file " + e.Message);

                returnResult = false;
            }

            finally
            {
                fstream.Close();
            }

            return returnResult;
        }



        public static bool WriteStringDataToFile(string path, string data) 
        {
            bool result = false;

            FileStream fstream = null;
            StreamWriter sw = null;

            try
            {

                fstream = new FileStream(path, FileMode.Create);
                sw = new StreamWriter(fstream);

                sw.Write(data);

                result = true;
            }

            catch (System.Exception e)
            {

                Console.WriteLine(e.Message);

                //System.Windows.Forms.MessageBox.Show("Error creating/saving file "+ e.Message);
            }

            finally
            {
                fstream.Close();
            }

            return result;
        }
    }
}
