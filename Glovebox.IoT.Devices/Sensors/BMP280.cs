using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Glovebox.IoT.Devices.Sensors
{


    // code migrated from https://raw.githubusercontent.com/adafruit/Adafruit_BMP280_Library/master/Adafruit_BMP280.cpp
    // http://lxr.free-electrons.com/source/drivers/iio/pressure/bmp280.c
    public class BMP280 : IDisposable
    {

        enum Register : byte
        {
            BMP280_REGISTER_DIG_T1 = 0x88,
            BMP280_REGISTER_DIG_T2 = 0x8A,
            BMP280_REGISTER_DIG_T3 = 0x8C,

            BMP280_REGISTER_DIG_P1 = 0x8E,
            BMP280_REGISTER_DIG_P2 = 0x90,
            BMP280_REGISTER_DIG_P3 = 0x92,
            BMP280_REGISTER_DIG_P4 = 0x94,
            BMP280_REGISTER_DIG_P5 = 0x96,
            BMP280_REGISTER_DIG_P6 = 0x98,
            BMP280_REGISTER_DIG_P7 = 0x9A,
            BMP280_REGISTER_DIG_P8 = 0x9C,
            BMP280_REGISTER_DIG_P9 = 0x9E,

            BMP280_REGISTER_CHIPID = 0xD0,
            BMP280_REGISTER_VERSION = 0xD1,
            BMP280_REGISTER_SOFTRESET = 0xE0,

            BMP280_REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            BMP280_REGISTER_CONTROL = 0xF4,
            BMP280_REGISTER_CONFIG = 0xF5,
            BMP280_REGISTER_PRESSUREDATA = 0xF7,
            BMP280_REGISTER_TEMPDATA = 0xFA,
        };


        public struct bmp280_calib_data
        {
            public ushort dig_T1;
            public short dig_T2;
            public short dig_T3;

            public ushort dig_P1;
            public short dig_P2;
            public short dig_P3;
            public short dig_P4;
            public short dig_P5;
            public short dig_P6;
            public short dig_P7;
            public short dig_P8;
            public short dig_P9;

            public byte dig_H1;
            public short dig_H2;
            public byte dig_H3;
            public short dig_H4;
            public short dig_H5;
            public sbyte dig_H6;
        }

        bmp280_calib_data bmp280_calib = new bmp280_calib_data();

        int t_fine;


        public enum BusMode
        {
            SPI,
            I2C
        }

        public int I2C_ADDRESS { get; set; } = 0x77;

        static object temperatureLock = new object();
        static object pressureLock = new object();

        public static bool IsInitialised { get; private set; } = false;

        private static I2cDevice I2CDevice;

        public BusMode Bus { get; set; } = BusMode.I2C;

        public string I2cControllerName { get; set; } = "I2C1";  /* For Raspberry Pi 2, use I2C1 */

        public Temperature Temperature => Temperature.From(GetTemperature(), TemperatureUnit.DegreeCelsius);

        public Pressure Pressure => Pressure.From(GetPressue(), PressureUnit.Pascal);

        public void Initialise()
        {
            if (!IsInitialised)
            {
                EnsureInitializedAsync().Wait();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (IsInitialised) { return; }

            try
            {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2cControllerName);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */
                I2CDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */


                ReadCoefficients();

                I2CDevice.Write(new byte[] { (byte)Register.BMP280_REGISTER_CONTROL, 0x3F });


                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }

        void ReadCoefficients()
        {
            bmp280_calib.dig_T1 = read16_LE(Register.BMP280_REGISTER_DIG_T1);
            bmp280_calib.dig_T2 = readS16_LE(Register.BMP280_REGISTER_DIG_T2);
            bmp280_calib.dig_T3 = readS16_LE(Register.BMP280_REGISTER_DIG_T3);

            bmp280_calib.dig_P1 = read16_LE(Register.BMP280_REGISTER_DIG_P1);
            bmp280_calib.dig_P2 = readS16_LE(Register.BMP280_REGISTER_DIG_P2);
            bmp280_calib.dig_P3 = readS16_LE(Register.BMP280_REGISTER_DIG_P3);
            bmp280_calib.dig_P4 = readS16_LE(Register.BMP280_REGISTER_DIG_P4);
            bmp280_calib.dig_P5 = readS16_LE(Register.BMP280_REGISTER_DIG_P5);
            bmp280_calib.dig_P6 = readS16_LE(Register.BMP280_REGISTER_DIG_P6);
            bmp280_calib.dig_P7 = readS16_LE(Register.BMP280_REGISTER_DIG_P7);
            bmp280_calib.dig_P8 = readS16_LE(Register.BMP280_REGISTER_DIG_P8);
            bmp280_calib.dig_P9 = readS16_LE(Register.BMP280_REGISTER_DIG_P9);
        }



        byte read8(Register reg)
        {
            byte[] result = new byte[1];
            I2CDevice.WriteRead(new byte[] { (byte)reg }, result);
            return result[0];
        }

        ushort read16(Register reg)
        {
            byte[] result = new byte[2];
            I2CDevice.WriteRead(new byte[] { (byte)reg, 0x00 }, result);
            return (ushort)(result[0] << 8 | result[1]);
        }


        ushort read16_LE(Register reg)
        {
            ushort temp = read16(reg);
            return (ushort)(temp >> 8 | temp << 8);
        }

        short readS16(Register reg) => (short)read16(reg);



        short readS16_LE(Register reg)
        {
            return (short)read16_LE(reg);

        }


        private double GetTemperature()
        {
            lock (temperatureLock)
            {

                Initialise();

                int var1, var2;

                int adc_T = read16(Register.BMP280_REGISTER_TEMPDATA);

                adc_T <<= 8;
                adc_T |= read8(Register.BMP280_REGISTER_TEMPDATA + 2);
                adc_T >>= 4;

                var1 = ((((adc_T >> 3) - ((int)bmp280_calib.dig_T1 << 1))) *
                     ((int)bmp280_calib.dig_T2)) >> 11;

                var2 = (((((adc_T >> 4) - ((int)bmp280_calib.dig_T1)) *
                       ((adc_T >> 4) - ((int)bmp280_calib.dig_T1))) >> 12) *
                     ((int)bmp280_calib.dig_T3)) >> 14;

                t_fine = var1 + var2;

                double T = (t_fine * 5 + 128) >> 8;
                return Math.Round(T / 100D, 2);
            }
        }

        private double GetPressue()
        {
            GetTemperature(); // the pressure reading has a dependency of temperature

            lock (pressureLock)
            {

                Initialise();

                long var1, var2, p;

                int adc_P = read16(Register.BMP280_REGISTER_PRESSUREDATA);
                adc_P <<= 8;
                adc_P |= read8(Register.BMP280_REGISTER_PRESSUREDATA + 2);
                adc_P >>= 4;

                var1 = ((long)t_fine) - 128000;
                var2 = var1 * var1 * (long)bmp280_calib.dig_P6;

                var2 = var2 + ((var1 * (long)bmp280_calib.dig_P5) << 17);

                var2 = var2 + (((long)bmp280_calib.dig_P4) << 35);
                var1 = ((var1 * var1 * (long)bmp280_calib.dig_P3) >> 8) +
                  ((var1 * (long)bmp280_calib.dig_P2) << 12);
                var1 = (((((long)1) << 47) + var1)) * ((long)bmp280_calib.dig_P1) >> 33;

                if (var1 == 0)
                {
                    return 0;  // avoid exception caused by division by zero
                }
                p = 1048576 - adc_P;
                p = (((p << 31) - var2) * 3125) / var1;
                var1 = (((long)bmp280_calib.dig_P9) * (p >> 13) * (p >> 13)) >> 25;
                var2 = (((long)bmp280_calib.dig_P8) * p) >> 19;

                p = ((p + var1 + var2) >> 8) + (((long)bmp280_calib.dig_P7) << 4);


                return Math.Round(p / 256D, 2);
            }
        }


        public void Dispose()
        {
            I2CDevice?.Dispose(); // c# checks for null then call dispose
            I2CDevice = null;
        }
    }
}
