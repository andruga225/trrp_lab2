using lab0;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Server;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace socketServer
{
    internal class socketServer
    {
        private static IPAddress iPAddress = IPAddress.Parse(Server.config.Default.address);
        private static IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, int.Parse(Server.config.Default.port));

        private static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = config.Default.address,
            Port = config.Default.queuePort,
            UserName = config.Default.user,
            Password = config.Default.password,
        };
        private static IConnection connection = factory.CreateConnection();
        private static IModel channel = connection.CreateModel();

        private static lab0.dbNormalizer db = new lab0.dbNormalizer();

        static async Task Main(string[] args)
        {
            while (true)
            {
                new Thread(() => QueueConnection()).Start();
                await SocketConnection();
            }
        }

        private static void QueueConnection()
        {   
            channel.QueueDeclare(queue: "db", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                var receivedObject = JsonConvert.DeserializeObject<string[]>(message);

                var newItem = new dbItem();
                newItem.shopName = receivedObject[0];
                newItem.shopAddress = receivedObject[1];
                newItem.clientName = receivedObject[2];
                newItem.clientEmail = receivedObject[3];
                newItem.clientPhone = receivedObject[4];
                newItem.categotyName = receivedObject[5];
                newItem.distrName = receivedObject[6];
                newItem.itemId = (int)long.Parse(receivedObject[7]);
                newItem.itemName = receivedObject[8];
                newItem.amount = int.Parse(receivedObject[9]);
                newItem.price = decimal.Parse(receivedObject[10]);
                newItem.purchDate = DateTime.Parse(receivedObject[11]);

                var inserted = db.insertRow(newItem);
                Console.WriteLine($"Server received message: \"{newItem}\"");
            };


            channel.BasicConsume(queue: "db",
                                autoAck: true,
                                consumer: consumer);

        }

        private static async Task SocketConnection()
        {
            
            Socket listener = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            listener.Bind(iPEndPoint);
            listener.Listen(100);

            

            while (true)
            {
                var handler = await listener.AcceptAsync();

                var buffer = new byte[1024];
                var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);
                var receivedObject = JsonConvert.DeserializeObject<string[]>(response);

                var newItem = new dbItem();
                newItem.shopName = receivedObject[0];
                newItem.shopAddress = receivedObject[1];
                newItem.clientName = receivedObject[2];
                newItem.clientEmail = receivedObject[3];
                newItem.clientPhone = receivedObject[4];
                newItem.categotyName = receivedObject[5];
                newItem.distrName = receivedObject[6];
                newItem.itemId = (int)long.Parse(receivedObject[7]);
                newItem.itemName = receivedObject[8];
                newItem.amount = int.Parse(receivedObject[9]);
                newItem.price = decimal.Parse(receivedObject[10]);
                newItem.purchDate = DateTime.Parse(receivedObject[11]);

                var eom = "<|EOM|>";
                if (receivedObject[12]==eom)
                {
                    Console.WriteLine($"Socket server received message: \"{response.Replace(eom, "")}\"");

                    var ackMessage = db.insertRow(newItem);
                    var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
                    await handler.SendAsync(echoBytes, 0);

                    Console.WriteLine($"Socket server sent acknowledgment: \"{ackMessage}\"");

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
            }
        }
    }
}
