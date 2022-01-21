using RippleDotNet.Model.Transaction;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using XRPLAirdrop.db.models;

namespace XRPLAirdrop
{
    public class database
    {
        static string connectionstring = System.IO.Path.Combine("Data Source=" + System.AppDomain.CurrentDomain.BaseDirectory, "db\\main_airdrop.db");
        public database()
        {
            Startup();
        }

        public void Startup()
        {
            //Create the db file if it doesn't exist
            if (!System.IO.File.Exists(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "db\\main_airdrop.db")))
            {
                System.Data.SQLite.SQLiteConnection.CreateFile(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "db\\main_airdrop.db"));
            }

            //Create Tables if they don't already exist
            string startupScript = File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "db/startup_script.txt"));

            var con = new System.Data.SQLite.SQLiteConnection(connectionstring);
            con.Open();
            using var cmd = new SQLiteCommand(startupScript, con);
            cmd.ExecuteNonQuery();
            con.Close();

        }

        public void DeleteTrustLines()
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    //Clear out table first
                    var cmdTruncate = new SQLiteCommand("DELETE FROM Airdrop; delete from sqlite_sequence where name='Airdrop'; vacuum;", conn);
                    cmdTruncate.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void DeleteExclusionData()
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    //Clear out table first
                    var cmdTruncate = new SQLiteCommand("DELETE FROM ExclusionList; delete from sqlite_sequence where name='ExclusionList'; vacuum;", conn);
                    cmdTruncate.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void AddTrustLineRecords(List<Airdrop> adList)
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    foreach (Airdrop a in adList)
                    {
                        var cmdInsert = new SQLiteCommand("Insert into Airdrop (address,dropped,balance) values (@address,@dropped,@balance)", conn);
                        cmdInsert.Parameters.Add(new SQLiteParameter("@address", a.address));
                        cmdInsert.Parameters.Add(new SQLiteParameter("@dropped", a.dropped));
                        cmdInsert.Parameters.Add(new SQLiteParameter("@balance", a.balance));
                        cmdInsert.ExecuteNonQuery();
                    }
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void AddExclusionListRecords(List<Models.ExclusionList> exclusionList)
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    foreach (Models.ExclusionList a in exclusionList)
                    {
                        var cmdInsert = new SQLiteCommand("Insert into ExclusionList (address,type) values (@address,@type)", conn);
                        cmdInsert.Parameters.Add(new SQLiteParameter("@address", a.account));
                        cmdInsert.Parameters.Add(new SQLiteParameter("@type", a.type));
                        cmdInsert.ExecuteNonQuery();
                    }
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateExclusionRecordsInAirdrop()
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmdExclusion = new SQLiteCommand("Update Airdrop SET dropped = -1, txn_verified = -1 txn_message = 'Excluded', txn_detail = 'Excluded from Airdrop by XRP Forensics data' where address in (Select address from ExclusionList)", conn);
                    cmdExclusion.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateAirdropRecord(string address, int dropped, int txn_verified, Submit response = null)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    var cmd = new SQLiteCommand();
                    conn.Open();
                    if (response != null)
                    {
                        cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = @dropped, txn_verified = @txnverified, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where address = @address ", conn);
                        cmd.Parameters.Add(new SQLiteParameter("@txnMessage", response.EngineResult));
                        cmd.Parameters.Add(new SQLiteParameter("@dropped", dropped));
                        cmd.Parameters.Add(new SQLiteParameter("@txnverified", txn_verified));
                        cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", response.EngineResultMessage));
                        cmd.Parameters.Add(new SQLiteParameter("@address", address));
                        cmd.Parameters.Add(new SQLiteParameter("@txnHash", response.Transaction.Hash));
                        cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                    }
                    else
                    {
                        cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = @dropped, txn_verified = @txnverified where address = @address ", conn);
                        cmd.Parameters.Add(new SQLiteParameter("@dropped", dropped));
                        cmd.Parameters.Add(new SQLiteParameter("@txnverified", txn_verified));
                        cmd.Parameters.Add(new SQLiteParameter("@address", address));
                        cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                    }

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public void UpdateSuccessfulAirdrop(string address, Submit response)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = 1, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where address = @address ", conn);
                    cmd.Parameters.Add(new SQLiteParameter("@txnMessage", response.EngineResult));
                    cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", response.EngineResultMessage));
                    cmd.Parameters.Add(new SQLiteParameter("@address", address));
                    cmd.Parameters.Add(new SQLiteParameter("@txnHash", response.Transaction.Hash));
                    cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateFailureAirdrop(string reason, Settings config)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("");
                    if (config.excludeIfUserHasABalance)
                    {
                        cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = -1, txn_verified = -1, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where balance > 0", conn);
                        cmd.Parameters.Add(new SQLiteParameter("@txnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@txnHash", ""));
                        cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                        cmd.ExecuteNonQuery();
                    }
                    if (config.includeOnlyIfHolder)
                    {
                        decimal threshold = config.includeOnlyIfHolderThreshold;
                        cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = -1, txn_verified = -1, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where balance < " + threshold + "", conn);
                        cmd.Parameters.Add(new SQLiteParameter("@txnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@txnHash", ""));
                        cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                        cmd.ExecuteNonQuery();
                    }
                    if (config.xrplVerifyEnabled)
                    {
                        cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = -1, txn_verified = -1, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where xrpl_verified = 0", conn);
                        cmd.Parameters.Add(new SQLiteParameter("@txnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", reason));
                        cmd.Parameters.Add(new SQLiteParameter("@txnHash", ""));
                        cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                        cmd.ExecuteNonQuery();
                    }

                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateFailureAirdropException(string address, string ExceptionMsg)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = 0, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where address = @address ", conn);
                    cmd.Parameters.Add(new SQLiteParameter("@txnMessage", ExceptionMsg));
                    cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", ExceptionMsg));
                    cmd.Parameters.Add(new SQLiteParameter("@address", address));
                    cmd.Parameters.Add(new SQLiteParameter("@txnHash", ""));
                    cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateFailureAirdrop(string address, Submit response)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET datetime = @datetime, dropped = 0, txn_message = @txnMessage, txn_detail = @detailTxnMessage, txn_hash = @txnHash where address = @address ", conn);
                    cmd.Parameters.Add(new SQLiteParameter("@txnMessage", response.EngineResult));
                    cmd.Parameters.Add(new SQLiteParameter("@detailTxnMessage", response.EngineResultMessage));
                    cmd.Parameters.Add(new SQLiteParameter("@address", address));
                    cmd.Parameters.Add(new SQLiteParameter("@txnHash", response.Transaction.Hash));
                    cmd.Parameters.Add(new SQLiteParameter("@datetime", DateTimeOffset.Now.ToUnixTimeSeconds()));
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public Queue<Airdrop> GetNonDroppedRecord(Settings config)
        {
            Queue<Airdrop> list = new Queue<Airdrop>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT * FROM Airdrop where dropped = 0 ORDER BY id asc LIMIT " + config.numberOfTrustlines.ToString() + ";";

                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Enqueue(new Airdrop
                            {
                                id = Convert.ToInt32(dr["id"]),
                                address = dr["address"].ToString(),
                                balance = (decimal)dr["balance"]
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public Queue<Airdrop> GetUnverifiedRecords(Settings config)
        {
            Queue<Airdrop> list = new Queue<Airdrop>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT * FROM Airdrop where dropped = 1 and txn_verified = 0 ORDER BY id asc LIMIT " + config.numberOfTrustlines.ToString() + ";";

                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Enqueue(new Airdrop
                            {
                                id = Convert.ToInt32(dr["id"]),
                                address = dr["address"].ToString(),
                                txn_hash = dr["txn_hash"].ToString()
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public Queue<Airdrop> GetUnverifiedFailedRecords(Settings config)
        {
            Queue<Airdrop> list = new Queue<Airdrop>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "SELECT * FROM Airdrop where dropped = 1 and txn_verified = -2 ORDER BY id asc LIMIT " + config.numberOfTrustlines.ToString() + ";";

                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Enqueue(new Airdrop
                            {
                                id = Convert.ToInt32(dr["id"]),
                                address = dr["address"].ToString(),
                                txn_hash = dr["txn_hash"].ToString()
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public List<Airdrop> GetAllAirdropRecords()
        {
            List<Airdrop> list = new List<Airdrop>();
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "Select * from Airdrop order by id asc";
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            list.Add(new Airdrop
                            {
                                id = Convert.ToInt32(dr["id"]),
                                address = dr["address"].ToString(),
                                balance = (decimal)dr["balance"],
                                dropped = Convert.ToInt32(dr["dropped"]),
                                datetime = Convert.ToInt32(dr["datetime"].ToString()),
                                txn_message = dr["txn_message"].ToString(),
                                txn_detail = dr["txn_detail"].ToString(),
                                txn_hash = dr["txn_hash"].ToString(),
                                txn_verified = Convert.ToInt32(dr["txn_verified"]),
                                xrpl_verified = Convert.ToInt32(dr["xrpl_verified"]),
                            });
                        }
                    }
                }
                conn.Close();
            }
            return list;
        }

        public int GetTotalAirdropRecords()
        {
            int returnTotal = 0;
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "Select count(*) as total from Airdrop";
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            returnTotal = Convert.ToInt32(dr["total"]);
                        }
                    }
                }
                conn.Close();
            }
            return returnTotal;
        }

        public int GetTotalExclusionListRecords()
        {
            int returnTotal = 0;
            using (SQLiteConnection conn = new SQLiteConnection(connectionstring))
            {
                conn.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText = "Select count(*) as total from ExclusionList";
                    using (SQLiteDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            returnTotal = Convert.ToInt32(dr["total"]);
                        }
                    }
                }
                conn.Close();
            }
            return returnTotal;
        }

        public void DeleteDuplicates()
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM Airdrop WHERE rowid NOT IN(SELECT MIN(rowid) FROM Airdrop GROUP BY address)", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void RemoveAirdropAccount(Settings config)
        {
            DateTime now = DateTime.Now;
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("DELETE FROM Airdrop WHERE address = '" + config.airdropAddress + "'", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateAlreadyDroppedMessage()
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET dropped = 1 where txn_message = 'terQUEUED'", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateDBForAddress(string address)
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET dropped = 0, txn_verified = 0 where address = '" + address + "'", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public void UpdateXRPLVerified(string address)
        {
            try
            {
                using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                {
                    conn.Open();
                    var cmd = new SQLiteCommand("Update Airdrop SET xrpl_verified = 1 where address = '" + address + "'", conn);
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {

            }
        }

        public int ImportCustomList(string filename)
        {
            int count = 0;
            try
            {
                using (var reader = new StreamReader(filename))
                {
                    List<string> accountList = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        foreach(string val in values)
                        {
                            if (!accountList.Contains(val) && val.Trim() != "")
                            {
                                accountList.Add(val.Trim());
                                count++;
                            }
                        }
                    }

                    using (var conn = new System.Data.SQLite.SQLiteConnection(connectionstring))
                    {
                        conn.Open();
                        foreach (string s in accountList)
                        {
                            var cmdInsert = new SQLiteCommand("Insert into Airdrop (address,balance) values (@address,0)", conn);
                            cmdInsert.Parameters.Add(new SQLiteParameter("@address", s));
                            cmdInsert.ExecuteNonQuery();

                        }
                        conn.Close();
                    }
                }
                return count;
            }
            catch (Exception ex)
            {
                throw new Exception("Error: " + ex.Message);
            }
        }
    }
}
