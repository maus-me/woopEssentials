# Th3Essentials

## Features:

- Discord integration (send messages from game to discord and back, Ingame-GenralChat to one specific Discord-channel, customizable color) [on/off]
  - restart/death/join messages are send to discord
  - discord slashcommands for restart-time , ingame time and online players
  - display playercount in the activity of the bot
  - display discord mentions (user/channel/role) correctly
- Shutdown - Set a time when the server should shutdown (starting of the server needs to be handled externaly!!!) [on/off]
  additionally warns the players about the restart x min befor restart
- Add homepoints to teleport to, limited by a configurable cooldown [on/off]
- starterkit - add a starterkit to be used only once (can be reset) [on/off]
  - starterkit can be set ingame by an admin
- /spawn command to teleport to spawn also respecting cooldown [on/off]
- /back command to teleport back to previous postion (/home or death) [on/off]
- Announcements messages (a list of messages that are send in an configurable interval) [on/off]
- /msg playername - to send private messages ingame (customizable color)

![](preview/discord-chat2.png)
![](preview/ingame-chat2.png)

## Discord Command usage

![](preview/setchannel-cmds.png)
![](preview/setchannel-cmd.png)

## Installation

Download the mod and put it into your mods folder. Start your server once to generate a default Th3Config.json file inside the ModConfig folder. Stop the server and now you can configure the mod.

### Enabling Features / Configuration

- Discord:

  If you wannt to use the Discord features you will need to create a Discord bot see [Creating a Discord Bot](#Creating-a-Discord-Bot)

  Once you copied the Discord Bot Token into the Th3Config.json you can use the build in commands to configure the guild (Discord server) and the channel to link to ingame.

  I recommend finishing the rest of the Th3Config.json file and once finished start the server. Now you can configure the Guild and Channel through one command in Discord itself. You should see that the bot went online, if not then there might be an issue with the Token. To initiate the setup just type on the server where you invited the Discord Bot to in the chat channel that you wanna link to ingame chat: `!setupth3essentials` it should respond with `Th3Essentials: Commands, Guild and Channel are setup 👍`. This command can only be used by some one with Administartiv permissions on that Discord Server.

  `!setupth3essentials` will create all commands that can be used from Discord and setup the Guild (Discord Server) to be used with the VS-Server. That should set everything up. If you want to change the linked channel type: `/setchannel` this will ask for an option called channel, type the channel name to link with the ingame chat and hit enter.

  Additionally you can customize the color of the playername that will be shown ingame form Discord messages via `"DiscordChatColor" : "7289DA"`, this is a hex color code, [Online ColorPicker](https://colorpicker.me/) and pick the Hex Code value without the #

  All system messages like Startup/Shutdown/Restart warnings and Player join/leave will be in italics.

- Shutdownsystem

  Notice: The shutdown system can only shut the server down, you will need something to automatically start the server when it is shutdown!!!
  Take a look at [scripts](https://gitlab.com/th3dilli_vintagestory/th3essentials/-/tree/main/scripts) folder for some very basic scripts for Linux and Windows to restart the vs server.

  The `ShutdownTime` indicates the time when the server will shutdown (only if `"ShutdownEnabled" : true`) the second functionality bound to this value is to announce the restart with ingame and discord messages.
  By setting `"ShutdownAnnounce" : [1,2,3,4,5,10,20,30]` it will send messages 30,20 ,..., 2, 1 minutes befor restart - "Server will shutdown in x minutes".

- Homesystem / Spawn and Back command

  With `"HomeLimit" : 5` every player can set upto 5 individual positions as so called homepoints and fasttravel to them using /home name. Points can be created with /sethome name and deleted with /delhome name. /home will list all your homepoints.
  To disable set `"HomeLimit" : 0`.

  `"HomeCooldown" : 60` will set a cooldown for teleportation using the /home name command aswell as the /back and /spawn commands.
  The /spawn and /back commands can be enabled with `"SpawnEnabled" : true` and `"BackEnabled" : true`.

- private messages

  can be enabled by setting `"MessageEnabled" : true`
  allows to send private messages between players ingame like: /msg playername hello this is a private message.
  The color of the sender and reciver names can be customized via `"MessageCmdColor" : "ff9102"`.

  `ff9102` is a hex color code, [Online ColorPicker](https://colorpicker.me/) and pick the Hex Code value without the #.

- starterkit

  The starterkit can be setup using the ingame command `/setstarterkit` used by an admin. Pick the items you want in the starterkit (it only uses the Hotabr) and then enter the above command, thats it. Players can now get it by using `/starterkit`

  To reset the starterkit you can either use `/resetstarterkitusage playername` to reset it for one player that is currently online or use `/resetstarterkitusageall` to reset it for all players, regardless if they are online.

- announcements
  The Announcement system allows to send messages automatically ingame in an interval.
  `"AnnouncementMessages" : ["message 1", "message 2"]`
  `"AnnouncementInterval" : 10` sets the time in minutes inbetween the messages
  if the intervall is `0` or the `"AnnouncementMessages" : null` then it is disabled

- Messages and Language
  Further you can unpack the .zip archive and navigate to assets/th3essentials/lang/en.json for example and customize allmost all messages send by this mod, except the death messages since those are reused from the game (you could override the games death messages and that would change them in the mod too).

  The only thing you have to keep in mind is that you need to keep the same amount of `{0}`,`{1}`... and so on in the text you replace it with. If you dont it will break the message output and cause unexpected behavior. So for example if we look at `"slc-restart-resp": "Server is restarting in {0}h {1}min",`
  you could change it to:
  `"slc-restart-resp": "Server will restart in {1}min {0}h",,` the `{0}` in this context would replace a hours and `{1}` minutes.
  Further for the messages send to discord you can also customize it with Discord emojis see `"connected": ":inbox_tray: Player {0} connected",`.

  If you wanna support a different language just copy the en.json and replace the `en` with whatever languagecode you wanna support and change all values inside .json file.

## Creating a Discord Bot

1.  Got to [Discord Developers](https://discord.com/developers/applications) and login with your discord account
2.  Create a new Application as Discord calls it. On the top right you should see a button for that.
3.  Once you created the Application click on the "Bot" menu entry on the left, there you need to click on "Add Bot" and confirm. After that you will see in in the center of the screen Token and "Click to Reveal Token" and also "Copy" here you will get your discord bot token that is needed in the Th3Config.json file, copy it and paste it into the Th3Config.json file.

    In the Th3Config.json it should look like this:

    `"Token": "your_bot_token",`

4.  Click on the OAuth2 menu entry on the left. Here you can setup the permission and invite your bot to your discord server. Scroll down to the "SCOPES" section and tick the box for

    - bot
    - applications.commands

    once those are ticked a section for "BOT PERMISSIONS" will appear on there tick

    - Send Messages

    after that you can click on "Copy" and open that link in your browser, this will ask you to invite your bot to one of your server where you have permissions to invite a bot to

    yay your should have your bot now on your discord server :)

## This sample config shows all features disabled:

```json
{
  // Discord Bot Token
  // to turn it off - "Token": null,
  // else set it to the token of your discord bot
  "Token": null,
  // Discord ChannelID to send messages from and to ingame chat
  "ChannelId": 0,
  // Discord GuildID to link all discord features to
  "GuildId": 0,

  // text displayed when using /serverinfo
  // example value: "This is a Info message"
  "InfoMessage": null,

  // messages to send preiodically in (AnnouncementInterval) in ingame chat
  // example value: ["message 1","message 2"]
  "AnnouncementMessages": null,
  // intervall (in min) to send AnnouncementMessages one after another
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
  // time on the server when the server should restart, also used for the ShutdownAnnounce messages - do not set this to null - if ShutdownEnabled is false and ShutdownAnnounce is null it wont do anything
  "ShutdownTime": "00:00:00",
  // time in minutes to annouce the restart befor it happens
  // example value: [1,2,3,4,5,10,20,30]
  // [1,2,3,4,5,10,20,30] for every entry a message will appear like: "Server restart in x minutes"
  "ShutdownAnnounce": null,

  // color to use for the name of the sender for the /msg command [hex color value] https://colorpicker.me/
  "MessageCmdColor": "ff9102",
  // color to use for messages send from discord to ingame [hex color value] https://colorpicker.me/
  "DiscordChatColor": "7289DA"
}
```
