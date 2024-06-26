﻿using System.IO.Ports;
using FanControl.Plugins;

namespace FanControl.DexControllerSensor
{
    public class DexPlugin : IPlugin2
    {
        public string Name => "DexFan Water Sensor";

        static SerialPort? port;

        WaterSensor? sensor_temp;
        private readonly IPluginLogger logger;
        private readonly IPluginDialog dialog;


        public void Close()
        {
            port?.Close();
        }

        public DexPlugin(IPluginLogger a_logger, IPluginDialog a_dialog)
        {
            logger = a_logger;
            dialog = a_dialog;
        }

        public void Initialize()
        {

            try
            {
                port = new SerialPort("COM14");
                port.Open();
            }
            catch (IOException ex)
            {
                logger.Log($"Failed to open serial port with exception {ex}");
            }

        }

        public void Load(IPluginSensorsContainer container)
        {
            sensor_temp = new() { };
            container.TempSensors.Add(sensor_temp);


        }

        public void Update()
        {
            string? msg = null;
            try
            {
                // "t" requests a new temperature reading. May eventually protobuf or json this if more sensors materialize
                port?.WriteLine("t");
                msg = port?.ReadLine();
            }
            // If the serial port doesn't work, attempt to recover it
            // Fail safe behavior lower down is triggered by the message here not being changed from null
            catch (SystemException ex)
            {
                logger.Log("Exception while reading serial port: " + ex.Message);
                if (ex.Message == "The port is closed.")
                {

                    try
                    {
                        port?.Open();
                    }
                    catch (IOException eex)
                    {
                        logger.Log($"Failed to open serial port with exception {eex}");
                    }
                }
            }

            try
            {
                if (sensor_temp != null && msg != null)
                {
                    sensor_temp.Value = Int32.Parse(msg);
                    // fail safe in case of loose wire (results in negative reading)
                    if (sensor_temp.Value < 0)
                    {
                        sensor_temp.Value = 50;
                    }
                }
                else if (sensor_temp != null)
                {
                    // Fail safe in case of null message
                    sensor_temp.Value = 50;
                }
            }
            catch (FormatException e)
            {
                logger.Log(e.Message);
                if (sensor_temp != null)
                {
                    // Fail safe in case of parse exception
                    sensor_temp.Value = 50;
                }
            }
        }
    }

    public class WaterSensor : IPluginSensor
    {
        public string Id => "DexControllerWaterTempCS";

        public string Name => "Water Temperature (C)";

        public float? Value
        {
            get; set;
        }

        // Updates from plugin context
        public void Update()
        {
        }

    }
}