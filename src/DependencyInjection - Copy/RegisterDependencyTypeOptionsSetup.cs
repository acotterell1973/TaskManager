namespace Task.PlugInsLoader
{
    public class RegisterDependencyTypeOptionsSetup : ConfigureOptions<RegisterDependencyTypeOptions>
    {
        public RegisterDependencyTypeOptionsSetup() : base(ConfigureAssemblyPathOptions)
        {
        }

        /// <summary>
        /// Set the default options
        /// </summary>
        public static void ConfigureAssemblyPathOptions(RegisterDependencyTypeOptions options)
        {
            options.QueueConnectionString = string.Empty;
            options.AssemblyPathLocation = string.Empty;
            options.InjectFromInterfaceName = string.Empty;
            options.TaskManifest = new List<string>();
        }
    }
}