﻿////////////////////////////////////////////////////////////////////////////
//
// Copyright 2016 Realm Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Newtonsoft.Json.Linq;
using Realms.Helpers;

namespace Realms.Sync
{
    /// <summary>
    /// A class, representing the credentials used for authenticating a <see cref="User"/>.
    /// </summary>
    public class Credentials
    {
        internal static class Provider
        {
            public const string Debug = "debug";

            public const string AdminToken = "adminToken";

            public const string Facebook = "facebook";

            public const string Google = "google";

            public const string UsernamePassword = "password";

            public const string AzureAD = "azuread";

            public const string JWT = "jwt";

            public const string Anonymous = "anonymous";

            public const string Nickname = "nickname";

            public const string CustomRefreshToken = "customRefreshToken";
        }

        internal static class Keys
        {
            internal const string CreateUser = "register";

            internal const string Password = "password";

            internal const string Identity = "identity";

            internal const string IsAdmin = "is_admin";
        }

        /// <summary>
        /// Creates an instance of <see cref="Credentials"/> with a custom provider and user identifier.
        /// </summary>
        /// <param name="identityProvider">Provider used to verify the credentials.</param>
        /// <param name="userIdentifier">String identifying the user. Usually a username of id.</param>
        /// <param name="userInfo">Data describing the user further or null if the user does not have any extra data. The data will be serialized to JSON, so all values must be mappable to a valid JSON data type.</param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials Custom(string identityProvider, string userIdentifier,
            IDictionary<string, object> userInfo)
        {
            return new Credentials
            {
                IdentityProvider = identityProvider,
                Token = userIdentifier,
                UserInfo = new ReadOnlyDictionary<string, object>(userInfo)
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on a Facebook login.
        /// </summary>
        /// <param name="facebookToken">A Facebook authentication token, obtained by logging into Facebook.</param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials Facebook(string facebookToken)
        {
            Argument.NotNull(facebookToken, nameof(facebookToken));

            return new Credentials
            {
                IdentityProvider = Provider.Facebook,
                Token = facebookToken
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on a Google login.
        /// </summary>
        /// <param name="googleToken">A Google authentication token, obtained by logging into Google.</param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials Google(string googleToken)
        {
            Argument.NotNull(googleToken, nameof(googleToken));

            return new Credentials
            {
                IdentityProvider = Provider.Google,
                Token = googleToken
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on a login with a username and a password.
        /// </summary>
        /// <param name="username">The username of the user.</param>
        /// <param name="password">The user's password.</param>
        /// <param name="createUser"><c>true</c> if the user should be created, <c>false</c> otherwise. It is not possible to create a user twice when logging in, so this flag should only be set to true the first time a user logs in.</param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials UsernamePassword(string username, string password, bool? createUser = null)
        {
            var userInfo = new Dictionary<string, object> { [Keys.Password] = password };
            if (createUser != null)
            {
                userInfo[Keys.CreateUser] = createUser;
            }

            return new Credentials
            {
                IdentityProvider = Provider.UsernamePassword,
                Token = username,
                UserInfo = userInfo,
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> for an anonymous user. These can only be used once - using them a second
        /// time will result in a different user being logged in. If you need to get a user that has already logged
        /// in with the Anonymous credentials, use <see cref="User.Current"/> or <see cref="User.AllLoggedIn"/>.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/>
        /// </returns>
        public static Credentials Anonymous()
        {
            return new Credentials
            {
                IdentityProvider = Provider.Anonymous
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on a login with a nickname. If multiple users try to login
        /// with the same nickname, they'll get the same underlying sync user.
        /// </summary>
        /// <param name="value">The nickname of the user.</param>
        /// <returns>
        /// An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/>
        /// </returns>
        [Obsolete("The Nickname auth provider is insecure and will be removed in a future version. Please use UsernamePassword or Anonymous instead.")]
        public static Credentials Nickname(string value)
        {
            return new Credentials
            {
                IdentityProvider = Provider.Nickname,
                Token = value
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on an Active Directory login.
        /// </summary>
        /// <param name="adToken">An access token, obtained by logging into Azure Active Directory.</param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials AzureAD(string adToken)
        {
            Argument.NotNull(adToken, nameof(adToken));

            return new Credentials
            {
                IdentityProvider = Provider.AzureAD,
                Token = adToken
            };
        }

        /// <summary>
        /// Creates <see cref="Credentials"/> based on a JWT access token.
        /// </summary>
        /// <param name="token">A Json Web Token, obtained by logging into your auth service.</param>
        /// <param name="providerName">
        /// The name of the jwt provider in ROS. By default, it will be jwt, unless explicitly overridden
        /// by the ROS configuration.
        /// </param>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials JWT(string token, string providerName = Provider.JWT)
        {
            Argument.NotNull(token, nameof(token));

            return new Credentials
            {
                IdentityProvider = providerName,
                Token = token
            };
        }

        /// <summary>
        /// Creates a <see cref="Credentials"/> based on a custom Refresh token.
        /// </summary>
        /// <param name="token">A Json Web Token, obtained by a 3rd party source that will be used instead of the ROS-issued refresh tokens.</param>
        /// <remarks>
        /// Unlike other <see cref="Credentials"/> methods, users logged in via the CustomRefreshToken API will not go through the regular
        /// login flow (since we already have a refresh token). Instead, the provided token will be used at any point when we need to exchange
        /// the refresh token for an access token, e.g. when opening a Realm file. If the refresh token is invalid or expired, the user instance
        /// will still be valid and they'll be able to create/open Realms, but those will never be synchronized with ROS. If the token is then updated
        /// with a new valid one, existing changes will be synchronized with the server as usual.
        /// <para/>
        /// To update a refresh token, just set <see cref="User.RefreshToken"/> to the new updated value.
        /// <para/>
        /// ROS must be configured with <c>refreshTokenValidators</c> for this user to ever be able to sync with it.
        /// </remarks>
        /// <returns>An instance of <see cref="Credentials"/> that can be used in <see cref="User.LoginAsync"/></returns>
        public static Credentials CustomRefreshToken(string token)
        {
            Argument.NotNull(token, nameof(token));

            var parts = token.Split('.');
            Argument.Ensure(parts.Length == 3, "Prodvided token is not a valid JWT", nameof(token));

            // C#'s base64 parser is strict about padding, but a lot of others are not.
            var charactersToPad = (4 - parts[1].Length % 4) % 4;
            var paddedPayload = parts[1].PadRight(parts[1].Length + charactersToPad, '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(paddedPayload));
            var deserialized = JObject.Parse(payload);

            var userId = deserialized["sub"]?.Value<string>();
            Argument.Ensure(userId != null, "The provided token must have a sub field.", nameof(token));

            return new Credentials
            {
                IdentityProvider = Provider.CustomRefreshToken,
                Token = token,
                UserInfo = new Dictionary<string, object>
                {
                    [Keys.IsAdmin] = deserialized["isAdmin"]?.Value<bool>() ?? false,
                    [Keys.Identity] = userId
                }
            };
        }

        internal static Credentials AdminToken(string token)
        {
            return new Credentials
            {
                IdentityProvider = Provider.AdminToken,
                Token = token
            };
        }

        /// <summary>
        /// Gets the identity provider for the credentials.
        /// </summary>
        /// <value>The identity provider, such as Google, Facebook, etc.</value>
        public string IdentityProvider { get; private set; }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string Token { get; private set; }

        /// <summary>
        /// Gets additional user information associated with the credentials.
        /// </summary>
        /// <value>A dictionary, containing the additional information.</value>
        public IReadOnlyDictionary<string, object> UserInfo { get; private set; } = new Dictionary<string, object>();

        private Credentials()
        {
        }

        internal IDictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["data"] = Token,
                ["provider"] = IdentityProvider,
                ["user_info"] = UserInfo
            };
        }
    }
}
