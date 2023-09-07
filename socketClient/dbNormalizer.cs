using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Configuration;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace lab0
{
    internal class dbNormalizer
    {

        private string _connStrSql;

        private static IPAddress iPAddress = IPAddress.Parse(config.Default.address);
        private static IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, int.Parse(config.Default.port));

        private static ConnectionFactory factory = new ConnectionFactory()
        {
            HostName = config.Default.address,
            Port = config.Default.queuePort,
            UserName = config.Default.user,
            Password = config.Default.password,
        };
        private static IConnection connection = factory.CreateConnection();
        private static IModel channel = connection.CreateModel();

        public dbNormalizer(string conStrSql, int type)
        {
            _connStrSql = conStrSql;
            readFromDbAsync(type);
        }

        private async Task readFromDbAsync(int type)
        {
            using var conn = new SqliteConnection(_connStrSql);
            conn.Open();

            using var sqlCommand = new SqliteCommand
            {
                Connection = conn,
                CommandText = @"SELECT * FROM data"
            };
            var reader = sqlCommand.ExecuteReader();

            while (reader.Read())
            {
                var newItem = new dbItem();
                newItem.shopName = (string)reader["shop_name"];
                newItem.shopAddress = (string)reader["shop_address"];
                newItem.clientName = (string)reader["client_name"];
                newItem.clientEmail = (string)reader["client_email"];
                newItem.clientPhone = (string)reader["client_phone"];
                newItem.categotyName = (string)reader["category"];
                newItem.distrName = (string)reader["distr"];
                newItem.itemId = (int)(long)reader["item_id"];
                newItem.itemName = (string)reader["item_name"];
                newItem.amount = (int)(long)reader["amount"];
                newItem.price = decimal.Parse((string)reader["price"]);
                newItem.purchDate = DateTime.Parse((string)reader["purch_date"]);

                if (type == 1)
                    await SocketConnection(newItem);
                if (type == 2)
                    QueueConnection(newItem);

            }

            channel.Close();
            connection.Close();
        }

        private void QueueConnection(dbItem item)
        {   
            //using (var connection = factory.CreateConnection())
            //{
                //using (var channel = connection.CreateModel())
                //{
                    channel.QueueDeclare(queue: "db", durable: false, exclusive: false, autoDelete: false, arguments: null);

                    var message = new string[13];
                    message[0] = item.shopName;
                    message[1] = item.shopAddress;
                    message[2] = item.clientName;
                    message[3] = item.clientEmail;
                    message[4] = item.clientPhone;
                    message[5] = item.categotyName;
                    message[6] = item.distrName;
                    message[7] = item.itemId.ToString();
                    message[8] = item.itemName;
                    message[9] = item.amount.ToString();
                    message[10] = item.price.ToString();
                    message[11] = item.purchDate.ToString();
                    message[12] = "<|EOM|>";

                    var serializableMessage = JsonConvert.SerializeObject(message);
                    var messageBytes = Encoding.UTF8.GetBytes(serializableMessage);

                    channel.BasicPublish(exchange: "",
                                        routingKey: "db",
                                        basicProperties: null,
                                        body: messageBytes);

                    Console.WriteLine($"Client sent message: \"{item}\"");
               // }
           // }
        }

        private static async Task SocketConnection(dbItem item)
        {
            Socket client = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            var result = client.BeginConnect(iPEndPoint, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(30000, true);

            if (client.Connected)
            {
                client.EndConnect(result);

                var message = new string[13];
                message[0] = item.shopName;
                message[1] = item.shopAddress;
                message[2] = item.clientName;
                message[3] = item.clientEmail;
                message[4] = item.clientPhone;
                message[5] = item.categotyName;
                message[6] = item.distrName;
                message[7] = item.itemId.ToString();
                message[8] = item.itemName;
                message[9] = item.amount.ToString();
                message[10] = item.price.ToString();
                message[11] = item.purchDate.ToString();
                message[12] = "<|EOM|>";

                while (true)
                {
                    var serializableMessage = JsonConvert.SerializeObject(message);
                    var messageBytes = Encoding.UTF8.GetBytes(serializableMessage);
                    _ = await client.SendAsync(messageBytes, SocketFlags.None);
                    Console.WriteLine($"Socket client sent message: \"{item}\"");

                    // Receive ack.
                    var buffer = new byte[1024];
                    var received = client.Receive(buffer, SocketFlags.None);
                    var response = Encoding.UTF8.GetString(buffer, 0, received);
                    if (!string.IsNullOrEmpty(response))
                    {
                        Console.WriteLine(
                            $"Socket client received acknowledgment: \"{response}\"");
                        break;
                    }
                }

            }
            else
            {
                client.Close();
                Console.WriteLine("Сервер не запущен");
            }

        }
    }
}
