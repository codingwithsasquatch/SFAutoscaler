using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;

namespace AutoscalerBot
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Newtonsoft.Json.Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services
            //Fixed docDb emulator local Uri.
            Uri docDbEmulatorUri = new Uri("https://codeslingerstour.documents.azure.com:443/");

            //Fixed docDb emulator key
            const string docDbEmulatorKey = "YoZqHb6KfBGcx3icWUyUMghSqd7gJpScL5N6QexlXEDNe4GIxhBfnbtZQl71r0WtJol7OUxiOK6MeudWwubRcg==";

            Conversation.UpdateContainer(
                builder =>
                {
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                    var store = new DocumentDbBotDataStore(docDbEmulatorUri, docDbEmulatorKey, "sfhack-autoscaler", "botstate");

                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();

                    // Register your Web API controllers.
                    builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
                    builder.RegisterWebApiFilterProvider(config);

                });

            config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);


            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
