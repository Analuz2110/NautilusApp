using Plugin.Maui.Audio;
using Plugin.LocalNotification;
using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System.Text;
using MQTTnet.Protocol;
using System.Diagnostics;
using System.Globalization;

namespace NautilusApp
{
    public partial class MainPage : ContentPage
    {
        private readonly IAudioManager _audioManager;
        private IAudioPlayer _audioPlayer;
        private IMqttClient _mqttClient;
        private MqttClientConnectResult _connectResult;
        private Animation _pulsingAnimation;
        private static bool _isAlarmActive = false;
        private static Stopwatch _alarmTimer = new Stopwatch();

        public MainPage(IAudioManager audioManager)
        {
            InitializeComponent();
            _audioManager = audioManager;

            // Add the pulsing animation
            var scaleAnimation = new Animation(v => PulsingIcon.Scale = v, 1, 1.5);
            var fadeAnimation = new Animation(v => PulsingIcon.Opacity = v, 1, 0);

            _pulsingAnimation = new Animation();
            _pulsingAnimation.Add(0, 0.5, scaleAnimation);
            _pulsingAnimation.Add(0.5, 1, fadeAnimation);           

        }

        private async Task ConexaoBroker(string id)
        {
            string broker = "broker.emqx.io";
            int port = 1883;
            string clientId = Guid.NewGuid().ToString();
            string topic = "ufabc/" + id;
            string username = "emqx";
            string password = "public";

            // Create a MQTT client factory
            var factory = new MqttFactory();

            // Create a MQTT client instance
            _mqttClient = factory.CreateMqttClient();

            // Create MQTT client options
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(broker, port) // MQTT broker address and port
                .WithCredentials(username, password) // Set username and password
                .WithClientId(clientId)
                .WithCleanSession()
                .Build();
           
            _connectResult = await _mqttClient.ConnectAsync(options);

            if (_connectResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                Console.WriteLine("Connected to MQTT broker successfully.");
                LabelDescricao.Text = "Dispositivo conectado com suceso.";
                AbsoluteLayoutConectado.IsVisible = true;

                // Subscribe to a topic
                await _mqttClient.SubscribeAsync(topic);

                double gyroX, gyroY, gyroZ;

                // Callback function when a message is received
                _mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    string message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    string[] values = message.Split(';');                    

                    if(_alarmTimer.ElapsedMilliseconds > 15000)
                    {
                        _isAlarmActive = false;
                        _alarmTimer.Stop();
                        _alarmTimer.Reset();
                        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAA");
                        _pulsingAnimation.Dispose();
                        AbsoluteLayoutIcone.IsVisible = false;
                        AbsoluteLayoutConectado.IsVisible = true;
                        Parar.IsVisible = false;
                        LabelDescricao.Text = "Dispositivo conectado com suceso.";
                    }

                    if (values.Length >= 3)
                    {
                        gyroX = double.Parse(values[0], CultureInfo.InvariantCulture);
                        gyroY = double.Parse(values[1], CultureInfo.InvariantCulture); 

                        if ((gyroX > 1.1 || gyroX < -1.1 || gyroY > 1.1 || gyroY < -1.1) && !_alarmTimer.IsRunning)
                        {
                            AcionarAlarme();
                            LabelDescricao.Text = "ATENÇÃO: movimento detectado.";
                            AbsoluteLayoutConectado.IsVisible = false;
                            AbsoluteLayoutIcone.IsVisible = true;
                            Parar.IsVisible = true;
                            _isAlarmActive = true;
                            _alarmTimer.Start();

                            Console.WriteLine("ALARME ACIONADO.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid message format.");
                    }
                    Console.WriteLine($"Received message: {message}");
                    return Task.CompletedTask;
                };               
            }
            else
            {
                Console.WriteLine($"Failed to connect to MQTT broker: {_connectResult.ResultCode}");
            }

            while (true)
            {
                
                    await Task.Delay(1000);  // Adjust the delay as needed
            }         
        }

        private async void AcionarAlarme()
        {
            var request = new NotificationRequest
            {
                NotificationId = 1337,
                Title = "ALERTA!",
                Subtitle = "NautilusGuard",
                Description = "Identificamos um sinal de movimento em seu dispositivo.",
                BadgeNumber = 42,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1)
                }
            };

            LocalNotificationCenter.Current.Show(request);

            _audioPlayer = _audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("alarm.wav"));
            _audioPlayer.Play();

            PulsingIcon.BackgroundColor = Color.FromHex("#FF0000");
            _pulsingAnimation.Commit(this, "PulseAnimation", length: 2000, repeat: () => true);
        }
        private async void OnCounterClicked(object sender, EventArgs e)
        {
            ConexaoBroker(await DisplayPromptAsync("Insira o ID do seu dispositivo", ""));
        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {
            if(_audioPlayer != null)
            {
                _audioPlayer.Stop();
            }
            
        }
    }

}
