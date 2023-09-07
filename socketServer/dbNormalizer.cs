using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace lab0
{
    internal class dbNormalizer
    { 

        private readonly string __connStrPostgres = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = "entering",
            Username = "postgres",
            Password = "postgres",
        }.ConnectionString;

        public dbNormalizer()
        {
            bool exist=checkDataBaseExisting();

            if(!exist)
            {
                createDataBase();
            }

        }

        private void createDataBase()
        {
            string path = "C:\\Users\\Admin\\Documents\\ПГНИУ\\ТРРП\\lab0\\lab0\\create4.sql";
            string script = File.ReadAllText(path);

            var str = new NpgsqlConnectionStringBuilder
            {
                Host = "localhost",
                Port = 5432,
                Username = "postgres",
                Password = "postgres",
                
            }.ConnectionString;
            using (var conn = new NpgsqlConnection(str))
            {
                conn.Open();
                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText= @"CREATE DATABASE entering WITH ENCODING = 'UTF8' LC_COLLATE = 'Russian_Russia.1251' LC_CTYPE = 'Russian_Russia.1251';
                                    ALTER DATABASE entering OWNER TO postgres;"
                })
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }

            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();
                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText=script
                })
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }
        }

        private bool checkDataBaseExisting()
        {
            using(var conn= new NpgsqlConnection(__connStrPostgres))
            {
                try
                {
                    conn.Open();
                    conn.Close();
                    return true;
                }catch(Exception e)
                {
                    return false;
                }
            }
        }

        public string insertRow(dbItem i)
        {
            var shop_id = checkShop(i.shopName, i.shopAddress) ?? insertShop(i.shopName, i.shopAddress);
            var client_id = checkClient(i.clientName, i.clientEmail, i.clientPhone) ?? insertClient(i.clientName, i.clientEmail, i.clientPhone);
            var category_id = checkCategory(i.categotyName) ?? insertCategory(i.categotyName);
            var distr_id = checkDistr(i.distrName) ?? insertDistr(i.distrName);
            var item_id = checkItem(i.itemId) ?? insertItem(i.itemId, i.itemName, category_id);

            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();
                try
                {
                    using (var sqlCommand = new NpgsqlCommand
                    {
                        Connection = conn,
                        CommandText = @"INSERT INTO main_table (shop_id, client_id, item_id, distr_id, purchase_date, amount, price) 
                                        VALUES (@shop, @client, @item, @distr, @date, @amount, @price);"
                    })
                    {
                        sqlCommand.Parameters.AddWithValue("shop", shop_id);
                        sqlCommand.Parameters.AddWithValue("client", client_id);
                        sqlCommand.Parameters.AddWithValue("item", item_id);
                        sqlCommand.Parameters.AddWithValue("distr", distr_id);
                        sqlCommand.Parameters.AddWithValue("date", i.purchDate);
                        sqlCommand.Parameters.AddWithValue("amount", i.amount);
                        sqlCommand.Parameters.AddWithValue("price", i.price);

                        sqlCommand.ExecuteNonQuery();

                        return "Added";
                    }
                }
                catch(Exception e)
                {
                    //Console.WriteLine("duplicate " + i.ToString());
                    return "duplicate" + i.ToString();
                }
            }
        }

        private int? checkItem(int itemId)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText=@"SELECT id FROM item WHERE id=@item_id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("item_id", itemId);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? insertItem(int itemId, string itemName, int? category_id)
        {
            using (var conn = new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"INSERT INTO item VALUES (@id, @name, @cat) RETURNING id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("id", itemId);
                    sqlCommand.Parameters.AddWithValue("name", itemName);
                    sqlCommand.Parameters.AddWithValue("cat", category_id);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? checkDistr(string distrName)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText= @"SELECT id FROM distributor WHERE name=@name;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("name", distrName);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? insertDistr(string distrName)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText=@"INSERT INTO distributor (id, name) VALUES (default, @name) RETURNING id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("name", distrName);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? insertCategory(string categotyName)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText= @"INSERT INTO category (id, name) VALUES (default, @name) RETURNING id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("name", categotyName);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? checkCategory(string categotyName)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"SELECT id FROM category WHERE name=@name"
                })
                {
                    sqlCommand.Parameters.AddWithValue("name", categotyName);

                    return (int?)sqlCommand.ExecuteScalar();
                }

            }
            
        }

        private int? insertClient(string clientName, string clientEmail, string clientPhone)
        {
            using (var conn = new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"INSERT INTO client (id, name, email, phone) VALUES (default, @nam, @em, @ph) RETURNING id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("nam", clientName);
                    sqlCommand.Parameters.AddWithValue("em", clientEmail);
                    sqlCommand.Parameters.AddWithValue("ph", clientPhone);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? checkClient(string clientName, string clientEmail, string clientPhone)
        {
            using (var conn = new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"SELECT id FROM client WHERE name=@nam AND email=@em AND phone=@ph"
                })
                {
                    sqlCommand.Parameters.AddWithValue("nam", clientName);
                    sqlCommand.Parameters.AddWithValue("em", clientEmail);
                    sqlCommand.Parameters.AddWithValue("ph", clientPhone);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? insertShop(string shopName, string shopAddress)
        {
            using (var conn = new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using (var sqlCommand = new NpgsqlCommand
                {
                    Connection = conn,
                    CommandText = @"INSERT INTO shop (id, name, address) VALUES(default, @nam, @adr) RETURNING id;"
                })
                {
                    sqlCommand.Parameters.AddWithValue("nam", shopName);
                    sqlCommand.Parameters.AddWithValue("adr", shopAddress);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }

        private int? checkShop(string shopName, string shopAddress)
        {
            using(var conn=new NpgsqlConnection(__connStrPostgres))
            {
                conn.Open();

                using(var sqlCommand=new NpgsqlCommand
                {
                    Connection=conn,
                    CommandText=@"SELECT id FROM shop WHERE name=@nam AND address=@adr"
                })
                {
                    sqlCommand.Parameters.AddWithValue("nam", shopName);
                    sqlCommand.Parameters.AddWithValue("adr", shopAddress);

                    return (int?)sqlCommand.ExecuteScalar();
                }
            }
        }
    }
}
