namespace Hicore.Units
{
    public class FinalResult
    {
        private string message;
        private JSONNode data = null;
        
        public string Message
        {
            get => message;
            set => message = value;
        }

        public JSONNode Data
        {
            get => data;
            set => data = value;
        }





    }
}