namespace Task.UpcDb
{
    public abstract class BaseSingleThreadedTask : BaseTask 
    {
        protected BaseSingleThreadedTask(string taskCode) : base(taskCode)
        {
            _taskCode = taskCode;

            //This is where you would construct contentings to business resource to do work.
        }

        public override void Dispose()
        {
        
        }
    }
}