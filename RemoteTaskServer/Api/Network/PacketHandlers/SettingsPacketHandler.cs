﻿#region

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Settings;
using UlteriusServer.WebSocketAPI.Authentication;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.Api.Network.PacketHandlers
{
    public class SettingsPacketHandler : PacketHandler
    {
        private AuthClient _authClient;
        private MessageBuilder _builder;
        private WebSocket _client;
        private Packet _packet;

        public void GetCurrentSettings()
        {
            _builder.WriteMessage(Config.GetRaw());
        }

        public void SaveSettings()
        {
            try
            {
                var base64Settings = _packet.Args[0].ToString();
                var data = Convert.FromBase64String(base64Settings);
                var decodedString = Encoding.UTF8.GetString(data);
                var fileName = "Config.json";
                var filePath = Path.Combine(AppEnvironment.DataPath, fileName);
                var config =  JsonConvert.DeserializeObject<Config>(decodedString);
                File.WriteAllText(filePath, JsonConvert.SerializeObject(config, Formatting.Indented));
                var response = new
                {
                    changedStatus = true
                };
                _builder.WriteMessage(response);
            }
            catch (Exception ex)
            {
                var response = new
                {
                    changedStatus = true,
                    message = ex.Message
                };
                _builder.WriteMessage(response);
            }
        }

        public override void HandlePacket(Packet packet)
        {
            _client = packet.Client;
            _authClient = packet.AuthClient;
            _packet = packet;
            _builder = new MessageBuilder(_authClient, _client, _packet.EndPoint, _packet.SyncKey);
            switch (_packet.PacketType)
            {
                case PacketManager.PacketTypes.SaveSettings:
                    SaveSettings();
                    break;
                case PacketManager.PacketTypes.GetCurrentSettings:
                    GetCurrentSettings();
                    break;
            }
        }
    }
}