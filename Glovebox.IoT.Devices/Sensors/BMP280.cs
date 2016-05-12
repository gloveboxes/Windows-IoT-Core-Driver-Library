using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Glovebox.IoT.Devices.Sensors {


    // code migrated from https://raw.githubusercontent.com/adafruit/Adafruit_BMP280_Library/master/Adafruit_BMP280.cpp
    // http://lxr.free-electrons.com/source/drivers/iio/pressure/BMP280.c
    // https://github.com/BoschSensortec/BMP280_driver
    // https://github.com/todotani/BME280_Test
    public class BMP280 : IDisposable {

        const byte DeviceSignature = 0x60;
        //Method to write the control measurment register (default 0xB7)
        //010  101  11 
        // ↑  ↑   ↑ mode
        // ↑  ↑ Pressure oversampling 0x05
        // ↑ Temperature oversampling 0x05
        const byte TempPressure16xOverSampling = 0xB7; // 16x ovesampling for Temperature and Pressure

        protected enum Register : byte {

            REGISTER_DIG_T1 = 0x88,
            REGISTER_DIG_T2 = 0x8A,
            REGISTER_DIG_T3 = 0x8C,

            REGISTER_DIG_P1 = 0x8E,
            REGISTER_DIG_P2 = 0x90,
            REGISTER_DIG_P3 = 0x92,
            REGISTER_DIG_P4 = 0x94,
            REGISTER_DIG_P5 = 0x96,
            REGISTER_DIG_P6 = 0x98,
            REGISTER_DIG_P7 = 0x9A,
            REGISTER_DIG_P8 = 0x9C,
            REGISTER_DIG_P9 = 0x9E,

            REGISTER_DIG_H1 = 0xA1,
            REGISTER_DIG_H2 = 0xE1,
            REGISTER_DIG_H3 = 0xE3,
            REGISTER_DIG_H4 = 0xE4,
            REGISTER_DIG_H5 = 0xE5,
            REGISTER_DIG_H6 = 0xE7,

            REGISTER_CHIPID = 0xD0,
            REGISTER_VERSION = 0xD1,
            REGISTER_SOFTRESET = 0xE0,

            REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            REGISTER_CONTROLHUMID = 0xF2,
            REGISTER_CONTROL = 0xF4,
            REGISTER_CONFIG = 0xF5,
            REGISTER_PRESSUREDATA = 0xF7,
            REGISTER_TEMPDATA = 0xFA,
            REGISTER_HUMIDDATA = 0xFD,
        };

        protected struct calib_data {
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

        public enum BusMode {
            I2C
        }

        protected calib_data calibration = new calib_data();

        protected int t_fine;

        private int I2C_ADDRESS { get; set; } = 0x77;
        public string I2cControllerName { get; set; } = "I2C1";  /* For Raspberry Pi 2, use I2C1 */

        static object temperatureLock = new object();
        static object pressureLock = new object();

        public static bool IsInitialised { get; private set; } = false;

        private static I2cDevice I2CDevice;

        public Temperature Temperature => Temperature.From(GetTemperature(), TemperatureUnit.DegreeCelsius);
        public Pressure Pressure => Pressure.From(GetPressure(), PressureUnit.Pascal);

        public BMP280(int i2cAddress = 0x77) {
            I2C_ADDRESS = i2cAddress;
        }

        private void Initialise() {
            if (!IsInitialised) {
                EnsureInitializedAsync().Wait();
            }
        }

        private async Task EnsureInitializedAsync() {
            if (IsInitialised) { return; }

            try {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;

                string aqs = I2cDevice.GetDeviceSelector(I2cControllerName);  /* Find the selector string for the I2C bus controller                   */
                var dis = await DeviceInformation.FindAllAsync(aqs);            /* Find the I2C bus controller device with our selector string           */
                I2CDevice = await I2cDevice.FromIdAsync(dis[0].Id, settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */

                byte[] readChipID = new byte[] { (byte)Register.REGISTER_CHIPID };
                byte[] ReadBuffer = new byte[] { 0xFF };


                //I2CDevice.WriteRead(readChipID, ReadBuffer);    //Read the device signature              
                //if (ReadBuffer[0] != DeviceSignature) {        //Verify the device signature
                //    return;
                //}

                ReadCoefficients();

                InitiliseRegisters();

                IsInitialised = true;
            }
            catch (Exception ex) {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }

        protected virtual void InitiliseRegisters() {
            Write8(Register.REGISTER_CONTROL, TempPressure16xOverSampling); 
        }


        protected virtual void ReadCoefficients() {
            calibration.dig_T1 = Read16_LE(Register.REGISTER_DIG_T1);
            calibration.dig_T2 = ReadS16_LE(Register.REGISTER_DIG_T2);
            calibration.dig_T3 = ReadS16_LE(Register.REGISTER_DIG_T3);

            calibration.dig_P1 = Read16_LE(Register.REGISTER_DIG_P1);
            calibration.dig_P2 = ReadS16_LE(Register.REGISTER_DIG_P2);
            calibration.dig_P3 = ReadS16_LE(Register.REGISTER_DIG_P3);
            calibration.dig_P4 = ReadS16_LE(Register.REGISTER_DIG_P4);
            calibration.dig_P5 = ReadS16_LE(Register.REGISTER_DIG_P5);
            calibration.dig_P6 = ReadS16_LE(Register.REGISTER_DIG_P6);
            calibration.dig_P7 = ReadS16_LE(Register.REGISTER_DIG_P7);
            calibration.dig_P8 = ReadS16_LE(Register.REGISTER_DIG_P8);
            calibration.dig_P9 = ReadS16_LE(Register.REGISTER_DIG_P9);
        }

        protected void Write8(Register reg, byte value) {
            I2CDevice.Write(new byte[] { (byte)reg, value });
        }

        protected byte Read8(Register reg) {
            byte[] result = new byte[1];
            I2CDevice.WriteRead(new byte[] { (byte)reg }, result);
            return result[0];
        }

        protected ushort Read16(Register reg) {
            byte[] result = new byte[2];
            I2CDevice.WriteRead(new byte[] { (byte)reg, 0x00 }, result);
            return (ushort)(result[0] << 8 | result[1]);
        }


        ushort Read16_LE(Register reg) {
            ushort temp = Read16(reg);
            return (ushort)(temp >> 8 | temp << 8);
        }

        short ReadS16(Register reg) => (short)Read16(reg);


        protected short ReadS16_LE(Register reg) {
            return (short)Read16_LE(reg);
        }

        Int32 Read24(Register reg) {
            byte[] result = new byte[3];
            I2CDevice.WriteRead(new byte[] { (byte)reg, 0x00 }, result);
            return result[0] << 16 | result[1] << 8 | result[2];
        }


        protected double GetTemperature() {
            lock (temperatureLock) {

                Initialise();

                Int32 var1, var2;
                Int32 adc_T = Read24(Register.REGISTER_TEMPDATA);

                adc_T >>= 4;

                var1 = ((((adc_T >> 3) - ((Int32)calibration.dig_T1 << 1))) * ((Int32)calibration.dig_T2)) >> 11;

                var2 = (((((adc_T >> 4) - ((Int32)calibration.dig_T1)) *
                       ((adc_T >> 4) - ((Int32)calibration.dig_T1))) >> 12) *
                     ((Int32)calibration.dig_T3)) >> 14;

                t_fine = var1 + var2;

                double T = (t_fine * 5 + 128) >> 8;
                return Math.Round(T / 100D, 2);
            }
        }

        private double GetPressure() {

            GetTemperature(); // the pressure reading has a dependency of temperature

            lock (pressureLock) {

                Initialise();

                long var1, var2, p;
                int adc_P = Read24(Register.REGISTER_PRESSUREDATA);

                adc_P >>= 4;

                var1 = ((long)t_fine) - 128000;
                var2 = var1 * var1 * (long)calibration.dig_P6;

                var2 = var2 + ((var1 * (long)calibration.dig_P5) << 17);

                var2 = var2 + (((long)calibration.dig_P4) << 35);
                var1 = ((var1 * var1 * (long)calibration.dig_P3) >> 8) + ((var1 * (long)calibration.dig_P2) << 12);
                var1 = (((((long)1) << 47) + var1)) * ((long)calibration.dig_P1) >> 33;

                if (var1 == 0) {
                    return 0;  // avoid exception caused by division by zero
                }
                p = 1048576 - adc_P;
                p = (((p << 31) - var2) * 3125) / var1;
                var1 = (((long)calibration.dig_P9) * (p >> 13) * (p >> 13)) >> 25;
                var2 = (((long)calibration.dig_P8) * p) >> 19;

                p = ((p + var1 + var2) >> 8) + (((long)calibration.dig_P7) << 4);

                return Math.Round(p / 256D, 2);
            }
        }

        public void Dispose() {
            I2CDevice?.Dispose(); // c# checks for null then call dispose
            I2CDevice = null;
        }
    }
}
