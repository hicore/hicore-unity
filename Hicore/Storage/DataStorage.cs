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

        private string createClassEvent = "createClass";
        private string updateClassEvent = "updateClass";
        private string incrementValueEvent = "incrementValue";
        private string catchDataEvent = "catchData";
        private string deleteObjectEvent = "deleteObject";

        private Action<Result> OnCrateClassResult;
        private Action<Result> OnUpdateClassResult;
        private Action<Result> OnIncrementValueResult;
        private Action<Result> OnFetchDataResult;
        private Action<Result> OnDeleteObjectResult;


        public DataStorage(HicoreSocket socket)
        {
            this._socket = socket;


            _socket.On(createClassEvent, res =>
            {
                OnCrateClassResult(new Result(res.Text));
            });

            _socket.On(updateClassEvent, res =>
            {
                OnUpdateClassResult(new Result(res.Text));
            });

            _socket.On(incrementValueEvent, res =>
            {
                OnIncrementValueResult(new Result(res.Text));
            });

            _socket.On(catchDataEvent, res =>
            {
                OnFetchDataResult(new Result(res.Text));
            });
            
            _socket.On(deleteObjectEvent, res =>
            {
                OnDeleteObjectResult(new Result(res.Text));
            });
        }

        public void CreateClass(DataObject data, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.create.ToString());
            json.Add("class" , data._class);
            json.Add("data", data.getData()); // send as json 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnCrateClassResult = (res) => { onResult(res); };
        }

        public void UpdateClass(DataObject data, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.update.ToString());
            json.Add("class", data._class);
            json.Add("data", data.getData()); // send as json 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnUpdateClassResult = (res) => { onResult(res); };
        }
        public void IncrementValue(DataObject data, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.increment.ToString());
            json.Add("class", data._class);
            json.Add("data", data.getData()); 
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnIncrementValueResult = (res) => { onResult(res); };
        }

        public void FetchData(string className, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.get.ToString());
            json.Add("class", className);
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnFetchDataResult = (res) => { onResult(res); };

        }
        
        public void DeleteObject(string className, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", StorageType.delete.ToString());
            json.Add("class", className);
            json.Add("token", Client.Token);

            _socket.Emit(storageEvent, json.ToString());

            OnDeleteObjectResult = (res) => { onResult(res); };

        }



        private enum StorageType
        {
            create,
            update,
            increment,
            get,
            delete,
        }
    }


    public class DataObject
    {
        private Dictionary<string, JSONNode> m_data = new Dictionary<string, JSONNode>();
        internal string _class;

        private string key;
        private JSONNode value;


        public DataObject(string className)
        {
            this._class = className;
        }

        public void add(string key, JSONNode value) 
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

        public void increment(string key , int value)
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



        public JSONObject getData()
        {
            JSONObject json = new JSONObject();
           
            foreach (KeyValuePair<string, JSONNode> pair in m_data)
            {
                json.Add(pair.Key, pair.Value);
            }

            return json;
        }

        public DataObject setKey(string key)
        {
            this.key = key;
            return this;
        }

        public DataObject setValue(JSONNode value)
        {
            this.value = value;
            return this;
        }

    }
}
