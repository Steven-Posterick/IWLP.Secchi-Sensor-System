using System;
using System.Collections.Generic;
using System.Text;

namespace IWLP.SecchiSensorSystem.Data
{
    public class TemperatureModel
    {

        public TemperatureModel(double celcius, DateTime now)
        {
            DateTime = now;
            Temperature = new Temperature(celcius);
        }

        public DateTime DateTime { get; set; }
        public Temperature Temperature { get; set; }
    }
}
