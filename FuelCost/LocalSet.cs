﻿using Android.App;
using Android.Content;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using System;
using static FuelCost.VehicleData;

namespace FuelCost
{
    static class LocalSet
    {
        #region Var
        public static SqliteConnection connection;
        static string dbPath;
        private static List<VehicleData> VehicleDatas = new List<VehicleData>();
        public static List<VehicleData> VehicleDataList { get { return VehicleDatas; } set { VehicleDatas = value; } }
        static public Dictionary<FuelTypeEnum, double> Prices = new Dictionary<FuelTypeEnum, double>();
        #endregion
        /// <summary>
        /// Pobiera dane
        /// </summary>


        public static void DelVehicle(int position)
        {
            try
            {
                Write(string.Format("DELETE FROM main WHERE name='{0}'", VehicleDatas[position].Name));
                VehicleDatas.Remove(VehicleDatas[position]);
                //VehicleDatas.Sort();
            }
            catch (Exception e)
            {
                Console.WriteLine(MainActivity.Log(e));
            }
        }

        public static void Open()
        {
            // determine the path for the database file
            dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "db.db3");
            // throw new Exception();
            bool exists = File.Exists(dbPath);

            if (!exists)
            {
                Console.WriteLine(MainActivity.Log("Creating database"));
                // Need to create the database before seeding it with some data
                SqliteConnection.CreateFile(dbPath);
                connection = new SqliteConnection("Data Source=" + dbPath);

                var commands = new[]
                {
                    "create table if not exists Main(ID INTEGER PRIMARY KEY AUTOINCREMENT,NAME          CHAR(50)      NOT NULL,FUELTYPE     INT           NOT NULL,PBINJECTION   BOOLEAN       NOT NULL,CONSUMPTION   DOUBLE         NOT NULL );",
                    "create table if not exists Price(FUEL INTEGER, VALUE DOUBLE)",
                    "INSERT INTO Price (FUEL, VALUE) VALUES (0, '2.12')",
                    "INSERT INTO Price (FUEL, VALUE) VALUES (1, '5.10')",
                    "INSERT INTO Price (FUEL, VALUE) VALUES (2, '5.30')",
            };

                // Open the database connection and create table with data

                connection.Open();
                foreach (var command in commands)
                {
                    using (var c = connection.CreateCommand())
                    {
                        c.CommandText = command;
                        try
                        {
                            var rowcount = c.ExecuteNonQuery();
                            Console.WriteLine(MainActivity.Log("\tExecuted " + command));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(MainActivity.Log("SQLite:\n" + e.Message));
                        }

                    }
                }
                connection.Close();
            }
            else
            {
                Console.WriteLine(MainActivity.Log("Database already exists"));
            }

        }
        private static void Write(string CommandText)
        {

            connection = new SqliteConnection("Data Source=" + dbPath);
            connection.Open();

            using (var c = connection.CreateCommand())
            {
                try
                {
                    c.CommandText = CommandText;
                    var rowcount = c.ExecuteNonQuery(); // rowcount will be 1
                }
                catch (Exception e)
                {
                    Console.WriteLine(MainActivity.Log("SQL write exception: " + e.Message));
                }
            }
            connection.Close();

        }
        public static void Write(VehicleData vehicle)
        {
            try
            {
                lock (VehicleDatas)
                {
                 //   VehicleDatas.Insert(VehicleDatas.Count, vehicle);
                    VehicleDatas.Add(vehicle);// 2 klasa jest zapisywana jako na 1 i 2 pozycji
                    /*
                     * Bug rozwiązany prze kazdorazowe konczenie intenta
                     */
                    Write(String.Format("INSERT INTO MAIN ( NAME, FUELTYPE, PBINJECTION, CONSUMPTION) VALUES ('{0}', {1}, {2}, {3});", vehicle.Name, ((int)vehicle.FuelType).ToString(), vehicle.Pbinjection.ToString(), Convert(vehicle.consumption)));
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(MainActivity.Log(e));
            }
        }

        public static void Write(VehicleData.FuelTypeEnum fuelType, double price)
        {
            Prices[fuelType] = price;
            Write(String.Format("UPDATE Price SET VALUE = '{1}' WHERE FUEL = {0}", (int)fuelType, Convert(price)));
        }

        public static void ReadVehicles()
        {

            connection = new SqliteConnection("Data Source=" + dbPath);
            connection.Open();

            using (var contents = connection.CreateCommand())
            {
                contents.CommandText = "SELECT * FROM MAIN";
                try
                {
                    var r = contents.ExecuteReader();
                    while (r.Read())
                    {
                        var Data = new VehicleData
                        {
                            Name = r["NAME"].ToString(),
                            FuelType = (VehicleData.FuelTypeEnum)int.Parse(r["FUELTYPE"].ToString()),
                            Pbinjection = bool.Parse(r["PBINJECTION"].ToString()),
                            consumption = float.Parse(r["CONSUMPTION"].ToString())
                        };

                        VehicleDatas.Add(Data);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(MainActivity.Log("SQL ask exception: " + e.Message));
                }
            }
            connection.Close();
        }
        public static void ReadPrices()
        {
            connection = new SqliteConnection("Data Source=" + dbPath);
            connection.Open();

            using (var contents = connection.CreateCommand())
            {
                contents.CommandText = "SELECT * FROM PRICE";
                try
                {
                    var r = contents.ExecuteReader();
                    while (r.Read())
                    {
                        var type = (FuelTypeEnum)int.Parse(r["FUEL"].ToString());
                        var value = r["VALUE"].ToString();
                        Prices[type] = Convert(value);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(MainActivity.Log("SQL ask exception: " + e.Message));
                }
            }
            connection.Close();
        }

        public static string Convert(double value)//2,12 out 2.12
        {
            string result = "";
            Task.Run(() =>
           {
               if (value == -1)
               {
                   return;
               }
               try
               {
                   result = value.ToString();
                   result = result.Replace(',', '.');
               }
               catch (Exception e)
               {
                   Console.WriteLine(MainActivity.Log("Input double: " + value + "\nConvert Exception: " + e.Message));
               }
           }).Wait();
            return result;
        }

        public static double Convert(string value) // 2,12
        {
            double result = 0;
            Task.Run(() =>
               {
                   try
                   {
                       if (value == "")
                       {
                           throw new KeyNotFoundException("Pusty ciąg znaków");
                       }

                       string[] tmp = value.Split(',');
                       if (tmp.Length == 1)
                       {
                           tmp = value.Split('.');
                       }
                       if (tmp.Length == 1)
                       {
                           result = double.Parse(tmp[0]);
                           //  Console.WriteLine(MainActivity.Log("I: " + value + "; O: " + result));
                           return;// result;
                       }


                       result = double.Parse(tmp[0]);
                       result += double.Parse(tmp[1]) / Math.Pow(10, tmp[1].Length);

                       //  Console.WriteLine(MainActivity.Log("I: " + value + "; O: " + result));
                   }
                   catch (Exception e)
                   {
                       Console.WriteLine(MainActivity.Log("Input string: " + value + "\nConvert Exception: " + e.Message));
                   }
               }).Wait();

            return result;
        }

    }
}