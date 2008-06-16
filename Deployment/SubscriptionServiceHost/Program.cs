namespace SubscriptionServiceHost
{
    using System.IO;
    using log4net;
    using MassTransit.Host;
    using MassTransit.ServiceBus;
    using MassTransit.ServiceBus.Subscriptions;
    using MassTransit.SubscriptionStorage;

    internal class Program
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof (Program));

        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.xml"));
            _log.Info("SubMgr Loading");

            HostedEnvironment env = new SubscriptionManagerEnvironment("pubsub.castle.xml");

            env.Container.AddComponent<IHostedService, SubscriptionService>();

            env.Container.AddComponent<ISubscriptionRepository, NHibernateSubscriptionStorage>();

            Runner.Run(env, args);
        }
    }
}