using System;
using Hicore.Logger;
using Hicore.Authentications;

namespace Hicore.Storage
{
    public class StaticDataStorage
    {
        private HicoreSocket _socket;
        private string staticStorageEvent = "staticStorage";
        
        private string fetchStaticDataEvent = "fetchStaticData";
        
        private Action<Result> OnFetchStaticDataResult;

        public StaticDataStorage(HicoreSocket socket)
        {
            this._socket = socket;
            
            _socket.On(fetchStaticDataEvent, res =>
            {
                OnFetchStaticDataResult(new Result(res.Text));
            });

        }


        public void FetchStaticDate(string collectionName,Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StaticStorageType.fetch.ToString());
            json.Add("collection", collectionName);
            json.Add("token", Client.Token);

            _socket.Emit(staticStorageEvent, json.ToString());

            OnFetchStaticDataResult = (res) => { onResult(res); };
        }
        private enum StaticStorageType
        {
         fetch,
        }
    }
}