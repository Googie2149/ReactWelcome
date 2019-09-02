using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace ReactWelcome
{
    public class Config
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("success_response")]
        public string SuccessResponse { get; set; } = ":thumbsup:";

        [JsonProperty("owner_ids")]
        public List<ulong> OwnerIds { get; set; } = new List<ulong>();

        // This bot is only set up to operate for a single server, specify its ID here
        [JsonProperty("guild_id")]
        public ulong HomeGuildId { get; set; }

        // The role needed to access normal channels
        [JsonProperty("access_role")]
        public ulong AccessRoleId { get; set; }

        // The initial channel users see that contains the reaction to click
        [JsonProperty("gateway_channel")]
        public ulong GatewayChannelId { get; set; }

        // The name of the emote that is in the reaction. Case sensitive.
        [JsonProperty("reaction_emote")]
        public string AccessReaction { get; set; } = "";

        // The channel to send a welcome message to after the user clicks the reaction
        [JsonProperty("welcome_channel")]
        public ulong WelcomeChannelId { get; set; }

        // The message sent in the welcome channel. '$user' will be replaced with the mention of the joining user
        [JsonProperty("welcome_message")]
        public string WelcomeString { get; set; } = "";

        public async static Task<Config> Load()
        {
            if (File.Exists("config.json"))
            {
                var json = File.ReadAllText("config.json");
                return JsonConvert.DeserializeObject<Config>(json);
            }
            var config = new Config();
            await config.Save();
            throw new InvalidOperationException("configuration file created; insert token and restart.");
        }

        public async Task Save()
        {
            //var json = JsonConvert.SerializeObject(this);
            //File.WriteAllText("config.json", json);
            await JsonStorage.SerializeObjectToFile(this, "config.json");
        }
    }
}
