
namespace Hicore.Logger
{
    public class Result
    {

        private string type;
        private string message;
        private JSONNode data = null;
        private int code;
        private string token;

        public Result() { }

        public Result(string data)
        {
            JSONNode jsonRes = JSON.Parse(data);

            this.Type = jsonRes["type"].Value;
            this.Message = jsonRes["msg"].Value;
            this.Code = jsonRes["code"].AsInt;
            if (jsonRes["data"] != null)
            {
                this.Data = jsonRes["data"];
            }

        }

        readonly string success = "success";
        readonly string warning = "warning";
        readonly string error = "error";


        public string Type { get => type; set => type = value; }
        public string Message { get => message; set => message = value; }
        public JSONNode Data { get => data; set => data = value; }
        public int Code { get => code; set => code = value; }
        public string Success => success;
        public string Warning => warning;
        public string Error => error;
    }
}
