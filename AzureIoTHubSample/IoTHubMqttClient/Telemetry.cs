using Newtonsoft.Json;
using System;
using System.Text;

namespace IoTHubMqttClient {
    public sealed class Telemetry {

        static int msgCount = 0;
        //static bool NtpInitalised = false;
        //static TimeSpan utcOffset;
        //static DateTime CorrectedUtcTime => utcOffset == TimeSpan.Zero ? DateTime.UtcNow : DateTime.UtcNow - utcOffset;

        public Telemetry(string geo, string deviceId) {
            this.Geo = geo;
            this.Dev = deviceId;
        }

        public string Geo { get; set; }
        public string Celsius { get; set; }
        public string Humidity { get; set; }
        public string HPa { get; set; }
        public string Light { get; set; }
        //      public string Utc { get; set; }
        public string Dev { get; set; }
        public int Id { get; set; }

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



        //DateTime CorrectedTime() { // useful for locations particularly conferences with Raspberry Pi failes to sync time
        //    try {
        //        if (NtpInitalised) { return CorrectedUtcTime; }

        //        NtpClient ntp = new NtpClient();

        //        var time = ntp.GetAsync("au.pool.ntp.org").Result;
        //        utcOffset = DateTime.UtcNow.Subtract(((DateTime)time).ToUniversalTime());

        //        NtpInitalised = true;
        //    }
        //    catch { }

        //    return CorrectedUtcTime;
        //}

 


        //public Telemetry(string geo, string deviceId) {
        //    this.Geo = geo;
        //    this.Dev = deviceId;
        //}

        //public string Geo { get; set; }
        //public string Celsius { get; set; }
        //public string Humidity { get; set; }
        //public string HPa { get; set; }
        //public string Light { get; set; }
        //public string Utc { get; set; }
        //public string Dev { get; set; }

        //public byte[] ToJson(double temperature, double light, double hpa, double humidity) {
        //    Celsius = RoundMeasurement(temperature, 2).ToString();
        //    Light = RoundMeasurement(light, 2).ToString();
        //    HPa = RoundMeasurement(hpa, 0).ToString();
        //    Humidity = RoundMeasurement(humidity, 2).ToString();
        //    return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        //}




        //public Telemetry(string guid, string measureName, string unitofmeasure) {
        //    this.guid = guid;
        //    this.measurename = measureName;
        //    this.unitofmeasure = unitofmeasure;
        //}



        //public string location => "USA";
        //public string organisation => "Fabrikam";
        //public string guid { get; set; }
        //public string measurename { get; set; }
        //public string unitofmeasure { get; set; }
        //public string value { get; set; }
        //public string timecreated { get; set; }
        //public int Id { get; set; }


        //public byte[] ToJson(double measurement) {
        //    value = RoundMeasurement(measurement, 2).ToString();
        //    //     timecreated = CorrectedTime().ToString("o");
        //    Id = ++msgCount;
        //    return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this));
        //}



    }
}
