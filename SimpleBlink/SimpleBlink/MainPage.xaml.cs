using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SimpleBlink
{
    public sealed partial class MainPage : Page
    {
        DispatcherTimer m_dt;
        private GpioPin m_pin26;
        private GpioPin m_pin47;
        private bool m_state;

        public MainPage()
        {
            this.InitializeComponent();
            setup();

        }

        public async void setup()
        {
            var gpio = GpioController.GetDefault();
            m_state = false;
            m_pin47 = gpio.OpenPin(47); //Taki pin na płycie
            m_pin26 = gpio.OpenPin(26); //26 - przedostatni z tyłu w 2 rzędzie od krawędzi. Tu czerwone, +. GND jest obok (ostatni)
            m_pin26.SetDriveMode(GpioPinDriveMode.Output);
            m_pin47.SetDriveMode(GpioPinDriveMode.Output);
            m_dt = new DispatcherTimer();
            m_dt.Interval = TimeSpan.FromMilliseconds(500);
            m_dt.Tick += M_dt_Tick;
            m_dt.Start();
        }

        private void M_dt_Tick(object sender, object e)
        {
            if (m_state)
            {
                m_pin26.Write(GpioPinValue.High);
                m_pin47.Write(GpioPinValue.Low);
            } else
            {
                m_pin26.Write(GpioPinValue.Low);
                m_pin47.Write(GpioPinValue.High);
            }
            m_state = !m_state;
        }
    }
}
