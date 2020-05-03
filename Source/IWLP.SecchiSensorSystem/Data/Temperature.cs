using System;
using System.Collections.Generic;
using System.Text;

namespace IWLP.SecchiSensorSystem.Data
{
    public class Temperature
    {
        private readonly double _farenheit;
        private readonly double _celcius;
        public Temperature(double celcius)
        {
            this._celcius = celcius;
            this._farenheit = (celcius * (9.0 / 5.0)) + 32;
        }

        public double GetTemperature(TemperatureUnit temperatureUnit)
        {
            return temperatureUnit == TemperatureUnit.FARENHEIT ? _farenheit : _celcius;
        }
    }
}
