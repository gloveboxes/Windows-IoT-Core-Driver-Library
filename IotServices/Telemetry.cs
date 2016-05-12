using Newtonsoft.Json;
using System;
using System.Text;

namespace IotServices
{

    public class Telemetry : Scheduler
    {
        static int msgCount = 0;

        public string Geo { get; set; }
        public string Celsius { get; set; }
        public string Humidity { get; set; }
        public string HPa { get; set; }
        public string Light { get; set; }
        public string Dev { get; set; }
        public int Id { get; set; }
        public int Exceptions { get; set; }


        public Telemetry(string geo, string deviceId, MeasureMethod measureMethod = null, int sampleRateInSeconds = 60) : base(measureMethod, sampleRateInSeconds) {
            this.Geo = geo;
            this.Dev = deviceId;
        }

        public byte[] ToJson(double temperature, double light, double hpa, double humidity) {
            Celsius = RoundMeasurement(temperature, 2).ToString();
            Light = RoundMeasurement(light, 2).ToString();
            HPa = RoundMeasurement(hpa, 0).ToString();
            Humidity = RoundMeasurement(humidity, 2).ToString();
            Id = ++msgCount;
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        }

        private string RoundMeasurement(double value, int places) {
            return Math.Round(value, places).ToString();
        }
    }
}
