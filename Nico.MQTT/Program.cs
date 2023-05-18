using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Nico.MQTT;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Text;

public class Program
{

    #region Fields

    public static Guid UNIQUE_IDENTIFIER { get; set; } = Guid.NewGuid(); //Identifieur unique pour le client MQTT
    public static string MQTT_SERVER_ADDRESS = "192.168.2.133"; //IP du serveur MQTT
    private static IMqttClient? _mqttClient { get; set; } //MQTT client interface


    static Meter s_meter = new Meter("DataStore", "1.0.0"); //Telemétrie
    static int s_alarm = 0;
    static int s_humidity = 0;
    static int s_luminosity = 0;
    static int s_temperature = 0;


    //champs contenant un lamda (fonction dynamique) pour le callBack
    //lorsque le client MQTT reçois des données aux quelles il a souscris
    static Func<MqttApplicationMessageReceivedEventArgs, Task> SubscribeCallBack = async msg =>
    {
        Console.WriteLine($"__________");
        var topic = msg?.ApplicationMessage?.Topic;
        if (string.IsNullOrWhiteSpace(topic))
        {
            Console.WriteLine($"Invalid Topic name : {topic}");
            Console.WriteLine($"__________");
            return;
        }

        var payloadText = await Task.Run(() => Encoding.UTF8.GetString(msg?.ApplicationMessage?.Payload ?? Array.Empty<byte>()));
        if (string.IsNullOrWhiteSpace(payloadText))
        {
            Console.WriteLine($"Invalid Payload : {payloadText}");
            Console.WriteLine($"__________");
            return;
        }


        if (!int.TryParse(payloadText, out var payloadInt))
        {
            Console.WriteLine($"Payload not int : {payloadText}");
            Console.WriteLine($"__________");
            return;
        }

        //CsvManager.AppendTopic(topic, payloadInt); //Enregistrement en local des données dans un CSV

        if (topic == "Alarm")
        {
            s_alarm = payloadInt;
        }
        else if (topic == "humidity")
        {
            s_humidity = payloadInt;
        }
        else if (topic == "luminosity")
        {
            s_luminosity = payloadInt;
        }
        else if (topic == "temperature")
        {
            s_temperature = payloadInt;
        }

        Console.WriteLine($"{topic} : {payloadInt}");
        Console.WriteLine($"__________");
    };

    #endregion

    //point d'entrée de l'app
    public static void Main()
    {
        if (File.Exists("ID.txt") && File.Exists("IP.txt"))
        {
            UNIQUE_IDENTIFIER = Guid.Parse(File.ReadAllText("ID.txt"));
            MQTT_SERVER_ADDRESS = File.ReadAllText("IP.txt");
        }
        else
            SaveSettings();

        Console.WriteLine($"Hello world ! Here is my ID : {UNIQUE_IDENTIFIER}");
        Console.WriteLine($"CMD : 'sub', 'red', 'exit'");
        SetUp(); //set up du client MQTT

        s_meter.CreateObservableCounter<int>("Alarm", () => s_alarm, "On/Off", "1 => Alarm enabled / 0 => alarm disabled");
        s_meter.CreateObservableCounter<int>("humidity", () => s_humidity, "%", "1 => Humidity");
        s_meter.CreateObservableCounter<int>("luminosity", () => s_luminosity, "%", "");
        s_meter.CreateObservableCounter<int>("temperature", () => s_temperature, "Degree Celcius", "temperature");

        using MeterProvider meterProvider = Sdk.CreateMeterProviderBuilder()
                .AddMeter("DataStore")
                .AddPrometheusExporter(opt =>
                {
                    opt.StartHttpListener = true;
                    opt.HttpListenerPrefixes = new string[] { $"http://localhost:9184/" };
                })
                .Build(); //création du reporting


        var cmd = ""; //boucles des commandes :
        do
        {
            cmd = Console.ReadLine();

            switch (cmd)
            {
                case "exit":
                    break;

                case "sub":
                    Console.WriteLine($"topic : ");
                    cmd = Console.ReadLine() ?? "#";
                    _ = SubscribeToTopic(cmd);
                    break;

                case "red":
                    Console.WriteLine($"topic : ");
                    cmd = Console.ReadLine() ?? "#";
                    CsvManager.ReadTopic(cmd);
                    break;

                default:
                    Console.WriteLine($"Uknown cmd : {cmd}");
                    break;
            }
        }
        while (cmd != "exit");

    }
    public static async void SetUp()
    {
        await InitiateClient();
    }
    public static void SaveSettings()
    {
        File.WriteAllText("ID.txt", UNIQUE_IDENTIFIER.ToString());
        File.WriteAllText("IP.txt", MQTT_SERVER_ADDRESS);
    }

    public static async Task InitiateClient()
    {
        var mqttFactory = new MqttFactory();
        var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(MQTT_SERVER_ADDRESS, 1883).Build();
        _mqttClient = mqttFactory.CreateMqttClient();
        await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        Console.WriteLine($"Client started");
        _mqttClient.ApplicationMessageReceivedAsync += SubscribeCallBack;
    }
    public static async Task SubscribeToTopic(string topic = "#")
    {
        topic = string.IsNullOrWhiteSpace(topic) ? "#" : topic;

        var mqttFactory = new MqttFactory();
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
           .WithTopicFilter(
               f =>
               {
                   f.WithTopic(topic);
               }).Build();

        var response = await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
    }

}