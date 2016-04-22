using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Glovebox.IoT.Devices.Sensors {


    // code migrated from https://raw.githubusercontent.com/adafruit/Adafruit_BME280_Library/master/Adafruit_BME280.cpp
    // http://lxr.free-electrons.com/source/drivers/iio/pressure/BME280.c
    // https://github.com/BoschSensortec/BME280_driver
    // https://raw.githubusercontent.com/todotani/BME280_Test/master/BME280_Test/BME280.cs
    public class BME280 : IDisposable {

        const byte BME280_Address = 0x76;
        const byte BME280_Signature = 0x60;


        enum Register : byte {

            BME280_REGISTER_DIG_T1 = 0x88,
            BME280_REGISTER_DIG_T2 = 0x8A,
            BME280_REGISTER_DIG_T3 = 0x8C,

            BME280_REGISTER_DIG_P1 = 0x8E,
            BME280_REGISTER_DIG_P2 = 0x90,
            BME280_REGISTER_DIG_P3 = 0x92,
            BME280_REGISTER_DIG_P4 = 0x94,
            BME280_REGISTER_DIG_P5 = 0x96,
            BME280_REGISTER_DIG_P6 = 0x98,
            BME280_REGISTER_DIG_P7 = 0x9A,
            BME280_REGISTER_DIG_P8 = 0x9C,
            BME280_REGISTER_DIG_P9 = 0x9E,

            BME280_REGISTER_DIG_H1 = 0xA1,
            BME280_REGISTER_DIG_H2 = 0xE1,
            BME280_REGISTER_DIG_H3 = 0xE3,
            BME280_REGISTER_DIG_H4 = 0xE4,
            BME280_REGISTER_DIG_H5 = 0xE5,
            BME280_REGISTER_DIG_H6 = 0xE7,

            BME280_REGISTER_CHIPID = 0xD0,
            BME280_REGISTER_VERSION = 0xD1,
            BME280_REGISTER_SOFTRESET = 0xE0,

            BME280_REGISTER_CAL26 = 0xE1,  // R calibration stored in 0xE1-0xF0

            BME280_REGISTER_CONTROLHUMID = 0xF2,
            BME280_REGISTER_CONTROL = 0xF4,
            BME280_REGISTER_CONFIG = 0xF5,
            BME280_REGISTER_PRESSUREDATA = 0xF7,
            BME280_REGISTER_TEMPDATA = 0xFA,
            BME280_REGISTER_HUMIDDATA = 0xFD,
        };



        public struct BME280_calib_data {
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

        BME280_calib_data BME280_calib = new BME280_calib_data();

        public enum interface_mode_e : byte {
            i2c = 0,
            spi = 1
        };

        // t_sb standby options - effectively the gap between automatic measurements 
        // when in "normal" mode
        public enum standbySettings_e : byte {
            tsb_0p5ms = 0,
            tsb_62p5ms = 1,
            tsb_125ms = 2,
            tsb_250ms = 3,
            tsb_500ms = 4,
            tsb_1000ms = 5,
            tsb_10ms = 6,
            tsb_20ms = 7
        };

        // sensor modes, it starts off in sleep mode on power on
        // forced is to take a single measurement now
        // normal takes measurements reqularly automatically
        public enum mode_e : byte {
            smSleep = 0,
            smForced = 1,
            smNormal = 3
        };


        // Filter coefficients
        // higher numbers slow down changes, such as slamming doors
        public enum filterCoefficient_e : byte {
            fc_off = 0,
            fc_2 = 1,
            fc_4 = 2,
            fc_8 = 3,
            fc_16 = 4
        };


        // Oversampling options for humidity
        // Oversampling reduces the noise from the sensor
        public enum oversampling_e : byte {
            osSkipped = 0,
            os1x = 1,
            os2x = 2,
            os4x = 3,
            os8x = 4,
            os16x = 5
        };

        // Value hold sensor operation parameters
        //private byte int_mode = (byte)interface_mode_e.i2c;
        //private byte t_sb;
        private byte mode;
        private byte filter;
        private byte osrs_p;
        private byte osrs_t;
        private byte osrs_h;

        int t_fine;


        public enum BusMode {
            SPI,
            I2C
        }

        public int I2C_ADDRESS { get; set; } = BME280_Address;

        static object temperatureLock = new object();
        static object pressureLock = new object();
        static object humidityLock = new object();

        public static bool IsInitialised { get; private set; } = false;

        private static I2cDevice I2CDevice;

        public BusMode Bus { get; set; } = BusMode.I2C;

        public string I2cControllerName { get; set; } = "I2C1";  /* For Raspberry Pi 2, use I2C1 */

        public Temperature Temperature => Temperature.From(GetTemperature(), TemperatureUnit.DegreeCelsius);

        public Pressure Pressure => Pressure.From(GetPressure(), PressureUnit.Pascal);

        public double Humidity => GetHumidity();

        public BME280(standbySettings_e t_sb = standbySettings_e.tsb_0p5ms,
              mode_e mode = mode_e.smNormal,
              filterCoefficient_e filter = filterCoefficient_e.fc_16,
              oversampling_e osrs_p = oversampling_e.os16x,
              oversampling_e osrs_t = oversampling_e.os16x,
              oversampling_e osrs_h = oversampling_e.os16x) {
            //this.t_sb = (byte)t_sb;
            this.mode = (byte)mode;
            this.filter = (byte)filter;
            this.osrs_p = (byte)osrs_p;
            this.osrs_t = (byte)osrs_t;
            this.osrs_h = (byte)osrs_h;
        }


        public void Initialise() {
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

                byte[] readChipID = new byte[] { (byte)Register.BME280_REGISTER_CHIPID };
                byte[] ReadBuffer = new byte[] { 0xFF };


                I2CDevice.WriteRead(readChipID, ReadBuffer);    //Read the device signature
              
                if (ReadBuffer[0] != BME280_Signature) {        //Verify the device signature
                    return;
                }

                ReadCoefficients();
 
                WriteControlRegisterHumidity();
                WriteControlRegisterTempPressureRegister();

                IsInitialised = true;
            }
            catch (Exception ex) {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }

        ////Method to write the config register (default 16)
        ////000  100  00 
        //// ↑  ↑   ↑I2C mode
        //// ↑  ↑Filter coefficient = 16
        //// ↑t_sb = 0.5ms
        //private void WriteConfigRegister() {
        //    byte value = (byte)(int_mode + (filter << 2) + (t_sb << 5));
        //    byte[] WriteBuffer = new byte[] { (byte)Register.BME280_REGISTER_CONFIG, value };
        //    I2CDevice.Write(WriteBuffer);
        //    return;
        //}

        //Method to write the control measurment register (default 87)
        //010  101  11 
        // ↑  ↑   ↑ mode
        // ↑  ↑ Pressure oversampling
        // ↑ Temperature oversampling
        private void WriteControlRegisterTempPressureRegister() {
            byte value = (byte)(mode + (osrs_p << 2) + (osrs_t << 5));
            I2CDevice.Write(new byte[] { (byte)Register.BME280_REGISTER_CONTROL, value });
        }

        //Method to write the humidity control register (default 01)
        private void WriteControlRegisterHumidity() {
            byte value = osrs_h;
            I2CDevice.Write(new byte[] { (byte)Register.BME280_REGISTER_CONTROLHUMID, value });
        }

        void ReadCoefficients() {
            BME280_calib.dig_T1 = read16_LE(Register.BME280_REGISTER_DIG_T1);
            BME280_calib.dig_T2 = readS16_LE(Register.BME280_REGISTER_DIG_T2);
            BME280_calib.dig_T3 = readS16_LE(Register.BME280_REGISTER_DIG_T3);

            BME280_calib.dig_P1 = read16_LE(Register.BME280_REGISTER_DIG_P1);
            BME280_calib.dig_P2 = readS16_LE(Register.BME280_REGISTER_DIG_P2);
            BME280_calib.dig_P3 = readS16_LE(Register.BME280_REGISTER_DIG_P3);
            BME280_calib.dig_P4 = readS16_LE(Register.BME280_REGISTER_DIG_P4);
            BME280_calib.dig_P5 = readS16_LE(Register.BME280_REGISTER_DIG_P5);
            BME280_calib.dig_P6 = readS16_LE(Register.BME280_REGISTER_DIG_P6);
            BME280_calib.dig_P7 = readS16_LE(Register.BME280_REGISTER_DIG_P7);
            BME280_calib.dig_P8 = readS16_LE(Register.BME280_REGISTER_DIG_P8);
            BME280_calib.dig_P9 = readS16_LE(Register.BME280_REGISTER_DIG_P9);

            BME280_calib.dig_H1 = read8(Register.BME280_REGISTER_DIG_H1);
            BME280_calib.dig_H2 = readS16_LE(Register.BME280_REGISTER_DIG_H2);
            BME280_calib.dig_H3 = read8(Register.BME280_REGISTER_DIG_H3);
            BME280_calib.dig_H4 = (short)((read8(Register.BME280_REGISTER_DIG_H4) << 4) | (read8(Register.BME280_REGISTER_DIG_H4 + 1) & 0xF));
            BME280_calib.dig_H5 = (short)((read8(Register.BME280_REGISTER_DIG_H5 + 1) << 4) | (read8(Register.BME280_REGISTER_DIG_H5) >> 4));
            BME280_calib.dig_H6 = (sbyte)read8(Register.BME280_REGISTER_DIG_H6);
        }



        byte read8(Register reg) {
            byte[] result = new byte[1];
            I2CDevice.WriteRead(new byte[] { (byte)reg }, result);
            return result[0];
        }

        ushort read16(Register reg) {
            byte[] result = new byte[2];
            I2CDevice.WriteRead(new byte[] { (byte)reg, 0x00 }, result);
            return (ushort)(result[0] << 8 | result[1]);
        }


        ushort read16_LE(Register reg) {
            ushort temp = read16(reg);
            return (ushort)(temp >> 8 | temp << 8);
        }

        short readS16(Register reg) => (short)read16(reg);



        short readS16_LE(Register reg) {
            return (short)read16_LE(reg);

        }


        private double GetTemperature() {
            lock (temperatureLock) {

                Initialise();

                int var1, var2;

                int adc_T = read16(Register.BME280_REGISTER_TEMPDATA);

                adc_T <<= 8;
                adc_T |= read8(Register.BME280_REGISTER_TEMPDATA + 2);
                adc_T >>= 4;

                var1 = ((((adc_T >> 3) - ((int)BME280_calib.dig_T1 << 1))) *
                     ((int)BME280_calib.dig_T2)) >> 11;

                var2 = (((((adc_T >> 4) - ((int)BME280_calib.dig_T1)) *
                       ((adc_T >> 4) - ((int)BME280_calib.dig_T1))) >> 12) *
                     ((int)BME280_calib.dig_T3)) >> 14;

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

                int adc_P = read16(Register.BME280_REGISTER_PRESSUREDATA);
                adc_P <<= 8;
                adc_P |= read8(Register.BME280_REGISTER_PRESSUREDATA + 2);
                adc_P >>= 4;

                var1 = ((long)t_fine) - 128000;
                var2 = var1 * var1 * (long)BME280_calib.dig_P6;

                var2 = var2 + ((var1 * (long)BME280_calib.dig_P5) << 17);

                var2 = var2 + (((long)BME280_calib.dig_P4) << 35);
                var1 = ((var1 * var1 * (long)BME280_calib.dig_P3) >> 8) + ((var1 * (long)BME280_calib.dig_P2) << 12);
                var1 = (((((long)1) << 47) + var1)) * ((long)BME280_calib.dig_P1) >> 33;

                if (var1 == 0) {
                    return 0;  // avoid exception caused by division by zero
                }
                p = 1048576 - adc_P;
                p = (((p << 31) - var2) * 3125) / var1;
                var1 = (((long)BME280_calib.dig_P9) * (p >> 13) * (p >> 13)) >> 25;
                var2 = (((long)BME280_calib.dig_P8) * p) >> 19;

                p = ((p + var1 + var2) >> 8) + (((long)BME280_calib.dig_P7) << 4);

                return Math.Round(p / 256D, 2);
            }
        }


        private double GetHumidity() {

            GetTemperature(); // the humidity reading has a dependency of temperature

            lock (humidityLock) {

                Int32 adc_H = read16(Register.BME280_REGISTER_HUMIDDATA);

                Int32 v_x1_u32r;

                v_x1_u32r = (t_fine - ((Int32)76800));

                v_x1_u32r = (((((adc_H << 14) - (((Int32)BME280_calib.dig_H4) << 20) -
                        (((Int32)BME280_calib.dig_H5) * v_x1_u32r)) + ((Int32)16384)) >> 15) *
                         (((((((v_x1_u32r * ((Int32)BME280_calib.dig_H6)) >> 10) *
                          (((v_x1_u32r * ((Int32)BME280_calib.dig_H3)) >> 11) + ((Int32)32768))) >> 10) +
                        ((Int32)2097152)) * ((Int32)BME280_calib.dig_H2) + 8192) >> 14));

                v_x1_u32r = (v_x1_u32r - (((((v_x1_u32r >> 15) * (v_x1_u32r >> 15)) >> 7) *
                               ((Int32)BME280_calib.dig_H1)) >> 4));

                v_x1_u32r = (v_x1_u32r < 0) ? 0 : v_x1_u32r;
                v_x1_u32r = (v_x1_u32r > 419430400) ? 419430400 : v_x1_u32r;
                float h = (v_x1_u32r >> 12);
                return h / 1024.0;
            }
        }



        public void Dispose() {
            I2CDevice?.Dispose(); // c# checks for null then call dispose
            I2CDevice = null;
        }
    }
}
