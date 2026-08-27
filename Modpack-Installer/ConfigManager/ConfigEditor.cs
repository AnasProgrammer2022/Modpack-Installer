using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.ConfigManager
{
    //This class offers utilities for reading and writing configuration parameters to the config file
    internal class ConfigEditor(string path)
    {
        //------------------------FILE READING SECTION------------------------

        //Deletes the value of the key, then overwrites it with "value"
        //If there's no matching parameter with the key, a new one will be created
        public void SetValue(string key, string value)
        {
            if(!File.Exists(path))
                File.WriteAllText(path, "");
            StreamReader readerCursor = new StreamReader(path);
            StringBuilder finalContents = new StringBuilder();
            string contents;
            string[] param = new string[2];
            bool exists = false;
            while (!readerCursor.EndOfStream)
            {
                contents = readerCursor.ReadLine();
                param = GetValueAndKeyFromKeyLine(contents);
                if (string.Compare(param[0], key) == 0)
                {
                    exists = true;
                    contents = contents.Remove(param[0].Length + 1);
                    finalContents.AppendLine(contents + value);
                    finalContents.Append(readerCursor.ReadToEnd());
                    break;
                }
                finalContents.AppendLine(contents);
            }
            if (!exists)
                finalContents.AppendLine(key + ':' + value);
            readerCursor.Close();
            File.WriteAllText(path, finalContents.ToString());
        }

        //Appends the value to the array value with "value"
        //If there's no matching parameter with the key, a new one will be created
        public void AddValue(string key, string value)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, "");
            StreamReader readerCursor = new StreamReader(path);
            StringBuilder finalContents = new StringBuilder();
            string contents;
            string[] param = new string[2];
            bool exists = false;
            while (!readerCursor.EndOfStream)
            {
                contents = readerCursor.ReadLine();
                param = GetValueAndKeyFromKeyLine(contents);
                if (string.Compare(param[0], key) == 0)
                {
                    exists = true;
                    contents = contents + ',' + value;
                    finalContents.AppendLine(contents);
                    finalContents.Append(readerCursor.ReadToEnd());
                    break;
                }
                finalContents.AppendLine(contents);
            }
            if (!exists)
                finalContents.AppendLine(key + ':' + value);
            readerCursor.Close();
            File.WriteAllText(path, finalContents.ToString());
        }

        //------------------------FILE WRITING SECTION------------------------
        
        //Gets the value of the key
        //If nothing is found then null is returned
        public string GetValue(string key)
        {
            StreamReader readerCursor = new StreamReader(path);
            string[] param = new string[2];
            if (File.Exists(path))
                while (!readerCursor.EndOfStream)
                {
                    param = GetValueAndKeyFromKeyLine(readerCursor.ReadLine());
                    if (string.Compare(param[0], key) == 0)
                    {
                        readerCursor.Close();
                        return param[1];
                    }
                }
            return null;
        }
        //Gets the value at the index in the array value of the key
        //If nothing is found or the index is OoB (Out of Bounds) then null is returned
        public string GetValue(string key, int index)
        {
            StreamReader readerCursor = new StreamReader(path);
            string[] param = new string[2];
            if (File.Exists(path))
                while (!readerCursor.EndOfStream)
                {
                    param = GetValueAndKeyFromKeyLine(readerCursor.ReadLine());
                    if (string.Compare(param[0], key) == 0)
                    {
                        readerCursor.Close();
                        try { return GrabValuesFromArrayValue(param[1])[index]; }
                        catch { return null; }
                    }
                }
            return null;
        }

        //Gets the array value of the key then splits it into values
        //If nothing is found then null is returned
        public string[] GetArrayValues(string key)
        {
            StreamReader readerCursor = new StreamReader(path);
            string[] param = new string[2];
            if(File.Exists(path))
                while (!readerCursor.EndOfStream)
                {
                    param = GetValueAndKeyFromKeyLine(readerCursor.ReadLine());
                    if (string.Compare(param[0], key) == 0)
                    {
                        readerCursor.Close();
                        return GrabValuesFromArrayValue(param[1]);
                    }
                }
            return null;
        }

        //A helper function in which it splits the parameter line into a key string and a value string
        private string[] GetValueAndKeyFromKeyLine(string keyLine)
        {
            string[] result = new string[2];
            int indexer = 0;
            indexer = keyLine.IndexOf(':');
            result[0] = keyLine.Remove(indexer);
            result[1] = keyLine.Substring(indexer + 1);
            return result;
        }

        //A helper function in which it splits the whole array value into values
        private string[] GrabValuesFromArrayValue(string value)
        {
            List<string> resultValues = new List<string>();
            int beforeValueIndexer = 0;
            int afterValueIndexer = value.IndexOf(',');
            if (afterValueIndexer != -1)
            {
                while (afterValueIndexer != -1)
                {
                    resultValues.Add(value.Substring(beforeValueIndexer, afterValueIndexer - beforeValueIndexer));
                    beforeValueIndexer = afterValueIndexer + 1;
                    afterValueIndexer = value.IndexOf(',', beforeValueIndexer);
                }
                resultValues.Add(value.Substring(beforeValueIndexer));
            }
            else return null;
            return resultValues.ToArray();
                
        }
    }
}
