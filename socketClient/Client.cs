using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using lab0;
using Microsoft.Data.Sqlite;

namespace socketClient
{
    internal class Client
    {
        /*
         * Считывать данные из БД
         * На каждую строку открывать новый сокет, отправлять данные и закрывать сокет
         * Сервер будет просто парсить полученные данные и кидать их в нормализованную БД
         */
        private static string dbPath;
        private static string _connStrSql;

        static async Task<int> Main(string[] args)
        {
            GetDbPath();
            int input;
            while (true)
            {
                Console.WriteLine("Specify the method of communication");
                Console.WriteLine("1. Socket");
                Console.WriteLine("2. MessageQueue");
                if (!int.TryParse(Console.ReadLine(), out input))
                    continue;

                switch (input)
                {
                    case 1:
                        var normalizer = new dbNormalizer(_connStrSql, input);
                        Console.ReadKey();
                        return 0;
                    case 2:
                        normalizer = new dbNormalizer(_connStrSql, input);
                        Console.ReadKey();
                        return 0;
                    default:
                        continue;
                }
            }
        }

        private static void GetDbPath()
        {
            Console.WriteLine("Укажите путь до базы данных");
            dbPath = Console.ReadLine();
            dbPath = dbPath.Replace("\"", "");
            //for(int i=0;i<dbPath.Length;i++)
            //{
            //    if (dbPath[i] == '\\')
            //        dbPath.Insert(i, "\\");
            //}

            _connStrSql = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
            }.ConnectionString;
        }

    }
}
