using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace VRayPoolManager
{
    class Program
    {
        private static BatchClient Client;

        static void Main(string[] args)
        {
            Client = GetClient();

            if (args.Length != 2)
            {
                Usage();
            }

            var action = args[0];
            var poolName = args[1];

            if (string.IsNullOrEmpty(poolName))
            {
                Usage();
            }

            if (action == "create")
            {
                CreatePool(poolName);
            }
            else if (action == "delete")
            {
                DeletePool(poolName);
            }
            else
            {
                Usage();
            }
        }

        public static BatchClient GetClient()
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["BatchAccount"]))
            {
                // AAD
                Func<Task<string>> tokenProvider = () => GetAuthenticationTokenAsync();
                return BatchClient.Open(new BatchTokenCredentials(ConfigurationManager.AppSettings["BatchUrl"], tokenProvider));
            }

            // Shared Key
            return BatchClient.Open(
                new BatchSharedKeyCredentials(
                    ConfigurationManager.AppSettings["BatchUrl"],
                    ConfigurationManager.AppSettings["BatchAccount"],
                    ConfigurationManager.AppSettings["BatchKey"]));
        }

        public static async Task<string> GetAuthenticationTokenAsync()
        {
            var authContext = new AuthenticationContext(ConfigurationManager.AppSettings["AuthorityUri"]);

            // Acquire the authentication token from Azure AD.
            var authResult = await authContext.AcquireTokenAsync(ConfigurationManager.AppSettings["BatchResourceUri"],
                ConfigurationManager.AppSettings["ApplicationId"],
                new Uri(ConfigurationManager.AppSettings["RedirectUri"]),
                new PlatformParameters(PromptBehavior.Auto));

            return authResult.AccessToken;
        }

        private static void CreatePool(string poolName)
        {
            var vmSize = ConfigurationManager.AppSettings["VirtualMachineSize"];

            var dedicatedVmCount = Int32.Parse(ConfigurationManager.AppSettings["VirtualMachineCount"]);
            var lowPriorityVmCount = 0;


            if (Boolean.Parse(ConfigurationManager.AppSettings["UseLowPriority"]))
            {
                lowPriorityVmCount = dedicatedVmCount;
                dedicatedVmCount = 0;
            }

            var pool = Client.PoolOperations.CreatePool(
                poolName,
                vmSize,
                new VirtualMachineConfiguration(
                    new ImageReference(
                        ConfigurationManager.AppSettings["ImageOffer"],
                        ConfigurationManager.AppSettings["ImagePublisher"],
                        ConfigurationManager.AppSettings["ImageSku"],
                        ConfigurationManager.AppSettings["ImageVersion"]),
                    ConfigurationManager.AppSettings["NodeAgentSku"]), 
                dedicatedVmCount, 
                lowPriorityVmCount);

            SetupPoolNetworking(pool);

            pool.ApplicationLicenses = new List<string> { "3dsmax", "vray" };
            pool.Commit();

            Console.WriteLine("Created pool {0} with {1} compute node(s)", poolName, dedicatedVmCount == 0 ? lowPriorityVmCount : dedicatedVmCount);

            var job = Client.JobOperations.CreateJob("vray-dr-" + poolName, new PoolInformation {PoolId = poolName});
            job.Commit();
            job.Refresh();

            var vmCount = Math.Max(lowPriorityVmCount, dedicatedVmCount);

            var task = new CloudTask("setup-vray-dr", "set");
            task.MultiInstanceSettings = new MultiInstanceSettings(
                string.Format(ConfigurationManager.AppSettings["VRaySetupCommand"], ConfigurationManager.AppSettings["VRayServerPort"]), 
                vmCount);
            task.Constraints = new TaskConstraints(maxTaskRetryCount: 3);
            job.AddTask(task);

            Console.WriteLine("Waiting for pool nodes to allocate...");
            pool.Refresh();
            while (pool.AllocationState.Value != AllocationState.Steady)
            {
                Thread.Sleep(10000);
                pool.Refresh();
            }

            if (pool.CurrentDedicatedComputeNodes != dedicatedVmCount)
            {
                Console.WriteLine("Failed to allocate enough dedicated nodes: {0} or {1}", pool.TargetDedicatedComputeNodes, dedicatedVmCount);
                Environment.Exit(1);
            }

            if (pool.CurrentLowPriorityComputeNodes != lowPriorityVmCount)
            {
                Console.WriteLine("Failed to allocate enough low priority nodes: {0} or {1}", pool.TargetDedicatedComputeNodes, dedicatedVmCount);
                Environment.Exit(1);
            }

            WriteVRayConfig(pool);

            Console.WriteLine("Waiting for configuration task to complete...");
            task = Client.JobOperations.GetTask(job.Id, task.Id);
            while (task.State.Value == TaskState.Active)
            {
                Thread.Sleep(15000);
                task = Client.JobOperations.GetTask(job.Id, task.Id);
            }

            Console.WriteLine("Done, press any key to exit...");
            Console.ReadLine();
        }

        private static void SetupPoolNetworking(CloudPool pool)
        {
            pool.NetworkConfiguration = new NetworkConfiguration();
            pool.InterComputeNodeCommunicationEnabled = true;

            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["SubnetResourceId"]))
            {
                // If no VNet, setup inbound endpoints
                var restrictToPublicIp = Boolean.Parse(ConfigurationManager.AppSettings["RestrictToPublicIp"]);

                NetworkSecurityGroupRule[] nsgRules = null;

                if (restrictToPublicIp)
                {
                    var publicIp = GetPublicIp();
                    nsgRules = new[]
                    {
                        new NetworkSecurityGroupRule(200, NetworkSecurityGroupRuleAccess.Allow, publicIp),
                        new NetworkSecurityGroupRule(201, NetworkSecurityGroupRuleAccess.Deny, "*"),
                    };
                }

                var portTuple = GetPublicPortRange();
                var inboundNatPools = new List<InboundNatPool>
                {
                    new InboundNatPool("VRay", InboundEndpointProtocol.Tcp, 20207, portTuple.Item1, portTuple.Item2, nsgRules)
                };

                pool.NetworkConfiguration.EndpointConfiguration = new PoolEndpointConfiguration(inboundNatPools.AsReadOnly());
            }
            else
            {
                pool.NetworkConfiguration.SubnetId = ConfigurationManager.AppSettings["SubnetResourceId"];
            }
        }

        private static void WriteVRayConfig(CloudPool pool)
        {
            var vrayConfig = Path.Combine(
                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                ConfigurationManager.AppSettings["VRayDRConfigPath"],
                "vray_dr.cfg");

            var vrayRtConfig = Path.Combine(
                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                ConfigurationManager.AppSettings["VRayDRConfigPath"],
                "vrayrt_dr.cfg");

            var vrayConfigContent = ConfigurationManager.AppSettings["VRayDRConfig"];
            var vrayRtConfigContent = ConfigurationManager.AppSettings["VRayRTDRConfig"];

            // If the config files already exist, preserve their content
            if (File.Exists(vrayConfig))
            {
                var content = File.ReadAllText(vrayConfig);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    vrayConfigContent = content;
                }
            }

            if (File.Exists(vrayRtConfig))
            {
                var content = File.ReadAllText(vrayRtConfig);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    vrayRtConfigContent = content;
                }
            }

            File.WriteAllText(vrayConfig, "");
            File.WriteAllText(vrayRtConfig, "");
            foreach (var computeNode in pool.ListComputeNodes())
            {
                var computeNodeEntry = GetComputeNodeEntry(computeNode);

                Console.WriteLine("    {0} {1}", computeNode.Id, computeNodeEntry);

                if (vrayConfigContent.Contains(computeNodeEntry))
                {
                    Console.WriteLine("Compute node {0} already exists in config file {1}", computeNodeEntry, vrayConfig);
                }
                else
                {
                    File.AppendAllText(vrayConfig, computeNodeEntry);
                }
                
                if (vrayRtConfigContent.Contains(computeNodeEntry))
                {
                    Console.WriteLine("Compute node {0} already exists in config file {1}", computeNodeEntry, vrayRtConfigContent);
                }
                else
                {
                    File.AppendAllText(vrayRtConfig, computeNodeEntry);
                }
            }

            File.AppendAllText(vrayConfig, vrayConfigContent);
            File.AppendAllText(vrayRtConfig, vrayRtConfigContent);

            Console.WriteLine("Updated VRay DR config file: " + vrayConfig);
            Console.WriteLine("Updated VRayRT DR config file: " + vrayConfig);
        }

        private static string GetComputeNodeEntry(ComputeNode computeNode)
        {
            if (string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["SubnetResourceId"]))
            {
                // Get public endpoint
                if (computeNode.EndpointConfiguration != null &&
                    computeNode.EndpointConfiguration.InboundEndpoints != null)
                {
                    foreach (var endpoint in computeNode.EndpointConfiguration.InboundEndpoints)
                    {
                        if (endpoint.Name.StartsWith("VRay"))
                        {
                            Console.WriteLine("    {0} {1}:{2}", computeNode.Id, endpoint.PublicIPAddress,
                                endpoint.FrontendPort);
                            return string.Format("{0} 1 {1}\n", endpoint.PublicIPAddress, endpoint.FrontendPort);
                        }
                    }
                }
            }

<<<<<<< Updated upstream
            return string.Format("{0} 1 {1}\n", computeNode.IPAddress, ConfigurationManager.AppSettings["VRaySererPort"]);
=======
            File.AppendAllText(vrayConfig, vrayConfigContent);
            File.AppendAllText(vrayRtConfig, vrayRtConfigContent);

            Console.WriteLine("Updated VRay DR config file: " + vrayConfig);
            Console.WriteLine("Updated VRayRT DR config file: " + vrayRtConfig);
>>>>>>> Stashed changes
        }

        private static Tuple<int, int> GetPublicPortRange()
        {
            var portRangeSetting = ConfigurationManager.AppSettings["PublicPortRange"];
            if (string.IsNullOrWhiteSpace(portRangeSetting) || !portRangeSetting.Contains(":"))
            {
                portRangeSetting = "20000:20099";
                Console.WriteLine("Empty or invalid port range specified, falling back to default " + portRangeSetting);
            }

            try
            {
                var tokens = portRangeSetting.Split(':');
                return Tuple.Create<int, int>(int.Parse(tokens[0]), int.Parse(tokens[1]));
            }
            catch (Exception)
            {
                throw new Exception("Invalid port range specified: " + portRangeSetting + ", please specify a port range in the format 20000:20099");
            }
        }

        private static void DeletePool(string poolName)
        {
            Client.PoolOperations.DeletePool(poolName);
        }

        private static string GetPublicIp()
        {
            var myIp = ConfigurationManager.AppSettings["MyPublicIp"];
            if (string.IsNullOrEmpty(myIp))
            {
                string url = "http://checkip.dyndns.org";
                System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                System.Net.WebResponse resp = req.GetResponse();
                System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                string response = sr.ReadToEnd().Trim();
                string[] a = response.Split(':');
                string a2 = a[1].Substring(1);
                string[] a3 = a2.Split('<');
                string a4 = a3[0];
                myIp = a4;
            }
            return myIp;
        }

        private static void Usage()
        {
            Console.WriteLine("Invalid arguments.");
            Console.WriteLine("Usage: VRayPoolManager create <poolName>");
            Console.WriteLine("Usage: VRayPoolManager delete <poolName>");
            Environment.Exit(1);
        }
    }
}
