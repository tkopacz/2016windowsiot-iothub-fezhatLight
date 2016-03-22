using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using GHIElectronics.UWP.Shields;
using Newtonsoft.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DemoFezHatLightIotServiceFabric
{
    public class MIoTBase
    {
        public DateTime Dt { get; set; }
        public string MsgType { get; set; }
    }
    public class MAll : MIoTBase
    {
        public double Light { get; set; }
        public double Temperature { get; internal set; }
        public bool DIO18 { get; internal set; }
        public bool DIO22 { get; internal set; }
    }

    /* ***************************
    Somewhere:
    public sealed partial class MainPage : Page
    {
        public static string TKConnectionString = "HostName=pltkdpepliot2016.azure-devices.net;DeviceId=RPi2FezHat;SharedAccessKey=<own>";
    }
    *************************** */
    public sealed partial class MainPage : Page
    {
        private DeviceClient m_clt;
        private DispatcherTimer m_t;
        private FEZHAT m_hat;

        /// <summary>
        /// Total number of messages (All, IoT Client Lib)
        /// </summary>
        int m_msgCount = 0;

        int MaxMsgCount = 2000;

        public MainPage()
        {
            this.InitializeComponent();
            setup();
        }
        private async void setup()
        {
            try
            {
                //0.IoTHub client
                m_clt = DeviceClient.CreateFromConnectionString(TKConnectionString, TransportType.Http1);
                Task.Run(() => ReceiveDataFromAzure()); //Loop. 

                //1. Fez Hat
                m_hat = await GHIElectronics.UWP.Shields.FEZHAT.CreateAsync();

                //2. Timest
                m_t = new DispatcherTimer();
                m_t.Interval = TimeSpan.FromMilliseconds(5000);
                m_t.Tick += M_t_Tick;

                //Can enable UI
                txtAll.IsEnabled = tgSend.IsEnabled = true;
                tgSend.IsOn = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                txtState.Text = ex.ToString();
            }


        }
        private async void M_t_Tick(object sender, object e)
        {
            if (m_msgCount >= MaxMsgCount && MaxMsgCount != -1)
            {
                //No more than MaxMsgCount messages / run
                m_t.Stop(); return;
            }

            MAll m = new MAll();
            if (m_hat != null)
            {
                m.Light = m_hat.GetLightLevel();
                m.Temperature = m_hat.GetTemperature();
                m.DIO18 = m_hat.IsDIO18Pressed();
                m.DIO22 = m_hat.IsDIO22Pressed();
            }
            m.MsgType = "FEZHATALL";
            m.Dt = DateTime.UtcNow;
            var obj = JsonConvert.SerializeObject(m);
            try
            {
                if (m_clt != null)
                {
                    await m_clt.SendEventAsync(new Message(System.Text.Encoding.UTF8.GetBytes(obj)));
                    m_msgCount++;
                }
                txtState.Text = obj + $", MSG:{m_msgCount}";
            }
            catch (Exception ex)
            {
                txtState.Text = ex.ToString();
            }
        }

        private void tgSend_Toggled(object sender, RoutedEventArgs e)
        {
            if (tgSend.IsOn)
            {
                m_t.Start();
            }
            else
            {
                m_t.Stop();
            }
        }

        private void txtAll_TextChanged(object sender, TextChangedEventArgs e)
        {
            double val;
            if (double.TryParse(txtAll.Text, out val) && val > 0)
            {
                m_t.Interval = TimeSpan.FromMilliseconds(val);
            }
        }


        public async Task ReceiveDataFromAzure()
        {

            Message receivedMessage;
            string messageData;
            if (m_clt != null)
            {
                while (true)
                {
                    try
                    {
                        receivedMessage = await m_clt.ReceiveAsync();

                        if (receivedMessage != null)
                        {
                            messageData = System.Text.Encoding.ASCII.GetString(receivedMessage.GetBytes());
                            //Wykonanie polecenia
                            if (messageData.Length >= 2)
                            {
                                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    double val;
                                    switch (messageData[0])
                                    {
                                        case 'L':
                                            if (m_hat!=null)
                                            {
                                                try
                                                {
                                                    string[] s = messageData.Substring(1).Split(',');
                                                    FEZHAT.Color c = new FEZHAT.Color(byte.Parse(s[0]), byte.Parse(s[1]), byte.Parse(s[2]));
                                                    m_hat.D2.Color = c;
                                                }
                                                catch { }
                                            }
                                            break;
                                        case 'A':
                                            //All messages - interval
                                            if (double.TryParse(messageData.Substring(1), out val))
                                            {
                                                txtAll.Text = val.ToString();
                                            }
                                            break;
                                        case 'O':
                                            //On / Off
                                            if (messageData[1] == '0')
                                                tgSend.IsOn = false;
                                            else
                                                tgSend.IsOn = true;
                                            break;
                                        default:
                                            break;
                                    }
                                });
                            }
                            //await m_clt.RejectAsync(receivedMessage);
                            //await m_clt.AbandonAsync(receivedMessage); - reject, will be redelivered
                            //Confirm
                            await m_clt.CompleteAsync(receivedMessage); //potwierdza odebranie
                        }
                    }
                    catch (Exception ex)
                    {
                        txtState.Text = ex.ToString();
                    }
                }
            }
        }


    }
}
