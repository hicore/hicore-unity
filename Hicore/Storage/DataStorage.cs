using System;
using System.Collections.Generic;
using Hicore.Authentications;
using Hicore.Logger;

namespace Hicore.Storage
{
    public class DataStorage
    {
        private HicoreSocket _socket;
        private string storageEvent = "storage";

        private string addObjectEvent = "addObject";
        private string updateClassEvent = "updateClass";
        private string incrementValueEvent = "incrementValue";
        private string fetchDataEvent = "fetchData";
        private string deleteObjectEvent = "deleteObject";

        private Action<Result> OnAddObjectResult;
        private Action<Result> OnUpdateClassResult;
        private Action<Result> OnIncrementValueResult;
        private Action<Result> OnFetchDataResult;
        private Action<Result> OnDeleteObjectResult;


        public DataStorage(HicoreSocket socket)
        {
            this._socket = socket;


            _socket.On(addObjectEvent, res =>
            {
                OnAddObjectResult(new Result(res.Text));
            });

            _socket.On(updateClassEvent, res =>
            {
                OnUpdateClassResult(new Result(res.Text));
            });

            _socket.On(incrementValueEvent, res =>
            {
                OnIncrementValueResult(new Result(res.Text));
            });

            _socket.On(fetchDataEvent, res =>
            {
                OnFetchDataResult(new Result(res.Text));
            });
            
            _socket.On(deleteObjectEvent, res =>
            {
                OnDeleteObjectResult(new Result(res.Text));
            });
        }

        public void AddObject(DataObject data, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.add.ToString());
            json.Add("collection" , data._collection);
            json.Add("data", data.GetData()); // send as json 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnAddObjectResult = (res) => { onResult(res); };
        }
        
        public void IncrementValue(DataObject data, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.increment.ToString());
            json.Add("collection", data._collection);
            json.Add("data", data.GetData()); 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnIncrementValueResult = (res) => { onResult(res); };
        }

        public void FetchData(string collectionName, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.fetch.ToString());
            json.Add("collection", collectionName);
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnFetchDataResult = (res) => { onResult(res); };

        }
        
        public void DeleteObject(string collectionName,List<String> keys, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.delete.ToString());
            json.Add("collection", collectionName);
            json.Add("keys", ConvertListToJsonObject(keys)); 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnDeleteObjectResult = (res) => { onResult(res); };

        }

        public JSONObject ConvertListToJsonObject(List<String> keys)
        {
            JSONObject json = new JSONObject();
           
            foreach (String key in keys)
            {
                json.Add(key, 1);
            }

            return json;
        }


        private enum StorageType
        {
            add,
            increment,
            fetch,
            delete,
        }
    }


    public class DataObject
    {
        private Dictionary<string, JSONNode> m_data = new Dictionary<string, JSONNode>();
        internal string _collection;

        private string key;
        private JSONNode value;


        public DataObject(string collectionName)
        {
            this._collection = collectionName;
        }

        public void Add(string key, JSONNode value) 
        {

            if (m_data.ContainsKey(key))
            {
                m_data[key] = value;
            }
            else
            {
                m_data.Add(key, value);
            }
        }

        public void Increment(string key , int value)
        {
            if (m_data.ContainsKey(key))
            {
                m_data[key] = value;
            }
            else
            {
                m_data.Add(key, value);
            }
        }



        public JSONObject GetData()
        {
            JSONObject json = new JSONObject();
           
            foreach (KeyValuePair<string, JSONNode> pair in m_data)
            {
                json.Add(pair.Key, pair.Value);
            }

            return json;
        }
        
        public DataObject SetKey(string key)
        {
            this.key = key;
            return this;
        }

        public DataObject SetValue(JSONNode value)
        {
            this.value = value;
            return this;
        }

    }
}
