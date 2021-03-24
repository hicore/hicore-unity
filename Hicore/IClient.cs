using System;
using Hicore.Logger;


namespace Hicore
{
    interface IClient
    {

        void AuthenticateDeviceId(string deviceId, Action<User, Result> onResult);
        void AuthenticateEmail(string email, string password, Action<User, Result> onResult);
        void AuthenticateGoogle(string token, string username, Action<User, Result> onResult);
        void AuthenticateFacebook(string token, string username, Action<User, Result> onResult);

    }
}
