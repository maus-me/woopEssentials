# Th3Essentials

For help, discussion, suggestions and polls on new fetures join the [Discord Server](https://discord.gg/GX65XawGWX)

- [Installation](#installation)
- [Updating](#updating)

## Features:

- Discord integration (send messages from game to discord and back, Ingame-GenralChat to one specific Discord-channel, customizable color) [on/off]
  - restart/death/join/temporal messages are send to discord
  - discord slashcommands for restart-time , ingame time and online players
  - display playercount in the activity of the bot
  - display discord mentions (user/channel/role) correctly
- Shutdown - Set a time when the server should shutdown (starting of the server needs to be handled externally!!!) [on/off]
  additionally warns the players about the restart x min before restart
- Add homepoints to teleport to, limited by a configurable cooldown [on/off]
- starterkit - add a starterkit to be used only once (can be reset) [on/off]
  - starterkit can be set ingame by an admin
- /spawn command to teleport to spawn also respecting cooldown [on/off]
- /back command to teleport back to previous position (/home or death) [on/off]
- Announcements messages (a list of messages that are send in an configurable interval) [on/off]
- /msg playername message - to send private messages ingame (customizable color) [on/off]
- /r message - to send private messages ingame to last messaging player (enabled if /msg is on)
- Server Metrics, logs metrics (/stats and more) to InfluxDB and visualize it with influxDB or **grafana**
- /requesthelp [message] , pings the `HelpRoleID` in Discord with the message [on/off]
- show role information infront of ingame chatname [on/off]
- /admin ingame/discord command , lists all roles specified by "AdminRoles" [on/off]
- reward system that allows you to add a text/icon to ingame chat if that player has a certain role in discord (Patreon)
- announce a message from discord to ingame
- /warp [ add \<warp name\> | remove \<warp name\> | list |\<warp name\> ] to predfined locations (setup by admin, also respecting home cooldown time) [on/off]
- automatic backup - will create a backup when using the shutdown feature [on/off]

![](preview/discord-chat2.png)
![](preview/discord-chat.png)
![](preview/ingame-chat2.png)

## Discord Command usage

![](preview/setchannel-cmds.png)
![](preview/setchannel-cmd.png)

## InfluxDB / Grafana Dashboard

![](preview/grafana1.png)

<details>
  <summary>More pictures</summary>

![](preview/grafana2.png)
![](preview/grafana3-logticks.png)
![](preview/grafana4-logticks.png)

</details>

## Installation

Download the mod and put it into your mods folder. Start your server once to generate a default Th3Config.json file inside the ModConfig folder. Stop the server and now you can configure the mod.

### Enabling Features / Configuration

- Discord:

  If you want to use the Discord features you will need to create a Discord bot see [Creating a Discord Bot](#Creating-a-Discord-Bot)

  Once you copied the Discord Bot Token into the Th3Config.json you can use the build in commands to configure the guild (Discord server) and the channel to link to ingame.

  I recommend finishing the rest of the Th3Config.json file and once finished start the server. Now you can configure the Guild and Channel through one command in Discord itself. You should see that the bot went online, if not then there might be an issue with the Token. To initiate the setup just type on the server where you invited the Discord Bot to in the chat channel that you wanna link to ingame chat: `!setupth3essentials` it should respond with `Th3Essentials: Commands, Guild and Channel are setup 👍`. This command can only be used by some one with Administrative permissions on that Discord Server.

  `!setupth3essentials` will create all commands that can be used from Discord and setup the Guild (Discord Server) to be used with the VS-Server. That should set everything up. If you want to change the linked channel type: `/setchannel` this will ask for an option called channel, type the channel name to link with the ingame chat and hit enter.

  With `"UseEphermalCmdResponse": true,` set to true only the user that uses a discord slashcommand will see the response from the bot, when set to false it will be send so everyone can see the command and response.

  Additionally you can customize the color of the playername that will be shown ingame form Discord messages via `"DiscordChatColor" : "7289DA"`, this is a hex color code, [Online ColorPicker](https://colorpicker.me/) and pick the Hex Code value without the #

  All system messages like Startup/Shutdown/Restart warnings and Player join/leave will be in italics.

  The ingame "General" and "Info log" channels will be redirected to discord. Why the Info log? because it uses the same channel identifier.

  ### Currently supported Slashcommands

  - /players - Get a list of online players (optional show the ping)
  - /date - Get the current ingame date and time
  - /restarttime - Show time until next restart
  - /setchannel - Set the channel to send to/from ingame chat [Admin]
  - /modifypermissions - add/remove/clear additional roles to use moderation commands [Admin]
  - /whitelist - Change the whitelist status of a player (also the time duration is customizable, default 50 years as with the ingame command) [Admin or Configured Role]
  - /allowcharselonce - Allows the player to re-select their class after doing so already [Admin or Configured Role]
  - /shutdown - Will shutdown the server (if configured server will restart see scripts at Shutdownsystem) [Admin or Configured Role]
  - /admins - lists all admins speciefied by "AdminRoles" in Th3Condfig.json
  - /serverinfo - prints game and mod versions
  - /stats - Print the output of the ingame /stats command [Admin or Configured Role]
  - /auth - start to link discord and ingame account for the reward system
  - /announce - announce a message from discord to ingame chat [Admin or Configured Role]

- Shutdownsystem

  Notice: The shutdown system can only shut the server down, you will need something to automatically start the server when it is shutdown!!!
  Take a look at [scripts](https://gitlab.com/th3dilli_vintagestory/th3essentials/-/tree/main/scripts) folder for some very basic scripts for Linux and Windows to restart the vs server.

  The `ShutdownTime` indicates the time when the server will shutdown (only if `"ShutdownEnabled" : true`) the second functionality bound to this value is to announce the restart with ingame and discord messages.
  By setting `"ShutdownAnnounce" : [1,2,3,4,5,10,20,30]` it will send messages 30,20 ,..., 2, 1 minutes before restart - "Server will shutdown in x minutes".

- Backupsystem

  when the shutdownsystem is turned on it will create a backup.
  Once it would shutdown it first kicks all players, then locks the server with a temporary password (printed to the console if someone really needs to connect), next it will start creating a backup and put it in the `/Backups` folder in the data folder of the server. Finally it will shutdown the server. 

- Homesystem / Spawn and Back command

  With `"HomeLimit" : 5` every player can set up to 5 individual positions as so called homepoints and fast-travel to them using /home name. Points can be created with /sethome name and deleted with /delhome name. /home will list all your homepoints.
  To disable set `"HomeLimit" : 0`.

  `"HomeCooldown" : 60` will set a cooldown for teleportation using the /home name command as well as the /back and /spawn commands.
  The /spawn and /back commands can be enabled with `"SpawnEnabled" : true` and `"BackEnabled" : true`.

- private messages

  can be enabled by setting `"MessageEnabled" : true`
  allows to send private messages between players ingame like: /msg playername hello this is a private message.
  The color of the sender and receiver names can be customized via `"MessageCmdColor" : "ff9102"`.

  `ff9102` is a hex color code, [Online ColorPicker](https://colorpicker.me/) and pick the Hex Code value without the #.

- starterkit

  The starterkit can be setup using the ingame command `/setstarterkit` used by an admin. Pick the items you want in the starterkit (it only uses the Hotbar) and then enter the above command, thats it. Players can now get it by using `/starterkit`

  To reset the starterkit you can either use `/resetstarterkitusage playername` to reset it for one player that is currently online or use `/resetstarterkitusageall` to reset it for all players, regardless if they are online.

- announcements
  The Announcement system allows to send messages automatically ingame in an interval.
  `"AnnouncementMessages" : ["message 1", "message 2"]`
  `"AnnouncementInterval" : 10` sets the time in minutes between the messages
  if the interval is `0` or the `"AnnouncementMessages" : null` then it is disabled

- Rewards
  The reward system will show a special text/icon that you can specify in the config `RewardIdToName`. This is only shown to players that have a certain role in discord that is linked to the rewards system. With this you can link for exmaple your Patropn discord roles to ingame. Every player that wants to recive their reward ingame needs to use the discord command `/auth` the bot will give you a comand you will have to enter ingame. After that the player needs to relog and they should see their reward when chatting. The rewards are loaded only on player login. To update the rward role mapping stop the server and add new ones see config example at the bottom.

- Messages and Language
  Further you can unpack the .zip archive and navigate to assets/th3essentials/lang/en.json for example and customize almost all messages send by this mod, except the death messages since those are reused from the game (you could override the games death messages and that would change them in the mod too).

  The only thing you have to keep in mind is that you need to keep the same amount of `{0}`,`{1}`... and so on in the text you replace it with. If you don't it will break the message output and cause unexpected behavior. So for example if we look at `"slc-restart-resp": "Server is restarting in {0}h {1}min",`
  you could change it to:
  `"slc-restart-resp": "Server will restart in {1}min {0}h",,` the `{0}` in this context would replace a hours and `{1}` minutes.
  Further for the messages send to discord you can also customize it with Discord emojis see `"connected": ":inbox_tray: Player {0} connected",`.

  If you wanna support a different language just copy the en.json and replace the `en` with whatever language-code you wanna support and change all values inside .json file.

  The language that is used by the mod can be changed in the serverconfig.json `"ServerLanguage": "en",`

  from VS-Wiki:` 2-letter code of localization to use on this server. Determines language of server messages.`

## Creating a Discord Bot

1.  Got to [Discord Developers](https://discord.com/developers/applications) and login with your discord account
2.  Create a new Application as Discord calls it. On the top right you should see a button for that.
3.  Once you created the Application click on the "Bot" menu entry on the left, there you need to click on "Add Bot" and confirm. \

- Look for "PUBLIC BOT"\
   Public bots can be added by anyone. When unchecked, only you can join this bot to servers.\
   And make sure you **uncheck** it. \
  <span style="color:red">Otherwise if someone has your Bots Application ID they could add the bot and link it to their discord server and interact with your server.</span>
- After look for Token and "Click to Reveal Token" and also "Copy" here you will get your discord bot token that is needed in the Th3Config.json file, copy it and paste it into the Th3Config.json file.

  In the Th3Config.json it should look like this:

  `"Token": "your_bot_token",`

4.  Click on the OAuth2 menu entry on the left and URL Generator. Here you can setup the permission and invite your bot to your discord server. Scroll down to the "SCOPES" section and tick the box for

    - bot
    - applications.commands

    once those are ticked a section for "BOT PERMISSIONS" will appear on there tick

    - Read Message/View Channels
    - Send Messages
    - Send Messages in Threads

    after that you can click on "Copy" and open that link in your browser, this will ask you to invite your bot to one of your server where you have permissions to invite a bot to

    Next you will have to enable the `MESSAGE CONTENT INTENT` which you can find in the Bot sidebar menu entry. This will allow the bot to read user messages and forward them and also enables the `!setupth3essentials` command

    if you wanna use the reward/auth system you need to enable in the "Bot" menu the "SERVER MEMBERS INTENT" toggle.

    yay you should have your bot now on your discord server :)

## InfluxDB / Grafana

The InfluxDB logger logs every 10 seconds to influxDB except player deaths, login/logout and warning/erros those are logged as they are encountered. Since the general server metrics are only logged every 10 seconds it might miss some spikes or dips in the metrics, keep that in mind.

For an exmple install using docker you can use the docker-compose.yaml. Check this [README.md](./influx_grafana/README.md) for more details.

- [Docker Engine Install](https://docs.docker.com/engine/install/)
- [Docker-Compose Install](https://docs.docker.com/compose/install/)
- [docker-compose.yaml](https://gitlab.com/th3dilli_vintagestory/th3essentials/-/tree/main/influx_grafana)
  change the path to the volumes if you wish so

if you wanna use docker and run the vs server in pterodactyl they need to be on the same network see [docker-compose.yaml](https://gitlab.com/th3dilli_vintagestory/th3essentials/-/tree/main/influx_grafana)

Both InfluxDB and Grafana allow you to configure it thorugh the Webinterface when not initilized yet.

If you need help or want to manually here are some useful links.\
Make sure to persist your influxdb data with docker volumes/mounts.

- [InfluxDB Docs](https://docs.influxdata.com/influxdb/v2.1/)
- [Grafana Docs](https://grafana.com/docs/grafana/latest/installation/?pg=docs)

## Updating

When updating make sure to run `!setupth3essentials` again to create all new Discord commands if any.

Further change the config value `IsDirty:false` to `IsDirty:true` and run `/autosavenow` on the server console after you started the server. This will save all new config options to the config file so you can change them.

## This sample config shows all default settings:

```json
{
  // this is just for internal purposes, you can ignore this
  "IsDirty": false,
  "DiscordConfig": {
    // Discord Bot Token
    // to turn it off - "Token": null,
    // else set it to the token of your discord bot surrounded by "
    // example value: "Token": "your_bot_token",
    "Token": null,
    // Discord ChannelID to send messages from and to ingame chat
    "ChannelId": 0,
    // Discord GuildID to link all discord features to
    "GuildId": 0,
    // Roles that are allowd to use whitelist/allowcharselonce - use the /modifypermissions slashcommand to add/remove roles
    "ModerationRoles": null,
    // if true only the user that uses a discord slashcommand will see the response from the bot
    "UseEphermalCmdResponse": true,
    // color to use for messages send from discord to ingame [hex color value] https://colorpicker.me/
    "DiscordChatColor": "7289DA",
    // ID of the role the gets pinged when some one uses /requesthelp ingame - 0 to turn it off
    // to get a role id you have to be in developer mode in the discord app (Advanced -> Developer mode) and then you can righclick on roles to copy the id
    "HelpRoleID": 0,
    // part of the reward system - no need to manually modify it use /auth in discord
    "LinkedAccounts": null,
    // here you can setup the rewards - link a discord role (Patreon) to a reward role ingame
    // exmaple: first is the ID of the role from discord and second is the text/icon to display in chat for them
    // to get a role id you have to be in developer mode in the discord app (Advanced -> Developer mode) and then you can righclick on roles to copy the id
    // if a discord user has multiple roles that should give a reward, only the first one in the list is applied
    // "RewardIdToName": {
    //   "951870816126111815": "☆",
    //   "951870898833608824": "✪",
    // },
    "RewardIdToName": null,
    // if the reward system should be turned on
    "Rewards": true,
    // if the reward system is activated you can modify how the chat is formated for someone who has a reward and role
    "RoleRewardsFormat": "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font><font size=\"18\" color=\"{2}\"><strong>[{3}]</strong></font>{4}",
    // if the reward system is activated you can modify how the chat is formated for someone who has a only reward and no admin/mod role
    "RewardsFormat": "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}",
    // allows to disable the chat relay function Discord <-> ingame chat and only show the system messages in discord if setup
    "DiscordChatRelay": true
  },

  "InfluxConfig": {
    // URL to the database including port
    // example value: "InlfuxDBURL": "http://localhost:8086",
    "InlfuxDBURL": null,
    // influx api token, https://docs.influxdata.com/influxdb/v2.1/security/tokens/
    "InlfuxDBToken": null,
    // the name of the bucket you setup where you wanna store your data
    "InlfuxDBBucket": null,
    // the name of the org you setup
    "InlfuxDBOrg": null,
    // if logticks is enabled (/debug logitcks 200) it will not log to the server-main.txt if set to true
    "InlfuxDBOverwriteLogTicks": true,
    // only systems from logticks with that threshold will be loged to influx
    "InlfuxDBLogtickThreshold": 20,
    // intervall in wich the data like logticks, ticktime, generating/active chunks, memory, entities will be collected (value in milliseconds)
    "DataCollectInterval": 10000,
    // enable some debug output for influx connection (mybeusefull if you have issues setting it up connection/token)
    "Debug": false
  },

  // text displayed when using /serverinfo
  // example value: "This is a Info message"
  "InfoMessage": null,

  // messages to send periodically in (AnnouncementInterval) in ingame chat
  // example value: ["message 1","message 2"]
  "AnnouncementMessages": null,
  // interval (in min) to send AnnouncementMessages one after another
  "AnnouncementInterval": 0,

  // number of homepoints a player can have
  "HomeLimit": 0,
  // time in seconds between usage of the home/spawn/back commands
  "HomeCooldown": 60,

  // if the /spawn command should be enabled [false, true]
  "SpawnEnabled": false,
  // if the /back command should be enabled [false, true]
  "BackEnabled": false,
  // if the /msg [Name] command should be enabled [false, true]
  "MessageEnabled": false,

  // items for the starterkit, use the /setstarterkit command to set it ingame
  "Items": null,

  // if the server should shutdown after the timer reaches 0 min till restart time [false, true]
  "ShutdownEnabled": false,
  // if ShutdownEnabled is this will create a backup and restart after that is finished
  "BackupOnShutdown": false,

  // time on the server when the server should restart, also used for the ShutdownAnnounce messages - do not set this to null - if ShutdownEnabled is false and ShutdownAnnounce is null it wont do anything
  "ShutdownTime": "00:00:00",
  // if you wanna have  multiple shutdowns per day you can define the time in here , will send shutodwn announce messages for every shutdown
  // on startup this will be checked for the next shutdown and written to ShutdownTime - so this value has priority over ShutdownTime
  // example value: ["00:00:00", "12:00:00"]
  "ShutdownTimes": null,

  // time in minutes to announce the restart before it happens
  // if this is set it will announce a restart even if shutdown ist not enabled but wont restart/shutdown the server
  // example value: [1,2,3,4,5,10,20,30]
  // [1,2,3,4,5,10,20,30] for every entry a message will appear like: "Server restart in x minutes"
  "ShutdownAnnounce": null,

  // color to use for the name of the sender for the /msg command [hex color value] https://colorpicker.me/
  "MessageCmdColor": "ff9102",
  
  // color to use for the system messages ingame (restart warnings) [hex color value] https://colorpicker.me/
  "SystemMsgColor": "ff9102",

  // shows the players role ingame and in discord like: [Admin] Th3Dilli: hello
  // this uses the the roles provided by the game itself see serverconfig.json -> Roles
  // each role has a Name which is used to display and a Color that the role name will be colored in
  // it wont print roles with PrivilegeLevel less then 1
  // color names for serverconfig.json [hex color value] https://colorpicker.me/ or https://docs.microsoft.com/en-us/dotnet/api/system.drawing.color?view=net-6.0#properties
  "ShowRole": false,
  // allows to format the ingame role information to your likeing
  // {0} will be the color specified in the serverconfig.json
  // {1} will be the role name specified in the serverconfig.json
  // {2} will be the message including one space and : like | Th3Dilli: message|
  // for format options check https://wiki.vintagestory.at/index.php?title=VTML
  // sample will show for role Admin and the role will be colerd according to the value in serverconfig.json
  // [Admin] Th3Dilli: message
  // info since fonts are different on windows and linux this may look different depending on the operating system
  "RoleFormat": "<font size=\"18\" color=\"{0}\"><strong>[{1}]</strong></font>{2}",
  // roles from serverconfig.json to be liste with the /admins ingame and discord command (enter the "Code" of a role for example Admin has Code admin or Creative Moderator has Code crmod)
  // ["admin","crmod"]
  "AdminRoles": null,
  // enable the ingame /warp command
  "WarpEnabled": false,
  // managed ingame with /warp add|remove name /warp name
  "WarpLocations": null
}
```
