namespace PlantGateway.Core.Config.Models.Contracts
{
    // POCO object, just for flat data from JSON appsettings!
    public sealed class SerilogContract
    {
        public string MinimumLevel { get; set; } = "Information";

        public UserLogContract UserLog { get; set; } = new UserLogContract();
        public AdminLogContract AdminLog { get; set; } = new AdminLogContract();

        public sealed class UserLogContract
        {
            public bool EnableConsole { get; set; } = true;
            public string FileName { get; set; } = "gateway-user.log";
            public string Format { get; set; } = "Text";
        }

        public sealed class AdminLogContract
        {
            public string Path { get; set; } = string.Empty;
            public string FileName { get; set; } = "gateway-admin.log";
            public string RollingInterval { get; set; } = "Day";
            public string Format { get; set; } = "Json";
        }
    }
}
