using System;
using System.IO;
using System.Text;

namespace IWLP.SecchiSensorSystem.Utils
{
    public class CsvWriter
    {
        private string _location;

        public CsvWriter(string location)
        {
            this._location = location; 
        }

        public void WriteToFile(string data, bool append)
        {
            try
            {
                using StreamWriter file = new StreamWriter(this._location, append);
                
                file.Write(data);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("CSV Writing error", ex);
            }
        }

        public void WriteArrayToFile(string[] data, bool append)
        {
            // Join all the data together delimited by ","
            StringBuilder sb = new StringBuilder();
            
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i]);
                    
                // Test if it's not the last element in the array. 
                if (i != data.Length - 1)
                {
                    sb.Append(",");
                }
            }

            sb.Append(Environment.NewLine);
            
            WriteToFile(sb.ToString(), append);
        }
    }
}