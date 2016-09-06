namespace Task.Common
{
    public class ArgumentDescriptor
    {
        /// <summary>
        /// The main /switch argument name.
        /// </summary>
        public string Argument { get; set; }

        /// <summary>
        /// Any additional arguments optional or required to follow after the main /switch argument.
        /// </summary>
        public string PostArguments { get; set; }

        /// <summary>
        /// The human-readable plain text description of the argument's usage and purpose.
        /// </summary>
        /// <remarks>
        /// Do not format with newline characters. It will be word-wrapped in the output.
        /// </remarks>
        public string Description { get; set; }

        /// <summary>
        /// Creates an empty argument descriptor.
        /// </summary>
        public ArgumentDescriptor()
        {
        }

        /// <summary>
        /// Creates an argument descriptor using just the argument and its description.
        /// </summary>
        /// <param name="arg">The argument name with a leading forward slash</param>
        /// <param name="description">Full-text description of the argument's usage and purpose. Do not insert line breaks.</param>
        public ArgumentDescriptor(string arg, string description)
        {
            Argument = arg;
            Description = description;
        }

        /// <summary>
        /// Creates an argument descriptor using the argument, post-argument syntax, and its description.
        /// </summary>
        /// <param name="arg">The argument name with a leading forward slash</param>
        /// <param name="postArgs">Syntax for the required pieces after the argument</param>
        /// <param name="description">Full-text description of the argument's usage and purpose. Do not insert line breaks.</param>
        public ArgumentDescriptor(string arg, string postArgs, string description)
        {
            Argument = arg;
            PostArguments = postArgs;
            Description = description;
        }
    }
}