using System;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace Glovebox.IoT.Devices.Sensors
{
    // support BMP085 and BMP180 chipsets
    // code migrated from :-
        //https://github.com/adafruit/Adafruit_BMP085_Unified/blob/master/Adafruit_BMP085_U.cpp
        // https://github.com/unixphere/bmp18x/blob/master/bmp18x-core.c
    public class BMP18x : IDisposable
    {
        enum Register : byte
        {
            BMP085_REGISTER_CAL_AC1 = 0xAA,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC2 = 0xAC,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC3 = 0xAE,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC4 = 0xB0,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC5 = 0xB2,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_AC6 = 0xB4,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_B1 = 0xB6,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_B2 = 0xB8,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MB = 0xBA,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MC = 0xBC,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CAL_MD = 0xBE,  // R   Calibration data (16 bits)
            BMP085_REGISTER_CHIPID = 0xD0,
            BMP085_REGISTER_VERSION = 0xD1,
            BMP085_REGISTER_SOFTRESET = 0xE0,
            BMP085_REGISTER_CONTROL = 0xF4,
            BMP085_REGISTER_TEMPDATA = 0xF6,
            BMP085_REGISTER_PRESSUREDATA = 0xF6,
            BMP085_REGISTER_READTEMPCMD = 0x2E,
            BMP085_REGISTER_READPRESSURECMD = 0x34           
        };

        public enum Mode
        {
            ULTRALOWPOWER = 0,
            STANDARD = 1,
            HIGHRES = 2,
            ULTRAHIGHRES = 3
        }


        public struct bmp085_calib_data
        {
            public short ac1;
            public short ac2;
            public short ac3;
            public ushort ac4;
            public ushort ac5;
            public ushort ac6;
            public short b1;
            public short b2;
            public short mb;
            public short mc;
            public short md;
        }


        bmp085_calib_data _bmp085_coeffs = new bmp085_calib_data();

        const int BMP085_TEMP_CONVERSION_TIME = 5;

        Mode _bmp085Mode = Mode.STANDARD;

        public int I2C_ADDRESS { get; set; } = 0x77;

        static object temperatureLock = new object();
        static object pressureLock = new object();

        public static bool IsInitialised { get; private set; } = false;

        private static I2cDevice I2CDevice;

        public string I2cControllerName { get; set; } = "I2C1";  /* For Raspberry Pi 2, use I2C1 */

        public Temperature Temperature => Temperature.From(GetTemperature(), TemperatureUnit.DegreeCelsius);

        public Pressure Pressure => Pressure.From(GetPressure(), PressureUnit.Pascal);


        public BMP18x(Mode mode= Mode.STANDARD)
        {
            _bmp085Mode = mode;
        }

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

                byte id = read8(Register.BMP085_REGISTER_CHIPID);
                if (id != 0x55)
                {
                    throw new Exception("Device is not a BMP180");
                }

                ReadCoefficients();

                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }

        void ReadCoefficients()
        {
            _bmp085_coeffs.ac1 = readS16(Register.BMP085_REGISTER_CAL_AC1);
            _bmp085_coeffs.ac2 = readS16(Register.BMP085_REGISTER_CAL_AC2);
            _bmp085_coeffs.ac3 = readS16(Register.BMP085_REGISTER_CAL_AC3);
            _bmp085_coeffs.ac4 = read16(Register.BMP085_REGISTER_CAL_AC4);
            _bmp085_coeffs.ac5 = read16(Register.BMP085_REGISTER_CAL_AC5);
            _bmp085_coeffs.ac6 = read16(Register.BMP085_REGISTER_CAL_AC6);
            _bmp085_coeffs.b1 = readS16(Register.BMP085_REGISTER_CAL_B1);
            _bmp085_coeffs.b2 = readS16(Register.BMP085_REGISTER_CAL_B2);
            _bmp085_coeffs.mb = readS16(Register.BMP085_REGISTER_CAL_MB);
            _bmp085_coeffs.mc = readS16(Register.BMP085_REGISTER_CAL_MC);
            _bmp085_coeffs.md = readS16(Register.BMP085_REGISTER_CAL_MD);
        }

        async Task<int> readRawTemperature()
        {
            writeCommand(Register.BMP085_REGISTER_CONTROL, Register.BMP085_REGISTER_READTEMPCMD);
            await Task.Delay(BMP085_TEMP_CONVERSION_TIME);
            return read16(Register.BMP085_REGISTER_TEMPDATA);
        }

        async Task<int> readRawPressure()
        {
            byte p8;
            uint p16;
            int p32;

            writeCommand(Register.BMP085_REGISTER_CONTROL, Register.BMP085_REGISTER_READPRESSURECMD + (byte)((byte)_bmp085Mode << 6));

            switch (_bmp085Mode)
            {
                case Mode.ULTRALOWPOWER:
                    await Task.Delay(5);
                    break;
                case Mode.STANDARD:
                    await Task.Delay(8);
                    break;
                case Mode.HIGHRES:
                    await Task.Delay(14);
                    break;
                case Mode.ULTRAHIGHRES:
                    await Task.Delay(26);
                    break;
            }

            p16 = read16(Register.BMP085_REGISTER_PRESSUREDATA);
            p32 = (int)(p16 << 8);
            p8 = read8(Register.BMP085_REGISTER_PRESSUREDATA + 2);
            p32 += p8;
            p32 >>= (8 - (byte)_bmp085Mode);

            return p32;
        }

        double GetTemperature()
        {
            lock (temperatureLock)
            {
                Initialise();

                int UT, B5;     // following ds convention
                float t;

                UT = readRawTemperature().Result;

                B5 = computeB5(UT);
                t = (B5 + 8) >> 4;
                t /= 10;

                return t;
            }
        }

        int computeB5(int ut)
        {
            int X1 = (ut - (int)_bmp085_coeffs.ac6) * ((int)_bmp085_coeffs.ac5) >> 15;
            int X2 = ((int)_bmp085_coeffs.mc << 11) / (X1 + (int)_bmp085_coeffs.md);
            return X1 + X2;
        }


        double GetPressure()
        {
            lock (pressureLock)
            {
                Initialise();

                int ut = 0, up = 0, compp = 0;
                int x1, x2, b5, b6, x3, b3, p;
                uint b4, b7;

                /* Get the raw pressure and temperature values */
                ut = readRawTemperature().Result;
                up = readRawPressure().Result;

                /* Temperature compensation */
                b5 = computeB5(ut);

                /* Pressure compensation */
                b6 = b5 - 4000;
                x1 = (_bmp085_coeffs.b2 * ((b6 * b6) >> 12)) >> 11;
                x2 = (_bmp085_coeffs.ac2 * b6) >> 11;
                x3 = x1 + x2;
                b3 = (((((int)_bmp085_coeffs.ac1) * 4 + x3) << (byte)_bmp085Mode) + 2) >> 2;
                x1 = (_bmp085_coeffs.ac3 * b6) >> 13;
                x2 = (_bmp085_coeffs.b1 * ((b6 * b6) >> 12)) >> 16;
                x3 = ((x1 + x2) + 2) >> 2;
                b4 = (_bmp085_coeffs.ac4 * (uint)(x3 + 32768)) >> 15;
                b7 = ((uint)(up - b3) * (uint)(50000 >> (byte)_bmp085Mode));

                if (b7 < 0x80000000)
                {
                    p = (int)((b7 << 1) / b4);
                }
                else
                {
                    p = (int)((b7 / b4) << 1);
                }

                x1 = (p >> 8) * (p >> 8);
                x1 = (x1 * 3038) >> 16;
                x2 = (-7357 * p) >> 16;
                compp = p + ((x1 + x2 + 3791) >> 4);

                /* Assign compensated pressure value */
                return compp;
            }
        }

        void writeCommand(Register reg, Register value)
        {
            byte[] result = new byte[2];
            I2CDevice.Write(new byte[] { (byte)reg, (byte)value });
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


        public void Dispose()
        {
            I2CDevice.Dispose();
        }
    }
}
