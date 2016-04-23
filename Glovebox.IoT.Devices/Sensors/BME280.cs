using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glovebox.IoT.Devices.Sensors {
    public class BME280 : BMP280{

        static object humidityLock = new object();
        const byte Humidity16xOverSampling = 0x05; // 16x ovesampling for Humidity

        public double Humidity => GetHumidity();

        public BME280() {
            I2C_ADDRESS = 0x76; 
        }

        protected override void ReadCoefficients() {
            base.ReadCoefficients();

            calibration.dig_H1 = Read8(Register.REGISTER_DIG_H1);
            calibration.dig_H2 = ReadS16_LE(Register.REGISTER_DIG_H2);
            calibration.dig_H3 = Read8(Register.REGISTER_DIG_H3);
            calibration.dig_H4 = (short)((Read8(Register.REGISTER_DIG_H4) << 4) | (Read8(Register.REGISTER_DIG_H4 + 1) & 0xF));
            calibration.dig_H5 = (short)((Read8(Register.REGISTER_DIG_H5 + 1) << 4) | (Read8(Register.REGISTER_DIG_H5) >> 4));
            calibration.dig_H6 = (sbyte)Read8(Register.REGISTER_DIG_H6);
        }

        protected override void InitiliseRegisters() {
            Write8(Register.REGISTER_CONTROLHUMID, Humidity16xOverSampling);

            base.InitiliseRegisters();
        }

        protected virtual double GetHumidity() {

            GetTemperature(); // the humidity reading has a dependency of temperature

            lock (humidityLock) {

                Int32 adc_H = Read16(Register.REGISTER_HUMIDDATA);

                Int32 v_x1_u32r;

                v_x1_u32r = (t_fine - ((Int32)76800));

                v_x1_u32r = (((((adc_H << 14) - (((Int32)calibration.dig_H4) << 20) -
                        (((Int32)calibration.dig_H5) * v_x1_u32r)) + ((Int32)16384)) >> 15) *
                         (((((((v_x1_u32r * ((Int32)calibration.dig_H6)) >> 10) *
                          (((v_x1_u32r * ((Int32)calibration.dig_H3)) >> 11) + ((Int32)32768))) >> 10) +
                        ((Int32)2097152)) * ((Int32)calibration.dig_H2) + 8192) >> 14));

                v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) *
                               ((Int32)calibration.dig_H1)) >> 4));

                v_x1_u32r = (v_x1_u32r < 0) ? 0 : v_x1_u32r;
                v_x1_u32r = (v_x1_u32r > 419430400) ? 419430400 : v_x1_u32r;
                float h = (v_x1_u32r >> 12);

                return h / 1024.0;
            }
        }
    }
}
