using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace imap
{
    class SetOfMails
    {
        public List<int> down;
        public List<int> clear;

        public SetOfMails()
        {
            down = new List<int>();
            clear = new List<int>();
        }

        public List<int> GetDown()
        {
            return this.down;
        }

        public List<int> GetClear()
        {
            return this.clear;
        }

        public void AddDown(int it)
        {
            this.down.Add(it);
        }

        public void AddClear(int it)
        {
           this.clear.Add(it);
        }
    }

    class Program
    {
        static StreamWriter sw = null;
        static System.Net.Sockets.TcpClient tcpc = null;
        static System.Net.Security.SslStream ssl = null;
        static StreamWriter sslw = null;
        static StreamReader sslr = null;
        static string username, password, pattern1, pattern2;
        static string path, imapresponse, imapresponse2, imapresponsetemp, imapresponsetemp2;
        static string center;
        static StringBuilder sb = new StringBuilder();
        static Dictionary<string, SetOfMails> mails = new Dictionary<string, SetOfMails>();
        static SetOfMails stmtemp1,stmtempx,stmtempx2;


        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("\nRequired parameter(s) missing:\n\nimap \"IMAP_login\" \"IMAP_password\" \"pattern1\" \"pattern2\"\n\nQuotation marks are mandatory.");
            }
            else try
                {
                    path = Environment.CurrentDirectory + "\\imap.log";

                    //sw = new StreamWriter(File.OpenWrite(path));
                    sw = File.AppendText(path);
                    sw.WriteLine(DateTime.Now.ToString(), sw);

                    tcpc = new System.Net.Sockets.TcpClient("imap.gmail.com", 993);
                    ssl = new System.Net.Security.SslStream(tcpc.GetStream());
                    ssl.AuthenticateAsClient("imap.gmail.com");

                    sslw = new StreamWriter(ssl);
                    sslr = new StreamReader(ssl);


                    username = args[0];
                    password = args[1];
                    pattern1 = args[2];
                    pattern2 = args[3];

                    //Console.Clear();

                    sslw.WriteLine("a1 LOGIN " + username + " " + password);
                    sslw.Flush();
                    ReadResponse("a1", sslr);

                    sslw.WriteLine("a2 SELECT INBOX");
                    sslw.Flush();
                    ReadResponse("a2", sslr);

                    sslw.WriteLine("a3 SEARCH HEADER SUBJECT \"" + pattern1 + "\"");
                    sslw.Flush();
                    imapresponse = ReadSearch("a3", sslr);
                    //now processing the messages marked at 'a3'
                    if (imapresponse.Length > 9)
                    {
                        //we have at least 1 message matching the pattern at 'a3'
                        /* code to process each message matched: */
                        imapresponsetemp = imapresponse.Substring(9);
                        string[] smsgs = imapresponsetemp.Split(' ');
                        int nummsgs = smsgs.Length;
                        //now we have all messages UID at smsgs[i] (string)
                        //we can fetch everyone of them
                        for (int i = 0; i < nummsgs; i++)
                        {
                            int j = Int32.Parse(smsgs[i]);
                            sslw.WriteLine("b1 FETCH " + smsgs[i] + ":" + smsgs[i] + " (FLAGS BODY[HEADER.FIELDS (SUBJECT)])");
                            //sslw.WriteLine("b1 FETCH " + smsgs[i] + ":" + smsgs[i] + " (FLAGS BODY.PEEK[HEADER.FIELDS (SUBJECT)])"); //with BODY.PEEK the mail is not flagged as read/seen
                            sslw.Flush();
                            imapresponsetemp = ReadFetch("b1", sslr);
                            string[] namecentertemp = imapresponsetemp.Split(' ');
                            center = namecentertemp[4];
                            if (mails.TryGetValue(center, out stmtempx))
                            {
                                Console.Write(".");
                                stmtempx.AddDown(j);
                                mails.Remove(center);
                                mails.Add(center, stmtempx);
                            } else
                            {
                                stmtemp1 = new SetOfMails();
                                stmtemp1.AddDown(j);
                                Console.Write(".");
                                mails.Add(center,stmtemp1);
                            }
                        }
                        Console.WriteLine();
                    } else
                    {
                        //no messages matching pattern at 'a3'
                        Console.WriteLine("-- MESSAGES MATCHING PATTERN1 NOT FOUND --");
                    }

                    sslw.WriteLine("a4 SEARCH HEADER SUBJECT \"" + pattern2 + "\"");
                    sslw.Flush();
                    imapresponse2 = ReadSearch("a4", sslr);
                    //now processing the messages marked at 'a4'
                    if (imapresponse2.Length > 9)
                    {
                        //we have at least 1 message matching the pattern at 'a4'
                        /* code to process each message matched: */
                        imapresponsetemp2 = imapresponse2.Substring(9);
                        string[] smsgs = imapresponsetemp2.Split(' ');
                        int nummsgs = smsgs.Length;
                        //now we have all messages UID at smsgs[i] (string)
                        //we can fetch everyone of them
                        for (int i = 0; i < nummsgs; i++)
                        {
                            int j = Int32.Parse(smsgs[i]);
                            sslw.WriteLine("b2 FETCH " + smsgs[i] + ":" + smsgs[i] + " (FLAGS BODY[HEADER.FIELDS (SUBJECT)])");
                            //sslw.WriteLine("b2 FETCH " + smsgs[i] + ":" + smsgs[i] + " (FLAGS BODY.PEEK[HEADER.FIELDS (SUBJECT)])");
                            sslw.Flush();
                            imapresponsetemp = ReadFetch("b2", sslr);
                            string[] namecentertemp = imapresponsetemp.Split(' ');
                            center = namecentertemp[5];
                            if (mails.TryGetValue(center, out stmtempx2))
                            {
                                Console.Write(".");
                                stmtempx2.AddClear(j);
                                mails.Remove(center);
                                mails.Add(center, stmtempx2);
                            }
                            else
                            {
                                stmtemp1 = new SetOfMails();
                                stmtemp1.AddClear(j);
                                Console.Write(".");
                                mails.Add(center, stmtemp1);
                            }
                            Console.WriteLine();
                        }
                    } else
                    {
                        //no messages matching pattern at 'a4'
                        Console.WriteLine("-- MESSAGES MATCHING PATTERN2 NOT FOUND --");
                    }
                    
                    var enumerator = mails.GetEnumerator();
                    int t = 0;
                    while (enumerator.MoveNext())
                    {
                        t++;
                        var pair = enumerator.Current;
                        List<int> j = pair.Value.GetClear();
                        List<int> k = pair.Value.GetDown();
                        int cj = j.Count;
                        int ck = k.Count;
                        int e = Math.Min(cj, ck);
                        for (int i = 0; i < e; i++)
                        {
                            //deleting mail id=j[i]
                            Console.WriteLine("c1" + t+i + " STORE " + j[i] + " +FLAGS (\\Deleted)");
                            sslw.WriteLine("c1"+t+i+" STORE " + j[i]+ " +FLAGS (\\Deleted)");
                            sslw.Flush();
                            ReadResponse("c1"+t+i, sslr); // cleaning the buffer
                            //deleting mail id=k[i]
                            Console.WriteLine("c2" +t+ i + " STORE " + k[i] + " +FLAGS (\\Deleted)");
                            sslw.WriteLine("c2"+t+i+" STORE " + k[i] + " +FLAGS (\\Deleted)");
                            sslw.Flush();
                            ReadResponse("c2"+t+i, sslr); // cleaning the buffer
                            sw.WriteLine(pair.Key + " : Deleted clear #" + j[i] + " with down #" + k[i]);
                        }
                        if (ck < cj) // to delete those extra clear - notice that ck>cj is for those extra down and we want to keep them!
                        {
                            for (int i = e; i < cj; i++)
                            {
                                //delete mails id=k[i]
                                sw.WriteLine(pair.Key + " : Deleted clear #" + j[i]);
                            }
                        }
                        if (ck > cj)
                        {
                            for (int i = e; i < ck; i++)
                            {
                                //only reporting mails id=k[i]
                                sw.WriteLine(pair.Key + " : STILL DOWN! - mail #" + k[i] + " kept");
                            }
                        }
                    }
                    sslw.WriteLine("c3 EXPUNGE"); //effectively delete the flagged mails
                    sslw.Flush();
                    ReadResponse("c3", sslr); //cleaning the buffer

                    //LOGING OUT
                    Console.WriteLine("");
                    sslw.WriteLine("a5 LOGOUT");
                    sslw.Flush();
                    ReadResponse("a5", sslr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex.Message);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                        sw.Dispose();
                    }
                    if (ssl != null)
                    {
                        ssl.Close();
                        ssl.Dispose();
                    }
                    if (sslr != null)
                    {
                        sslr.Close();
                        sslr.Dispose();
                    }
                    if (sslw != null)
                    {
                        sslw.Close();
                        sslw.Dispose();
                    }
                    if (tcpc != null)
                    {
                        tcpc.Close();
                    }
                }
        }

        private static string ReadResponse(string tag, StreamReader sr)
        {
            string response;

            //discarding everything but the result code
            while ((response = sr.ReadLine()) != null)
            {
                Console.WriteLine(response);
                if (response.StartsWith(tag, StringComparison.Ordinal))
                {
                    break;
                }
            }
            return response;
        }

        private static string ReadSearch(string tag, StreamReader sr)
        {
            string response,responsebreak;

            response = sr.ReadLine(); //reading the command=result
            //discarding the result code
            while ((responsebreak=sr.ReadLine()) != null)
            {
                if (responsebreak.StartsWith(tag, StringComparison.Ordinal))
                {
                    break;
                }
            }
            return response;
        }


        private static string ReadFetch(string tag, StreamReader sr)
        {
            string response, responsebreak;

            sr.ReadLine(); //discarding the command
            response = sr.ReadLine(); //reading the result
            //discarding result code
            while ((responsebreak = sr.ReadLine()) != null)
            {
                if (responsebreak.StartsWith(tag, StringComparison.Ordinal))
                {
                    break;
                }
            }
            return response;
        }
    }
}
